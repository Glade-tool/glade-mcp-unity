using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Runtime
{
    /// <summary>
    /// Arms the runtime-event observer. Returns the current cursor so the
    /// caller has a baseline for incremental polls of get_runtime_events.
    ///
    /// Idempotent: re-arming is allowed and refreshes the baseline cursor.
    /// </summary>
    public class StartRuntimeObservationTool : ITool
    {
        public string Name => "start_runtime_observation";

        public string Execute(Dictionary<string, object> args)
        {
            PlayModeObserver.StartObservation();

            var extras = new Dictionary<string, object>
            {
                { "observationActive", true },
                { "startCursor", PlayModeObserver.ObservationStartCursor },
                { "isPlaying", PlayModeObserver.IsPlaying },
                { "ringBufferSize", RuntimeLogStream.CurrentSize },
            };
            return ToolUtils.CreateSuccessResponse(
                "Runtime observation started",
                extras);
        }
    }
}
