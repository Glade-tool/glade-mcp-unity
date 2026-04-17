using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class ModifyAnimationClipTool : ITool
    {
        public string Name => "modify_animation_clip";

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
            
            bool modified = false;
            
            // Record for undo
            Undo.RecordObject(clip, $"Modify AnimationClip: {clipPath}");
            
            // Modify frame rate
            if (args.ContainsKey("frameRate"))
            {
                float frameRate = 60f;
                if (args["frameRate"] is float f) frameRate = f;
                else float.TryParse(args["frameRate"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out frameRate);
                
                clip.frameRate = frameRate;
                modified = true;
            }
            
            // Modify wrap mode
            if (args.ContainsKey("wrapMode"))
            {
                string wrapModeStr = args["wrapMode"].ToString();
                if (System.Enum.TryParse<WrapMode>(wrapModeStr, true, out WrapMode wrapMode))
                {
                    clip.wrapMode = wrapMode;
                    modified = true;
                }
            }
            
            // Modify loop time, loop pose, cycle offset, and additive reference pose using AnimationClipSettings
            bool hasSettingsChanges = args.ContainsKey("loopTime") || args.ContainsKey("loopPose") || 
                                     args.ContainsKey("cycleOffset") || args.ContainsKey("additiveReferencePoseClipPath") || 
                                     args.ContainsKey("hasAdditiveReferencePose");
            if (hasSettingsChanges)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                
                if (args.ContainsKey("loopTime"))
                {
                    bool loopTime = false;
                    if (args["loopTime"] is bool b) loopTime = b;
                    else bool.TryParse(args["loopTime"].ToString(), out loopTime);
                    
                    settings.loopTime = loopTime;
                    modified = true;
                }
                
                if (args.ContainsKey("loopPose"))
                {
                    bool loopPose = false;
                    if (args["loopPose"] is bool b) loopPose = b;
                    else bool.TryParse(args["loopPose"].ToString(), out loopPose);
                    
                    // Use SerializedObject to set loopPose as it's not directly accessible
                    SerializedObject serializedClip = new SerializedObject(clip);
                    SerializedProperty loopPoseProp = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopPose");
                    if (loopPoseProp != null)
                    {
                        loopPoseProp.boolValue = loopPose;
                        serializedClip.ApplyModifiedProperties();
                        modified = true;
                    }
                }
                
                if (args.ContainsKey("cycleOffset"))
                {
                    float cycleOffset = 0f;
                    if (args["cycleOffset"] is float f) cycleOffset = f;
                    else float.TryParse(args["cycleOffset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cycleOffset);
                    settings.cycleOffset = cycleOffset;
                    modified = true;
                }
                
                if (args.ContainsKey("additiveReferencePoseClipPath"))
                {
                    string refClipPath = args["additiveReferencePoseClipPath"].ToString();
                    if (string.IsNullOrEmpty(refClipPath))
                    {
                        settings.additiveReferencePoseClip = null;
                    }
                    else
                    {
                        if (!refClipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                            refClipPath = "Assets/" + refClipPath;
                        AnimationClip refClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(refClipPath);
                        if (refClip != null)
                        {
                            settings.additiveReferencePoseClip = refClip;
                        }
                    }
                    modified = true;
                }
                
                if (args.ContainsKey("hasAdditiveReferencePose"))
                {
                    bool hasAdditiveReferencePose = false;
                    if (args["hasAdditiveReferencePose"] is bool b) hasAdditiveReferencePose = b;
                    else bool.TryParse(args["hasAdditiveReferencePose"].ToString(), out hasAdditiveReferencePose);
                    settings.hasAdditiveReferencePose = hasAdditiveReferencePose;
                    modified = true;
                }
                
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
            
            if (modified)
            {
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "modified", modified }
            };
            
            return ToolUtils.CreateSuccessResponse(modified ? $"Modified AnimationClip at '{clipPath}'" : $"No changes made to AnimationClip at '{clipPath}'", extras);
        }
    }
}
