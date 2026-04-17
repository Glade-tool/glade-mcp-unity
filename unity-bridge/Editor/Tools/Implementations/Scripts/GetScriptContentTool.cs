using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    public class GetScriptContentTool : ITool
    {
        public string Name => "get_script_content";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("scriptPath"))
                return ToolUtils.CreateErrorResponse("scriptPath is required");

            string scriptPath = args["scriptPath"]?.ToString() ?? "";
            if (UnityContextGatherer.TryGetScriptContent(scriptPath, out var content, out var error))
            {
                var extras = new Dictionary<string, object>
                {
                    { "scriptPath", scriptPath },
                    { "content", content }
                };
                return ToolUtils.CreateSuccessResponse($"Read script: {scriptPath}", extras);
            }
            return ToolUtils.CreateErrorResponse(error ?? "Failed to read script");
        }
    }
}
