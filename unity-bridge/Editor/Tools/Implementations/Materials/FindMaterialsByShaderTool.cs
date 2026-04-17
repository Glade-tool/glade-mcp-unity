using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    /// <summary>
    /// Finds all materials in the project that use a specific shader.
    /// Useful for finding materials that need to be converted between render pipelines.
    /// </summary>
    public class FindMaterialsByShaderTool : ITool
    {
        public string Name => "find_materials_by_shader";

        public string Execute(Dictionary<string, object> args)
        {
            string shaderName = args.ContainsKey("shaderName") ? args["shaderName"].ToString() : "";
            
            if (string.IsNullOrEmpty(shaderName))
            {
                return ToolUtils.CreateErrorResponse("shaderName is required");
            }
            
            // Find the shader first to verify it exists
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                return ToolUtils.CreateErrorResponse($"Shader '{shaderName}' not found. Available shaders can be found using list_available_shaders or checking Unity's shader list.");
            }
            
            // Find all materials in the project
            string[] guids = AssetDatabase.FindAssets("t:Material");
            var materialsWithShader = new List<Dictionary<string, object>>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.shader != null && mat.shader.name == shaderName)
                {
                    materialsWithShader.Add(new Dictionary<string, object>
                    {
                        ["materialPath"] = path.StartsWith("Assets/") ? path.Substring(7) : path,
                        ["materialName"] = mat.name,
                        ["shaderName"] = mat.shader.name
                    });
                }
            }
            
            var result = new Dictionary<string, object>
            {
                ["shaderName"] = shaderName,
                ["materials"] = materialsWithShader,
                ["count"] = materialsWithShader.Count
            };
            
            string message = materialsWithShader.Count == 0
                ? $"No materials found using shader '{shaderName}'"
                : $"Found {materialsWithShader.Count} material(s) using shader '{shaderName}'";
            
            return ToolUtils.CreateSuccessResponse(message, result);
        }
    }
}
