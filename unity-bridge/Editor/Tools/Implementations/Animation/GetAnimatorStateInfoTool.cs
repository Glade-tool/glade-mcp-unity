using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimatorStateInfoTool : ITool
    {
        public string Name => "get_animator_state_info";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required");
            
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
            
            // Find state by name
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    state = childState.state;
                    break;
                }
            }
            
            if (state == null)
                return ToolUtils.CreateErrorResponse($"State '{stateName}' not found in layer {layerIndex}");
            
            // Get motion path
            string motionPath = "";
            if (state.motion != null)
            {
                motionPath = AssetDatabase.GetAssetPath(state.motion);
            }
            
            // Get transitions
            List<string> transitionNames = new List<string>();
            foreach (var transition in state.transitions)
            {
                if (transition.destinationState != null)
                    transitionNames.Add(transition.destinationState.name);
                else if (transition.destinationStateMachine != null)
                    transitionNames.Add(transition.destinationStateMachine.name);
            }
            
            // Determine motion type
            string motionType = null;
            if (state.motion is AnimationClip)
                motionType = "AnimationClip";
            else if (state.motion is BlendTree)
                motionType = "BlendTree";
            
            // Check if this is the default state
            bool isDefaultState = stateMachine.defaultState == state;
            
            var extras = new Dictionary<string, object>
            {
                { "motionPath", motionPath },
                { "speed", state.speed },
                { "mirror", state.mirror },
                { "cycleOffset", state.cycleOffset },
                { "writeDefaultValues", state.writeDefaultValues },
                { "iKOnFeet", state.iKOnFeet },
                { "tag", state.tag },
                { "timeParameter", state.timeParameter },
                { "timeParameterActive", state.timeParameterActive },
                { "motionType", motionType },
                { "isDefaultState", isDefaultState },
                { "transitions", transitionNames }
            };
            
            return ToolUtils.CreateSuccessResponse($"Retrieved state info for '{stateName}'", extras);
        }
    }
}
