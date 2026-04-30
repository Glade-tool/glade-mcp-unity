using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Utility
{
    /// <summary>
    /// Returns a grouped summary of every mutation recorded by SessionTracker
    /// since the Unity Editor session started (or since the last Reset).
    ///
    /// Read-only. Lets the AI answer "what did you just do?" and lets UI
    /// surfaces show users exactly what changed during a session.
    /// </summary>
    public class GetSessionSummaryTool : ITool
    {
        public string Name => "get_session_summary";

        public string Execute(Dictionary<string, object> args)
        {
            int maxTimeline = 50;
            if (args != null && args.ContainsKey("maxTimelineEntries"))
            {
                int.TryParse(args["maxTimelineEntries"]?.ToString(), out maxTimeline);
                if (maxTimeline < 0) maxTimeline = 0;
                if (maxTimeline > 500) maxTimeline = 500;
            }

            var summary = SessionTracker.BuildSummary(maxTimeline);
            summary["success"] = true;
            return ToolUtils.SerializeDictToJson(summary);
        }
    }
}
