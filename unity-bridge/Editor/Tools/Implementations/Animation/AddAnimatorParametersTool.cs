using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AddAnimatorParametersTool : ITool
    {
        public string Name => "add_animator_parameters";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
            {
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            }
            
            // Accept both "parameterList" (new) and "parameters" (legacy) parameter names
            string paramsKey = args.ContainsKey("parameterList") ? "parameterList" : "parameters";
            if (!args.ContainsKey(paramsKey))
            {
                return ToolUtils.CreateErrorResponse("parameterList is required");
            }
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            // Load controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            }
            
            var parametersObj = args[paramsKey];
            var addedParams = new List<string>();

            if (parametersObj is string parametersJson)
            {
                if (ToolUtils.TryParseJsonArrayToList(parametersJson, out var parsedList))
                {
                    parametersObj = parsedList;
                }
            }
            
            if (parametersObj is List<object> paramList)
            {
                foreach (var paramObj in paramList)
                {
                    if (paramObj is Dictionary<string, object> param)
                    {
                        string paramName = param.ContainsKey("name") ? param["name"].ToString() : "";
                        // Accept both "paramType" (new) and "type" (legacy) parameter names
                        string paramType = param.ContainsKey("paramType") ? param["paramType"].ToString()
                            : param.ContainsKey("type") ? param["type"].ToString() : "Float";
                        string defaultValue = param.ContainsKey("defaultValue") ? param["defaultValue"].ToString() : "";
                        
                        if (string.IsNullOrEmpty(paramName)) continue;
                        
                        AnimatorControllerParameterType type = paramType.ToLower() switch
                        {
                            "int" => AnimatorControllerParameterType.Int,
                            "bool" => AnimatorControllerParameterType.Bool,
                            "trigger" => AnimatorControllerParameterType.Trigger,
                            _ => AnimatorControllerParameterType.Float
                        };
                        
                        // Check if parameter already exists
                        bool exists = false;
                        foreach (var p in controller.parameters)
                        {
                            if (p.name == paramName) { exists = true; break; }
                        }
                        
                        if (!exists)
                        {
                            controller.AddParameter(paramName, type);
                            
                            // Set default value
                            if (!string.IsNullOrEmpty(defaultValue))
                            {
                                var parameters = controller.parameters;
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    if (parameters[i].name == paramName)
                                    {
                                        switch (type)
                                        {
                                            case AnimatorControllerParameterType.Float:
                                                if (float.TryParse(defaultValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fVal))
                                                {
                                                    parameters[i].defaultFloat = fVal;
                                                }
                                                break;
                                            case AnimatorControllerParameterType.Int:
                                                if (int.TryParse(defaultValue, out int iVal))
                                                {
                                                    parameters[i].defaultInt = iVal;
                                                }
                                                break;
                                            case AnimatorControllerParameterType.Bool:
                                                parameters[i].defaultBool = defaultValue.ToLower() == "true";
                                                break;
                                        }
                                        controller.parameters = parameters;
                                        break;
                                    }
                                }
                            }
                            
                            addedParams.Add(paramName);
                        }
                    }
                }
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "addedCount", addedParams.Count }
            };
            
            return ToolUtils.CreateSuccessResponse($"Added {addedParams.Count} parameter(s) to controller", extras);
        }
    }
}
