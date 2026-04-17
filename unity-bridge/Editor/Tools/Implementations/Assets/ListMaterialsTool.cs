using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class ListMaterialsTool : ITool
    {
        public string Name => "list_materials";

        public string Execute(Dictionary<string, object> args)
        {
            string searchPattern = args.ContainsKey("searchPattern") ? args["searchPattern"]?.ToString() : "";
            int maxResults = 200;
            if (args.ContainsKey("maxResults") && int.TryParse(args["maxResults"]?.ToString(), out var parsed))
                maxResults = Mathf.Clamp(parsed, 1, 500);

            string[] guids = AssetDatabase.FindAssets("t:Material" + (string.IsNullOrEmpty(searchPattern) ? "" : $" {searchPattern}"));
            var materials = new List<string>();
            foreach (string guid in guids)
            {
                if (materials.Count >= maxResults) break;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                materials.Add(path);
            }
            int totalFound = guids.Length;
            string message = totalFound <= maxResults
                ? $"Found {totalFound} material(s)."
                : $"Found {totalFound} material(s), showing first {maxResults}. Use searchPattern to filter or increase maxResults.";
            return ToolUtils.BuildStringArrayResultWithCount("materials", materials, message);
        }
    }
}
