using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimatorParameterInfoTool : ITool
    {
        public string Name => "get_animator_parameter_info";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string parameterName = args.ContainsKey("parameterName") ? args["parameterName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            if (string.IsNullOrEmpty(parameterName))
                return ToolUtils.CreateErrorResponse("parameterName is required");
            
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            // Find parameter by name
            AnimatorControllerParameter parameter = null;
            foreach (var param in controller.parameters)
            {
                if (param.name == parameterName)
                {
                    parameter = param;
                    break;
                }
            }
            
            if (parameter == null)
                return ToolUtils.CreateErrorResponse($"Parameter '{parameterName}' not found in controller");
            
            // Convert type to string
            string typeStr = parameter.type.ToString();
            
            // Get default value based on type
            object defaultBool = null;
            object defaultFloat = null;
            object defaultInt = null;
            
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    defaultBool = parameter.defaultBool;
                    break;
                case AnimatorControllerParameterType.Float:
                    defaultFloat = parameter.defaultFloat;
                    break;
                case AnimatorControllerParameterType.Int:
                    defaultInt = parameter.defaultInt;
                    break;
                case AnimatorControllerParameterType.Trigger:
                    // Triggers don't have default values
                    break;
            }
            
            var extras = new Dictionary<string, object>
            {
                { "name", parameter.name },
                { "type", typeStr },
                { "defaultBool", defaultBool },
                { "defaultFloat", defaultFloat },
                { "defaultInt", defaultInt }
            };
            
            return ToolUtils.CreateSuccessResponse($"Retrieved parameter info for '{parameterName}'", extras);
        }
    }
}
