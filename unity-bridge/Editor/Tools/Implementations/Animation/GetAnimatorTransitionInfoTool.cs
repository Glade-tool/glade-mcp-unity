using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimatorTransitionInfoTool : ITool
    {
        public string Name => "get_animator_transition_info";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string fromState = args.ContainsKey("fromState") ? args["fromState"].ToString() : "";
            string toState = args.ContainsKey("toState") ? args["toState"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            if (string.IsNullOrEmpty(fromState))
                return ToolUtils.CreateErrorResponse("fromState is required");
            
            if (string.IsNullOrEmpty(toState))
                return ToolUtils.CreateErrorResponse("toState is required");
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            // Load controller
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
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range. Controller has {controller.layers.Length} layer(s).");
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            // Find from state
            AnimatorState fromStateObj = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == fromState)
                {
                    fromStateObj = childState.state;
                    break;
                }
            }
            
            if (fromStateObj == null)
                return ToolUtils.CreateErrorResponse($"From state '{fromState}' not found in layer {layerIndex}");
            
            // Find transition to toState
            int transitionIndex = 0;
            if (args.ContainsKey("transitionIndex"))
            {
                if (args["transitionIndex"] is int i) transitionIndex = i;
                else if (args["transitionIndex"] is float f) transitionIndex = (int)f;
                else int.TryParse(args["transitionIndex"].ToString(), out transitionIndex);
            }
            
            AnimatorStateTransition transition = null;
            int matchingIndex = 0;
            
            foreach (var trans in fromStateObj.transitions)
            {
                string destName = "";
                if (trans.destinationState != null)
                    destName = trans.destinationState.name;
                else if (trans.destinationStateMachine != null)
                    destName = trans.destinationStateMachine.name;
                
                if (destName == toState)
                {
                    if (matchingIndex == transitionIndex)
                    {
                        transition = trans;
                        break;
                    }
                    matchingIndex++;
                }
            }
            
            if (transition == null)
                return ToolUtils.CreateErrorResponse($"Transition from '{fromState}' to '{toState}' (index {transitionIndex}) not found in layer {layerIndex}");
            
            // Get conditions
            List<Dictionary<string, object>> conditions = new List<Dictionary<string, object>>();
            foreach (var condition in transition.conditions)
            {
                conditions.Add(new Dictionary<string, object>
                {
                    { "parameter", condition.parameter },
                    { "mode", condition.mode.ToString() },
                    { "threshold", condition.threshold }
                });
            }
            
            // Check if transition is from AnyState
            bool isAnyState = false;
            foreach (var anyStateTransition in stateMachine.anyStateTransitions)
            {
                if (anyStateTransition == transition)
                {
                    isAnyState = true;
                    break;
                }
            }
            
            // Check if transition is to Exit
            bool isExit = transition.destinationState == null && transition.destinationStateMachine == null;
            
            var extras = new Dictionary<string, object>
            {
                { "hasExitTime", transition.hasExitTime },
                { "exitTime", transition.exitTime },
                { "duration", transition.duration },
                { "offset", transition.offset },
                { "interruptionSource", transition.interruptionSource.ToString() },
                { "orderedInterruption", transition.orderedInterruption },
                { "canTransitionToSelf", transition.canTransitionToSelf },
                { "solo", transition.solo },
                { "mute", transition.mute },
                { "isExit", isExit },
                { "isAnyState", isAnyState },
                { "conditions", conditions }
            };
            
            return ToolUtils.CreateSuccessResponse($"Retrieved transition info from '{fromState}' to '{toState}'", extras);
        }
    }
}
