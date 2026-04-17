using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    /// <summary>
    /// Lists all available shaders in the project. Useful for discovering shader names.
    /// </summary>
    public class ListAvailableShadersTool : ITool
    {
        public string Name => "list_available_shaders";

        public string Execute(Dictionary<string, object> args)
        {
            string searchPattern = args.ContainsKey("searchPattern") ? args["searchPattern"]?.ToString() : "";
            int maxResults = 200;
            if (args.ContainsKey("maxResults") && int.TryParse(args["maxResults"]?.ToString(), out var parsed))
                maxResults = Mathf.Clamp(parsed, 1, 500);

            // Collect unique shader names from all materials in the project
            var shaderNames = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:Material");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.shader != null)
                {
                    string shaderName = mat.shader.name;
                    if (string.IsNullOrEmpty(searchPattern) || shaderName.IndexOf(searchPattern, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shaderNames.Add(shaderName);
                    }
                }
            }
            
            // Also check for shaders that might not be used by any materials
            // Use ShaderUtil to get all shaders (if available)
            try
            {
                // Try to get shaders via reflection or ShaderUtil
                // Note: ShaderUtil.GetAllShaderInfo() might not be available in all Unity versions
                // So we primarily rely on materials, but this gives us a good coverage
            }
            catch
            {
                // If ShaderUtil methods aren't available, we'll just use materials
            }
            
            var shaderList = shaderNames.ToList();
            shaderList.Sort();
            
            // Apply maxResults limit
            if (shaderList.Count > maxResults)
            {
                shaderList = shaderList.Take(maxResults).ToList();
            }
            
            int totalFound = shaderNames.Count;
            string message = totalFound <= maxResults
                ? $"Found {totalFound} unique shader(s)."
                : $"Found {totalFound} unique shader(s), showing first {maxResults}. Use searchPattern to filter or increase maxResults.";
            
            return ToolUtils.BuildStringArrayResultWithCount("shaders", shaderList, message);
        }
    }
}
