using System.Collections.Generic;

namespace GladeAgenticAI.Services
{
    /// <summary>
    /// Bridge-side idempotency registry for the apply_queued_fix tool.
    ///
    /// Each multi-step fix carries a proposalId. Subsequent applies of
    /// the same id return <c>alreadyApplied: true</c> rather than
    /// re-executing the changes. This guards against:
    ///   - Network retries (the client fires apply twice on a connection blip)
    ///   - Race conditions between auto-apply paths and manual accept clicks
    ///
    /// Scope: in-memory only. Reset on Editor domain reload — proposals
    /// that survived a full reload are old enough that we don't promise
    /// idempotency across that boundary. Cap of
    /// <see cref="MaxTrackedProposals"/> guards against unbounded growth
    /// in long sessions.
    /// </summary>
    public static class FixApplyTracker
    {
        public const int MaxTrackedProposals = 200;

        public class AppliedRecord
        {
            public string ProposalId;
            public bool Success;
            public string Summary;
            public double Timestamp;
        }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, AppliedRecord> _byId = new Dictionary<string, AppliedRecord>();
        private static readonly Queue<string> _insertionOrder = new Queue<string>();

        /// <summary>
        /// Returns the prior apply result if <paramref name="proposalId"/>
        /// has been applied this domain; null otherwise.
        /// </summary>
        public static AppliedRecord TryGet(string proposalId)
        {
            if (string.IsNullOrEmpty(proposalId)) return null;
            lock (_lock)
            {
                return _byId.TryGetValue(proposalId, out var rec) ? rec : null;
            }
        }

        /// <summary>
        /// Records an apply outcome. Subsequent <see cref="TryGet"/> calls
        /// for the same <paramref name="proposalId"/> return this record.
        /// Insertion-order eviction past <see cref="MaxTrackedProposals"/>.
        /// </summary>
        public static void Record(string proposalId, bool success, string summary, double timestamp)
        {
            if (string.IsNullOrEmpty(proposalId)) return;
            lock (_lock)
            {
                if (_byId.ContainsKey(proposalId))
                {
                    // Don't downgrade a prior success record. If a retry
                    // arrives after a successful apply, the registry already
                    // returns alreadyApplied=true and we never reach here —
                    // so this branch is defensive only.
                    return;
                }
                var rec = new AppliedRecord
                {
                    ProposalId = proposalId,
                    Success = success,
                    Summary = summary ?? string.Empty,
                    Timestamp = timestamp,
                };
                _byId[proposalId] = rec;
                _insertionOrder.Enqueue(proposalId);
                while (_insertionOrder.Count > MaxTrackedProposals)
                {
                    string evicted = _insertionOrder.Dequeue();
                    _byId.Remove(evicted);
                }
            }
        }

        /// <summary>Test / diagnostic helper.</summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _byId.Clear();
                _insertionOrder.Clear();
            }
        }

        /// <summary>Diagnostic: number of tracked proposals (post-eviction).</summary>
        public static int Count
        {
            get { lock (_lock) { return _byId.Count; } }
        }
    }
}
