using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    public class CreateMaterialTool : ITool
    {
        public string Name => "create_material";

        public string Execute(Dictionary<string, object> args)
        {
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";
            if (string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("materialPath is required");
            }
            
            // Ensure path starts with Assets/
            if (!materialPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                materialPath = "Assets/" + materialPath;
            }
            
            // Detect render pipeline and get appropriate default shader
            string defaultShaderName = ToolUtils.GetDefaultShaderForRenderPipeline();
            bool isURPProject = defaultShaderName.Contains("Universal Render Pipeline");
            bool isHDRPProject = defaultShaderName.Contains("HDRP");
            
            // Find or create shader
            bool aiProvidedShader = args.ContainsKey("shaderName") && !string.IsNullOrEmpty(args["shaderName"].ToString());
            string requestedShaderName = aiProvidedShader 
                ? args["shaderName"].ToString() 
                : defaultShaderName;
            
            Shader shader = Shader.Find(requestedShaderName);
            
            // If shader not found, try pipeline-appropriate fallbacks
            if (shader == null)
            {
                // Try the default shader for this pipeline
                shader = Shader.Find(defaultShaderName);
                
                // If still not found and it's URP, try alternative URP shaders
                if (shader == null && isURPProject)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null)
                        shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                }
                
                // If still not found and it's HDRP, try alternative HDRP shaders
                if (shader == null && isHDRPProject)
                {
                    shader = Shader.Find("HDRP/Unlit");
                    if (shader == null)
                        shader = Shader.Find("HDRP/Lit");
                }
                
                // Only fall back to Standard if we're using Built-in pipeline
                if (shader == null && !isURPProject && !isHDRPProject)
                {
                    shader = Shader.Find("Standard");
                }
                
                // Last resort: if still null, create with default shader name (Unity will handle it)
                if (shader == null)
                {
                    UnityEngine.Debug.LogWarning($"[CreateMaterialTool] Could not find shader '{requestedShaderName}' or '{defaultShaderName}'. Using default shader.");
                    shader = Shader.Find(defaultShaderName) ?? Shader.Find("Standard");
                }
            }
            
            Material mat = new Material(shader);
            mat.name = System.IO.Path.GetFileNameWithoutExtension(materialPath);
            
            // Determine property names based on ACTUAL shader that was found (not requested)
            string actualShaderName = shader.name;
            bool isURP = actualShaderName.Contains("Universal Render Pipeline");
            bool isHDRP = actualShaderName.Contains("HDRP");
            bool isStandard = actualShaderName == "Standard" || (!isURP && !isHDRP);
            
            // Set color if provided (use correct property name for pipeline)
            if (args.ContainsKey("color"))
            {
                string colorStr = args["color"].ToString();
                Color color = ToolUtils.ParseColor(colorStr);
                
                if (isURP)
                {
                    mat.SetColor("_BaseColor", color);
                }
                else if (isHDRP)
                {
                    mat.SetColor("_BaseColor", color);
                }
                else
                {
                    // Standard/Built-in
                    mat.color = color; // Uses _Color property
                }
            }
            
            // Set metallic if provided (use correct property name for pipeline)
            if (args.ContainsKey("metallic"))
            {
                float metallic = float.Parse(args["metallic"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                if (isURP || isHDRP)
                {
                    mat.SetFloat("_Metallic", metallic);
                }
                else
                {
                    mat.SetFloat("_Metallic", metallic);
                }
            }
            
            // Set smoothness if provided (use correct property name for pipeline)
            if (args.ContainsKey("smoothness"))
            {
                float smoothness = float.Parse(args["smoothness"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                if (isURP)
                {
                    mat.SetFloat("_Smoothness", smoothness);
                }
                else if (isHDRP)
                {
                    mat.SetFloat("_Smoothness", smoothness);
                }
                else
                {
                    // Standard/Built-in uses _Glossiness
                    mat.SetFloat("_Glossiness", smoothness);
                }
            }
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(materialPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            // Save material
            AssetDatabase.CreateAsset(mat, materialPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Return detailed result including shader info
            string requestedInfo = requestedShaderName != actualShaderName 
                ? $" (requested: '{requestedShaderName}')" 
                : "";
            
            var extras = new Dictionary<string, object>
            {
                { "materialPath", materialPath },
                { "shaderUsed", actualShaderName },
                { "shaderRequested", requestedShaderName }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created material at '{materialPath}' using shader '{actualShaderName}'{requestedInfo}", extras);
        }
    }
}
