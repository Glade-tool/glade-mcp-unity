using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimationClipCurvesTool : ITool
    {
        public string Name => "set_animation_clip_curves";

        // Common animation binding types surfaced when the model sends an unrecognized
        // type string. Hardcoded list matches what AnimationUtility actually accepts.
        private static readonly string[] CommonBindingTypes = new[]
        {
            "Transform", "GameObject", "SpriteRenderer", "MeshRenderer",
            "SkinnedMeshRenderer", "Animator", "Light", "Camera", "AudioSource",
            "Rigidbody", "Rigidbody2D", "MonoBehaviour"
        };

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

            // ParseJsonToDict leaves nested JSON arrays as raw strings; re-hydrate
            // via TryParseJsonArrayToList so this works whether args arrive
            // already-typed (e.g. WebSocket dispatch) or string-encoded
            // (e.g. batch_execute or external MCP clients).
            var curvesObj = args["curves"];
            if (curvesObj is string curvesJson && ToolUtils.TryParseJsonArrayToList(curvesJson, out var parsedCurves))
                curvesObj = parsedCurves;
            if (!(curvesObj is List<object> curvesList))
                return ToolUtils.CreateErrorResponse("curves must be an array");

            int curvesAdded = 0;
            var skipped = new List<Dictionary<string, object>>();
            bool anyUnresolvedType = false;

            Undo.RecordObject(clip, $"Set Animation Curves: {clipPath}");

            foreach (var curveObj in curvesList)
            {
                if (!(curveObj is Dictionary<string, object> curve))
                {
                    skipped.Add(SkipEntry("", "", "", "curve entry is not an object"));
                    continue;
                }

                string path = curve.ContainsKey("path") ? curve["path"].ToString() : "";
                string propertyName = curve.ContainsKey("propertyName") ? curve["propertyName"].ToString() : "";

                // Accept both `type` (canonical) and `componentType` (legacy schema name).
                // Pre-2026-04-30 the Python schema advertised `componentType` while C# read `type`,
                // so curves authored from the schema were silently dropped.
                string typeStr = "";
                if (curve.ContainsKey("type")) typeStr = curve["type"].ToString();
                else if (curve.ContainsKey("componentType")) typeStr = curve["componentType"].ToString();

                if (string.IsNullOrEmpty(propertyName))
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "propertyName is required"));
                    continue;
                }

                if (string.IsNullOrEmpty(typeStr))
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "type is required (e.g. 'Transform', 'SpriteRenderer', 'GameObject')"));
                    continue;
                }

                System.Type bindingType = ToolUtils.FindAnimationBindingType(typeStr);
                if (bindingType == null)
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr,
                        $"could not resolve type '{typeStr}'. Common types: {string.Join(", ", CommonBindingTypes)}"));
                    anyUnresolvedType = true;
                    continue;
                }

                // Accept both `keyframes` (canonical) and `keys` (legacy schema name).
                object keyframesObj = null;
                if (curve.ContainsKey("keyframes")) keyframesObj = curve["keyframes"];
                else if (curve.ContainsKey("keys")) keyframesObj = curve["keys"];

                // Re-hydrate JSON-array strings (see ParseJsonToDict comment above).
                if (keyframesObj is string kfJson && ToolUtils.TryParseJsonArrayToList(kfJson, out var parsedKeyframes))
                    keyframesObj = parsedKeyframes;

                if (!(keyframesObj is List<object> keyframesList))
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "keyframes is required and must be an array"));
                    continue;
                }

                var keyframes = new List<Keyframe>();
                foreach (var kfObj in keyframesList)
                {
                    if (!(kfObj is Dictionary<string, object> kf)) continue;

                    float time = ParseFloat(kf, "time");
                    float value = ParseFloat(kf, "value");
                    float inTangent = ParseFloat(kf, "inTangent");
                    float outTangent = ParseFloat(kf, "outTangent");
                    keyframes.Add(new Keyframe(time, value, inTangent, outTangent));
                }

                if (keyframes.Count == 0)
                {
                    skipped.Add(SkipEntry(path, propertyName, typeStr, "no valid keyframes parsed (each keyframe needs at least 'time' and 'value')"));
                    continue;
                }

                var binding = new EditorCurveBinding
                {
                    path = path ?? "",
                    propertyName = propertyName,
                    type = bindingType
                };

                AnimationCurve animCurve = new AnimationCurve(keyframes.ToArray());
                AnimationUtility.SetEditorCurve(clip, binding, animCurve);
                curvesAdded++;
            }

            if (curvesAdded > 0)
            {
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }

            var extras = new Dictionary<string, object>
            {
                { "curvesAdded", curvesAdded },
                { "skippedCount", skipped.Count }
            };

            if (skipped.Count > 0)
            {
                extras["skippedCurves"] = skipped;
                if (anyUnresolvedType)
                {
                    extras["knownBindingTypes"] = new List<string>(CommonBindingTypes);
                }
            }

            // Surface a hard error when nothing landed and skips exist — silent
            // success-with-zero is the failure mode this audit is closing.
            if (curvesAdded == 0 && skipped.Count > 0)
            {
                return ToolUtils.CreateErrorResponse(
                    $"No curves applied. {skipped.Count} curve(s) skipped — see skippedCurves for per-entry reasons.",
                    extras);
            }

            string msg = skipped.Count == 0
                ? $"Set {curvesAdded} curve(s) on AnimationClip"
                : $"Set {curvesAdded} curve(s); {skipped.Count} skipped — see skippedCurves";
            return ToolUtils.CreateSuccessResponse(msg, extras);
        }

        private static float ParseFloat(Dictionary<string, object> kf, string key)
        {
            if (!kf.ContainsKey(key)) return 0f;
            object v = kf[key];
            if (v is float f) return f;
            if (v is double d) return (float)d;
            if (v is int i) return i;
            if (v is long l) return l;
            float.TryParse(v.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed);
            return parsed;
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
