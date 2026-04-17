using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveAnimatorStateMachineTool : ITool
    {
        public string Name => "remove_animator_state_machine";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateMachinePath = args.ContainsKey("stateMachinePath") ? args["stateMachinePath"].ToString() : "";

            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(stateMachinePath))
                return ToolUtils.CreateErrorResponse("stateMachinePath is required");

            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");

            int layerIndex = 0;
            if (args.ContainsKey("layerIndex"))
            {
                if (args["layerIndex"] is int i) layerIndex = i;
                else if (args["layerIndex"] is float f) layerIndex = (int)f;
                else int.TryParse(args["layerIndex"].ToString(), out layerIndex);
            }

            if (layerIndex >= controller.layers.Length)
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range");

            var rootStateMachine = controller.layers[layerIndex].stateMachine;

            string parentPath = "";
            string childName = stateMachinePath;
            int lastSlash = stateMachinePath.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                parentPath = stateMachinePath.Substring(0, lastSlash);
                childName = stateMachinePath.Substring(lastSlash + 1);
            }

            var parentStateMachine = FindStateMachineByPath(rootStateMachine, parentPath);
            if (parentStateMachine == null)
                return ToolUtils.CreateErrorResponse($"State machine path '{parentPath}' not found");

            ChildAnimatorStateMachine targetChild = default;
            bool found = false;
            foreach (var child in parentStateMachine.stateMachines)
            {
                if (child.stateMachine != null && child.stateMachine.name == childName)
                {
                    targetChild = child;
                    found = true;
                    break;
                }
            }

            if (!found || targetChild.stateMachine == null)
                return ToolUtils.CreateErrorResponse($"State machine '{stateMachinePath}' not found");

            parentStateMachine.RemoveStateMachine(targetChild.stateMachine);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return ToolUtils.CreateSuccessResponse($"Removed state machine '{stateMachinePath}'");
        }
        
        private static AnimatorStateMachine FindStateMachineByPath(AnimatorStateMachine root, string path)
        {
            if (root == null)
                return null;

            if (string.IsNullOrEmpty(path))
                return root;

            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var current = root;
            foreach (var segment in segments)
            {
                AnimatorStateMachine next = null;
                foreach (var child in current.stateMachines)
                {
                    if (child.stateMachine != null && child.stateMachine.name == segment)
                    {
                        next = child.stateMachine;
                        break;
                    }
                }

                if (next == null)
                    return null;

                current = next;
            }

            return current;
        }
    }
}
