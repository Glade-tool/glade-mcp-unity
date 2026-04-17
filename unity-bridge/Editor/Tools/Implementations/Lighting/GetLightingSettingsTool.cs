using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    /// <summary>
    /// Gets scene LightingSettings asset information including lightmap settings, GI (Global Illumination) settings, and baking configuration.
    /// </summary>
    public class GetLightingSettingsTool : ITool
    {
        public string Name => "get_lighting_settings";

        public string Execute(Dictionary<string, object> args)
        {
            // READ ONLY - No Undo needed
            UnityEngine.SceneManagement.Scene targetScene = SceneManager.GetActiveScene();
            
            // Check if scene path is provided
            if (args.ContainsKey("scenePath") && !string.IsNullOrEmpty(args["scenePath"]?.ToString()))
            {
                string scenePath = args["scenePath"].ToString();
                if (!scenePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    scenePath = "Assets/" + scenePath;
                }
                
                // Try to load the scene
                UnityEngine.SceneManagement.Scene loadedScene = EditorSceneManager.GetSceneByPath(scenePath);
                if (loadedScene.IsValid())
                {
                    targetScene = loadedScene;
                }
                else
                {
                    return ToolUtils.CreateErrorResponse($"Scene not found at '{scenePath}' or scene is not loaded");
                }
            }
            
            // Get LightingSettings for the scene
            var settings = new Dictionary<string, object>();
            
            // Try to get LightingSettings via LightmapSettings
            try
            {
                // LightmapSettings provides access to lighting data
                settings["lightmapIndex"] = LightmapSettings.lightmaps != null ? LightmapSettings.lightmaps.Length : 0;
                settings["lightProbes"] = LightmapSettings.lightProbes != null ? "Present" : "None";
                
                // Get lighting data asset path if available
                if (Lightmapping.lightingDataAsset != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                    settings["lightingDataAsset"] = assetPath.StartsWith("Assets/") ? assetPath.Substring(7) : assetPath;
                }
                else
                {
                    settings["lightingDataAsset"] = "";
                }
            }
            catch (Exception e)
            {
                // If we can't access lighting settings, return what we can
                settings["error"] = $"Could not fully access lighting settings: {e.Message}";
            }
            
            // Get scene path
            settings["scenePath"] = targetScene.path.StartsWith("Assets/") ? targetScene.path.Substring(7) : targetScene.path;
            settings["sceneName"] = targetScene.name;
            
            // Check if scene has lighting data
            bool hasLightingData = Lightmapping.lightingDataAsset != null;
            settings["hasLightingData"] = hasLightingData;
            
            string message = hasLightingData
                ? $"Retrieved lighting settings for scene '{targetScene.name}': Lighting data asset present"
                : $"Retrieved lighting settings for scene '{targetScene.name}': No lighting data asset (lightmaps not baked)";
            
            return ToolUtils.CreateSuccessResponse(message, settings);
        }
    }
}
