using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class GetGcAllocationsTool : ITool
    {
        public string Name => "get_gc_allocations";

        public string Execute(Dictionary<string, object> args)
        {
            if (!ProfilerDriver.enabled)
                return ToolUtils.CreateErrorResponse("Profiler is not running. Call start_profiler first.");

            int frameOffset = 0;
            if (args.ContainsKey("frameOffset") && int.TryParse(args["frameOffset"].ToString(), out int fo))
                frameOffset = fo;

            int maxResults = 20;
            if (args.ContainsKey("maxResults") && int.TryParse(args["maxResults"].ToString(), out int mr))
                maxResults = Mathf.Clamp(mr, 1, 100);

            int frameIndex = ProfilerDriver.lastFrameIndex - frameOffset;
            if (frameIndex < ProfilerDriver.firstFrameIndex || frameIndex > ProfilerDriver.lastFrameIndex)
                return ToolUtils.CreateErrorResponse($"Frame {frameIndex} is out of range");

            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
            {
                if (frameData == null || !frameData.valid)
                    return ToolUtils.CreateErrorResponse($"No profiler data available for frame {frameIndex}");

                var allocLines = new List<string>();
                long totalGcAlloc = 0;

                for (int i = 0; i < frameData.sampleCount && allocLines.Count < maxResults; i++)
                {
                    long gcAlloc = frameData.GetSampleMetadataAsLong(i, 0);
                    if (gcAlloc <= 0) continue;

                    string sampleName = frameData.GetSampleName(i);
                    totalGcAlloc += gcAlloc;
                    allocLines.Add($"{sampleName}: {gcAlloc} bytes");
                }

                var extras = new Dictionary<string, object>
                {
                    { "frameIndex", frameIndex },
                    { "totalGcAllocBytes", totalGcAlloc },
                    { "totalGcAllocKB", System.Math.Round(totalGcAlloc / 1024.0, 2) },
                    { "allocationCount", allocLines.Count },
                    { "allocations", string.Join("; ", allocLines) }
                };

                return ToolUtils.CreateSuccessResponse($"GC allocations for frame {frameIndex}", extras);
            }
        }
    }
}
