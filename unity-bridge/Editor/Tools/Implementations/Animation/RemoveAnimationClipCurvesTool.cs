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
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Record for undo
            Undo.RecordObject(clip, $"Remove AnimationClip Curves: {clipPath}");
            
            // Get object reference bindings (GetEditorCurveBindings doesn't exist in this Unity version)
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            
            // Parse curves to remove
            var curvesObj = args["curves"];
            int curvesRemoved = 0;
            
            if (curvesObj is List<object> curvesList)
            {
                foreach (var curveObj in curvesList)
                {
                    if (curveObj is Dictionary<string, object> curve)
                    {
                        string path = curve.ContainsKey("path") ? curve["path"].ToString() : "";
                        string propertyName = curve.ContainsKey("propertyName") ? curve["propertyName"].ToString() : "";
                        string typeStr = curve.ContainsKey("type") ? curve["type"].ToString() : "";
                        
                        if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(typeStr))
                            continue;
                        
                        System.Type type = System.Type.GetType(typeStr);
                        if (type == null)
                            continue;
                        
                        // Try to remove editor curve by creating binding and setting to null
                        // (GetEditorCurveBindings doesn't exist, so we create the binding from the provided info)
                        EditorCurveBinding editorBinding = new EditorCurveBinding
                        {
                            path = path,
                            propertyName = propertyName,
                            type = type
                        };
                        
                        // Try to get the curve - if it exists, remove it
                        AnimationCurve existingCurve = AnimationUtility.GetEditorCurve(clip, editorBinding);
                        if (existingCurve != null)
                        {
                            AnimationUtility.SetEditorCurve(clip, editorBinding, null);
                            curvesRemoved++;
                        }
                        
                        // Try to find and remove object reference curve
                        foreach (var binding in objectBindings)
                        {
                            if (binding.path == path && binding.propertyName == propertyName && binding.type == type)
                            {
                                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                                curvesRemoved++;
                                break;
                            }
                        }
                    }
                }
            }
            
            if (curvesRemoved > 0)
            {
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "curvesRemoved", curvesRemoved }
            };
            
            return ToolUtils.CreateSuccessResponse($"Removed {curvesRemoved} curve(s) from AnimationClip", extras);
        }
    }
}
