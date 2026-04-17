using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveAnimatorTransitionTool : ITool
    {
        public string Name => "remove_animator_transition";

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
            
            bool removeAll = false;
            if (args.ContainsKey("removeAll"))
            {
                if (args["removeAll"] is bool b) removeAll = b;
                else bool.TryParse(args["removeAll"].ToString(), out removeAll);
            }
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            bool isAnyState = string.Equals(fromState, "any state", StringComparison.OrdinalIgnoreCase);
            
            AnimatorState destState = null;
            foreach (var s in stateMachine.states)
            {
                if (s.state.name == toState)
                {
                    destState = s.state;
                    break;
                }
            }
            
            if (destState == null)
                return ToolUtils.CreateErrorResponse($"Destination state '{toState}' not found");
            
            var conditionSpecs = new List<(string parameter, AnimatorConditionMode mode, bool hasThreshold, float threshold)>();
            if (args.ContainsKey("conditions"))
            {
                var conditionsObj = args["conditions"];
                if (conditionsObj is string conditionsJson && ToolUtils.TryParseJsonArrayToList(conditionsJson, out var parsedConditions))
                    conditionsObj = parsedConditions;
                if (conditionsObj is List<object> conditionList)
                {
                    foreach (var condObj in conditionList)
                    {
                        if (condObj is Dictionary<string, object> cond)
                        {
                            string paramName = cond.ContainsKey("parameter") ? cond["parameter"].ToString() : "";
                            string modeStr = cond.ContainsKey("mode") ? cond["mode"].ToString() : "If";
                            if (string.IsNullOrEmpty(paramName))
                                continue;
                            
                            AnimatorConditionMode mode = modeStr.ToLower() switch
                            {
                                "ifnot" => AnimatorConditionMode.IfNot,
                                "greater" => AnimatorConditionMode.Greater,
                                "less" => AnimatorConditionMode.Less,
                                "equals" => AnimatorConditionMode.Equals,
                                "notequal" => AnimatorConditionMode.NotEqual,
                                _ => AnimatorConditionMode.If
                            };
                            
                            bool hasThreshold = false;
                            float threshold = 0f;
                            if (cond.ContainsKey("threshold"))
                            {
                                hasThreshold = true;
                                if (cond["threshold"] is float f) threshold = f;
                                else float.TryParse(cond["threshold"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out threshold);
                            }
                            
                            conditionSpecs.Add((paramName, mode, hasThreshold, threshold));
                        }
                    }
                }
            }
            
            bool HasMatchingConditions(AnimatorStateTransition transition)
            {
                if (conditionSpecs.Count == 0)
                    return true;
                
                if (transition.conditions == null || transition.conditions.Length == 0)
                    return false;
                
                foreach (var spec in conditionSpecs)
                {
                    bool found = false;
                    foreach (var cond in transition.conditions)
                    {
                        if (cond.parameter == spec.parameter && cond.mode == spec.mode)
                        {
                            if (!spec.hasThreshold || Mathf.Approximately(cond.threshold, spec.threshold))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    
                    if (!found)
                        return false;
                }
                
                return true;
            }
            
            int removedCount = 0;
            if (isAnyState)
            {
                var toRemove = new List<AnimatorStateTransition>();
                foreach (var transition in stateMachine.anyStateTransitions)
                {
                    if (transition.destinationState == destState && HasMatchingConditions(transition))
                    {
                        toRemove.Add(transition);
                        if (!removeAll)
                            break;
                    }
                }
                
                foreach (var transition in toRemove)
                {
                    stateMachine.RemoveAnyStateTransition(transition);
                    removedCount++;
                }
            }
            else
            {
                AnimatorState sourceState = null;
                foreach (var s in stateMachine.states)
                {
                    if (s.state.name == fromState)
                    {
                        sourceState = s.state;
                        break;
                    }
                }
                
                if (sourceState == null)
                    return ToolUtils.CreateErrorResponse($"Source state '{fromState}' not found");
                
                var toRemove = new List<AnimatorStateTransition>();
                foreach (var transition in sourceState.transitions)
                {
                    if (transition.destinationState == destState && HasMatchingConditions(transition))
                    {
                        toRemove.Add(transition);
                        if (!removeAll)
                            break;
                    }
                }
                
                foreach (var transition in toRemove)
                {
                    sourceState.RemoveTransition(transition);
                    removedCount++;
                }
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            if (removedCount == 0)
            {
                return ToolUtils.CreateErrorResponse($"No matching transitions found from '{fromState}' to '{toState}'");
            }
            
            var extras = new Dictionary<string, object>
            {
                { "removed", removedCount }
            };
            
            return ToolUtils.CreateSuccessResponse($"Removed {removedCount} transition(s) from '{fromState}' to '{toState}'", extras);
        }
    }
}
