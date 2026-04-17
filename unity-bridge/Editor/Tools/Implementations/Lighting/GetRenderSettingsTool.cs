using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    /// <summary>
    /// Gets current RenderSettings including fog settings, ambient lighting, skybox, and other global rendering properties.
    /// </summary>
    public class GetRenderSettingsTool : ITool
    {
        public string Name => "get_render_settings";

        public string Execute(Dictionary<string, object> args)
        {
            // READ ONLY - No Undo needed
            var settings = new Dictionary<string, object>();
            
            // Fog settings
            settings["fogEnabled"] = RenderSettings.fog;
            settings["fogColor"] = $"{RenderSettings.fogColor.r},{RenderSettings.fogColor.g},{RenderSettings.fogColor.b},{RenderSettings.fogColor.a}";
            settings["fogMode"] = RenderSettings.fogMode.ToString();
            settings["fogDensity"] = RenderSettings.fogDensity;
            settings["fogStartDistance"] = RenderSettings.fogStartDistance;
            settings["fogEndDistance"] = RenderSettings.fogEndDistance;
            
            // Ambient settings
#if GLADE_SRP
            settings["ambientMode"] = RenderSettings.ambientMode.ToString();
#endif
            settings["ambientColor"] = $"{RenderSettings.ambientLight.r},{RenderSettings.ambientLight.g},{RenderSettings.ambientLight.b},{RenderSettings.ambientLight.a}";
            settings["ambientIntensity"] = RenderSettings.ambientIntensity;
            
            // Skybox
            if (RenderSettings.skybox != null)
            {
                string skyboxPath = AssetDatabase.GetAssetPath(RenderSettings.skybox);
                settings["skyboxMaterial"] = skyboxPath.StartsWith("Assets/") ? skyboxPath.Substring(7) : skyboxPath;
            }
            else
            {
                settings["skyboxMaterial"] = "";
            }
            
            // Default reflection mode
            settings["defaultReflectionMode"] = RenderSettings.defaultReflectionMode.ToString();
            
            // Default reflection resolution
            settings["defaultReflectionResolution"] = RenderSettings.defaultReflectionResolution;
            
            string message = $"Retrieved render settings: Fog {(RenderSettings.fog ? "enabled" : "disabled")}, Ambient mode: {RenderSettings.ambientMode}";
            
            return ToolUtils.CreateSuccessResponse(message, settings);
        }
    }
}
