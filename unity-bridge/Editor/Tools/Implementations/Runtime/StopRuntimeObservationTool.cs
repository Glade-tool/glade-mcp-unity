using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Runtime
{
    /// <summary>
    /// Disarms the runtime-event observer. The runtime log stream keeps
    /// recording events; this just tells the bridge that the caller is
    /// no longer interested.
    /// </summary>
    public class StopRuntimeObservationTool : ITool
    {
        public string Name => "stop_runtime_observation";

        public string Execute(Dictionary<string, object> args)
        {
            PlayModeObserver.StopObservation();
            var extras = new Dictionary<string, object>
            {
                { "observationActive", false },
            };
            return ToolUtils.CreateSuccessResponse(
                "Runtime observation stopped",
                extras);
        }
    }
}
