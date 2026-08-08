using System.Collections.Generic;
using GladeAgenticAI.Core.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Build
{
    /// <summary>
    /// Report the outcome of the last build_player call.
    ///
    /// The state is persisted in SessionState rather than a static field
    /// because a build can trigger a script recompile for the target platform,
    /// and the domain reload that follows would wipe a plain static — telling
    /// the agent "no build has run" seconds after one finished.
    ///
    /// Read-only: it never starts, cancels, or mutates anything.
    /// </summary>
    public class GetBuildStatusTool : ITool
    {
        public string Name => "get_build_status";

        public string Execute(Dictionary<string, object> args)
        {
            var state = BuildManager.GetResult();

            if (state == null)
            {
                return ToolUtils.CreateSuccessResponse(
                    "No build has been run in this editor session.",
                    new Dictionary<string, object>
                    {
                        { "status", "none" },
                        { "next_step", "Call get_build_info to check what can be built, " +
                                       "then build_player." },
                    });
            }

            string status = (string)state["status"];

            if (status == "running")
            {
                return ToolUtils.CreateSuccessResponse(
                    "A build is still running. Unity blocks its main thread while building, so " +
                    "this call only returns once the editor is free — if it timed out, retry.",
                    state);
            }

            bool succeeded = status == "succeeded";
            return ToolUtils.CreateSuccessResponse(
                succeeded
                    ? "Last build succeeded: " + state["outputPath"] + " (" + state["sizeHuman"] +
                      " in " + state["buildSeconds"] + "s)"
                    : "Last build FAILED — read `errors` for the reason.",
                state);
        }
    }
}
