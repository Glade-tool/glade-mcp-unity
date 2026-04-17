using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    public class SetLightPropertiesTool : ITool
    {
        public string Name => "set_light_properties";

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
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' has no Light component");
            }
            
            Undo.RecordObject(light, $"Set Light Properties: {gameObjectPath}");
            
            if (args.ContainsKey("color"))
            {
                light.color = ToolUtils.ParseColor(args["color"].ToString());
            }
            
            if (args.ContainsKey("intensity"))
            {
                float intensity = light.intensity;
                if (args["intensity"] is float f) intensity = f;
                else float.TryParse(args["intensity"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out intensity);
                light.intensity = intensity;
            }
            
            if (args.ContainsKey("range"))
            {
                float range = light.range;
                if (args["range"] is float f) range = f;
                else float.TryParse(args["range"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out range);
                light.range = range;
            }
            
            if (args.ContainsKey("spotAngle"))
            {
                float spotAngle = light.spotAngle;
                if (args["spotAngle"] is float f) spotAngle = f;
                else float.TryParse(args["spotAngle"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out spotAngle);
                light.spotAngle = spotAngle;
            }
            
            if (args.ContainsKey("shadows"))
            {
                string shadowStr = args["shadows"].ToString().ToLower();
                light.shadows = shadowStr switch
                {
                    "hard" => LightShadows.Hard,
                    "soft" => LightShadows.Soft,
                    _ => LightShadows.None
                };
            }
            
            if (args.ContainsKey("shadowStrength"))
            {
                float strength = light.shadowStrength;
                if (args["shadowStrength"] is float f) strength = f;
                else float.TryParse(args["shadowStrength"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out strength);
                light.shadowStrength = strength;
            }
            
            if (args.ContainsKey("colorTemperature"))
            {
                float temp = light.colorTemperature;
                if (args["colorTemperature"] is float f) temp = f;
                else float.TryParse(args["colorTemperature"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out temp);
                light.colorTemperature = temp;
            }
            
            if (args.ContainsKey("useColorTemperature"))
            {
                bool useTemp = false;
                if (args["useColorTemperature"] is bool b) useTemp = b;
                else bool.TryParse(args["useColorTemperature"].ToString(), out useTemp);
                light.useColorTemperature = useTemp;
            }
            
            return ToolUtils.CreateSuccessResponse($"Updated light properties on '{gameObjectPath}'");
        }
    }
}
