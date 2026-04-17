using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class GetMemoryStatsTool : ITool
    {
        public string Name => "get_memory_stats";

        public string Execute(Dictionary<string, object> args)
        {
            long totalAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            long totalReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
            long totalUnused = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong();
            long monoUsed = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            long monoHeap = UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong();
            long tempAllocator = UnityEngine.Profiling.Profiler.GetTempAllocatorSize();
            long gfxDriver = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver();

            var extras = new Dictionary<string, object>
            {
                { "totalAllocatedMB", System.Math.Round(totalAllocated / (1024.0 * 1024.0), 2) },
                { "totalReservedMB", System.Math.Round(totalReserved / (1024.0 * 1024.0), 2) },
                { "totalUnusedReservedMB", System.Math.Round(totalUnused / (1024.0 * 1024.0), 2) },
                { "managedUsedMB", System.Math.Round(monoUsed / (1024.0 * 1024.0), 2) },
                { "managedHeapMB", System.Math.Round(monoHeap / (1024.0 * 1024.0), 2) },
                { "tempAllocatorMB", System.Math.Round(tempAllocator / (1024.0 * 1024.0), 2) },
                { "gfxDriverMB", System.Math.Round(gfxDriver / (1024.0 * 1024.0), 2) }
            };

            return ToolUtils.CreateSuccessResponse("Memory stats retrieved", extras);
        }
    }
}
