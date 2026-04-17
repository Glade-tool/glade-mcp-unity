using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.VFX
{
    /// <summary>
    /// Gets detailed information about a ParticleSystem component including all main properties and module settings.
    /// </summary>
    public class GetParticleSystemPropertiesTool : ITool
    {
        public string Name => "get_particle_system_properties";

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
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a ParticleSystem component");
            }
            
            // READ ONLY - No Undo needed
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var velocityOverLifetime = ps.velocityOverLifetime;
            var colorOverLifetime = ps.colorOverLifetime;
            var sizeOverLifetime = ps.sizeOverLifetime;
            var rotationOverLifetime = ps.rotationOverLifetime;
            var textureSheetAnimation = ps.textureSheetAnimation;
            
            var properties = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                
                // Main Module
                ["duration"] = main.duration,
                ["looping"] = main.loop,
                ["startLifetime"] = main.startLifetime.constant,
                ["startSpeed"] = main.startSpeed.constant,
                ["startSize"] = main.startSize.constant,
                ["startColor"] = $"{main.startColor.color.r},{main.startColor.color.g},{main.startColor.color.b},{main.startColor.color.a}",
                ["maxParticles"] = main.maxParticles,
                ["gravityModifier"] = main.gravityModifier.constant,
                ["simulationSpace"] = main.simulationSpace.ToString(),
                ["playOnAwake"] = main.playOnAwake,
                
                // Emission Module
                ["emissionRateOverTime"] = emission.rateOverTime.constant,
                
                // Shape Module
                ["enableShape"] = shape.enabled,
                ["shapeType"] = shape.shapeType.ToString(),
                ["shapeRadius"] = shape.radius,
                ["shapeAngle"] = shape.angle,
                
                // Velocity Over Lifetime Module
                ["enableVelocityOverLifetime"] = velocityOverLifetime.enabled,
                ["velocityOverLifetimeX"] = velocityOverLifetime.x.constant,
                ["velocityOverLifetimeY"] = velocityOverLifetime.y.constant,
                ["velocityOverLifetimeZ"] = velocityOverLifetime.z.constant,
                
                // Color Over Lifetime Module
                ["enableColorOverLifetime"] = colorOverLifetime.enabled,
                ["colorOverLifetime"] = $"{colorOverLifetime.color.color.r},{colorOverLifetime.color.color.g},{colorOverLifetime.color.color.b},{colorOverLifetime.color.color.a}",
                
                // Size Over Lifetime Module
                ["enableSizeOverLifetime"] = sizeOverLifetime.enabled,
                ["sizeOverLifetimeMultiplier"] = sizeOverLifetime.size.constant,
                
                // Rotation Over Lifetime Module
                ["enableRotationOverLifetime"] = rotationOverLifetime.enabled,
                ["rotationOverLifetimeZ"] = rotationOverLifetime.z.constant,
                
                // Texture Sheet Animation Module
                ["enableTextureSheetAnimation"] = textureSheetAnimation.enabled,
                ["textureSheetAnimationTilesX"] = textureSheetAnimation.numTilesX,
                ["textureSheetAnimationTilesY"] = textureSheetAnimation.numTilesY
            };
            
            string message = $"Retrieved particle system properties for '{gameObjectPath}': Duration={main.duration}, Looping={main.loop}, MaxParticles={main.maxParticles}";
            
            return ToolUtils.CreateSuccessResponse(message, properties);
        }
    }
}
