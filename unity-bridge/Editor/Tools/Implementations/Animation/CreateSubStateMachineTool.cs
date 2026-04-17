using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateSubStateMachineTool : ITool
    {
        public string Name => "create_sub_state_machine";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string subStateMachineName = args.ContainsKey("subStateMachineName") ? args["subStateMachineName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(subStateMachineName))
                return ToolUtils.CreateErrorResponse("subStateMachineName is required");
            
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
            
            var parentStateMachine = controller.layers[layerIndex].stateMachine;
            
            // Navigate to parent sub-state machine if specified
            if (args.ContainsKey("parentPath"))
            {
                string parentPath = args["parentPath"].ToString();
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var pathParts = parentPath.Split('/');
                    foreach (var part in pathParts)
                    {
                        bool found = false;
                        foreach (var sm in parentStateMachine.stateMachines)
                        {
                            if (sm.stateMachine.name == part)
                            {
                                parentStateMachine = sm.stateMachine;
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            return ToolUtils.CreateErrorResponse($"Sub-state machine '{part}' not found in path '{parentPath}'");
                    }
                }
            }
            
            // Create the sub-state machine
            var newSubStateMachine = parentStateMachine.AddStateMachine(subStateMachineName);
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "subStateMachineName", subStateMachineName }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created sub-state machine '{subStateMachineName}'", extras);
        }
    }
}
