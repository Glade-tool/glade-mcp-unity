using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class GetFrameDebuggerEventsTool : ITool
    {
        public string Name => "get_frame_debugger_events";

        private static Type _fdType;
        private static PropertyInfo _countProp;
        private static MethodInfo _getEventInfoName;
        private static bool _resolved;

        private static bool Resolve()
        {
            if (_resolved) return _fdType != null;
            _resolved = true;

            string[] candidates = {
                "UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility",
                "UnityEditorInternal.FrameDebuggerUtility",
                "UnityEditor.FrameDebuggerUtility",
            };
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var name in candidates)
                {
                    _fdType = asm.GetType(name);
                    if (_fdType != null) goto found;
                }
            }
            return false;

            found:
            _countProp = _fdType.GetProperty("count",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            _getEventInfoName = _fdType.GetMethod("GetFrameEventInfoName",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            return _countProp != null;
        }

        public string Execute(Dictionary<string, object> args)
        {
            if (!Resolve())
                return ToolUtils.CreateErrorResponse("FrameDebuggerUtility not available. Use Window > Analysis > Frame Debugger manually.");

            try
            {
                // Check if locally supported / enabled via the locallySupported property
                var supportedProp = _fdType.GetProperty("locallySupported",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

                int count = (int)_countProp.GetValue(null);
                if (count == 0)
                    return ToolUtils.CreateSuccessResponse(
                        "Frame debugger has no events. Call enable_frame_debugger first, then ensure the Game view is rendering.",
                        new Dictionary<string, object> { { "totalEvents", 0 } });

                int maxEvents = 50;
                if (args.ContainsKey("maxEvents") && int.TryParse(args["maxEvents"].ToString(), out int me))
                    maxEvents = UnityEngine.Mathf.Clamp(me, 1, 200);

                int limit = UnityEngine.Mathf.Min(count, maxEvents);
                var eventLines = new List<string>();

                if (_getEventInfoName != null)
                {
                    for (int i = 0; i < limit; i++)
                    {
                        try
                        {
                            string eventName = (string)_getEventInfoName.Invoke(null, new object[] { i });
                            eventLines.Add($"[{i}] {eventName}");
                        }
                        catch
                        {
                            eventLines.Add($"[{i}] (unable to read)");
                        }
                    }
                }
                else
                {
                    return ToolUtils.CreateSuccessResponse(
                        $"Frame debugger has {count} events but event names are not accessible in this Unity version.",
                        new Dictionary<string, object> { { "totalEvents", count } });
                }

                var extras = new Dictionary<string, object>
                {
                    { "totalEvents", count },
                    { "returnedEvents", eventLines.Count },
                    { "events", string.Join("; ", eventLines) }
                };

                return ToolUtils.CreateSuccessResponse($"Retrieved {eventLines.Count} of {count} frame debugger events", extras);
            }
            catch (Exception ex)
            {
                return ToolUtils.CreateErrorResponse($"Frame debugger error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
