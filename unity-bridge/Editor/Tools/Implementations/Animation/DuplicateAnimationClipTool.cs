using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class DuplicateAnimationClipTool : ITool
    {
        public string Name => "duplicate_animation_clip";

        public string Execute(Dictionary<string, object> args)
        {
            string sourceClipPath = args.ContainsKey("sourceClipPath") ? args["sourceClipPath"].ToString() : "";
            string destinationClipPath = args.ContainsKey("destinationClipPath") ? args["destinationClipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(sourceClipPath))
                return ToolUtils.CreateErrorResponse("sourceClipPath is required");
            
            if (string.IsNullOrEmpty(destinationClipPath))
                return ToolUtils.CreateErrorResponse("destinationClipPath is required");
            
            // Ensure paths start with Assets/
            if (!sourceClipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                sourceClipPath = "Assets/" + sourceClipPath;
            
            if (!destinationClipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                destinationClipPath = "Assets/" + destinationClipPath;
            
            // Ensure destination has .anim extension
            if (!destinationClipPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                destinationClipPath += ".anim";
            
            // Load source clip
            AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourceClipPath);
            if (sourceClip == null)
                return ToolUtils.CreateErrorResponse($"Source AnimationClip not found at '{sourceClipPath}'");
            
            // Check if destination already exists
            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationClipPath);
            if (existingClip != null)
                return ToolUtils.CreateErrorResponse($"Destination AnimationClip already exists at '{destinationClipPath}'");
            
            // Ensure destination directory exists
            string dir = System.IO.Path.GetDirectoryName(destinationClipPath);
            // Normalize path separators (Windows uses backslashes, Unity needs forward slashes)
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }
            
            // Clone the clip
            AnimationClip clone = UnityEngine.Object.Instantiate(sourceClip);
            clone.name = System.IO.Path.GetFileNameWithoutExtension(destinationClipPath);
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(clone, $"Duplicate AnimationClip: {destinationClipPath}");
            
            AssetDatabase.CreateAsset(clone, destinationClipPath);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "newClipPath", destinationClipPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Duplicated AnimationClip to '{destinationClipPath}'", extras);
        }
    }
}
