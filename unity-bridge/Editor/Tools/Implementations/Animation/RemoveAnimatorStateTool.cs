using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveAnimatorStateTool : ITool
    {
        public string Name => "remove_animator_state";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            string stateMachinePath = args.ContainsKey("stateMachinePath") ? args["stateMachinePath"].ToString() : "";

            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required");

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
            var stateMachine = FindStateMachineByPath(rootStateMachine, stateMachinePath);
            if (stateMachine == null)
                return ToolUtils.CreateErrorResponse($"State machine path '{stateMachinePath}' not found");

            AnimatorState targetState = null;
            foreach (var s in stateMachine.states)
            {
                if (s.state.name == stateName)
                {
                    targetState = s.state;
                    break;
                }
            }

            if (targetState == null)
                return ToolUtils.CreateErrorResponse($"State '{stateName}' not found");

            // Remove transitions that point to the target state
            var anyStateToRemove = new List<AnimatorStateTransition>();
            foreach (var transition in stateMachine.anyStateTransitions)
            {
                if (transition.destinationState == targetState)
                {
                    anyStateToRemove.Add(transition);
                }
            }
            foreach (var transition in anyStateToRemove)
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (var s in stateMachine.states)
            {
                var transitionsToRemove = new List<AnimatorStateTransition>();
                foreach (var transition in s.state.transitions)
                {
                    if (transition.destinationState == targetState || s.state == targetState)
                    {
                        transitionsToRemove.Add(transition);
                    }
                }
                foreach (var transition in transitionsToRemove)
                {
                    s.state.RemoveTransition(transition);
                }
            }

            if (stateMachine.defaultState == targetState)
            {
                stateMachine.defaultState = null;
                foreach (var s in stateMachine.states)
                {
                    if (s.state != targetState)
                    {
                        stateMachine.defaultState = s.state;
                        break;
                    }
                }
            }

            stateMachine.RemoveState(targetState);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return ToolUtils.CreateSuccessResponse($"Removed state '{stateName}'");
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
