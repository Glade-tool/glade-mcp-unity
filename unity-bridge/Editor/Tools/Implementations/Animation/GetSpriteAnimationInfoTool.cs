using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetSpriteAnimationInfoTool : ITool
    {
        public string Name => "get_sprite_animation_info";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Get object reference curve bindings (for sprites)
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            
            List<Dictionary<string, object>> sprites = new List<Dictionary<string, object>>();
            
            foreach (var binding in bindings)
            {
                // Filter for sprite curves
                if (binding.propertyName == "m_Sprite" && binding.type == typeof(SpriteRenderer))
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                    
                    if (keyframes != null)
                    {
                        foreach (var keyframe in keyframes)
                        {
                            if (keyframe.value is Sprite sprite)
                            {
                                string spritePath = AssetDatabase.GetAssetPath(sprite);
                                
                                sprites.Add(new Dictionary<string, object>
                                {
                                    { "time", keyframe.time },
                                    { "spriteName", sprite.name },
                                    { "spritePath", spritePath }
                                });
                            }
                        }
                    }
                }
            }
            
            // Sort by time
            sprites.Sort((a, b) => 
            {
                float timeA = (float)a["time"];
                float timeB = (float)b["time"];
                return timeA.CompareTo(timeB);
            });
            
            float frameRate = clip.frameRate;
            float duration = clip.length;
            
            var extras = new Dictionary<string, object>
            {
                { "spriteCount", sprites.Count },
                { "sprites", sprites },
                { "frameRate", frameRate },
                { "duration", duration }
            };
            
            return ToolUtils.CreateSuccessResponse($"Found {sprites.Count} sprite keyframe(s) in AnimationClip", extras);
        }
    }
}
