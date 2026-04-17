using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ProjectSettings
{
    /// <summary>
    /// Gets QualitySettings information including current quality level, all available quality levels, and settings for shadow quality, pixel light count, anti-aliasing, etc.
    /// </summary>
    public class GetQualitySettingsTool : ITool
    {
        public string Name => "get_quality_settings";

        public string Execute(Dictionary<string, object> args)
        {
            // READ ONLY - No Undo needed
            var settings = new Dictionary<string, object>();
            
            // Get all quality level names
            string[] allQualityLevels = QualitySettings.names;
            settings["allQualityLevels"] = allQualityLevels;
            
            // Get current quality level
            int currentLevelIndex = QualitySettings.GetQualityLevel();
            string currentLevelName = currentLevelIndex >= 0 && currentLevelIndex < allQualityLevels.Length 
                ? allQualityLevels[currentLevelIndex] 
                : "Unknown";
            settings["currentQualityLevel"] = currentLevelName;
            settings["currentQualityLevelIndex"] = currentLevelIndex;
            
            // Determine which quality level to read
            int targetLevelIndex = currentLevelIndex;
            string targetLevelName = currentLevelName;
            
            if (args.ContainsKey("qualityLevel") && args["qualityLevel"] != null)
            {
                string qualityLevelStr = args["qualityLevel"].ToString();
                
                // Try to parse as index
                if (int.TryParse(qualityLevelStr, out int levelIndex))
                {
                    if (levelIndex >= 0 && levelIndex < allQualityLevels.Length)
                    {
                        targetLevelIndex = levelIndex;
                        targetLevelName = allQualityLevels[levelIndex];
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Quality level index {levelIndex} is out of range. Available levels: 0-{allQualityLevels.Length - 1}");
                    }
                }
                else
                {
                    // Try to find by name
                    int foundIndex = Array.IndexOf(allQualityLevels, qualityLevelStr);
                    if (foundIndex >= 0)
                    {
                        targetLevelIndex = foundIndex;
                        targetLevelName = qualityLevelStr;
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Quality level '{qualityLevelStr}' not found. Available levels: {string.Join(", ", allQualityLevels)}");
                    }
                }
            }
            
            // Set the quality level temporarily to read its settings
            int originalLevel = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(targetLevelIndex);
            
            // Read quality settings for the target level (while it's active)
            settings["qualityLevel"] = targetLevelName;
            settings["qualityLevelIndex"] = targetLevelIndex;
            settings["pixelLightCount"] = QualitySettings.pixelLightCount;
            settings["shadows"] = QualitySettings.shadows.ToString();
            settings["shadowResolution"] = QualitySettings.shadowResolution.ToString();
            settings["shadowDistance"] = QualitySettings.shadowDistance;
            settings["shadowCascades"] = QualitySettings.shadowCascades.ToString();
            settings["antiAliasing"] = QualitySettings.antiAliasing;
            settings["softParticles"] = QualitySettings.softParticles;
            settings["realtimeReflectionProbes"] = QualitySettings.realtimeReflectionProbes;
            settings["billboardsFaceCameraPosition"] = QualitySettings.billboardsFaceCameraPosition;
            settings["vSyncCount"] = QualitySettings.vSyncCount;
            settings["lodBias"] = QualitySettings.lodBias;
            settings["maximumLODLevel"] = QualitySettings.maximumLODLevel;
            settings["particleRaycastBudget"] = QualitySettings.particleRaycastBudget;
            
            // Store values for message before restoring
            string shadowsStr = QualitySettings.shadows.ToString();
            int pixelLights = QualitySettings.pixelLightCount;
            int antiAliasing = QualitySettings.antiAliasing;
            
            // Restore original quality level
            QualitySettings.SetQualityLevel(originalLevel);
            
            string message = $"Retrieved quality settings for '{targetLevelName}': Shadows={shadowsStr}, PixelLights={pixelLights}, AntiAliasing={antiAliasing}";
            
            return ToolUtils.CreateSuccessResponse(message, settings);
        }
    }
}
