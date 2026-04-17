using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AddAnimatorStateTool : ITool
    {
        public string Name => "add_animator_state";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
            {
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            }
            
            if (string.IsNullOrEmpty(stateName))
            {
                return ToolUtils.CreateErrorResponse("stateName is required");
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
            
            int layerIndex = 0;
            if (args.ContainsKey("layerIndex"))
            {
                if (args["layerIndex"] is int i) layerIndex = i;
                else if (args["layerIndex"] is float f) layerIndex = (int)f;
                else int.TryParse(args["layerIndex"].ToString(), out layerIndex);
            }
            
            if (layerIndex >= controller.layers.Length)
            {
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range. Controller has {controller.layers.Length} layer(s).");
            }
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            // Check if state already exists
            foreach (var s in stateMachine.states)
            {
                if (s.state.name == stateName)
                {
                    var extras = new Dictionary<string, object>
                    {
                        { "stateName", stateName }
                    };
                    return ToolUtils.CreateSuccessResponse($"State '{stateName}' already exists", extras);
                }
            }
            
            // Add state
            Vector3 position = new Vector3(250, 0, 0);
            if (args.ContainsKey("position"))
            {
                var pos = ToolUtils.ParseVector3(args["position"].ToString());
                position = new Vector3(pos.x, pos.y, 0);
            }
            else
            {
                // Auto-position based on existing states
                position = new Vector3(250, stateMachine.states.Length * 50, 0);
            }
            
            var state = stateMachine.AddState(stateName, position);
            
            // Assign clip if provided
            if (args.ContainsKey("clipPath"))
            {
                string clipPath = args["clipPath"].ToString();
                if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    clipPath = "Assets/" + clipPath;
                    
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip != null)
                {
                    state.motion = clip;
                }
            }
            
            // Set as default if requested
            if (args.ContainsKey("isDefault"))
            {
                bool isDefault = false;
                if (args["isDefault"] is bool b) isDefault = b;
                else bool.TryParse(args["isDefault"].ToString(), out isDefault);
                
                if (isDefault)
                {
                    stateMachine.defaultState = state;
                }
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var resultExtras = new Dictionary<string, object>
            {
                { "stateName", stateName }
            };
            
            return ToolUtils.CreateSuccessResponse($"Added state '{stateName}' to layer {layerIndex}", resultExtras);
        }
    }
}
