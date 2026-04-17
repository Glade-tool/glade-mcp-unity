using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimatorTransitionPropertiesTool : ITool
    {
        public string Name => "set_animator_transition_properties";

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
            
            bool modified = false;
            
            // Record for undo
            Undo.RecordObject(transition, $"Set Animator Transition Properties: {fromState} -> {toState}");
            
            // Modify hasExitTime
            if (args.ContainsKey("hasExitTime"))
            {
                bool hasExitTime = false;
                if (args["hasExitTime"] is bool b) hasExitTime = b;
                else bool.TryParse(args["hasExitTime"].ToString(), out hasExitTime);
                
                transition.hasExitTime = hasExitTime;
                modified = true;
            }
            
            // Modify exitTime
            if (args.ContainsKey("exitTime"))
            {
                float exitTime = 0f;
                if (args["exitTime"] is float f) exitTime = f;
                else float.TryParse(args["exitTime"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out exitTime);
                
                transition.exitTime = exitTime;
                modified = true;
            }
            
            // Modify duration
            if (args.ContainsKey("duration"))
            {
                float duration = 0f;
                if (args["duration"] is float f) duration = f;
                else float.TryParse(args["duration"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out duration);
                
                transition.duration = duration;
                modified = true;
            }
            
            // Modify offset
            if (args.ContainsKey("offset"))
            {
                float offset = 0f;
                if (args["offset"] is float f) offset = f;
                else float.TryParse(args["offset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out offset);
                transition.offset = offset;
                modified = true;
            }
            
            // Modify interruptionSource
            if (args.ContainsKey("interruptionSource"))
            {
                string interruptionSourceStr = args["interruptionSource"].ToString();
                if (Enum.TryParse<TransitionInterruptionSource>(interruptionSourceStr, true, out TransitionInterruptionSource interruptionSource))
                {
                    transition.interruptionSource = interruptionSource;
                    modified = true;
                }
            }
            
            // Modify orderedInterruption
            if (args.ContainsKey("orderedInterruption"))
            {
                bool orderedInterruption = false;
                if (args["orderedInterruption"] is bool b) orderedInterruption = b;
                else bool.TryParse(args["orderedInterruption"].ToString(), out orderedInterruption);
                transition.orderedInterruption = orderedInterruption;
                modified = true;
            }
            
            // Modify canTransitionToSelf
            if (args.ContainsKey("canTransitionToSelf"))
            {
                bool canTransitionToSelf = false;
                if (args["canTransitionToSelf"] is bool b) canTransitionToSelf = b;
                else bool.TryParse(args["canTransitionToSelf"].ToString(), out canTransitionToSelf);
                transition.canTransitionToSelf = canTransitionToSelf;
                modified = true;
            }
            
            // Modify solo
            if (args.ContainsKey("solo"))
            {
                bool solo = false;
                if (args["solo"] is bool b) solo = b;
                else bool.TryParse(args["solo"].ToString(), out solo);
                transition.solo = solo;
                modified = true;
            }
            
            // Modify mute
            if (args.ContainsKey("mute"))
            {
                bool mute = false;
                if (args["mute"] is bool b) mute = b;
                else bool.TryParse(args["mute"].ToString(), out mute);
                transition.mute = mute;
                modified = true;
            }
            
            if (modified)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "modified", modified }
            };
            
            return ToolUtils.CreateSuccessResponse(modified ? $"Modified transition from '{fromState}' to '{toState}'" : $"No changes made to transition", extras);
        }
    }
}
