using System;
using System.Collections.Generic;
using System.Linq;
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

            // Re-hydrate JSON-array strings (ParseJsonToDict doesn't deep-parse arrays).
            var spriteKeyframesObj = args["spriteKeyframes"];
            if (spriteKeyframesObj is string skJson && ToolUtils.TryParseJsonArrayToList(skJson, out var parsedSpriteKeyframes))
                spriteKeyframesObj = parsedSpriteKeyframes;

            var keyframes = new List<ObjectReferenceKeyframe>();
            var skipped = new List<Dictionary<string, object>>();

            if (spriteKeyframesObj is List<object> keyframesList)
            {
                int index = 0;
                foreach (var kfObj in keyframesList)
                {
                    int currentIndex = index++;

                    if (!(kfObj is Dictionary<string, object> kf))
                    {
                        skipped.Add(SkipEntry(currentIndex, "", "keyframe entry is not an object"));
                        continue;
                    }

                    float time = 0f;
                    string spritePath = "";

                    if (kf.ContainsKey("time"))
                    {
                        if (kf["time"] is float tf) time = tf;
                        else if (kf["time"] is double td) time = (float)td;
                        else if (kf["time"] is int ti) time = ti;
                        else float.TryParse(kf["time"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out time);
                    }

                    if (kf.ContainsKey("spritePath"))
                        spritePath = kf["spritePath"].ToString();

                    if (string.IsNullOrEmpty(spritePath))
                    {
                        skipped.Add(SkipEntry(currentIndex, spritePath, "spritePath is required"));
                        continue;
                    }

                    if (!spritePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        spritePath = "Assets/" + spritePath;

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(spritePath);
                        if (obj is Sprite s)
                        {
                            sprite = s;
                        }
                        else if (obj is Texture2D)
                        {
                            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                            if (importer != null && importer.textureType != TextureImporterType.Sprite)
                            {
                                importer.textureType = TextureImporterType.Sprite;
                                if (importer.spriteImportMode == SpriteImportMode.None)
                                    importer.spriteImportMode = SpriteImportMode.Single;
                                importer.SaveAndReimport();
                                AssetDatabase.Refresh();

                                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                                if (sprite == null)
                                {
                                    UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
                                    foreach (var asset in allAssets)
                                    {
                                        if (asset is Sprite sp) { sprite = sp; break; }
                                    }
                                }
                            }
                        }
                    }

                    if (sprite == null)
                    {
                        var skipEntry = SkipEntry(currentIndex, spritePath, "sprite asset not found");
                        var similar = FindSimilarSpritePaths(spritePath);
                        if (similar.Count > 0)
                            skipEntry["similarSprites"] = similar;
                        skipped.Add(skipEntry);
                        continue;
                    }

                    keyframes.Add(new ObjectReferenceKeyframe { time = time, value = sprite });
                }
            }
            else
            {
                return ToolUtils.CreateErrorResponse("spriteKeyframes must be an array");
            }

            if (keyframes.Count == 0)
            {
                var extrasErr = new Dictionary<string, object>
                {
                    { "skippedCount", skipped.Count }
                };
                if (skipped.Count > 0)
                    extrasErr["skippedKeyframes"] = skipped;
                return ToolUtils.CreateErrorResponse(
                    skipped.Count > 0
                        ? $"No valid sprite keyframes parsed; {skipped.Count} skipped — see skippedKeyframes."
                        : "No valid sprite keyframes provided",
                    extrasErr);
            }

            Undo.RecordObject(clip, $"Set Sprite Animation Curves: {clipPath}");

            var binding = new EditorCurveBinding
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
                { "keyframesAdded", keyframes.Count },
                { "skippedCount", skipped.Count }
            };
            if (skipped.Count > 0)
                extras["skippedKeyframes"] = skipped;

            string msg = skipped.Count == 0
                ? $"Set {keyframes.Count} sprite keyframe(s) on AnimationClip"
                : $"Set {keyframes.Count} sprite keyframe(s); {skipped.Count} skipped — see skippedKeyframes";
            return ToolUtils.CreateSuccessResponse(msg, extras);
        }

        private static Dictionary<string, object> SkipEntry(int index, string spritePath, string reason)
        {
            return new Dictionary<string, object>
            {
                { "index", index },
                { "spritePath", spritePath ?? "" },
                { "reason", reason }
            };
        }

        private static List<string> FindSimilarSpritePaths(string requestedPath)
        {
            var similar = new List<string>();
            string fileName = System.IO.Path.GetFileName(requestedPath ?? "");
            string searchName = !string.IsNullOrEmpty(fileName) ? System.IO.Path.GetFileNameWithoutExtension(fileName) : "";
            if (string.IsNullOrEmpty(searchName)) return similar;

            string[] guids = AssetDatabase.FindAssets($"{searchName} t:Sprite");
            foreach (var guid in guids.Take(5))
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(foundPath) && !foundPath.StartsWith("Packages/"))
                    similar.Add(foundPath);
            }

            if (similar.Count == 0)
            {
                guids = AssetDatabase.FindAssets($"{searchName} t:Texture2D");
                foreach (var guid in guids.Take(5))
                {
                    string foundPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(foundPath) && !foundPath.StartsWith("Packages/"))
                        similar.Add(foundPath);
                }
            }

            return similar;
        }
    }
}
