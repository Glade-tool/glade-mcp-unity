using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Audio
{
    /// <summary>
    /// Gets detailed information about an AudioSource component including clip, volume, pitch, spatial settings, and other properties.
    /// </summary>
    public class GetAudioSourcePropertiesTool : ITool
    {
        public string Name => "get_audio_source_properties";

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
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have an AudioSource component");
            }
            
            // READ ONLY - No Undo needed
            var properties = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["clipPath"] = source.clip != null ? AssetDatabase.GetAssetPath(source.clip) : null,
                ["playOnAwake"] = source.playOnAwake,
                ["loop"] = source.loop,
                ["volume"] = source.volume,
                ["pitch"] = source.pitch,
                ["spatialBlend"] = source.spatialBlend,
                ["minDistance"] = source.minDistance,
                ["maxDistance"] = source.maxDistance,
                ["mute"] = source.mute,
                ["enabled"] = source.enabled
            };
            
            string message = $"Retrieved audio source properties for '{gameObjectPath}': Volume={source.volume}, Pitch={source.pitch}, Loop={source.loop}";
            
            return ToolUtils.CreateSuccessResponse(message, properties);
        }
    }
}
