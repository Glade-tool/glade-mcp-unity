using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    public class SearchScriptsTool : ITool
    {
        public string Name => "search_scripts";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("query"))
                return ToolUtils.CreateErrorResponse("query is required");

            string query = args["query"]?.ToString() ?? "";
            int maxResults = 10;
            if (args.ContainsKey("maxResults") && int.TryParse(args["maxResults"]?.ToString(), out var parsed))
                maxResults = Mathf.Clamp(parsed, 1, 100);

            var paths = UnityContextGatherer.SearchScriptsContent(query, maxResults);
            return ToolUtils.BuildStringArrayResult("scripts", paths);
        }
    }
}
