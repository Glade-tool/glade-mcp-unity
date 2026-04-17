using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Audio
{
    public class SetAudioSourcePropertiesTool : ITool
    {
        public string Name => "set_audio_source_properties";

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
            
            AudioSource source = obj.GetComponent<AudioSource>();
            if (source == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' has no AudioSource component");
            }
            
            Undo.RecordObject(source, $"Set Audio Source Properties: {gameObjectPath}");
            
            if (args.ContainsKey("clipPath"))
            {
                string clipPath = args["clipPath"].ToString();
                if (!clipPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    clipPath = "Assets/" + clipPath;
                    
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                    source.clip = clip;
            }
            
            if (args.ContainsKey("playOnAwake"))
            {
                bool playOnAwake = source.playOnAwake;
                if (args["playOnAwake"] is bool b) playOnAwake = b;
                else bool.TryParse(args["playOnAwake"].ToString(), out playOnAwake);
                source.playOnAwake = playOnAwake;
            }
            
            if (args.ContainsKey("loop"))
            {
                bool loop = source.loop;
                if (args["loop"] is bool b) loop = b;
                else bool.TryParse(args["loop"].ToString(), out loop);
                source.loop = loop;
            }
            
            if (args.ContainsKey("volume"))
            {
                float volume = source.volume;
                if (args["volume"] is float f) volume = f;
                else float.TryParse(args["volume"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out volume);
                source.volume = volume;
            }
            
            if (args.ContainsKey("pitch"))
            {
                float pitch = source.pitch;
                if (args["pitch"] is float f) pitch = f;
                else float.TryParse(args["pitch"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pitch);
                source.pitch = pitch;
            }
            
            if (args.ContainsKey("spatialBlend"))
            {
                float blend = source.spatialBlend;
                if (args["spatialBlend"] is float f) blend = f;
                else float.TryParse(args["spatialBlend"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out blend);
                source.spatialBlend = blend;
            }
            
            if (args.ContainsKey("minDistance"))
            {
                float minDist = source.minDistance;
                if (args["minDistance"] is float f) minDist = f;
                else float.TryParse(args["minDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out minDist);
                source.minDistance = minDist;
            }
            
            if (args.ContainsKey("maxDistance"))
            {
                float maxDist = source.maxDistance;
                if (args["maxDistance"] is float f) maxDist = f;
                else float.TryParse(args["maxDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out maxDist);
                source.maxDistance = maxDist;
            }
            
            if (args.ContainsKey("mute"))
            {
                bool mute = source.mute;
                if (args["mute"] is bool b) mute = b;
                else bool.TryParse(args["mute"].ToString(), out mute);
                source.mute = mute;
            }
            
            return ToolUtils.CreateSuccessResponse($"Updated audio source properties on '{gameObjectPath}'");
        }
    }
}
