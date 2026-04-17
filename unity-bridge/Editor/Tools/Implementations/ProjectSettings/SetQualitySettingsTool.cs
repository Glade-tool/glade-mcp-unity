using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ProjectSettings
{
    /// <summary>
    /// Sets QualitySettings properties including quality level, shadow quality, pixel light count, anti-aliasing, etc.
    /// Note: QualitySettings may have limited Undo support in Unity, but changes are persisted to ProjectSettings/QualitySettings.asset.
    /// </summary>
    public class SetQualitySettingsTool : ITool
    {
        public string Name => "set_quality_settings";

        public string Execute(Dictionary<string, object> args)
        {
            // Get all quality level names
            string[] allQualityLevels = QualitySettings.names;
            int originalLevel = QualitySettings.GetQualityLevel();
            int targetLevelIndex = originalLevel;
            string targetLevelName = allQualityLevels[originalLevel];
            
            // Handle quality level selection
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
            
            // Set quality level if different from current
            if (targetLevelIndex != originalLevel)
            {
                // Note: QualitySettings is a static class and cannot use Undo.RecordObject
                // Changes are persisted to ProjectSettings/QualitySettings.asset
                QualitySettings.SetQualityLevel(targetLevelIndex);
            }
            
            // Note: QualitySettings is a static class and cannot use Undo.RecordObject
            // Changes are persisted directly to ProjectSettings/QualitySettings.asset
            
            // Modify pixel light count
            if (args.ContainsKey("pixelLightCount"))
            {
                if (int.TryParse(args["pixelLightCount"].ToString(), out int pixelLightCount))
                {
                    QualitySettings.pixelLightCount = pixelLightCount;
                }
            }
            
            // Modify shadows
            if (args.ContainsKey("shadows"))
            {
                string shadowsStr = args["shadows"].ToString();
                ShadowQuality currentShadows = QualitySettings.shadows;
                QualitySettings.shadows = shadowsStr.ToLower() switch
                {
                    "disable" => ShadowQuality.Disable,
                    "hardonly" => ShadowQuality.HardOnly,
                    "all" => ShadowQuality.All,
                    _ => currentShadows
                };
            }
            
            // Modify shadow resolution
            if (args.ContainsKey("shadowResolution"))
            {
                string resolutionStr = args["shadowResolution"].ToString();
                ShadowResolution currentResolution = QualitySettings.shadowResolution;
                QualitySettings.shadowResolution = resolutionStr.ToLower() switch
                {
                    "low" => ShadowResolution.Low,
                    "medium" => ShadowResolution.Medium,
                    "high" => ShadowResolution.High,
                    "veryhigh" => ShadowResolution.VeryHigh,
                    _ => currentResolution
                };
            }
            
            // Modify shadow distance
            if (args.ContainsKey("shadowDistance"))
            {
                if (float.TryParse(args["shadowDistance"].ToString(), 
                    System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out float shadowDistance))
                {
                    QualitySettings.shadowDistance = shadowDistance;
                }
            }
            
            // Modify shadow cascades
            // Note: shadowCascades is an int: 0=NoCascades, 1=TwoCascades, 2=FourCascades
            if (args.ContainsKey("shadowCascades"))
            {
                string cascadesStr = args["shadowCascades"].ToString();
                int currentCascades = QualitySettings.shadowCascades;
                int cascadeValue = cascadesStr.ToLower() switch
                {
                    "nocascades" => 0,
                    "twocascades" => 1,
                    "fourcascades" => 2,
                    _ => currentCascades
                };
                QualitySettings.shadowCascades = cascadeValue;
            }
            
            // Modify anti-aliasing
            if (args.ContainsKey("antiAliasing"))
            {
                if (int.TryParse(args["antiAliasing"].ToString(), out int antiAliasing))
                {
                    // Validate: 0, 2, 4, or 8
                    if (antiAliasing == 0 || antiAliasing == 2 || antiAliasing == 4 || antiAliasing == 8)
                    {
                        QualitySettings.antiAliasing = antiAliasing;
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Invalid antiAliasing value: {antiAliasing}. Must be 0, 2, 4, or 8");
                    }
                }
            }
            
            // Note: QualitySettings is a static class and changes are automatically persisted
            // to ProjectSettings/QualitySettings.asset. No need to call EditorUtility.SetDirty
            
            return ToolUtils.CreateSuccessResponse($"Updated quality settings for '{targetLevelName}'");
        }
    }
}
