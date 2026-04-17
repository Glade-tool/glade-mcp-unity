using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetSpriteAnimationCurvesTool : ITool
    {
        public string Name => "set_sprite_animation_curves";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            if (!args.ContainsKey("spriteKeyframes"))
                return ToolUtils.CreateErrorResponse("spriteKeyframes is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            string path = "";
            if (args.ContainsKey("path"))
                path = args["path"].ToString();
            
            bool clearExisting = true;
            if (args.ContainsKey("clearExisting"))
            {
                if (args["clearExisting"] is bool b) clearExisting = b;
                else bool.TryParse(args["clearExisting"].ToString(), out clearExisting);
            }
            
            // Clear existing sprite curves if requested
            if (clearExisting)
            {
                EditorCurveBinding[] existingBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                foreach (var existingBinding in existingBindings)
                {
                    if (existingBinding.propertyName == "m_Sprite" && existingBinding.type == typeof(SpriteRenderer) && existingBinding.path == path)
                    {
                        AnimationUtility.SetObjectReferenceCurve(clip, existingBinding, null);
                    }
                }
            }
            
            // Parse sprite keyframes
            var spriteKeyframesObj = args["spriteKeyframes"];
            List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
            
            if (spriteKeyframesObj is List<object> keyframesList)
            {
                foreach (var kfObj in keyframesList)
                {
                    if (kfObj is Dictionary<string, object> kf)
                    {
                        float time = 0f;
                        string spritePath = "";
                        
                        if (kf.ContainsKey("time"))
                        {
                            if (kf["time"] is float tf) time = tf;
                            else float.TryParse(kf["time"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out time);
                        }
                        
                        if (kf.ContainsKey("spritePath"))
                        {
                            spritePath = kf["spritePath"].ToString();
                        }
                        
                        if (string.IsNullOrEmpty(spritePath))
                            continue;
                        
                        // Ensure sprite path starts with Assets/
                        if (!spritePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                            spritePath = "Assets/" + spritePath;
                        
                        // Load sprite
                        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        if (sprite == null)
                        {
                            // Try loading as Object and checking if it's a Sprite
                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(spritePath);
                            if (obj is Sprite s)
                            {
                                sprite = s;
                            }
                            else if (obj is Texture2D)
                            {
                                // Automatically convert Texture2D to Sprite import type
                                var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                                {
                                    importer.textureType = TextureImporterType.Sprite;
                                    if (importer.spriteImportMode == SpriteImportMode.None)
                                    {
                                        importer.spriteImportMode = SpriteImportMode.Single;
                                    }
                                    importer.SaveAndReimport();
                                    AssetDatabase.Refresh();
                                    
                                    // Try loading again as Sprite
                                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                                    if (sprite == null)
                                    {
                                        // Try loading all sub-assets (for Multiple sprite sheets)
                                        UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
                                        foreach (var asset in allAssets)
                                        {
                                            if (asset is Sprite sp)
                                            {
                                                sprite = sp;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (sprite == null)
                            continue;
                        
                        keyframes.Add(new ObjectReferenceKeyframe
                        {
                            time = time,
                            value = sprite
                        });
                    }
                }
            }
            
            if (keyframes.Count == 0)
                return ToolUtils.CreateErrorResponse("No valid sprite keyframes provided");
            
            // Record for undo
            Undo.RecordObject(clip, $"Set Sprite Animation Curves: {clipPath}");
            
            // Set sprite curve
            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = path,
                propertyName = "m_Sprite",
                type = typeof(SpriteRenderer)
            };
            
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
            
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "keyframesAdded", keyframes.Count }
            };
            
            return ToolUtils.CreateSuccessResponse($"Set {keyframes.Count} sprite keyframe(s) on AnimationClip", extras);
        }
    }
}
