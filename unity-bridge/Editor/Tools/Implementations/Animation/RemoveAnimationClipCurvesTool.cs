using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveAnimationClipCurvesTool : ITool
    {
        public string Name => "remove_animation_clip_curves";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";

            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");

            if (!args.ContainsKey("curves"))
                return ToolUtils.CreateErrorResponse("curves is required");

            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");

            Undo.RecordObject(clip, $"Remove AnimationClip Curves: {clipPath}");

            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            // Re-hydrate JSON-array strings (ParseJsonToDict doesn't deep-parse arrays).
            var curvesObj = args["curves"];
            if (curvesObj is string curvesJson && ToolUtils.TryParseJsonArrayToList(curvesJson, out var parsedCurves))
                curvesObj = parsedCurves;
            if (!(curvesObj is List<object> curvesList))
                return ToolUtils.CreateErrorResponse("curves must be an array");

            int curvesRemoved = 0;
            var skipped = new List<Dictionary<string, object>>();

            foreach (var curveObj in curvesList)
            {
                if (!(curveObj is Dictionary<string, object> curve))
                {
                    skipped.Add(SkipEntry("", "", "", "curve entry is not an object"));
                    continue;
                }

                string path = curve.ContainsKey("path") ? curve["path"].ToString() : "";
                string propertyName = curve.ContainsKey("propertyName") ? curve["propertyName"].ToString() : "";
                string typeStr = curve.ContainsKey("type") ? curve["type"].ToString() : "";

                if (string.IsNullOrEmpty(propertyName))
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "propertyName is required"));
                    continue;
                }
                if (string.IsNullOrEmpty(typeStr))
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "type is required (e.g. 'Transform', 'SpriteRenderer')"));
                    continue;
                }

                System.Type type = ToolUtils.FindAnimationBindingType(typeStr);
                if (type == null)
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr,
                        $"could not resolve type '{typeStr}'"));
                    continue;
                }

                var binding = new EditorCurveBinding
                {
                    path = path ?? "",
                    propertyName = propertyName,
                    type = type
                };

                bool hit = false;

                AnimationCurve existingCurve = AnimationUtility.GetEditorCurve(clip, binding);
                if (existingCurve != null)
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    curvesRemoved++;
                    hit = true;
                }

                foreach (var objBinding in objectBindings)
                {
                    if (objBinding.path == binding.path && objBinding.propertyName == propertyName && objBinding.type == type)
                    {
                        AnimationUtility.SetObjectReferenceCurve(clip, objBinding, null);
                        curvesRemoved++;
                        hit = true;
                        break;
                    }
                }

                if (!hit)
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "no matching curve on this clip"));
                }
            }

            if (curvesRemoved > 0)
            {
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }

            var extras = new Dictionary<string, object>
            {
                { "curvesRemoved", curvesRemoved },
                { "skippedCount", skipped.Count }
            };

            if (skipped.Count > 0)
                extras["skippedCurves"] = skipped;

            string msg = skipped.Count == 0
                ? $"Removed {curvesRemoved} curve(s) from AnimationClip"
                : $"Removed {curvesRemoved} curve(s); {skipped.Count} skipped — see skippedCurves";

            return ToolUtils.CreateSuccessResponse(msg, extras);
        }

        private static Dictionary<string, object> SkipEntry(string path, string propertyName, string type, string reason)
        {
            return new Dictionary<string, object>
            {
                { "path", path ?? "" },
                { "propertyName", propertyName ?? "" },
                { "type", type ?? "" },
                { "reason", reason }
            };
        }
    }
}
