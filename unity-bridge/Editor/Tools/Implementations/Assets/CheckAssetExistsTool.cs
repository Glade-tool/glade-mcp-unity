using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class CheckAssetExistsTool : ITool
    {
        public string Name => "check_asset_exists";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"]?.ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
                return "{\"exists\":false}";

            // Normalize path and try case-insensitive lookup
            string normalizedPath;
            string actualPath;
            normalizedPath = ToolUtils.NormalizeAssetPath(assetPath, out actualPath);
            
            // Try loading with case-insensitive search
            UnityEngine.Object asset = ToolUtils.LoadAssetAtPathCaseInsensitive<UnityEngine.Object>(normalizedPath);
            bool exists = asset != null;
            
            var extras = new Dictionary<string, object>
            {
                { "exists", exists },
                { "path", exists ? actualPath : normalizedPath }
            };
            
            // If not found, try to find similar paths to help the AI
            if (!exists)
            {
                string fileName = System.IO.Path.GetFileName(normalizedPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    string searchName = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string[] guids = AssetDatabase.FindAssets(searchName);
                    var similarPaths = new List<string>();
                    
                    foreach (var guid in guids.Take(5)) // Limit to 5 suggestions
                    {
                        string foundPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(foundPath) && !foundPath.StartsWith("Packages/"))
                        {
                            similarPaths.Add(foundPath);
                        }
                    }
                    
                    if (similarPaths.Count > 0)
                    {
                        extras["similarPaths"] = similarPaths;
                    }
                }
            }
            
            return ToolUtils.SerializeDictToJson(extras);
        }
    }
}
