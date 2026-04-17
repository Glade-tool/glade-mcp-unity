using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class DuplicateAnimatorControllerTool : ITool
    {
        public string Name => "duplicate_animator_controller";

        public string Execute(Dictionary<string, object> args)
        {
            string sourceControllerPath = args.ContainsKey("sourceControllerPath") ? args["sourceControllerPath"].ToString() : "";
            string destinationControllerPath = args.ContainsKey("destinationControllerPath") ? args["destinationControllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(sourceControllerPath))
                return ToolUtils.CreateErrorResponse("sourceControllerPath is required");
            
            if (string.IsNullOrEmpty(destinationControllerPath))
                return ToolUtils.CreateErrorResponse("destinationControllerPath is required");
            
            // Ensure paths start with Assets/
            if (!sourceControllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                sourceControllerPath = "Assets/" + sourceControllerPath;
            
            if (!destinationControllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                destinationControllerPath = "Assets/" + destinationControllerPath;
            
            // Ensure destination has .controller extension
            if (!destinationControllerPath.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
                destinationControllerPath += ".controller";
            
            // Load source controller
            AnimatorController sourceController = AssetDatabase.LoadAssetAtPath<AnimatorController>(sourceControllerPath);
            if (sourceController == null)
                return ToolUtils.CreateErrorResponse($"Source Animator Controller not found at '{sourceControllerPath}'");
            
            // Check if destination already exists
            AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(destinationControllerPath);
            if (existingController != null)
                return ToolUtils.CreateErrorResponse($"Destination Animator Controller already exists at '{destinationControllerPath}'");
            
            // Ensure destination directory exists
            string dir = System.IO.Path.GetDirectoryName(destinationControllerPath);
            // Normalize path separators (Windows uses backslashes, Unity needs forward slashes)
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }
            
            // Clone the controller
            AnimatorController clone = UnityEngine.Object.Instantiate(sourceController);
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(clone, $"Duplicate Animator Controller: {destinationControllerPath}");
            
            AssetDatabase.CreateAsset(clone, destinationControllerPath);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "newControllerPath", destinationControllerPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Duplicated Animator Controller to '{destinationControllerPath}'", extras);
        }
    }
}
