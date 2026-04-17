using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class ModifyBlendTreeChildTool : ITool
    {
        public string Name => "modify_blend_tree_child";

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
            
            if (childIndex < 0)
                return ToolUtils.CreateErrorResponse("childIndex is required");
            
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
            
            if (childIndex >= blendTree.children.Length)
                return ToolUtils.CreateErrorResponse($"Child index {childIndex} out of range. Blend tree has {blendTree.children.Length} child(ren)");
            
            // Record for undo BEFORE modifications
            Undo.RecordObject(blendTree, $"Modify Blend Tree Child: {stateName}");
            
            // ChildMotion is a struct, so we need to get, modify, and reassign
            var children = blendTree.children;
            var child = children[childIndex];
            bool modified = false;
            
            // Parse threshold (1D only)
            if (args.ContainsKey("threshold"))
            {
                if (blendTree.blendType == BlendTreeType.Simple1D)
                {
                    float threshold = 0f;
                    if (args["threshold"] is float f) threshold = f;
                    else float.TryParse(args["threshold"].ToString(), 
                        System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out threshold);
                    
                    if (child.threshold != threshold)
                    {
                        child.threshold = threshold;
                        modified = true;
                    }
                }
            }
            
            // Parse positionX/positionY (2D only)
            if (args.ContainsKey("positionX") || args.ContainsKey("positionY"))
            {
                if (blendTree.blendType == BlendTreeType.SimpleDirectional2D || 
                    blendTree.blendType == BlendTreeType.FreeformDirectional2D || 
                    blendTree.blendType == BlendTreeType.FreeformCartesian2D)
                {
                    Vector2 newPos = child.position;
                    bool posModified = false;
                    
                    if (args.ContainsKey("positionX"))
                    {
                        float positionX = 0f;
                        if (args["positionX"] is float f) positionX = f;
                        else float.TryParse(args["positionX"].ToString(), 
                            System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out positionX);
                        
                        if (newPos.x != positionX)
                        {
                            newPos.x = positionX;
                            posModified = true;
                        }
                    }
                    
                    if (args.ContainsKey("positionY"))
                    {
                        float positionY = 0f;
                        if (args["positionY"] is float f) positionY = f;
                        else float.TryParse(args["positionY"].ToString(), 
                            System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out positionY);
                        
                        if (newPos.y != positionY)
                        {
                            newPos.y = positionY;
                            posModified = true;
                        }
                    }
                    
                    if (posModified)
                    {
                        child.position = newPos;
                        modified = true;
                    }
                }
            }
            
            // Parse mirror
            if (args.ContainsKey("mirror"))
            {
                bool mirror = false;
                if (args["mirror"] is bool b) mirror = b;
                else bool.TryParse(args["mirror"].ToString(), out mirror);
                
                if (child.mirror != mirror)
                {
                    child.mirror = mirror;
                    modified = true;
                }
            }
            
            // Parse cycleOffset
            if (args.ContainsKey("cycleOffset"))
            {
                float cycleOffset = 0f;
                if (args["cycleOffset"] is float f) cycleOffset = f;
                else float.TryParse(args["cycleOffset"].ToString(), 
                    System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out cycleOffset);
                
                if (child.cycleOffset != cycleOffset)
                {
                    child.cycleOffset = cycleOffset;
                    modified = true;
                }
            }
            
            // Parse directBlendParameter
            if (args.ContainsKey("directBlendParameter"))
            {
                string directBlendParameter = args["directBlendParameter"].ToString();
                
                if (child.directBlendParameter != directBlendParameter)
                {
                    child.directBlendParameter = directBlendParameter;
                    modified = true;
                }
            }
            
            // Reassign modified child back to array
            if (modified)
            {
                children[childIndex] = child;
                blendTree.children = children;
                
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "modified", modified },
                { "childIndex", childIndex }
            };
            
            return ToolUtils.CreateSuccessResponse(
                modified ? $"Modified child at index {childIndex} in blend tree '{stateName}'" : $"No changes made to child at index {childIndex}",
                extras);
        }
    }
}
