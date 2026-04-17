using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetBlendTreeInfoTool : ITool
    {
        public string Name => "get_blend_tree_info";

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
            
            // Convert blend type enum to string
            string blendTypeStr = blendTree.blendType switch
            {
                BlendTreeType.Simple1D => "Simple1D",
                BlendTreeType.SimpleDirectional2D => "SimpleDirectional2D",
                BlendTreeType.FreeformDirectional2D => "FreeformDirectional2D",
                BlendTreeType.FreeformCartesian2D => "FreeformCartesian2D",
                _ => blendTree.blendType.ToString()
            };
            
            // Build children list
            var childrenList = new List<Dictionary<string, object>>();
            
            for (int i = 0; i < blendTree.children.Length; i++)
            {
                var child = blendTree.children[i];
                bool isBlendTree = child.motion is BlendTree;
                
                string motionPath = null;
                string motionName = "";
                
                if (child.motion != null)
                {
                    if (isBlendTree)
                    {
                        motionName = (child.motion as BlendTree)?.name ?? "BlendTree";
                    }
                    else if (child.motion is AnimationClip clip)
                    {
                        motionPath = AssetDatabase.GetAssetPath(clip);
                        motionName = clip.name;
                    }
                }
                
                var childDict = new Dictionary<string, object>
                {
                    { "index", i },
                    { "motionPath", motionPath ?? (object)null },
                    { "motionName", motionName },
                    { "isBlendTree", isBlendTree },
                    { "mirror", child.mirror },
                    { "cycleOffset", child.cycleOffset }
                };
                
                // Add type-specific properties
                if (blendTree.blendType == BlendTreeType.Simple1D)
                {
                    childDict["threshold"] = child.threshold;
                    childDict["positionX"] = null;
                    childDict["positionY"] = null;
                }
                else
                {
                    childDict["threshold"] = null;
                    childDict["positionX"] = child.position.x;
                    childDict["positionY"] = child.position.y;
                }
                
                // Add direct blend parameter if set
                if (!string.IsNullOrEmpty(child.directBlendParameter))
                {
                    childDict["directBlendParameter"] = child.directBlendParameter;
                }
                else
                {
                    childDict["directBlendParameter"] = null;
                }
                
                childrenList.Add(childDict);
            }
            
            // Build response extras
            var extras = new Dictionary<string, object>
            {
                { "blendType", blendTypeStr },
                { "blendParameter", blendTree.blendParameter },
                { "blendParameterY", blendTree.blendParameterY ?? (object)null },
                { "childCount", childrenList.Count },
                { "children", childrenList }
            };
            
            // Add threshold properties for 1D only
            if (blendTree.blendType == BlendTreeType.Simple1D)
            {
                extras["minThreshold"] = blendTree.minThreshold;
                extras["maxThreshold"] = blendTree.maxThreshold;
            }
            else
            {
                extras["minThreshold"] = null;
                extras["maxThreshold"] = null;
            }
            
            return ToolUtils.CreateSuccessResponse($"Retrieved blend tree info for '{stateName}'", extras);
        }
    }
}
