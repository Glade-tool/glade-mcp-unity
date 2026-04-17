using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class DeleteAnimationClipTool : ITool
    {
        public string Name => "delete_animation_clip";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            // Check if asset exists
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"AnimationClip not found at '{clipPath}'");
            
            // Delete asset
            // Note: AssetDatabase.DeleteAsset doesn't support undo, but we document this limitation
            bool deleted = AssetDatabase.DeleteAsset(clipPath);
            
            if (deleted)
            {
                AssetDatabase.SaveAssets();
                var extras = new Dictionary<string, object>
                {
                    { "deleted", true }
                };
                return ToolUtils.CreateSuccessResponse($"Deleted AnimationClip at '{clipPath}'", extras);
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Failed to delete AnimationClip at '{clipPath}'");
            }
        }
    }
}
