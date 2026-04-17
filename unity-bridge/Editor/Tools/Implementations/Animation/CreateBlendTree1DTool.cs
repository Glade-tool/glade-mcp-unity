using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateBlendTree1DTool : ITool
    {
        public string Name => "create_blend_tree_1d";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            string parameterName = args.ContainsKey("parameterName") ? args["parameterName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required");
            if (string.IsNullOrEmpty(parameterName))
                return ToolUtils.CreateErrorResponse("parameterName is required");
            if (!args.ContainsKey("motions"))
                return ToolUtils.CreateErrorResponse("motions is required");
            
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
            
            // Ensure parameter exists
            bool paramExists = false;
            foreach (var p in controller.parameters)
            {
                if (p.name == parameterName && p.type == AnimatorControllerParameterType.Float)
                {
                    paramExists = true;
                    break;
                }
            }
            
            if (!paramExists)
            {
                controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
            }
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            // Create blend tree
            BlendTree blendTree;
            var state = controller.CreateBlendTreeInController(stateName, out blendTree, layerIndex);
            
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = parameterName;
            
            // Add motions
            var motionsObj = args["motions"];
            int motionCount = 0;
            
            if (motionsObj is List<object> motionList)
            {
                foreach (var motionObj in motionList)
                {
                    if (motionObj is Dictionary<string, object> motion)
                    {
                        string clipPath = motion.ContainsKey("clipPath") ? motion["clipPath"].ToString() : "";
                        float threshold = 0f;
                        if (motion.ContainsKey("threshold"))
                        {
                            if (motion["threshold"] is float f) threshold = f;
                            else float.TryParse(motion["threshold"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out threshold);
                        }
                        
                        if (!string.IsNullOrEmpty(clipPath))
                        {
                            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                                clipPath = "Assets/" + clipPath;
                                
                            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                            if (clip != null)
                            {
                                blendTree.AddChild(clip, threshold);
                                motionCount++;
                            }
                        }
                    }
                }
            }
            
            // Set as default if requested
            bool isDefault = true;
            if (args.ContainsKey("isDefault"))
            {
                if (args["isDefault"] is bool b) isDefault = b;
                else bool.TryParse(args["isDefault"].ToString(), out isDefault);
            }
            
            if (isDefault)
            {
                stateMachine.defaultState = state;
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "stateName", stateName },
                { "motionCount", motionCount }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created 1D BlendTree '{stateName}' with {motionCount} motion(s)", extras);
        }
    }
}
