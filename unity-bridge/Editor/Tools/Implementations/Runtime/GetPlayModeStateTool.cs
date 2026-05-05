using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Runtime
{
    /// <summary>
    /// Returns Unity Play Mode state. Read-only. Useful for callers that
    /// need to detect Play exit (e.g., to apply a queued fix only after
    /// the user has stopped the simulation) or Play re-entry mid-flight.
    /// </summary>
    public class GetPlayModeStateTool : ITool
    {
        public string Name => "get_play_mode_state";

        public string Execute(Dictionary<string, object> args)
        {
            var extras = new Dictionary<string, object>
            {
                { "isPlaying", PlayModeObserver.IsPlaying },
                { "willChangePlayMode", PlayModeObserver.WillChangePlayMode },
                { "lastTransition", PlayModeObserver.LastTransition.ToString() },
                { "lastTransitionTimestamp", PlayModeObserver.LastTransitionTimestamp },
                { "lastPlayEnterTimestamp", PlayModeObserver.LastPlayEnterTimestamp },
                { "lastPlayExitTimestamp", PlayModeObserver.LastPlayExitTimestamp },
                { "playEnterCountThisDomain", PlayModeObserver.PlayEnterCountThisDomain },
                { "playExitCountThisDomain", PlayModeObserver.PlayExitCountThisDomain },
                { "observationActive", PlayModeObserver.ObservationActive },
                { "observationStartCursor", PlayModeObserver.ObservationStartCursor },
                { "observationStartTimestamp", PlayModeObserver.ObservationStartTimestamp },
            };
            return ToolUtils.CreateSuccessResponse("Play mode state", extras);
        }
    }
}
