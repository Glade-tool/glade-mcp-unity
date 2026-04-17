using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    /// <summary>
    /// Converts materials from one render pipeline shader to another.
    /// Specifically designed for converting Built-in/Standard materials to URP or HDRP.
    /// Handles common shader property mappings automatically.
    /// </summary>
    public class ConvertMaterialsToRenderPipelineTool : ITool
    {
        public string Name => "convert_materials_to_render_pipeline";

        public string Execute(Dictionary<string, object> args)
        {
            string shaderName = args.ContainsKey("shaderName") ? args["shaderName"].ToString() : "";
            string targetPipeline = args.ContainsKey("targetPipeline") ? args["targetPipeline"].ToString() : "";
            string targetShaderName = args.ContainsKey("targetShader") ? args["targetShader"].ToString() : "";
            
            if (string.IsNullOrEmpty(shaderName))
            {
                return ToolUtils.CreateErrorResponse("shaderName is required - the shader name to find materials using (e.g., 'Nature/Soft Occlusion', 'Standard')");
            }
            
            if (string.IsNullOrEmpty(targetPipeline))
            {
                return ToolUtils.CreateErrorResponse("targetPipeline is required - must be 'URP' or 'HDRP'");
            }
            
            targetPipeline = targetPipeline.ToUpper();
            if (targetPipeline != "URP" && targetPipeline != "HDRP")
            {
                return ToolUtils.CreateErrorResponse("targetPipeline must be 'URP' or 'HDRP'");
            }
            
            // Target shader must be provided by AI (from RAG context)
            if (string.IsNullOrEmpty(targetShaderName))
            {
                return ToolUtils.CreateErrorResponse($"targetShader is required. Use RAG context to determine the appropriate {targetPipeline} shader for '{shaderName}'. Common mappings: 'Nature/Soft Occlusion' (BIRP) -> 'Universal Render Pipeline/Nature/SpeedTree7' (URP), 'Standard' (BIRP) -> 'Universal Render Pipeline/Lit' (URP).");
            }
            
            // Verify target shader exists
            Shader targetShader = Shader.Find(targetShaderName);
            if (targetShader == null)
            {
                return ToolUtils.CreateErrorResponse($"Target shader '{targetShaderName}' not found. Check that {targetPipeline} is installed and the shader name is correct.");
            }
            
            // Find all materials using the source shader
            string[] guids = AssetDatabase.FindAssets("t:Material");
            var convertedMaterials = new List<Dictionary<string, object>>();
            var failedMaterials = new List<Dictionary<string, object>>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                
                if (mat != null && mat.shader != null && mat.shader.name == shaderName)
                {
                    try
                    {
                        // Record BEFORE modifying
                        Undo.RecordObject(mat, $"Convert Material to {targetPipeline}: {path}");
                        
                        // Store properties before conversion
                        var preservedProps = PreserveMaterialProperties(mat);
                        
                        // Change shader
                        mat.shader = targetShader;
                        
                        // Restore properties with new shader property names
                        RestoreMaterialProperties(mat, preservedProps, targetPipeline);
                        
                        EditorUtility.SetDirty(mat);
                        
                        convertedMaterials.Add(new Dictionary<string, object>
                        {
                            ["materialPath"] = path.StartsWith("Assets/") ? path.Substring(7) : path,
                            ["materialName"] = mat.name,
                            ["oldShader"] = shaderName,
                            ["newShader"] = targetShaderName
                        });
                    }
                    catch (Exception e)
                    {
                        failedMaterials.Add(new Dictionary<string, object>
                        {
                            ["materialPath"] = path.StartsWith("Assets/") ? path.Substring(7) : path,
                            ["error"] = e.Message
                        });
                    }
                }
            }
            
            if (convertedMaterials.Count > 0)
            {
                AssetDatabase.SaveAssets();
            }
            
            var result = new Dictionary<string, object>
            {
                ["sourceShader"] = shaderName,
                ["targetPipeline"] = targetPipeline,
                ["targetShader"] = targetShaderName,
                ["convertedCount"] = convertedMaterials.Count,
                ["failedCount"] = failedMaterials.Count,
                ["convertedMaterials"] = convertedMaterials,
                ["failedMaterials"] = failedMaterials
            };
            
            string message = convertedMaterials.Count == 0
                ? $"No materials found using shader '{shaderName}'"
                : $"Converted {convertedMaterials.Count} material(s) from '{shaderName}' to {targetPipeline} shader '{targetShaderName}'" +
                  (failedMaterials.Count > 0 ? $". {failedMaterials.Count} material(s) failed to convert." : "");
            
            return ToolUtils.CreateSuccessResponse(message, result);
        }
        
        private Dictionary<string, object> PreserveMaterialProperties(Material mat)
        {
            var props = new Dictionary<string, object>();
            
            // Color/BaseColor
            if (mat.HasProperty("_Color"))
                props["_Color"] = mat.GetColor("_Color");
            else if (mat.HasProperty("_BaseColor"))
                props["_BaseColor"] = mat.GetColor("_BaseColor");
            
            // Main texture
            if (mat.HasProperty("_MainTex"))
                props["_MainTex"] = mat.GetTexture("_MainTex");
            else if (mat.HasProperty("_BaseMap"))
                props["_BaseMap"] = mat.GetTexture("_BaseMap");
            
            // Metallic
            if (mat.HasProperty("_Metallic"))
                props["_Metallic"] = mat.GetFloat("_Metallic");
            
            // Smoothness/Glossiness
            if (mat.HasProperty("_Smoothness"))
                props["_Smoothness"] = mat.GetFloat("_Smoothness");
            else if (mat.HasProperty("_Glossiness"))
                props["_Glossiness"] = mat.GetFloat("_Glossiness");
            
            // Normal map
            if (mat.HasProperty("_BumpMap"))
                props["_BumpMap"] = mat.GetTexture("_BumpMap");
            else if (mat.HasProperty("_NormalMap"))
                props["_NormalMap"] = mat.GetTexture("_NormalMap");
            
            // Emission
            if (mat.HasProperty("_EmissionColor"))
                props["_EmissionColor"] = mat.GetColor("_EmissionColor");
            
            return props;
        }
        
        private void RestoreMaterialProperties(Material mat, Dictionary<string, object> props, string targetPipeline)
        {
            // Color/BaseColor mapping
            if (props.ContainsKey("_Color") || props.ContainsKey("_BaseColor"))
            {
                Color color = props.ContainsKey("_Color") ? (Color)props["_Color"] : (Color)props["_BaseColor"];
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
            }
            
            // Main texture mapping
            if (props.ContainsKey("_MainTex") || props.ContainsKey("_BaseMap"))
            {
                Texture tex = props.ContainsKey("_MainTex") ? (Texture)props["_MainTex"] : (Texture)props["_BaseMap"];
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }
            
            // Metallic
            if (props.ContainsKey("_Metallic") && mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", (float)props["_Metallic"]);
            }
            
            // Smoothness/Glossiness mapping
            if (props.ContainsKey("_Smoothness") || props.ContainsKey("_Glossiness"))
            {
                float smoothness = props.ContainsKey("_Smoothness") ? (float)props["_Smoothness"] : (float)props["_Glossiness"];
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", smoothness);
                else if (mat.HasProperty("_Glossiness"))
                    mat.SetFloat("_Glossiness", smoothness);
            }
            
            // Normal map mapping
            if (props.ContainsKey("_BumpMap") || props.ContainsKey("_NormalMap"))
            {
                Texture normal = props.ContainsKey("_BumpMap") ? (Texture)props["_BumpMap"] : (Texture)props["_NormalMap"];
                if (mat.HasProperty("_NormalMap"))
                    mat.SetTexture("_NormalMap", normal);
                else if (mat.HasProperty("_BumpMap"))
                    mat.SetTexture("_BumpMap", normal);
            }
            
            // Emission
            if (props.ContainsKey("_EmissionColor") && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", (Color)props["_EmissionColor"]);
            }
        }
    }
}
