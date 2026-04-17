using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AssignAnimatorControllerTool : ITool
    {
        public string Name => "assign_animator_controller";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            Animator animator = obj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = obj.AddComponent<Animator>();
                Undo.RegisterCreatedObjectUndo(animator, "Add Animator");
            }
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            Undo.RecordObject(animator, "Assign Animator Controller");
            animator.runtimeAnimatorController = controller;
            
            return ToolUtils.CreateSuccessResponse($"Assigned controller '{controllerPath}' to '{gameObjectPath}'");
        }
    }
}
