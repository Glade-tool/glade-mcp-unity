using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class GetFrameTimingTool : ITool
    {
        public string Name => "get_frame_timing";

        public string Execute(Dictionary<string, object> args)
        {
            int frameOffset = 0;
            if (args.ContainsKey("frameOffset") && int.TryParse(args["frameOffset"].ToString(), out int fo))
                frameOffset = fo;

            if (!ProfilerDriver.enabled)
                return ToolUtils.CreateErrorResponse("Profiler is not running. Call start_profiler first.");

            int frameIndex = ProfilerDriver.lastFrameIndex - frameOffset;
            if (frameIndex < ProfilerDriver.firstFrameIndex || frameIndex > ProfilerDriver.lastFrameIndex)
                return ToolUtils.CreateErrorResponse($"Frame {frameIndex} is out of range [{ProfilerDriver.firstFrameIndex}, {ProfilerDriver.lastFrameIndex}]");

            // Use FrameTimingManager (public API, works in all Unity versions)
            FrameTimingManager.CaptureFrameTimings();
            var timings = new FrameTiming[1];
            uint count = FrameTimingManager.GetLatestTimings((uint)1, timings);

            double cpuMs = 0;
            double gpuMs = 0;

            if (count > 0)
            {
                cpuMs = timings[0].cpuFrameTime;
                gpuMs = timings[0].gpuFrameTime;
            }

            var extras = new Dictionary<string, object>
            {
                { "frameIndex", frameIndex },
                { "cpuTimeMs", System.Math.Round(cpuMs, 3) },
                { "gpuTimeMs", System.Math.Round(gpuMs, 3) },
                { "fps", cpuMs > 0 ? System.Math.Round(1000.0 / cpuMs, 1) : 0.0 },
                { "profilerFirstFrame", ProfilerDriver.firstFrameIndex },
                { "profilerLastFrame", ProfilerDriver.lastFrameIndex }
            };

            return ToolUtils.CreateSuccessResponse($"Frame {frameIndex} timing retrieved", extras);
        }
    }
}
