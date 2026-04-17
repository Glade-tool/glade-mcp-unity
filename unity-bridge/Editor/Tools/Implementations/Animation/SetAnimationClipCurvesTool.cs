using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimationClipCurvesTool : ITool
    {
        public string Name => "set_animation_clip_curves";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            if (!args.ContainsKey("curves"))
                return ToolUtils.CreateErrorResponse("curves is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            var curvesObj = args["curves"];
            int curvesAdded = 0;
            
            if (curvesObj is List<object> curvesList)
            {
                foreach (var curveObj in curvesList)
                {
                    if (curveObj is Dictionary<string, object> curve)
                    {
                        string path = curve.ContainsKey("path") ? curve["path"].ToString() : "";
                        string propertyName = curve.ContainsKey("propertyName") ? curve["propertyName"].ToString() : "";
                        string type = curve.ContainsKey("type") ? curve["type"].ToString() : "";
                        
                        if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(type))
                            continue;
                        
                        if (!curve.ContainsKey("keyframes"))
                            continue;
                        
                        var keyframesObj = curve["keyframes"];
                        if (keyframesObj is List<object> keyframesList)
                        {
                            var keyframes = new List<Keyframe>();
                            foreach (var kfObj in keyframesList)
                            {
                                if (kfObj is Dictionary<string, object> kf)
                                {
                                    float time = 0f;
                                    float value = 0f;
                                    float inTangent = 0f;
                                    float outTangent = 0f;
                                    
                                    if (kf.ContainsKey("time"))
                                    {
                                        if (kf["time"] is float tf) time = tf;
                                        else float.TryParse(kf["time"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out time);
                                    }
                                    
                                    if (kf.ContainsKey("value"))
                                    {
                                        if (kf["value"] is float vf) value = vf;
                                        else float.TryParse(kf["value"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
                                    }
                                    
                                    if (kf.ContainsKey("inTangent"))
                                    {
                                        if (kf["inTangent"] is float itf) inTangent = itf;
                                        else float.TryParse(kf["inTangent"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out inTangent);
                                    }
                                    
                                    if (kf.ContainsKey("outTangent"))
                                    {
                                        if (kf["outTangent"] is float otf) outTangent = otf;
                                        else float.TryParse(kf["outTangent"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out outTangent);
                                    }
                                    
                                    var keyframe = new Keyframe(time, value, inTangent, outTangent);
                                    keyframes.Add(keyframe);
                                }
                            }
                            
                            if (keyframes.Count > 0)
                            {
                                var binding = new EditorCurveBinding
                                {
                                    path = path,
                                    propertyName = propertyName,
                                    type = System.Type.GetType(type)
                                };
                                
                                if (binding.type != null)
                                {
                                    AnimationCurve animCurve = new AnimationCurve(keyframes.ToArray());
                                    AnimationUtility.SetEditorCurve(clip, binding, animCurve);
                                    curvesAdded++;
                                }
                            }
                        }
                    }
                }
            }
            
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "curvesAdded", curvesAdded }
            };
            
            return ToolUtils.CreateSuccessResponse($"Set {curvesAdded} curve(s) on AnimationClip", extras);
        }
    }
}
