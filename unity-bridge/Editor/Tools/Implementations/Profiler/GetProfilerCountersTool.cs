using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class GetProfilerCountersTool : ITool
    {
        public string Name => "get_profiler_counters";

        public string Execute(Dictionary<string, object> args)
        {
            if (!ProfilerDriver.enabled)
                return ToolUtils.CreateErrorResponse("Profiler is not running. Call start_profiler first.");

            string category = args.ContainsKey("category") ? args["category"].ToString() : "";

            int frameIndex = ProfilerDriver.lastFrameIndex;
            if (args.ContainsKey("frameOffset") && int.TryParse(args["frameOffset"].ToString(), out int fo))
                frameIndex = ProfilerDriver.lastFrameIndex - fo;

            if (frameIndex < ProfilerDriver.firstFrameIndex || frameIndex > ProfilerDriver.lastFrameIndex)
                return ToolUtils.CreateErrorResponse($"Frame {frameIndex} is out of range");

            var counters = new Dictionary<string, object>();

            // Rendering: use FrameTimingManager (public API)
            if (string.IsNullOrEmpty(category) || category == "rendering")
            {
                UnityEngine.FrameTimingManager.CaptureFrameTimings();
                var timings = new UnityEngine.FrameTiming[1];
                uint count = UnityEngine.FrameTimingManager.GetLatestTimings(1, timings);
                if (count > 0)
                {
                    counters["cpuTimeMs"] = System.Math.Round(timings[0].cpuFrameTime, 3);
                    counters["gpuTimeMs"] = System.Math.Round(timings[0].gpuFrameTime, 3);
                    counters["fps"] = timings[0].cpuFrameTime > 0
                        ? System.Math.Round(1000.0 / timings[0].cpuFrameTime, 1)
                        : 0.0;
                }
            }

            // Memory: use RawFrameDataView for GC + live Profiler API
            if (string.IsNullOrEmpty(category) || category == "memory")
            {
                using (var rawData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
                {
                    if (rawData != null && rawData.valid)
                    {
                        long totalGcAlloc = 0;
                        int gcAllocCount = 0;
                        for (int i = 0; i < rawData.sampleCount; i++)
                        {
                            try
                            {
                                long gcAlloc = rawData.GetSampleMetadataAsLong(i, 0);
                                if (gcAlloc > 0)
                                {
                                    totalGcAlloc += gcAlloc;
                                    gcAllocCount++;
                                }
                            }
                            catch { break; }
                        }
                        counters["gcAllocTotalBytes"] = totalGcAlloc;
                        counters["gcAllocTotalKB"] = System.Math.Round(totalGcAlloc / 1024.0, 2);
                        counters["gcAllocSampleCount"] = gcAllocCount;
                        counters["sampleCount"] = rawData.sampleCount;
                    }
                }

                // Live memory stats (always available)
                counters["totalAllocatedMB"] = System.Math.Round(
                    UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0), 2);
                counters["managedUsedMB"] = System.Math.Round(
                    UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024.0 * 1024.0), 2);
                counters["gfxDriverMB"] = System.Math.Round(
                    UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024.0 * 1024.0), 2);
            }

            var extras = new Dictionary<string, object>
            {
                { "frameIndex", frameIndex },
                { "counters", counters }
            };

            if (!string.IsNullOrEmpty(category))
                extras["filteredCategory"] = category;

            return ToolUtils.CreateSuccessResponse($"Profiler counters retrieved for frame {frameIndex}", extras);
        }
    }
}
