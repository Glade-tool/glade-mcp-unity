using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class DeleteAnimatorControllerTool : ITool
    {
        public string Name => "delete_animator_controller";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            // Check if asset exists
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            // Delete asset
            // Note: AssetDatabase.DeleteAsset doesn't support undo, but we document this limitation
            bool deleted = AssetDatabase.DeleteAsset(controllerPath);
            
            if (deleted)
            {
                AssetDatabase.SaveAssets();
                var extras = new Dictionary<string, object>
                {
                    { "deleted", true }
                };
                return ToolUtils.CreateSuccessResponse($"Deleted Animator Controller at '{controllerPath}'", extras);
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Failed to delete Animator Controller at '{controllerPath}'");
            }
        }
    }
}
