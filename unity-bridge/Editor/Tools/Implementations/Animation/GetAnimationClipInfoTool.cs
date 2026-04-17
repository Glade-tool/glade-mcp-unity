using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimationClipInfoTool : ITool
    {
        public string Name => "get_animation_clip_info";

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
            
            // Get clip info
            float length = clip.length;
            float frameRate = clip.frameRate;
            bool isLooping = clip.isLooping;
            bool isHumanMotion = clip.isHumanMotion;
            bool hasRootMotion = clip.hasGenericRootTransform || clip.hasMotionCurves;
            WrapMode wrapMode = clip.wrapMode;
            
            // Get animation clip settings
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            
            string additiveReferencePoseClipPath = null;
            if (settings.additiveReferencePoseClip != null)
            {
                additiveReferencePoseClipPath = AssetDatabase.GetAssetPath(settings.additiveReferencePoseClip);
            }
            
            // Use SerializedObject to access properties that may not be in AnimationClipSettings
            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty loopPoseProp = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopPose");
            SerializedProperty firstFrameProp = serializedClip.FindProperty("m_AnimationClipSettings.m_FirstFrame");
            SerializedProperty lastFrameProp = serializedClip.FindProperty("m_AnimationClipSettings.m_LastFrame");
            
            object loopPose = loopPoseProp != null ? loopPoseProp.boolValue : (object)null;
            object firstFrame = firstFrameProp != null ? firstFrameProp.floatValue : (object)null;
            object lastFrame = lastFrameProp != null ? lastFrameProp.floatValue : (object)null;
            
            var extras = new Dictionary<string, object>
            {
                { "name", clip.name },
                { "length", length },
                { "frameRate", frameRate },
                { "frameCount", (int)(length * frameRate) },
                { "isLooping", isLooping },
                { "isHumanMotion", isHumanMotion },
                { "hasRootMotion", hasRootMotion },
                { "wrapMode", wrapMode.ToString() },
                { "loopPose", loopPose },
                { "cycleOffset", settings.cycleOffset },
                { "additiveReferencePoseClipPath", additiveReferencePoseClipPath },
                { "hasAdditiveReferencePose", settings.hasAdditiveReferencePose },
                { "firstFrame", firstFrame },
                { "lastFrame", lastFrame }
            };
            
            return ToolUtils.CreateSuccessResponse($"Clip '{clip.name}': {length:F2}s, {frameRate}fps, looping={isLooping}", extras);
        }
    }
}
