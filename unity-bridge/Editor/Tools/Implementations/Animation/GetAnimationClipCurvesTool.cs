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
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            List<Dictionary<string, object>> curves = new List<Dictionary<string, object>>();
            
            // Get editor curves (float/int/bool curves)
            // Note: GetEditorCurveBindings doesn't exist in this Unity version
            // We use SerializedObject to access the clip's bindings
            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty bindingsProperty = serializedClip.FindProperty("m_EditorCurves");
            
            if (bindingsProperty != null && bindingsProperty.isArray)
            {
                for (int i = 0; i < bindingsProperty.arraySize; i++)
                {
                    SerializedProperty bindingProp = bindingsProperty.GetArrayElementAtIndex(i);
                    SerializedProperty pathProp = bindingProp.FindPropertyRelative("path");
                    SerializedProperty propertyNameProp = bindingProp.FindPropertyRelative("propertyName");
                    SerializedProperty typeProp = bindingProp.FindPropertyRelative("type");
                    
                    if (pathProp != null && propertyNameProp != null && typeProp != null)
                    {
                        string path = pathProp.stringValue;
                        string propertyName = propertyNameProp.stringValue;
                        string typeName = typeProp.stringValue;
                        
                        // Create binding to get the curve
                        EditorCurveBinding binding = new EditorCurveBinding
                        {
                            path = path,
                            propertyName = propertyName,
                            type = System.Type.GetType(typeName)
                        };
                        
                        if (binding.type != null)
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                            int keyframeCount = curve != null ? curve.keys.Length : 0;
                            
                            curves.Add(new Dictionary<string, object>
                            {
                                { "path", path },
                                { "propertyName", propertyName },
                                { "type", typeName },
                                { "keyframeCount", keyframeCount }
                            });
                        }
                    }
                }
            }
            
            // Get object reference curves (sprite curves, etc.)
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var binding in objectBindings)
            {
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                int keyframeCount = keyframes != null ? keyframes.Length : 0;
                
                curves.Add(new Dictionary<string, object>
                {
                    { "path", binding.path },
                    { "propertyName", binding.propertyName },
                    { "type", binding.type != null ? binding.type.FullName : "" },
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
