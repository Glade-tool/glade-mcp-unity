using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ProjectSettings
{
    /// <summary>
    /// Gets Render Pipeline Asset settings (URP/HDRP) including shadow settings, render scale, HDR, and MSAA configuration.
    /// </summary>
    public class GetRenderPipelineAssetSettingsTool : ITool
    {
        public string Name => "get_render_pipeline_asset_settings";

        public string Execute(Dictionary<string, object> args)
        {
            // READ ONLY - No Undo needed
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
            
            var settings = new Dictionary<string, object>();
            string typeName = rpAsset.GetType().Name;
            string namespaceName = rpAsset.GetType().Namespace ?? "";
            
            // Detect pipeline type
            bool isURP = typeName.Contains("UniversalRenderPipelineAsset") || 
                        typeName.Contains("Universal") ||
                        namespaceName.Contains("Universal");
            bool isHDRP = typeName.Contains("HDRenderPipelineAsset") || 
                         typeName.Contains("HD") ||
                         namespaceName.Contains("HighDefinition");
            
            settings["pipelineType"] = isURP ? "URP" : (isHDRP ? "HDRP" : "Unknown");
            settings["assetPath"] = assetPath;
            settings["assetType"] = typeName;
            
            // Use SerializedObject to access properties that may not be directly accessible
            var serializedObject = new SerializedObject(rpAsset);
            
            // Common properties (both URP and HDRP)
            var shadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            if (shadowDistanceProp != null)
                settings["shadowDistance"] = shadowDistanceProp.floatValue;
            
            var shadowCascadeCountProp = serializedObject.FindProperty("m_ShadowCascadeCount");
            if (shadowCascadeCountProp != null)
                settings["shadowCascadeCount"] = shadowCascadeCountProp.intValue;
            
            var renderScaleProp = serializedObject.FindProperty("m_RenderScale");
            if (renderScaleProp != null)
                settings["renderScale"] = renderScaleProp.floatValue;
            
            var supportsHDRProp = serializedObject.FindProperty("m_SupportsHDR");
            if (supportsHDRProp != null)
                settings["supportsHDR"] = supportsHDRProp.boolValue;
            
            var msaaProp = serializedObject.FindProperty("m_MSAA");
            if (msaaProp != null)
                settings["msaaSampleCount"] = msaaProp.intValue;
            
            // URP-specific properties
            if (isURP)
            {
                var mainLightShadowmapResolutionProp = serializedObject.FindProperty("m_MainLightShadowmapResolution");
                if (mainLightShadowmapResolutionProp != null)
                    settings["mainLightShadowmapResolution"] = mainLightShadowmapResolutionProp.intValue;
                
                var mainLightShadowsSupportedProp = serializedObject.FindProperty("m_MainLightShadowsSupported");
                if (mainLightShadowsSupportedProp != null)
                    settings["mainLightShadowsSupported"] = mainLightShadowsSupportedProp.boolValue;
                
                var additionalLightsShadowResolutionTierLowProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierLow");
                if (additionalLightsShadowResolutionTierLowProp != null)
                    settings["additionalLightsShadowResolutionTierLow"] = additionalLightsShadowResolutionTierLowProp.intValue;
                
                var additionalLightsShadowResolutionTierMediumProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierMedium");
                if (additionalLightsShadowResolutionTierMediumProp != null)
                    settings["additionalLightsShadowResolutionTierMedium"] = additionalLightsShadowResolutionTierMediumProp.intValue;
                
                var additionalLightsShadowResolutionTierHighProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierHigh");
                if (additionalLightsShadowResolutionTierHighProp != null)
                    settings["additionalLightsShadowResolutionTierHigh"] = additionalLightsShadowResolutionTierHighProp.intValue;
                
                var additionalLightShadowsSupportedProp = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
                if (additionalLightShadowsSupportedProp != null)
                    settings["additionalLightShadowsSupported"] = additionalLightShadowsSupportedProp.boolValue;
            }
            
            // HDRP-specific properties (if needed in the future)
            if (isHDRP)
            {
                // HDRP has different property names and structure
                // Add HDRP-specific property reading here if needed
                settings["hdrpNote"] = "HDRP detected - specific properties may vary by version";
            }
            
            object shadowDist = settings.ContainsKey("shadowDistance") ? settings["shadowDistance"] : "N/A";
            object renderScale = settings.ContainsKey("renderScale") ? settings["renderScale"] : "N/A";
            string message = $"Retrieved render pipeline asset settings: {settings["pipelineType"]}, Shadow Distance={shadowDist}, Render Scale={renderScale}";
            
            return ToolUtils.CreateSuccessResponse(message, settings);
#else
            return ToolUtils.CreateErrorResponse("Render pipeline tools require SRP (Scriptable Render Pipeline) support. GLADE_SRP is not defined.");
#endif
        }
    }
}
