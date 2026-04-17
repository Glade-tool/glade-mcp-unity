using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    /// <summary>
    /// Gets detailed information about a Light component including type, color, intensity, range, shadows, and other properties.
    /// </summary>
    public class GetLightInfoTool : ITool
    {
        public string Name => "get_light_info";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            Light light = obj.GetComponent<Light>();
            if (light == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a Light component");
            }
            
            // READ ONLY - No Undo needed
            string lightType = light.type.ToString();
            string shadows = light.shadows.ToString();
            
            var info = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["lightType"] = lightType,
                ["color"] = $"{light.color.r},{light.color.g},{light.color.b},{light.color.a}",
                ["intensity"] = light.intensity,
                ["range"] = light.range,
                ["spotAngle"] = light.spotAngle,
                ["shadows"] = shadows,
                ["shadowStrength"] = light.shadowStrength,
                ["shadowBias"] = light.shadowBias,
                ["shadowNormalBias"] = light.shadowNormalBias,
                ["shadowNearPlane"] = light.shadowNearPlane,
                ["colorTemperature"] = light.colorTemperature,
                ["useColorTemperature"] = light.useColorTemperature,
                ["bounceIntensity"] = light.bounceIntensity,
                ["cullingMask"] = light.cullingMask,
                ["enabled"] = light.enabled
            };
            
            // Add type-specific properties
            if (light.type == LightType.Spot)
            {
                info["innerSpotAngle"] = light.innerSpotAngle;
            }
            
            string message = $"Retrieved light information for '{gameObjectPath}': {lightType} light, intensity {light.intensity}, range {light.range}";
            
            return ToolUtils.CreateSuccessResponse(message, info);
        }
    }
}
