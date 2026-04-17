using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Profiler
{
    public class EnableFrameDebuggerTool : ITool
    {
        public string Name => "enable_frame_debugger";

        private static Type _frameDebuggerType;
        private static MethodInfo _setEnabled;
        private static bool _resolved;

        private static bool Resolve()
        {
            if (_resolved) return _setEnabled != null;
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
                    _frameDebuggerType = asm.GetType(name);
                    if (_frameDebuggerType != null) goto found;
                }
            }
            return false;

            found:
            // Unity 6: SetEnabled(bool, int)
            _setEnabled = _frameDebuggerType.GetMethod("SetEnabled",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(bool), typeof(int) }, null);
            // Older Unity: SetEnabled(bool, string)
            _setEnabled ??= _frameDebuggerType.GetMethod("SetEnabled",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(bool), typeof(string) }, null);
            // Fallback: SetEnabled(bool)
            _setEnabled ??= _frameDebuggerType.GetMethod("SetEnabled",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(bool) }, null);

            return _setEnabled != null;
        }

        public string Execute(Dictionary<string, object> args)
        {
            if (!Resolve())
                return ToolUtils.CreateErrorResponse("FrameDebuggerUtility not available. Use Window > Analysis > Frame Debugger manually.");

            bool enable = true;
            if (args.ContainsKey("enable") && bool.TryParse(args["enable"].ToString(), out bool e))
                enable = e;

            try
            {
                var paramTypes = _setEnabled.GetParameters();
                if (paramTypes.Length == 2 && paramTypes[1].ParameterType == typeof(int))
                    _setEnabled.Invoke(null, new object[] { enable, 0 });
                else if (paramTypes.Length == 2 && paramTypes[1].ParameterType == typeof(string))
                    _setEnabled.Invoke(null, new object[] { enable, "" });
                else
                    _setEnabled.Invoke(null, new object[] { enable });

                return enable
                    ? ToolUtils.CreateSuccessResponse("Frame debugger enabled. Use get_frame_debugger_events to inspect render passes.")
                    : ToolUtils.CreateSuccessResponse("Frame debugger disabled");
            }
            catch (Exception ex)
            {
                return ToolUtils.CreateErrorResponse($"Frame debugger error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
