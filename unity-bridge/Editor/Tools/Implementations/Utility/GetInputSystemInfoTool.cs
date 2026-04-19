using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Utility
{
    /// <summary>
    /// Returns project input system configuration. Call before creating input actions or legacy axes.
    /// </summary>
    public class GetInputSystemInfoTool : ITool
    {
        public string Name => "get_input_system_info";

        public string Execute(Dictionary<string, object> args)
        {
            string activeInputHandling;
            bool newPackageInstalled;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            activeInputHandling = "NEW";
            newPackageInstalled = true;
#elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            activeInputHandling = "BOTH";
            newPackageInstalled = true;
#else
            activeInputHandling = "OLD";
            newPackageInstalled = false;
#endif

            string recommended = activeInputHandling;
            string reason = activeInputHandling == "BOTH"
                ? "BOTH – proxy will decide from scripts"
                : "Active input handling is " + activeInputHandling;

            var extras = new Dictionary<string, object>
            {
                { "activeInputHandling", activeInputHandling },
                { "newPackageInstalled", newPackageInstalled },
                { "recommended", recommended },
                { "reason", reason }
            };
            return ToolUtils.CreateSuccessResponse("Input system info", extras);
        }
    }
}
