using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Audio
{
    public class AssignAudioClipTool : ITool
    {
        public string Name => "assign_audio_clip";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            if (string.IsNullOrEmpty(clipPath))
            {
                return ToolUtils.CreateErrorResponse("clipPath is required");
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
            
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
                
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                return ToolUtils.CreateErrorResponse($"AudioClip not found at '{clipPath}'");
            }
            
            Undo.RecordObject(source, $"Assign Audio Clip: {gameObjectPath}");
            source.clip = clip;
            
            var extras = new Dictionary<string, object>
            {
                { "clipPath", clipPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Assigned audio clip '{clip.name}' to '{gameObjectPath}'", extras);
        }
    }
}
