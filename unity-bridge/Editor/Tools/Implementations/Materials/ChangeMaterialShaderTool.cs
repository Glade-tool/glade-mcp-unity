using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    /// <summary>
    /// Changes a material's shader to a different shader.
    /// Attempts to preserve common shader properties when converting between similar shaders.
    /// </summary>
    public class ChangeMaterialShaderTool : ITool
    {
        public string Name => "change_material_shader";

        public string Execute(Dictionary<string, object> args)
        {
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";
            string newShaderName = args.ContainsKey("newShaderName") ? args["newShaderName"].ToString() : "";
            
            if (string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("materialPath is required");
            }
            
            if (string.IsNullOrEmpty(newShaderName))
            {
                return ToolUtils.CreateErrorResponse("newShaderName is required");
            }
            
            // Ensure path starts with Assets/
            if (!materialPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                materialPath = "Assets/" + materialPath;
            }
            
            // Load material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                return ToolUtils.CreateErrorResponse($"Material not found at '{materialPath}'");
            }
            
            // Find the new shader
            Shader newShader = Shader.Find(newShaderName);
            if (newShader == null)
            {
                return ToolUtils.CreateErrorResponse($"Shader '{newShaderName}' not found. Check the system prompt for available shaders based on the active render pipeline.");
            }
            
            try
            {
                // Record BEFORE changing shader
                Undo.RecordObject(mat, $"Change Material Shader: {materialPath}");
                
                // Store old shader name and properties before changing
                string oldShaderName = mat.shader != null ? mat.shader.name : "Unknown";
                
                // Store common properties that might be transferable
                Color? oldColor = null;
                Texture2D oldMainTex = null;
                float? oldMetallic = null;
                float? oldSmoothness = null;
                Texture2D oldNormalMap = null;
                Color? oldEmissionColor = null;
                
                // Try to preserve common properties
                if (mat.HasProperty("_Color"))
                {
                    oldColor = mat.GetColor("_Color");
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    oldColor = mat.GetColor("_BaseColor");
                }
                
                if (mat.HasProperty("_MainTex"))
                {
                    oldMainTex = mat.GetTexture("_MainTex") as Texture2D;
                }
                else if (mat.HasProperty("_BaseMap"))
                {
                    oldMainTex = mat.GetTexture("_BaseMap") as Texture2D;
                }
                
                if (mat.HasProperty("_Metallic"))
                {
                    oldMetallic = mat.GetFloat("_Metallic");
                }
                
                if (mat.HasProperty("_Smoothness"))
                {
                    oldSmoothness = mat.GetFloat("_Smoothness");
                }
                else if (mat.HasProperty("_Glossiness"))
                {
                    oldSmoothness = mat.GetFloat("_Glossiness");
                }
                
                if (mat.HasProperty("_BumpMap"))
                {
                    oldNormalMap = mat.GetTexture("_BumpMap") as Texture2D;
                }
                else if (mat.HasProperty("_NormalMap"))
                {
                    oldNormalMap = mat.GetTexture("_NormalMap") as Texture2D;
                }
                
                if (mat.HasProperty("_EmissionColor"))
                {
                    oldEmissionColor = mat.GetColor("_EmissionColor");
                }
                
                // Change the shader
                mat.shader = newShader;
                
                // Try to restore common properties with new shader property names
                if (oldColor.HasValue)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", oldColor.Value);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", oldColor.Value);
                    }
                }
                
                if (oldMainTex != null)
                {
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTexture("_BaseMap", oldMainTex);
                    }
                    else if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", oldMainTex);
                    }
                }
                
                if (oldMetallic.HasValue && mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", oldMetallic.Value);
                }
                
                if (oldSmoothness.HasValue)
                {
                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", oldSmoothness.Value);
                    }
                    else if (mat.HasProperty("_Glossiness"))
                    {
                        mat.SetFloat("_Glossiness", oldSmoothness.Value);
                    }
                }
                
                if (oldNormalMap != null)
                {
                    if (mat.HasProperty("_NormalMap"))
                    {
                        mat.SetTexture("_NormalMap", oldNormalMap);
                    }
                    else if (mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", oldNormalMap);
                    }
                }
                
                if (oldEmissionColor.HasValue && mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", oldEmissionColor.Value);
                }
                
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
                
                var extras = new Dictionary<string, object>
                {
                    ["materialPath"] = materialPath.StartsWith("Assets/") ? materialPath.Substring(7) : materialPath,
                    ["oldShaderName"] = oldShaderName,
                    ["newShaderName"] = newShaderName,
                    ["propertiesPreserved"] = new Dictionary<string, bool>
                    {
                        ["color"] = oldColor.HasValue,
                        ["mainTexture"] = oldMainTex != null,
                        ["metallic"] = oldMetallic.HasValue,
                        ["smoothness"] = oldSmoothness.HasValue,
                        ["normalMap"] = oldNormalMap != null,
                        ["emission"] = oldEmissionColor.HasValue
                    }
                };
                
                return ToolUtils.CreateSuccessResponse(
                    $"Changed material '{materialPath}' shader from '{oldShaderName}' to '{newShaderName}'. Common properties were preserved where possible.",
                    extras
                );
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to change material shader: {e.Message}");
            }
        }
    }
}
