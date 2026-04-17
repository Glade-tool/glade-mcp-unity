using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AddAnimatorTransitionTool : ITool
    {
        public string Name => "add_animator_transition";

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
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            // Find states
            AnimatorState sourceState = null;
            AnimatorState destState = null;
            bool isAnyState = fromState.ToLower() == "any state";
            
            foreach (var s in stateMachine.states)
            {
                if (!isAnyState && s.state.name == fromState)
                    sourceState = s.state;
                if (s.state.name == toState)
                    destState = s.state;
            }
            
            if (!isAnyState && sourceState == null)
                return ToolUtils.CreateErrorResponse($"Source state '{fromState}' not found");
            if (destState == null)
                return ToolUtils.CreateErrorResponse($"Destination state '{toState}' not found");
            
            // Create transition
            AnimatorStateTransition transition;
            if (isAnyState)
            {
                var existing = stateMachine.anyStateTransitions
                    .Where(t => t.destinationState == destState)
                    .ToList();
                foreach (var t in existing)
                    stateMachine.RemoveAnyStateTransition(t);
                transition = stateMachine.AddAnyStateTransition(destState);
            }
            else
            {
                var existing = sourceState.transitions
                    .Where(t => t.destinationState == destState)
                    .ToList();
                foreach (var t in existing)
                    sourceState.RemoveTransition(t);
                transition = sourceState.AddTransition(destState);
            }
            
            // Configure transition
            bool hasExitTime = false;
            if (args.ContainsKey("hasExitTime"))
            {
                if (args["hasExitTime"] is bool b) hasExitTime = b;
                else bool.TryParse(args["hasExitTime"].ToString(), out hasExitTime);
            }
            transition.hasExitTime = hasExitTime;
            
            if (args.ContainsKey("exitTime"))
            {
                float exitTime = 1f;
                if (args["exitTime"] is float f) exitTime = f;
                else float.TryParse(args["exitTime"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out exitTime);
                transition.exitTime = exitTime;
            }
            
            if (args.ContainsKey("duration"))
            {
                float duration = 0.25f;
                if (args["duration"] is float f) duration = f;
                else float.TryParse(args["duration"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out duration);
                transition.duration = duration;
            }
            
            // Add conditions
            int conditionsAdded = 0;
            var conditionsSummary = new List<string>();
            if (args.ContainsKey("conditions"))
            {
                var conditionsObj = args["conditions"];
                conditionsAdded = AddAnimatorTransitionConditionsInternal(controller, transition, conditionsObj, conditionsSummary);
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            string conditionsMsg = conditionsAdded > 0 ? $" with {conditionsAdded} condition(s): {string.Join(", ", conditionsSummary)}" : " (no conditions)";
            
            var extras = new Dictionary<string, object>
            {
                { "conditionsAdded", conditionsAdded }
            };
            
            return ToolUtils.CreateSuccessResponse($"Added transition from '{fromState}' to '{toState}'{conditionsMsg}", extras);
        }
        
        private static int AddAnimatorTransitionConditionsInternal(AnimatorController controller, AnimatorStateTransition transition, object conditionsObj, List<string> conditionsSummary)
        {
            if (transition == null || conditionsObj == null) return 0;

            IEnumerable<object> conditionList = null;
            if (conditionsObj is List<object> list)
                conditionList = list;
            else if (conditionsObj is object[] arr)
                conditionList = arr;
            else if (conditionsObj is string conditionsJson && ToolUtils.TryParseJsonArrayToList(conditionsJson, out var parsedList))
                conditionList = parsedList;
            else if (conditionsObj is System.Collections.IEnumerable enumerable)
                conditionList = enumerable.Cast<object>();

            if (conditionList == null) return 0;

            int conditionsAdded = 0;
            foreach (var condObj in conditionList)
            {
                Dictionary<string, object> cond = null;
                if (condObj is Dictionary<string, object> dict)
                    cond = dict;
                else if (condObj is System.Collections.IDictionary idict)
                    cond = idict.Keys.Cast<object>().ToDictionary(k => k.ToString(), k => idict[k]);

                if (cond == null) continue;

                string paramName = cond.ContainsKey("parameter") ? cond["parameter"]?.ToString() ?? "" : "";
                string modeStr = cond.ContainsKey("mode") ? cond["mode"]?.ToString() ?? "If" : "If";
                float threshold = 0f;
                if (cond.ContainsKey("threshold"))
                {
                    var thresholdVal = cond["threshold"];
                    if (thresholdVal is float f) threshold = f;
                    else if (thresholdVal is double d) threshold = (float)d;
                    else if (thresholdVal is int i) threshold = i;
                    else if (thresholdVal is long l) threshold = l;
                    else float.TryParse(thresholdVal?.ToString() ?? "0", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out threshold);
                }

                if (string.IsNullOrEmpty(paramName)) continue;

                AnimatorConditionMode mode = modeStr.ToLower() switch
                {
                    "ifnot" => AnimatorConditionMode.IfNot,
                    "greater" => AnimatorConditionMode.Greater,
                    "less" => AnimatorConditionMode.Less,
                    "equals" => AnimatorConditionMode.Equals,
                    "notequal" => AnimatorConditionMode.NotEqual,
                    _ => AnimatorConditionMode.If
                };

                EnsureAnimatorParameterExists(controller, paramName, mode, cond);

                transition.AddCondition(mode, threshold, paramName);
                conditionsAdded++;
                conditionsSummary.Add($"{paramName} {modeStr} {threshold}");
            }

            return conditionsAdded;
        }
        
        private static void EnsureAnimatorParameterExists(AnimatorController controller, string paramName, AnimatorConditionMode mode, Dictionary<string, object> cond)
        {
            if (controller == null || string.IsNullOrEmpty(paramName)) return;

            foreach (var p in controller.parameters)
            {
                if (p.name == paramName)
                    return;
            }

            AnimatorControllerParameterType paramType = GuessParameterType(mode, cond);
            controller.AddParameter(paramName, paramType);

            // Set a sensible default for non-trigger params
            if (paramType != AnimatorControllerParameterType.Trigger)
            {
                var parameters = controller.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].name == paramName)
                    {
                        switch (paramType)
                        {
                            case AnimatorControllerParameterType.Float:
                                parameters[i].defaultFloat = 0f;
                                break;
                            case AnimatorControllerParameterType.Int:
                                parameters[i].defaultInt = 0;
                                break;
                            case AnimatorControllerParameterType.Bool:
                                parameters[i].defaultBool = false;
                                break;
                        }
                        controller.parameters = parameters;
                        break;
                    }
                }
            }
        }
        
        private static AnimatorControllerParameterType GuessParameterType(AnimatorConditionMode mode, Dictionary<string, object> cond)
        {
            if (cond != null)
            {
                if (cond.TryGetValue("type", out var typeObj) || cond.TryGetValue("parameterType", out typeObj))
                {
                    string typeStr = typeObj?.ToString() ?? "";
                    switch (typeStr.ToLower())
                    {
                        case "int":
                            return AnimatorControllerParameterType.Int;
                        case "bool":
                            return AnimatorControllerParameterType.Bool;
                        case "trigger":
                            return AnimatorControllerParameterType.Trigger;
                        default:
                            return AnimatorControllerParameterType.Float;
                    }
                }
            }

            return mode switch
            {
                AnimatorConditionMode.Greater => AnimatorControllerParameterType.Float,
                AnimatorConditionMode.Less => AnimatorControllerParameterType.Float,
                AnimatorConditionMode.Equals => AnimatorControllerParameterType.Int,
                AnimatorConditionMode.NotEqual => AnimatorControllerParameterType.Int,
                _ => AnimatorControllerParameterType.Bool
            };
        }
    }
}
