using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimatorParameterTool : ITool
    {
        public string Name => "set_animator_parameter";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string parameterName = args.ContainsKey("parameterName") ? args["parameterName"].ToString() : "";
            string parameterType = args.ContainsKey("parameterType") ? args["parameterType"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            if (string.IsNullOrEmpty(parameterName))
                return ToolUtils.CreateErrorResponse("parameterName is required");
            
            if (string.IsNullOrEmpty(parameterType))
                return ToolUtils.CreateErrorResponse("parameterType is required");
            
            // Find GameObject and Animator component
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            Animator animator = obj.GetComponent<Animator>();
            if (animator == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have an Animator component");
            
            // Validate parameter exists in controller
            if (animator.runtimeAnimatorController == null)
                return ToolUtils.CreateErrorResponse($"Animator on '{gameObjectPath}' has no runtime controller assigned");
            
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                // Try to get from asset path if it's a RuntimeAnimatorController
                return ToolUtils.CreateErrorResponse($"Animator controller is not an AnimatorController asset");
            }
            
            // Check if parameter exists
            bool parameterExists = false;
            AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Float;
            foreach (var param in controller.parameters)
            {
                if (param.name == parameterName)
                {
                    parameterExists = true;
                    paramType = param.type;
                    break;
                }
            }
            
            if (!parameterExists)
                return ToolUtils.CreateErrorResponse($"Parameter '{parameterName}' not found in animator controller");
            
            // Validate parameter type matches
            AnimatorControllerParameterType expectedType = parameterType.ToLower() switch
            {
                "bool" => AnimatorControllerParameterType.Bool,
                "int" => AnimatorControllerParameterType.Int,
                "trigger" => AnimatorControllerParameterType.Trigger,
                _ => AnimatorControllerParameterType.Float
            };
            
            if (paramType != expectedType)
                return ToolUtils.CreateErrorResponse($"Parameter '{parameterName}' is of type '{paramType}', not '{expectedType}'");
            
            // Record Undo BEFORE modifications
            Undo.RecordObject(animator, $"Set Animator Parameter: {parameterName}");
            
            // Call appropriate Animator method
            string resultMessage = "";
            object resultValue = null;
            
            switch (parameterType.ToLower())
            {
                case "bool":
                    if (!args.ContainsKey("value"))
                        return ToolUtils.CreateErrorResponse("value is required for Bool parameter");
                    
                    bool boolValue = false;
                    if (args["value"] is bool b) boolValue = b;
                    else if (!bool.TryParse(args["value"].ToString(), out boolValue))
                        return ToolUtils.CreateErrorResponse($"Invalid bool value: {args["value"]}");
                    
                    animator.SetBool(parameterName, boolValue);
                    resultValue = boolValue;
                    resultMessage = $"Set bool parameter '{parameterName}' to {boolValue}";
                    break;
                
                case "float":
                    if (!args.ContainsKey("value"))
                        return ToolUtils.CreateErrorResponse("value is required for Float parameter");
                    
                    float floatValue = 0f;
                    if (args["value"] is float f) floatValue = f;
                    else if (args["value"] is double d) floatValue = (float)d;
                    else if (!float.TryParse(args["value"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out floatValue))
                        return ToolUtils.CreateErrorResponse($"Invalid float value: {args["value"]}");
                    
                    animator.SetFloat(parameterName, floatValue);
                    resultValue = floatValue;
                    resultMessage = $"Set float parameter '{parameterName}' to {floatValue}";
                    break;
                
                case "int":
                    if (!args.ContainsKey("value"))
                        return ToolUtils.CreateErrorResponse("value is required for Int parameter");
                    
                    int intValue = 0;
                    if (args["value"] is int i) intValue = i;
                    else if (args["value"] is float floatVal) intValue = (int)floatVal;
                    else if (!int.TryParse(args["value"].ToString(), out intValue))
                        return ToolUtils.CreateErrorResponse($"Invalid int value: {args["value"]}");
                    
                    animator.SetInteger(parameterName, intValue);
                    resultValue = intValue;
                    resultMessage = $"Set int parameter '{parameterName}' to {intValue}";
                    break;
                
                case "trigger":
                    bool resetTrigger = false;
                    if (args.ContainsKey("resetTrigger"))
                    {
                        if (args["resetTrigger"] is bool rt) resetTrigger = rt;
                        else bool.TryParse(args["resetTrigger"].ToString(), out resetTrigger);
                    }
                    
                    if (resetTrigger)
                    {
                        animator.ResetTrigger(parameterName);
                        resultMessage = $"Reset trigger parameter '{parameterName}'";
                    }
                    else
                    {
                        animator.SetTrigger(parameterName);
                        resultMessage = $"Set trigger parameter '{parameterName}'";
                    }
                    resultValue = !resetTrigger;
                    break;
                
                default:
                    return ToolUtils.CreateErrorResponse($"Invalid parameterType: {parameterType}. Must be 'Bool', 'Float', 'Int', or 'Trigger'");
            }
            
            var extras = new Dictionary<string, object>
            {
                { "parameterName", parameterName },
                { "parameterType", parameterType },
                { "value", resultValue }
            };
            
            return ToolUtils.CreateSuccessResponse(resultMessage, extras);
        }
    }
}
