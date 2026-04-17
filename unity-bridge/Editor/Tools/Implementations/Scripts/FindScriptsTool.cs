using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    public class FindScriptsTool : ITool
    {
        public string Name => "find_scripts";

        public string Execute(Dictionary<string, object> args)
        {
            string nameContains = args.ContainsKey("nameContains") ? args["nameContains"]?.ToString() : "";
            int maxResults = 20;
            if (args.ContainsKey("maxResults") && int.TryParse(args["maxResults"]?.ToString(), out var parsed))
                maxResults = Mathf.Clamp(parsed, 1, 100);

            var paths = UnityContextGatherer.FindScriptPaths(nameContains, maxResults);
            return ToolUtils.BuildStringArrayResult("scripts", paths);
        }
    }
}
