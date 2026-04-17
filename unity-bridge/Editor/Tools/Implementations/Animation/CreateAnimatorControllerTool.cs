using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateAnimatorControllerTool : ITool
    {
        public string Name => "create_animator_controller";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
            {
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            }
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            // Ensure has .controller extension
            if (!controllerPath.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
                controllerPath += ".controller";
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(controllerPath);
            // Normalize path separators (Windows uses backslashes, Unity needs forward slashes)
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }
            
            // Create controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            
            if (controller == null)
            {
                return ToolUtils.CreateErrorResponse($"Failed to create animator controller at '{controllerPath}'");
            }
            
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "controllerPath", controllerPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Animator Controller at '{controllerPath}'", extras);
        }
    }
}
