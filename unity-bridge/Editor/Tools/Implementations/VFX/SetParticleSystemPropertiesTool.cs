using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.VFX
{
    public class SetParticleSystemPropertiesTool : ITool
    {
        public string Name => "set_particle_system_properties";

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
            
            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' has no ParticleSystem component");
            }
            
            Undo.RecordObject(ps, $"Set Particle System Properties: {gameObjectPath}");
            
            var main = ps.main;
            
            if (args.ContainsKey("duration"))
            {
                float duration = main.duration;
                if (args["duration"] is float f) duration = f;
                else float.TryParse(args["duration"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out duration);
                main.duration = duration;
            }
            
            if (args.ContainsKey("looping"))
            {
                bool looping = main.loop;
                if (args["looping"] is bool b) looping = b;
                else bool.TryParse(args["looping"].ToString(), out looping);
                main.loop = looping;
            }
            
            if (args.ContainsKey("startLifetime"))
            {
                float lifetime = main.startLifetime.constant;
                if (args["startLifetime"] is float f) lifetime = f;
                else float.TryParse(args["startLifetime"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lifetime);
                main.startLifetime = lifetime;
            }
            
            if (args.ContainsKey("startSpeed"))
            {
                float speed = main.startSpeed.constant;
                if (args["startSpeed"] is float f) speed = f;
                else float.TryParse(args["startSpeed"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out speed);
                main.startSpeed = speed;
            }
            
            if (args.ContainsKey("startSize"))
            {
                float size = main.startSize.constant;
                if (args["startSize"] is float f) size = f;
                else float.TryParse(args["startSize"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out size);
                main.startSize = size;
            }
            
            if (args.ContainsKey("startColor"))
            {
                main.startColor = ToolUtils.ParseColor(args["startColor"].ToString());
            }
            
            if (args.ContainsKey("maxParticles"))
            {
                int maxParticles = main.maxParticles;
                if (args["maxParticles"] is int i) maxParticles = i;
                else if (args["maxParticles"] is float f) maxParticles = (int)f;
                else int.TryParse(args["maxParticles"].ToString(), out maxParticles);
                main.maxParticles = maxParticles;
            }
            
            if (args.ContainsKey("gravityModifier"))
            {
                float gravity = main.gravityModifier.constant;
                if (args["gravityModifier"] is float f) gravity = f;
                else float.TryParse(args["gravityModifier"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out gravity);
                main.gravityModifier = gravity;
            }
            
            if (args.ContainsKey("simulationSpace"))
            {
                string spaceStr = args["simulationSpace"].ToString().ToLower();
                main.simulationSpace = spaceStr == "world" ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            }
            
            if (args.ContainsKey("playOnAwake"))
            {
                bool playOnAwake = main.playOnAwake;
                if (args["playOnAwake"] is bool b) playOnAwake = b;
                else bool.TryParse(args["playOnAwake"].ToString(), out playOnAwake);
                main.playOnAwake = playOnAwake;
            }
            
            // Emission rate
            if (args.ContainsKey("emissionRateOverTime"))
            {
                var emission = ps.emission;
                float rate = emission.rateOverTime.constant;
                if (args["emissionRateOverTime"] is float f) rate = f;
                else float.TryParse(args["emissionRateOverTime"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rate);
                emission.rateOverTime = rate;
            }
            
            // Shape Module
            var shape = ps.shape;
            if (args.ContainsKey("enableShape"))
            {
                bool enable = false;
                if (args["enableShape"] is bool b) enable = b;
                else bool.TryParse(args["enableShape"].ToString(), out enable);
                shape.enabled = enable;
            }

            if (args.ContainsKey("shapeType"))
            {
                string typeStr = args["shapeType"].ToString();
                if (System.Enum.TryParse<ParticleSystemShapeType>(typeStr, true, out var shapeType))
                    shape.shapeType = shapeType;
            }

            if (args.ContainsKey("shapeRadius"))
            {
                float radius = shape.radius;
                if (args["shapeRadius"] is float f) radius = f;
                else float.TryParse(args["shapeRadius"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out radius);
                shape.radius = radius;
            }

            if (args.ContainsKey("shapeAngle"))
            {
                float angle = shape.angle;
                if (args["shapeAngle"] is float f) angle = f;
                else float.TryParse(args["shapeAngle"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out angle);
                shape.angle = angle;
            }

            // Velocity Over Lifetime Module
            var velocityOverLifetime = ps.velocityOverLifetime;
            if (args.ContainsKey("enableVelocityOverLifetime"))
            {
                bool enable = false;
                if (args["enableVelocityOverLifetime"] is bool b) enable = b;
                else bool.TryParse(args["enableVelocityOverLifetime"].ToString(), out enable);
                velocityOverLifetime.enabled = enable;
            }

            if (args.ContainsKey("velocityOverLifetimeX"))
            {
                float x = velocityOverLifetime.x.constant;
                if (args["velocityOverLifetimeX"] is float f) x = f;
                else float.TryParse(args["velocityOverLifetimeX"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x);
                velocityOverLifetime.x = x;
            }

            if (args.ContainsKey("velocityOverLifetimeY"))
            {
                float y = velocityOverLifetime.y.constant;
                if (args["velocityOverLifetimeY"] is float f) y = f;
                else float.TryParse(args["velocityOverLifetimeY"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y);
                velocityOverLifetime.y = y;
            }

            if (args.ContainsKey("velocityOverLifetimeZ"))
            {
                float z = velocityOverLifetime.z.constant;
                if (args["velocityOverLifetimeZ"] is float f) z = f;
                else float.TryParse(args["velocityOverLifetimeZ"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z);
                velocityOverLifetime.z = z;
            }

            // Color Over Lifetime Module
            var colorOverLifetime = ps.colorOverLifetime;
            if (args.ContainsKey("enableColorOverLifetime"))
            {
                bool enable = false;
                if (args["enableColorOverLifetime"] is bool b) enable = b;
                else bool.TryParse(args["enableColorOverLifetime"].ToString(), out enable);
                colorOverLifetime.enabled = enable;
            }

            if (args.ContainsKey("colorOverLifetime"))
            {
                // Parse color string (e.g., "1,0,0,1" or "red")
                Color color = ToolUtils.ParseColor(args["colorOverLifetime"].ToString());
                // Note: This sets a constant color. For gradients, would need Gradient type handling
                colorOverLifetime.color = color;
            }

            // Size Over Lifetime Module
            var sizeOverLifetime = ps.sizeOverLifetime;
            if (args.ContainsKey("enableSizeOverLifetime"))
            {
                bool enable = false;
                if (args["enableSizeOverLifetime"] is bool b) enable = b;
                else bool.TryParse(args["enableSizeOverLifetime"].ToString(), out enable);
                sizeOverLifetime.enabled = enable;
            }

            if (args.ContainsKey("sizeOverLifetimeMultiplier"))
            {
                float multiplier = sizeOverLifetime.size.constant;
                if (args["sizeOverLifetimeMultiplier"] is float f) multiplier = f;
                else float.TryParse(args["sizeOverLifetimeMultiplier"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out multiplier);
                sizeOverLifetime.size = multiplier;
            }

            // Rotation Over Lifetime Module
            var rotationOverLifetime = ps.rotationOverLifetime;
            if (args.ContainsKey("enableRotationOverLifetime"))
            {
                bool enable = false;
                if (args["enableRotationOverLifetime"] is bool b) enable = b;
                else bool.TryParse(args["enableRotationOverLifetime"].ToString(), out enable);
                rotationOverLifetime.enabled = enable;
            }

            if (args.ContainsKey("rotationOverLifetimeZ"))
            {
                float z = rotationOverLifetime.z.constant;
                if (args["rotationOverLifetimeZ"] is float f) z = f;
                else float.TryParse(args["rotationOverLifetimeZ"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z);
                rotationOverLifetime.z = z;
            }

            // Texture Sheet Animation Module
            var textureSheetAnimation = ps.textureSheetAnimation;
            if (args.ContainsKey("enableTextureSheetAnimation"))
            {
                bool enable = false;
                if (args["enableTextureSheetAnimation"] is bool b) enable = b;
                else bool.TryParse(args["enableTextureSheetAnimation"].ToString(), out enable);
                textureSheetAnimation.enabled = enable;
            }

            if (args.ContainsKey("textureSheetAnimationTilesX"))
            {
                int tilesX = textureSheetAnimation.numTilesX;
                if (args["textureSheetAnimationTilesX"] is int i) tilesX = i;
                else if (args["textureSheetAnimationTilesX"] is float f) tilesX = (int)f;
                else int.TryParse(args["textureSheetAnimationTilesX"].ToString(), out tilesX);
                textureSheetAnimation.numTilesX = tilesX;
            }

            if (args.ContainsKey("textureSheetAnimationTilesY"))
            {
                int tilesY = textureSheetAnimation.numTilesY;
                if (args["textureSheetAnimationTilesY"] is int i) tilesY = i;
                else if (args["textureSheetAnimationTilesY"] is float f) tilesY = (int)f;
                else int.TryParse(args["textureSheetAnimationTilesY"].ToString(), out tilesY);
                textureSheetAnimation.numTilesY = tilesY;
            }
            
            return ToolUtils.CreateSuccessResponse($"Updated particle system properties on '{gameObjectPath}'");
        }
    }
}
