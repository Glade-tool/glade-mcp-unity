using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Audio
{
    public class CreateAudioSourceTool : ITool
    {
        public string Name => "create_audio_source";

        public string Execute(Dictionary<string, object> args)
        {
            string name = args.ContainsKey("name") ? args["name"].ToString() : "Audio Source";
            
            UnityEngine.GameObject audioObj = new UnityEngine.GameObject(name);
            AudioSource source = audioObj.AddComponent<AudioSource>();
            
            // Set position
            if (args.ContainsKey("position"))
            {
                audioObj.transform.position = ToolUtils.ParseVector3(args["position"].ToString());
            }
            
            // Set parent
            if (args.ContainsKey("parentPath"))
            {
                string parentPath = args["parentPath"].ToString();
                UnityEngine.GameObject parent = ToolUtils.FindGameObjectByPath(parentPath);
                if (parent != null)
                    audioObj.transform.SetParent(parent.transform);
            }
            
            // Assign clip if provided
            if (args.ContainsKey("clipPath"))
            {
                string clipPath = args["clipPath"].ToString();
                if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    clipPath = "Assets/" + clipPath;
                    
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                    source.clip = clip;
            }
            
            // Configure audio source
            if (args.ContainsKey("playOnAwake"))
            {
                bool playOnAwake = false;
                if (args["playOnAwake"] is bool b) playOnAwake = b;
                else bool.TryParse(args["playOnAwake"].ToString(), out playOnAwake);
                source.playOnAwake = playOnAwake;
            }
            else
            {
                source.playOnAwake = false;
            }
            
            if (args.ContainsKey("loop"))
            {
                bool loop = false;
                if (args["loop"] is bool b) loop = b;
                else bool.TryParse(args["loop"].ToString(), out loop);
                source.loop = loop;
            }
            
            if (args.ContainsKey("volume"))
            {
                float volume = 1f;
                if (args["volume"] is float f) volume = f;
                else float.TryParse(args["volume"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out volume);
                source.volume = volume;
            }
            
            if (args.ContainsKey("pitch"))
            {
                float pitch = 1f;
                if (args["pitch"] is float f) pitch = f;
                else float.TryParse(args["pitch"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pitch);
                source.pitch = pitch;
            }
            
            if (args.ContainsKey("spatialBlend"))
            {
                float blend = 0f;
                if (args["spatialBlend"] is float f) blend = f;
                else float.TryParse(args["spatialBlend"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out blend);
                source.spatialBlend = blend;
            }
            
            if (args.ContainsKey("minDistance"))
            {
                float minDist = 1f;
                if (args["minDistance"] is float f) minDist = f;
                else float.TryParse(args["minDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out minDist);
                source.minDistance = minDist;
            }
            
            if (args.ContainsKey("maxDistance"))
            {
                float maxDist = 500f;
                if (args["maxDistance"] is float f) maxDist = f;
                else float.TryParse(args["maxDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out maxDist);
                source.maxDistance = maxDist;
            }
            
            Undo.RegisterCreatedObjectUndo(audioObj, $"Create Audio Source: {name}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(audioObj) }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Audio Source '{name}'", extras);
        }
    }
}
