using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.VFX
{
    public class CreateParticleSystemTool : ITool
    {
        public string Name => "create_particle_system";

        public string Execute(Dictionary<string, object> args)
        {
            string name = args.ContainsKey("name") ? args["name"].ToString() : "Particle System";
            
            UnityEngine.GameObject particleObj = new UnityEngine.GameObject(name);
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            
            // Set position
            if (args.ContainsKey("position"))
            {
                particleObj.transform.position = ToolUtils.ParseVector3(args["position"].ToString());
            }
            
            // Set parent
            if (args.ContainsKey("parentPath"))
            {
                string parentPath = args["parentPath"].ToString();
                UnityEngine.GameObject parent = ToolUtils.FindGameObjectByPath(parentPath);
                if (parent != null)
                    particleObj.transform.SetParent(parent.transform);
            }
            
            // Configure main module
            var main = ps.main;
            
            if (args.ContainsKey("duration"))
            {
                float duration = 5f;
                if (args["duration"] is float f) duration = f;
                else float.TryParse(args["duration"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out duration);
                main.duration = duration;
            }
            
            if (args.ContainsKey("looping"))
            {
                bool looping = true;
                if (args["looping"] is bool b) looping = b;
                else bool.TryParse(args["looping"].ToString(), out looping);
                main.loop = looping;
            }
            
            if (args.ContainsKey("startLifetime"))
            {
                float lifetime = 5f;
                if (args["startLifetime"] is float f) lifetime = f;
                else float.TryParse(args["startLifetime"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lifetime);
                main.startLifetime = lifetime;
            }
            
            if (args.ContainsKey("startSpeed"))
            {
                float speed = 5f;
                if (args["startSpeed"] is float f) speed = f;
                else float.TryParse(args["startSpeed"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out speed);
                main.startSpeed = speed;
            }
            
            if (args.ContainsKey("startSize"))
            {
                float size = 1f;
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
                int maxParticles = 1000;
                if (args["maxParticles"] is int i) maxParticles = i;
                else if (args["maxParticles"] is float f) maxParticles = (int)f;
                else int.TryParse(args["maxParticles"].ToString(), out maxParticles);
                main.maxParticles = maxParticles;
            }
            
            if (args.ContainsKey("playOnAwake"))
            {
                bool playOnAwake = true;
                if (args["playOnAwake"] is bool b) playOnAwake = b;
                else bool.TryParse(args["playOnAwake"].ToString(), out playOnAwake);
                main.playOnAwake = playOnAwake;
            }
            
            Undo.RegisterCreatedObjectUndo(particleObj, $"Create Particle System: {name}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(particleObj) }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Particle System '{name}'", extras);
        }
    }
}
