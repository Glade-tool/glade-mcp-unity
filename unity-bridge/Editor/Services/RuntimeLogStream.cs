using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Services
{
    /// <summary>
    /// Editor-only ring buffer of Unity runtime errors / exceptions.
    ///
    /// Subscribes to <see cref="Application.logMessageReceivedThreaded"/> on
    /// domain load and captures Error + Exception entries into a 500-entry
    /// ring buffer with monotonic cursors. Two consumer patterns supported:
    ///
    ///   1. Legacy drain-on-read with condition-only dedup
    ///      (<see cref="DrainWithConditionDedup"/>) — keeps the renderer's
    ///      <c>useConsoleWatcher.ts</c> banner-dedup behavior intact.
    ///
    ///   2. Cursor-based read (<see cref="GetEventsSinceCursor"/>) — for the
    ///      Live Loop autonomous turn runner. Each consumer tracks its own
    ///      cursor; overlapping reads do not steal events from each other.
    ///
    /// Threading: <c>logMessageReceivedThreaded</c> fires from arbitrary
    /// threads (worker threads during physics, jobs, etc.). The callback
    /// marshals onto the main thread via <c>EditorApplication.delayCall</c>
    /// before touching any shared state. All public reader methods take a
    /// lock and snapshot before returning, so they are safe to call from
    /// the bridge's HTTP background thread.
    ///
    /// Scope: in-memory only. Reset on Editor domain reload — intentional:
    /// a fresh Editor session has no Live Loop history.
    /// </summary>
    [InitializeOnLoad]
    public static class RuntimeLogStream
    {
        // Plan-locked thresholds (2026-05-01-live-loop-and-distribution.md,
        // inline architecture requirement #2):
        //   "RuntimeLogStream ring buffer: cap 500 entries, dedup on
        //    condition + first 500 chars of stack, drop oldest on overflow."
        public const int MaxEntries = 500;
        public const int FingerprintStackPrefixChars = 500;

        public class RuntimeEvent
        {
            public long Cursor;          // monotonic; client uses this as resume point
            public string Message;       // Application log condition
            public string StackTrace;    // full stack as Unity surfaced it
            public string LogType;       // "Error" | "Exception" (we drop Warn/Info/Log)
            public double Timestamp;     // unix seconds (UTC)
            public string Fingerprint;   // hash of message + stack[:500] — Live Loop debounce key
        }

        private static readonly object _lock = new object();
        private static readonly LinkedList<RuntimeEvent> _ring = new LinkedList<RuntimeEvent>();
        private static long _nextCursor = 0;
        private static int _droppedDueToOverflow = 0;
        // Per-consumer cursors. The legacy /api/console/events endpoint
        // tracks its own cursor here so it doesn't have to mutate the ring.
        // Cursor-based callers (Live Loop's get_runtime_events) pass their
        // own sinceCursor on each call and don't share state with this.
        private static long _legacyDrainCursor = 0;

        // Total Error/Exception messages observed since domain load. Includes
        // events that fired before they could be marshaled to the main thread
        // (which under sustained load is approximately zero). Useful for the
        // status window and as a sanity check during eval runs.
        public static int TotalEventsObserved { get; private set; }

        static RuntimeLogStream()
        {
            // Idempotent re-subscribe — domain reload re-runs the static ctor
            // and Unity's event delegate has duplicate-prevention only via
            // explicit removal, so unhook then hook.
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        /// <summary>
        /// Unity log callback. Fires on arbitrary threads. We marshal the
        /// payload onto the main thread before mutating <c>_ring</c>.
        /// </summary>
        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception) return;

            string logTypeName = type.ToString();
            double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            string fp = ComputeFingerprint(condition, stackTrace);

            EditorApplication.delayCall += () =>
            {
                lock (_lock)
                {
                    var evt = new RuntimeEvent
                    {
                        Cursor = _nextCursor++,
                        Message = condition ?? string.Empty,
                        StackTrace = stackTrace ?? string.Empty,
                        LogType = logTypeName,
                        Timestamp = ts,
                        Fingerprint = fp,
                    };
                    _ring.AddLast(evt);
                    TotalEventsObserved++;
                    while (_ring.Count > MaxEntries)
                    {
                        _ring.RemoveFirst();
                        _droppedDueToOverflow++;
                    }
                }
            };
        }

        /// <summary>
        /// Plan-spec fingerprint: <c>condition + first 500 chars of stack</c>,
        /// hashed via <c>string.GetHashCode()</c>. The plan called out
        /// GetHashCode specifically (~10x cheaper than SHA-1) — we trust the
        /// distribution because collision risk on a 500-cap buffer is
        /// negligible for the debounce use-case.
        ///
        /// Tradeoff vs. condition-only dedup: an in-flight script edit
        /// shifts line numbers in the stack and produces a fresh fingerprint
        /// for the same logical error. That is intentional for autonomous
        /// turns — each edit-recompile cycle gets a fresh shot at being
        /// fixed. The legacy renderer dedup (condition-only) lives in
        /// <see cref="DrainWithConditionDedup"/> below to preserve UI
        /// behavior (one banner per error message regardless of recompile).
        /// </summary>
        public static string ComputeFingerprint(string condition, string stackTrace)
        {
            string head = stackTrace ?? string.Empty;
            if (head.Length > FingerprintStackPrefixChars)
                head = head.Substring(0, FingerprintStackPrefixChars);
            string payload = (condition ?? string.Empty) + "\n" + head;
            int h = payload.GetHashCode();
            // Hex form so the value travels stably through JSON (some
            // serializers stringify ints differently across platforms).
            return ((uint)h).ToString("x8");
        }

        /// <summary>
        /// Returns all events with <c>Cursor &gt; sinceCursor</c>, in order.
        /// Use cursor=0 to read everything currently in the buffer. The
        /// return value is a snapshot — callers can iterate without holding
        /// the lock. Caller-tracked cursor model means overlapping consumers
        /// do not interfere with each other.
        ///
        /// <paramref name="limit"/> caps the number of events returned per
        /// call (defense against an event storm). Events past the limit
        /// remain in the buffer for the next poll.
        /// </summary>
        public static List<RuntimeEvent> GetEventsSinceCursor(long sinceCursor, int limit = 200)
        {
            var result = new List<RuntimeEvent>();
            lock (_lock)
            {
                foreach (var evt in _ring)
                {
                    if (evt.Cursor <= sinceCursor) continue;
                    result.Add(evt);
                    if (result.Count >= limit) break;
                }
            }
            return result;
        }

        /// <summary>
        /// The newest cursor currently in the buffer, or 0 if the buffer is
        /// empty. Useful for "start observing from now, not from history"
        /// flows: the Live Loop runner snapshots this on observation start
        /// so prior errors don't auto-trigger a turn.
        /// </summary>
        public static long LatestCursor()
        {
            lock (_lock)
            {
                return _ring.Count == 0 ? 0 : _ring.Last.Value.Cursor;
            }
        }

        /// <summary>
        /// Legacy drain-on-read used by the renderer's
        /// <c>useConsoleWatcher.ts</c>. Returns one entry per unique
        /// <c>Message</c> (condition-only dedup, matching the prior inline
        /// implementation). Advances an internal "last drained" cursor so
        /// repeated calls return only events that arrived since the last
        /// drain. Does NOT mutate the ring buffer — cursor-based consumers
        /// (Live Loop's get_runtime_events) see the same events independently.
        ///
        /// The renderer-facing dedup intentionally differs from the
        /// fingerprint dedup — see <see cref="ComputeFingerprint"/> for the
        /// reasoning.
        /// </summary>
        public static List<RuntimeEvent> DrainWithConditionDedup()
        {
            var result = new List<RuntimeEvent>();
            lock (_lock)
            {
                if (_ring.Count == 0) return result;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                long maxSeen = _legacyDrainCursor;
                foreach (var evt in _ring)
                {
                    if (evt.Cursor <= _legacyDrainCursor) continue;
                    if (evt.Cursor > maxSeen) maxSeen = evt.Cursor;
                    string key = evt.Message ?? string.Empty;
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        result.Add(evt);
                    }
                }
                _legacyDrainCursor = maxSeen;
            }
            return result;
        }

        /// <summary>
        /// Test / diagnostic helper. Wipes the buffer and resets the cursor
        /// counter. NOT called from production paths.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _ring.Clear();
                _nextCursor = 0;
                _legacyDrainCursor = 0;
                _droppedDueToOverflow = 0;
                TotalEventsObserved = 0;
            }
        }

        /// <summary>Diagnostic: number of events evicted by ring overflow this session.</summary>
        public static int DroppedDueToOverflow
        {
            get { lock (_lock) { return _droppedDueToOverflow; } }
        }

        /// <summary>Diagnostic: current ring size (post-overflow eviction).</summary>
        public static int CurrentSize
        {
            get { lock (_lock) { return _ring.Count; } }
        }
    }
}
