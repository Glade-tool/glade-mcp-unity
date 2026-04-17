"""
Profiler & debug category tools — Unity Profiler, frame timing, memory stats, frame debugger.
"""

from typing import List, Dict

CATEGORY = {
    "name": "profiler",
    "display_name": "Profiler & Debug",
    "keywords": [
        "profiler",
        "profiling",
        "performance",
        "frame time",
        "frame timing",
        "fps",
        "memory",
        "gc",
        "garbage collection",
        "draw calls",
        "batches",
        "frame debugger",
        "render pass",
        "optimization",
        "slow",
        "lag",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "start_profiler",
            "description": "Start the Unity Profiler to begin collecting frame timing, memory, and rendering data. Optionally enable deep profiling for detailed call stacks (slower).",
            "parameters": {
                "type": "object",
                "properties": {
                    "deepProfile": {
                        "type": "boolean",
                        "description": "Enable deep profiling for detailed call stacks. Adds significant overhead — use only when needed.",
                    }
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "stop_profiler",
            "description": "Stop the Unity Profiler. Profiler data from the session is retained for inspection.",
            "parameters": {
                "type": "object",
                "properties": {},
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_frame_timing",
            "description": "Get CPU and GPU frame timing for a profiled frame. Returns cpuTimeMs, gpuTimeMs, fps. Profiler must be running.",
            "parameters": {
                "type": "object",
                "properties": {
                    "frameOffset": {
                        "type": "integer",
                        "description": "How many frames back from the latest to read (0 = latest, 1 = previous, etc.)",
                    }
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_memory_stats",
            "description": "Get current memory usage: total allocated, reserved, managed heap, GC, graphics driver memory. Works without the Profiler running.",
            "parameters": {
                "type": "object",
                "properties": {},
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_gc_allocations",
            "description": "Get per-sample GC allocation breakdown for a profiled frame. Shows which functions allocated managed memory. Profiler must be running.",
            "parameters": {
                "type": "object",
                "properties": {
                    "frameOffset": {
                        "type": "integer",
                        "description": "How many frames back from the latest (0 = latest)",
                    },
                    "maxResults": {
                        "type": "integer",
                        "description": "Maximum allocation entries to return (default: 20, max: 100)",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_profiler_counters",
            "description": "Get rendering and memory profiler counters for a frame: draw calls, batches, triangles, vertices, GC alloc. Profiler must be running.",
            "parameters": {
                "type": "object",
                "properties": {
                    "category": {
                        "type": "string",
                        "enum": ["rendering", "memory"],
                        "description": "Filter to a specific counter category. Omit for all.",
                    },
                    "frameOffset": {
                        "type": "integer",
                        "description": "How many frames back from the latest (0 = latest)",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "enable_frame_debugger",
            "description": "Enable or disable the Unity Frame Debugger for render pass inspection. Use get_frame_debugger_events to read captured events.",
            "parameters": {
                "type": "object",
                "properties": {
                    "enable": {
                        "type": "boolean",
                        "description": "True to enable, false to disable (default: true)",
                    }
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_frame_debugger_events",
            "description": "List render pass events from the Frame Debugger. Shows draw call names and order. Frame debugger must be enabled first.",
            "parameters": {
                "type": "object",
                "properties": {
                    "maxEvents": {
                        "type": "integer",
                        "description": "Maximum events to return (default: 50, max: 200)",
                    }
                },
                "required": [],
            },
        },
    },
]
