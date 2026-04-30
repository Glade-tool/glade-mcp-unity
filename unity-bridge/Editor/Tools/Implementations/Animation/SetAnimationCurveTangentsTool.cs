using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimationCurveTangentsTool : ITool
    {
        public string Name => "set_animation_curve_tangents";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            string bindingPath = args.ContainsKey("bindingPath") ? args["bindingPath"].ToString() : "";
            string propertyName = args.ContainsKey("propertyName") ? args["propertyName"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            if (string.IsNullOrEmpty(propertyName))
                return ToolUtils.CreateErrorResponse("propertyName is required");
            
            int keyframeIndex = -1;
            if (args.ContainsKey("keyframeIndex"))
            {
                if (args["keyframeIndex"] is int i) keyframeIndex = i;
                else if (args["keyframeIndex"] is float f) keyframeIndex = (int)f;
                else int.TryParse(args["keyframeIndex"].ToString(), out keyframeIndex);
            }
            
            if (keyframeIndex < 0)
                return ToolUtils.CreateErrorResponse("keyframeIndex is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Resolve binding type (defaults to Transform for backwards compat).
            // Pre-2026-04-30 this was hardcoded — silent fail when tangent-tuning
            // SpriteRenderer / MeshRenderer / custom-script curves.
            string typeStr = args.ContainsKey("type") ? args["type"].ToString() : "Transform";
            System.Type bindingType = ToolUtils.FindAnimationBindingType(typeStr) ?? typeof(UnityEngine.Transform);

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = bindingPath ?? "",
                propertyName = propertyName,
                type = bindingType
            };

            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
            {
                // Enumerate up to 20 actual bindings on the clip so the model can retry
                // against a real binding instead of guessing again.
                var availableBindings = new List<string>();
                EditorCurveBinding[] allBindings = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < allBindings.Length && availableBindings.Count < 20; i++)
                {
                    var b = allBindings[i];
                    availableBindings.Add($"{b.path}|{b.propertyName}|{b.type.Name}");
                }
                var extrasOnMiss = new Dictionary<string, object>
                {
                    { "requestedPath", bindingPath ?? "" },
                    { "requestedPropertyName", propertyName },
                    { "requestedType", bindingType.Name },
                    { "availableBindings", availableBindings },
                    { "hint", "availableBindings entries are 'path|propertyName|type'. Match propertyName exactly (e.g. 'm_LocalPosition.x', not 'position')." }
                };
                return ToolUtils.CreateErrorResponse(
                    $"Curve not found for property '{propertyName}' at path '{bindingPath}' (type {bindingType.Name}). Clip has {allBindings.Length} editor curve binding(s) — see availableBindings.",
                    extrasOnMiss);
            }
            
            // Validate keyframeIndex
            if (keyframeIndex >= curve.keys.Length)
                return ToolUtils.CreateErrorResponse($"Keyframe index {keyframeIndex} out of range. Curve has {curve.keys.Length} keyframe(s).");
            
            // Record Undo BEFORE modifications
            Undo.RecordObject(clip, $"Set Animation Curve Tangents: {clipPath}");
            
            bool modified = false;
            
            // Modify keyframe tangents
            Keyframe[] keys = curve.keys;
            Keyframe keyframe = keys[keyframeIndex];
            
            // If inTangent/outTangent provided, create new keyframe with custom tangents
            if (args.ContainsKey("inTangent") || args.ContainsKey("outTangent"))
            {
                float inTangent = keyframe.inTangent;
                float outTangent = keyframe.outTangent;
                
                if (args.ContainsKey("inTangent"))
                {
                    if (args["inTangent"] is float it) inTangent = it;
                    else float.TryParse(args["inTangent"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out inTangent);
                }
                
                if (args.ContainsKey("outTangent"))
                {
                    if (args["outTangent"] is float ot) outTangent = ot;
                    else float.TryParse(args["outTangent"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out outTangent);
                }
                
                keyframe = new Keyframe(keyframe.time, keyframe.value, inTangent, outTangent);
                keys[keyframeIndex] = keyframe;
                modified = true;
            }
            
            // Set broken tangents
            if (args.ContainsKey("broken"))
            {
                bool broken = false;
                if (args["broken"] is bool b) broken = b;
                else bool.TryParse(args["broken"].ToString(), out broken);
                
                AnimationUtility.SetKeyBroken(curve, keyframeIndex, broken);
                modified = true;
            }
            
            // Set tangent mode
            if (args.ContainsKey("tangentMode"))
            {
                string tangentModeStr = args["tangentMode"].ToString();
                
                // Parse tangent mode enum
                AnimationUtility.TangentMode leftMode = AnimationUtility.TangentMode.Auto;
                AnimationUtility.TangentMode rightMode = AnimationUtility.TangentMode.Auto;
                
                switch (tangentModeStr.ToLower())
                {
                    case "auto":
                        leftMode = AnimationUtility.TangentMode.Auto;
                        rightMode = AnimationUtility.TangentMode.Auto;
                        break;
                    case "linear":
                        leftMode = AnimationUtility.TangentMode.Linear;
                        rightMode = AnimationUtility.TangentMode.Linear;
                        break;
                    case "constant":
                        leftMode = AnimationUtility.TangentMode.Constant;
                        rightMode = AnimationUtility.TangentMode.Constant;
                        break;
                    case "clampedauto":
                        leftMode = AnimationUtility.TangentMode.ClampedAuto;
                        rightMode = AnimationUtility.TangentMode.ClampedAuto;
                        break;
                    default:
                        return ToolUtils.CreateErrorResponse($"Invalid tangentMode: {tangentModeStr}. Must be 'Auto', 'Linear', 'Constant', or 'ClampedAuto'");
                }
                
                AnimationUtility.SetKeyLeftTangentMode(curve, keyframeIndex, leftMode);
                AnimationUtility.SetKeyRightTangentMode(curve, keyframeIndex, rightMode);
                modified = true;
            }
            
            // Set weighted mode
            if (args.ContainsKey("weightedMode"))
            {
                string weightedModeStr = args["weightedMode"].ToString();
                WeightedMode weightedMode = WeightedMode.None;
                
                switch (weightedModeStr.ToLower())
                {
                    case "none":
                        weightedMode = WeightedMode.None;
                        break;
                    case "in":
                        weightedMode = WeightedMode.In;
                        break;
                    case "out":
                        weightedMode = WeightedMode.Out;
                        break;
                    case "both":
                        weightedMode = WeightedMode.Both;
                        break;
                    default:
                        return ToolUtils.CreateErrorResponse($"Invalid weightedMode: {weightedModeStr}. Must be 'None', 'In', 'Out', or 'Both'");
                }
                
                // Set weighted mode using keyframe directly (SetKeyWeightedMode may not be available in all Unity versions)
                Keyframe key = keys[keyframeIndex];
                key.weightedMode = weightedMode;
                keys[keyframeIndex] = key;
                modified = true;
            }
            
            // Replace curve if modified
            if (modified)
            {
                AnimationUtility.SetEditorCurve(clip, binding, curve);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "keyframeIndex", keyframeIndex },
                { "modified", modified }
            };
            
            return ToolUtils.CreateSuccessResponse(modified ? $"Modified tangents for keyframe {keyframeIndex}" : $"No changes made to keyframe {keyframeIndex}", extras);
        }
    }
}
