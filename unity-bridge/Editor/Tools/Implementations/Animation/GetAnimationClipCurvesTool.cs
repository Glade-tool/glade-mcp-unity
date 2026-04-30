using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimationClipCurvesTool : ITool
    {
        public string Name => "get_animation_clip_curves";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";

            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");

            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");

            var curves = new List<Dictionary<string, object>>();

            // Editor curves (float/int/bool). Pre-2026-04-30 this used a hand-rolled
            // SerializedObject walk against `m_EditorCurves` reading non-existent
            // `propertyName`/`type` fields — the actual YAML keys are `attribute`
            // and `classID`. Result was every curve silently dropped from the read.
            // AnimationUtility.GetCurveBindings has existed since Unity 5.x.
            EditorCurveBinding[] editorBindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in editorBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                int keyframeCount = curve != null ? curve.keys.Length : 0;
                curves.Add(new Dictionary<string, object>
                {
                    { "path", binding.path ?? "" },
                    { "propertyName", binding.propertyName ?? "" },
                    { "type", binding.type != null ? binding.type.Name : "" },
                    { "keyframeCount", keyframeCount }
                });
            }

            // Object reference curves (sprite curves, etc.)
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in objectBindings)
            {
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                int keyframeCount = keyframes != null ? keyframes.Length : 0;
                curves.Add(new Dictionary<string, object>
                {
                    { "path", binding.path ?? "" },
                    { "propertyName", binding.propertyName ?? "" },
                    { "type", binding.type != null ? binding.type.Name : "" },
                    { "keyframeCount", keyframeCount }
                });
            }

            var extras = new Dictionary<string, object>
            {
                { "curves", curves }
            };

            return ToolUtils.CreateSuccessResponse($"Found {curves.Count} curve(s) in AnimationClip", extras);
        }
    }
}
