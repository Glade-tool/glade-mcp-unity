using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Runtime
{
    /// <summary>
    /// Returns runtime errors / exceptions captured since the caller's
    /// cursor. Read-only. Designed for incremental polling — the agent
    /// passes its last <c>nextCursor</c> to get only new events.
    ///
    /// Args:
    ///   sinceCursor (long, optional, default 0): return events with
    ///     Cursor &gt; sinceCursor. Pass the previous response's
    ///     <c>nextCursor</c> on subsequent polls. Pass 0 to read every
    ///     event currently in the buffer.
    ///   limit (int, optional, default 200): cap on events returned per
    ///     call. Events past the limit remain for the next poll.
    ///
    /// Inactive Play Mode behavior: the call still returns — any prior
    /// errors in the buffer are surfaced — but <c>playModeActive</c> is
    /// false, signaling the caller to stop polling until Play resumes.
    /// </summary>
    public class GetRuntimeEventsTool : ITool
    {
        public string Name => "get_runtime_events";

        public string Execute(Dictionary<string, object> args)
        {
            long sinceCursor = 0;
            if (args != null && args.TryGetValue("sinceCursor", out var scObj) && scObj != null)
            {
                long.TryParse(scObj.ToString(), out sinceCursor);
            }
            int limit = 200;
            if (args != null && args.TryGetValue("limit", out var lObj) && lObj != null)
            {
                int.TryParse(lObj.ToString(), out limit);
            }
            if (limit <= 0) limit = 200;

            var events = RuntimeLogStream.GetEventsSinceCursor(sinceCursor, limit);
            long nextCursor = events.Count > 0
                ? events[events.Count - 1].Cursor
                : sinceCursor;

            var eventsOut = new List<Dictionary<string, object>>();
            foreach (var e in events)
            {
                eventsOut.Add(new Dictionary<string, object>
                {
                    { "cursor", e.Cursor },
                    { "message", e.Message },
                    { "stackTrace", e.StackTrace },
                    { "logType", e.LogType },
                    { "timestamp", e.Timestamp },
                    { "fingerprint", e.Fingerprint },
                });
            }

            var extras = new Dictionary<string, object>
            {
                { "events", eventsOut },
                { "nextCursor", nextCursor },
                { "playModeActive", PlayModeObserver.IsPlaying },
                { "observationActive", PlayModeObserver.ObservationActive },
                { "lastTransition", PlayModeObserver.LastTransition.ToString() },
                { "ringBufferSize", RuntimeLogStream.CurrentSize },
                { "droppedDueToOverflow", RuntimeLogStream.DroppedDueToOverflow },
                { "totalEventsObserved", RuntimeLogStream.TotalEventsObserved },
            };

            string msg = events.Count == 0
                ? "No new runtime events"
                : $"{events.Count} runtime event(s)";
            return ToolUtils.CreateSuccessResponse(msg, extras);
        }
    }
}
