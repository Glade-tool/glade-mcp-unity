using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class RemoveBlendTreeChildTool : ITool
    {
        public string Name => "remove_blend_tree_child";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required");
            
            int childIndex = -1;
            if (args.ContainsKey("childIndex"))
            {
                if (args["childIndex"] is int i) childIndex = i;
                else if (args["childIndex"] is float f) childIndex = (int)f;
                else int.TryParse(args["childIndex"].ToString(), out childIndex);
            }
            
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (childIndex < 0 && string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("Either childIndex or clipPath is required");
            
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
            Undo.RecordObject(blendTree, $"Remove Blend Tree Child: {stateName}");
            
            bool removed = false;
            int removedIndex = -1;
            
            if (childIndex >= 0)
            {
                // Remove by index
                if (childIndex >= blendTree.children.Length)
                    return ToolUtils.CreateErrorResponse($"Child index {childIndex} out of range. Blend tree has {blendTree.children.Length} child(ren)");
                
                blendTree.RemoveChild(childIndex);
                removed = true;
                removedIndex = childIndex;
            }
            else if (!string.IsNullOrEmpty(clipPath))
            {
                // Remove by clip path
                if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    clipPath = "Assets/" + clipPath;
                
                bool found = false;
                for (int i = 0; i < blendTree.children.Length; i++)
                {
                    var child = blendTree.children[i];
                    if (child.motion is AnimationClip clip)
                    {
                        string childPath = AssetDatabase.GetAssetPath(clip);
                        if (childPath.Equals(clipPath, StringComparison.OrdinalIgnoreCase))
                        {
                            blendTree.RemoveChild(i);
                            removed = true;
                            removedIndex = i;
                            found = true;
                            break;
                        }
                    }
                }
                
                if (!found)
                    return ToolUtils.CreateErrorResponse($"Clip '{clipPath}' not found in blend tree children");
            }
            
            // Save changes if removed
            if (removed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "removed", removed },
                { "childIndex", removedIndex }
            };
            
            return ToolUtils.CreateSuccessResponse(
                removed ? $"Removed child at index {removedIndex} from blend tree '{stateName}'" : $"Child not found in blend tree '{stateName}'",
                extras);
        }
    }
}
