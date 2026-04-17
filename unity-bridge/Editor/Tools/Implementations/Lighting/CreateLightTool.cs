using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    public class CreateLightTool : ITool
    {
        public string Name => "create_light";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("lightType"))
            {
                return ToolUtils.CreateErrorResponse("lightType is required");
            }
            
            string lightTypeStr = args["lightType"].ToString();
            LightType lightType = lightTypeStr.ToLower() switch
            {
                "directional" => LightType.Directional,
                "point" => LightType.Point,
                "spot" => LightType.Spot,
                "area" => LightType.Rectangle,
                _ => LightType.Directional
            };
            
            string name = args.ContainsKey("name") ? args["name"].ToString() : $"{lightTypeStr} Light";
            
            UnityEngine.GameObject lightObj = new UnityEngine.GameObject(name);
            Light light = lightObj.AddComponent<Light>();
            light.type = lightType;
            
            // Set position
            if (args.ContainsKey("position"))
            {
                lightObj.transform.position = ToolUtils.ParseVector3(args["position"].ToString());
            }
            else if (lightType == LightType.Directional)
            {
                lightObj.transform.position = new Vector3(0, 3, 0);
            }
            
            // Set rotation
            if (args.ContainsKey("rotation"))
            {
                lightObj.transform.rotation = Quaternion.Euler(ToolUtils.ParseVector3(args["rotation"].ToString()));
            }
            else if (lightType == LightType.Directional)
            {
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
            
            // Set color
            if (args.ContainsKey("color"))
            {
                light.color = ToolUtils.ParseColor(args["color"].ToString());
            }
            
            // Set intensity
            if (args.ContainsKey("intensity"))
            {
                float intensity = light.intensity;
                if (args["intensity"] is float f) intensity = f;
                else float.TryParse(args["intensity"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out intensity);
                light.intensity = intensity;
            }
            
            // Set range (Point/Spot only)
            if (args.ContainsKey("range") && (lightType == LightType.Point || lightType == LightType.Spot))
            {
                float range = 10f;
                if (args["range"] is float f) range = f;
                else float.TryParse(args["range"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out range);
                light.range = range;
            }
            
            // Set spot angle (Spot only)
            if (args.ContainsKey("spotAngle") && lightType == LightType.Spot)
            {
                float spotAngle = 30f;
                if (args["spotAngle"] is float f) spotAngle = f;
                else float.TryParse(args["spotAngle"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out spotAngle);
                light.spotAngle = spotAngle;
            }
            
            Undo.RegisterCreatedObjectUndo(lightObj, $"Create {lightTypeStr} Light");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(lightObj) },
                { "lightType", lightTypeStr }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created {lightTypeStr} Light '{name}'", extras);
        }
    }
}
