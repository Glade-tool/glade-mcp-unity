using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ProjectSettings
{
    /// <summary>
    /// Sets Render Pipeline Asset settings (URP/HDRP) including shadow settings, render scale, HDR, and MSAA configuration.
    /// </summary>
    public class SetRenderPipelineAssetSettingsTool : ITool
    {
        public string Name => "set_render_pipeline_asset_settings";

        public string Execute(Dictionary<string, object> args)
        {
#if GLADE_SRP
            UnityEngine.Object rpAsset = null;
            string assetPath = "";
            
            // Load asset from path if provided, otherwise use active pipeline
            if (args.ContainsKey("assetPath") && args["assetPath"] != null && !string.IsNullOrEmpty(args["assetPath"].ToString()))
            {
                string path = args["assetPath"].ToString();
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    path = "Assets/" + path;
                
                rpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (rpAsset == null)
                {
                    return ToolUtils.CreateErrorResponse($"Render pipeline asset not found at '{path}'");
                }
                assetPath = path.StartsWith("Assets/") ? path.Substring(7) : path;
            }
            else
            {
                // Use active render pipeline
                rpAsset = GraphicsSettings.defaultRenderPipeline;
                if (rpAsset != null)
                {
                    string fullPath = AssetDatabase.GetAssetPath(rpAsset);
                    assetPath = fullPath.StartsWith("Assets/") ? fullPath.Substring(7) : fullPath;
                }
            }
            
            if (rpAsset == null)
            {
                return ToolUtils.CreateErrorResponse("No render pipeline asset assigned. Use Built-in render pipeline or assign a URP/HDRP asset.");
            }
            
            // Record asset for undo BEFORE modifying properties
            Undo.RecordObject(rpAsset, $"Set Render Pipeline Asset Settings: {assetPath}");
            
            // Use SerializedObject to access and modify properties
            var serializedObject = new SerializedObject(rpAsset);
            bool hasChanges = false;
            
            // Modify shadow distance
            if (args.ContainsKey("shadowDistance"))
            {
                if (float.TryParse(args["shadowDistance"].ToString(), 
                    System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out float shadowDistance))
                {
                    var prop = serializedObject.FindProperty("m_ShadowDistance");
                    if (prop != null)
                    {
                        prop.floatValue = shadowDistance;
                        hasChanges = true;
                    }
                }
            }
            
            // Modify shadow cascade count
            if (args.ContainsKey("shadowCascadeCount"))
            {
                if (int.TryParse(args["shadowCascadeCount"].ToString(), out int cascadeCount))
                {
                    // Validate: 1, 2, or 4
                    if (cascadeCount == 1 || cascadeCount == 2 || cascadeCount == 4)
                    {
                        var prop = serializedObject.FindProperty("m_ShadowCascadeCount");
                        if (prop != null)
                        {
                            prop.intValue = cascadeCount;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Invalid shadowCascadeCount: {cascadeCount}. Must be 1, 2, or 4");
                    }
                }
            }
            
            // Modify render scale
            if (args.ContainsKey("renderScale"))
            {
                if (float.TryParse(args["renderScale"].ToString(), 
                    System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out float renderScale))
                {
                    // Validate: 0.1 to 2.0
                    if (renderScale >= 0.1f && renderScale <= 2.0f)
                    {
                        var prop = serializedObject.FindProperty("m_RenderScale");
                        if (prop != null)
                        {
                            prop.floatValue = renderScale;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Invalid renderScale: {renderScale}. Must be between 0.1 and 2.0");
                    }
                }
            }
            
            // Modify HDR support
            if (args.ContainsKey("supportsHDR"))
            {
                bool supportsHDR = false;
                if (args["supportsHDR"] is bool b) supportsHDR = b;
                else bool.TryParse(args["supportsHDR"].ToString(), out supportsHDR);
                
                var prop = serializedObject.FindProperty("m_SupportsHDR");
                if (prop != null)
                {
                    prop.boolValue = supportsHDR;
                    hasChanges = true;
                }
            }
            
            // Modify MSAA sample count
            if (args.ContainsKey("msaaSampleCount"))
            {
                if (int.TryParse(args["msaaSampleCount"].ToString(), out int msaa))
                {
                    // Validate: 1, 2, 4, or 8
                    if (msaa == 1 || msaa == 2 || msaa == 4 || msaa == 8)
                    {
                        var prop = serializedObject.FindProperty("m_MSAA");
                        if (prop != null)
                        {
                            prop.intValue = msaa;
                            hasChanges = true;
                        }
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Invalid msaaSampleCount: {msaa}. Must be 1, 2, 4, or 8");
                    }
                }
            }
            
            // URP-specific properties
            string typeName = rpAsset.GetType().Name;
            string namespaceName = rpAsset.GetType().Namespace ?? "";
            bool isURP = typeName.Contains("UniversalRenderPipelineAsset") || 
                        typeName.Contains("Universal") ||
                        namespaceName.Contains("Universal");
            
            if (isURP)
            {
                // Main light shadowmap resolution
                if (args.ContainsKey("mainLightShadowmapResolution"))
                {
                    if (int.TryParse(args["mainLightShadowmapResolution"].ToString(), out int resolution))
                    {
                        // Validate: 256, 512, 1024, 2048, 4096
                        if (resolution == 256 || resolution == 512 || resolution == 1024 || resolution == 2048 || resolution == 4096)
                        {
                            var prop = serializedObject.FindProperty("m_MainLightShadowmapResolution");
                            if (prop != null)
                            {
                                prop.intValue = resolution;
                                hasChanges = true;
                            }
                        }
                        else
                        {
                            return ToolUtils.CreateErrorResponse($"Invalid mainLightShadowmapResolution: {resolution}. Must be 256, 512, 1024, 2048, or 4096");
                        }
                    }
                }
                
                // Main light shadows supported
                if (args.ContainsKey("mainLightShadowsSupported"))
                {
                    bool mainLightShadowsSupported = false;
                    if (args["mainLightShadowsSupported"] is bool b) mainLightShadowsSupported = b;
                    else bool.TryParse(args["mainLightShadowsSupported"].ToString(), out mainLightShadowsSupported);
                    
                    var prop = serializedObject.FindProperty("m_MainLightShadowsSupported");
                    if (prop != null)
                    {
                        prop.boolValue = mainLightShadowsSupported;
                        hasChanges = true;
                    }
                }
                
                // Additional lights shadow resolution tiers
                if (args.ContainsKey("additionalLightsShadowResolutionTierLow"))
                {
                    if (int.TryParse(args["additionalLightsShadowResolutionTierLow"].ToString(), out int resolution))
                    {
                        var prop = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierLow");
                        if (prop != null)
                        {
                            prop.intValue = resolution;
                            hasChanges = true;
                        }
                    }
                }
                
                if (args.ContainsKey("additionalLightsShadowResolutionTierMedium"))
                {
                    if (int.TryParse(args["additionalLightsShadowResolutionTierMedium"].ToString(), out int resolution))
                    {
                        var prop = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierMedium");
                        if (prop != null)
                        {
                            prop.intValue = resolution;
                            hasChanges = true;
                        }
                    }
                }
                
                if (args.ContainsKey("additionalLightsShadowResolutionTierHigh"))
                {
                    if (int.TryParse(args["additionalLightsShadowResolutionTierHigh"].ToString(), out int resolution))
                    {
                        var prop = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierHigh");
                        if (prop != null)
                        {
                            prop.intValue = resolution;
                            hasChanges = true;
                        }
                    }
                }
                
                // Additional light shadows supported
                if (args.ContainsKey("additionalLightShadowsSupported"))
                {
                    bool additionalLightShadowsSupported = false;
                    if (args["additionalLightShadowsSupported"] is bool b) additionalLightShadowsSupported = b;
                    else bool.TryParse(args["additionalLightShadowsSupported"].ToString(), out additionalLightShadowsSupported);
                    
                    var prop = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
                    if (prop != null)
                    {
                        prop.boolValue = additionalLightShadowsSupported;
                        hasChanges = true;
                    }
                }
            }
            
            // Apply changes if any were made
            if (hasChanges)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(rpAsset);
                AssetDatabase.SaveAssets();
            }
            
            return ToolUtils.CreateSuccessResponse($"Updated render pipeline asset settings: {assetPath}");
#else
            return ToolUtils.CreateErrorResponse("Render pipeline tools require SRP (Scriptable Render Pipeline) support. GLADE_SRP is not defined.");
#endif
        }
    }
}
