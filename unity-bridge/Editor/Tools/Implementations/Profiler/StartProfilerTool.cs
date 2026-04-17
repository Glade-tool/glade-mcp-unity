using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class StartProfilerTool : ITool
    {
        public string Name => "start_profiler";

        public string Execute(Dictionary<string, object> args)
        {
            bool deepProfile = false;
            if (args.ContainsKey("deepProfile") && bool.TryParse(args["deepProfile"].ToString(), out bool dp))
                deepProfile = dp;

            ProfilerDriver.enabled = true;
            ProfilerDriver.deepProfiling = deepProfile;

            var extras = new Dictionary<string, object>
            {
                { "deepProfiling", deepProfile },
                { "enabled", true }
            };

            return ToolUtils.CreateSuccessResponse(
                deepProfile ? "Profiler started with deep profiling enabled" : "Profiler started",
                extras);
        }
    }
}
