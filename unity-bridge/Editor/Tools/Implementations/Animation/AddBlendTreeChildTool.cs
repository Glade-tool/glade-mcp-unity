using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AddBlendTreeChildTool : ITool
    {
        public string Name => "add_blend_tree_child";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required (the blend tree state name)");
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return ToolUtils.CreateErrorResponse($"Animation clip not found at '{clipPath}'");
            
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
            
            // Check blend tree type and add appropriately
            if (blendTree.blendType == BlendTreeType.Simple1D)
            {
                float threshold = 0f;
                if (args.ContainsKey("threshold"))
                {
                    if (args["threshold"] is float f) threshold = f;
                    else float.TryParse(args["threshold"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out threshold);
                }
                blendTree.AddChild(clip, threshold);
            }
            else
            {
                // 2D blend tree
                float posX = 0f, posY = 0f;
                if (args.ContainsKey("positionX"))
                {
                    if (args["positionX"] is float f) posX = f;
                    else float.TryParse(args["positionX"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out posX);
                }
                if (args.ContainsKey("positionY"))
                {
                    if (args["positionY"] is float f) posY = f;
                    else float.TryParse(args["positionY"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out posY);
                }
                blendTree.AddChild(clip, new Vector2(posX, posY));
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "stateName", stateName },
                { "clipPath", clipPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Added motion to BlendTree '{stateName}'", extras);
        }
    }
}
