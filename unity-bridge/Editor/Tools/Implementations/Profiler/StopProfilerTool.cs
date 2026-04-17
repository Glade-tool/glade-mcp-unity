using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class StopProfilerTool : ITool
    {
        public string Name => "stop_profiler";

        public string Execute(Dictionary<string, object> args)
        {
            bool wasEnabled = ProfilerDriver.enabled;
            ProfilerDriver.enabled = false;

            if (!wasEnabled)
                return ToolUtils.CreateSuccessResponse("Profiler was already stopped");

            return ToolUtils.CreateSuccessResponse("Profiler stopped");
        }
    }
}
