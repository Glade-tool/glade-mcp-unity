using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class ModifyBlendTreePropertiesTool : ITool
    {
        public string Name => "modify_blend_tree_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            
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
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            // Find the state
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
            
            if (targetState.motion == null || !(targetState.motion is BlendTree))
                return ToolUtils.CreateErrorResponse($"State '{stateName}' does not contain a BlendTree");
            
            BlendTree blendTree = targetState.motion as BlendTree;
            
            // Record for undo BEFORE modifications
            Undo.RecordObject(blendTree, $"Modify Blend Tree Properties: {stateName}");
            
            bool modified = false;
            
            // Parse blendType
            string blendTypeStr = args.ContainsKey("blendType") ? args["blendType"].ToString() : "";
            if (!string.IsNullOrEmpty(blendTypeStr))
            {
                BlendTreeType newBlendType = blendTypeStr.ToLower() switch
                {
                    "simple1d" => BlendTreeType.Simple1D,
                    "simpledirectional2d" => BlendTreeType.SimpleDirectional2D,
                    "freeformdirectional2d" => BlendTreeType.FreeformDirectional2D,
                    "freeformcartesian2d" => BlendTreeType.FreeformCartesian2D,
                    _ => blendTree.blendType // Keep existing if invalid
                };
                
                if (newBlendType != blendTree.blendType)
                {
                    blendTree.blendType = newBlendType;
                    modified = true;
                }
            }
            
            // Parse blendParameter
            string blendParameter = args.ContainsKey("blendParameter") ? args["blendParameter"].ToString() : "";
            if (!string.IsNullOrEmpty(blendParameter))
            {
                // Validate and add parameter if needed
                bool paramExists = false;
                foreach (var p in controller.parameters)
                {
                    if (p.name == blendParameter && p.type == AnimatorControllerParameterType.Float)
                    {
                        paramExists = true;
                        break;
                    }
                }
                
                if (!paramExists)
                {
                    controller.AddParameter(blendParameter, AnimatorControllerParameterType.Float);
                }
                
                if (blendTree.blendParameter != blendParameter)
                {
                    blendTree.blendParameter = blendParameter;
                    modified = true;
                }
            }
            
            // Parse blendParameterY (2D only)
            string blendParameterY = args.ContainsKey("blendParameterY") ? args["blendParameterY"].ToString() : "";
            if (!string.IsNullOrEmpty(blendParameterY))
            {
                // Validate and add parameter if needed
                bool paramExists = false;
                foreach (var p in controller.parameters)
                {
                    if (p.name == blendParameterY && p.type == AnimatorControllerParameterType.Float)
                    {
                        paramExists = true;
                        break;
                    }
                }
                
                if (!paramExists)
                {
                    controller.AddParameter(blendParameterY, AnimatorControllerParameterType.Float);
                }
                
                if (blendTree.blendParameterY != blendParameterY)
                {
                    blendTree.blendParameterY = blendParameterY;
                    modified = true;
                }
            }
            
            // Parse minThreshold (1D only)
            if (args.ContainsKey("minThreshold"))
            {
                float minThreshold = 0f;
                if (args["minThreshold"] is float f) minThreshold = f;
                else float.TryParse(args["minThreshold"].ToString(), 
                    System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out minThreshold);
                
                if (blendTree.minThreshold != minThreshold)
                {
                    blendTree.minThreshold = minThreshold;
                    modified = true;
                }
            }
            
            // Parse maxThreshold (1D only)
            if (args.ContainsKey("maxThreshold"))
            {
                float maxThreshold = 0f;
                if (args["maxThreshold"] is float f) maxThreshold = f;
                else float.TryParse(args["maxThreshold"].ToString(), 
                    System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out maxThreshold);
                
                if (blendTree.maxThreshold != maxThreshold)
                {
                    blendTree.maxThreshold = maxThreshold;
                    modified = true;
                }
            }
            
            // Modify useAutomaticThresholds
            if (args.ContainsKey("useAutomaticThresholds"))
            {
                bool useAutomaticThresholds = false;
                if (args["useAutomaticThresholds"] is bool b) useAutomaticThresholds = b;
                else bool.TryParse(args["useAutomaticThresholds"].ToString(), out useAutomaticThresholds);
                
                if (blendTree.useAutomaticThresholds != useAutomaticThresholds)
                {
                    blendTree.useAutomaticThresholds = useAutomaticThresholds;
                    modified = true;
                }
            }
            
            // Modify normalizedBlendValues (use SerializedObject as property may not be directly accessible)
            if (args.ContainsKey("normalizedBlendValues"))
            {
                bool normalizedBlendValues = false;
                if (args["normalizedBlendValues"] is bool b) normalizedBlendValues = b;
                else bool.TryParse(args["normalizedBlendValues"].ToString(), out normalizedBlendValues);
                
                // Use SerializedObject to access normalizedBlendValues
                SerializedObject serializedBlendTree = new SerializedObject(blendTree);
                SerializedProperty normalizedProp = serializedBlendTree.FindProperty("m_NormalizedBlendValues");
                if (normalizedProp != null && normalizedProp.boolValue != normalizedBlendValues)
                {
                    normalizedProp.boolValue = normalizedBlendValues;
                    serializedBlendTree.ApplyModifiedProperties();
                    modified = true;
                }
            }
            
            // Save changes if modified
            if (modified)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "modified", modified }
            };
            
            return ToolUtils.CreateSuccessResponse(
                modified ? $"Modified blend tree properties for '{stateName}'" : $"No changes made to '{stateName}'", 
                extras);
        }
    }
}
