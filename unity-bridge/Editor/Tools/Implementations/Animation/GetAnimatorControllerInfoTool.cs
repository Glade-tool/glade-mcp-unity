using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimatorControllerInfoTool : ITool
    {
        public string Name => "get_animator_controller_info";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            // Load controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            // Get layers info
            List<Dictionary<string, object>> layers = new List<Dictionary<string, object>>();
            int totalStateCount = 0;
            int totalTransitionCount = 0;
            
            for (int i = 0; i < controller.layers.Length; i++)
            {
                var layer = controller.layers[i];
                var stateMachine = layer.stateMachine;
                
                int layerStateCount = stateMachine.states.Length;
                int layerTransitionCount = 0;
                
                // Count transitions
                foreach (var state in stateMachine.states)
                {
                    layerTransitionCount += state.state.transitions.Length;
                }
                
                totalStateCount += layerStateCount;
                totalTransitionCount += layerTransitionCount;
                
                layers.Add(new Dictionary<string, object>
                {
                    { "index", i },
                    { "name", layer.name },
                    { "stateCount", layerStateCount },
                    { "transitionCount", layerTransitionCount },
                    { "defaultWeight", layer.defaultWeight },
                    { "blendingMode", layer.blendingMode.ToString() }
                });
            }
            
            // Get parameters info
            List<Dictionary<string, object>> parameters = new List<Dictionary<string, object>>();
            foreach (var param in controller.parameters)
            {
                parameters.Add(new Dictionary<string, object>
                {
                    { "name", param.name },
                    { "type", param.type.ToString() },
                    { "defaultBool", param.type == AnimatorControllerParameterType.Bool ? param.defaultBool : (object)null },
                    { "defaultFloat", param.type == AnimatorControllerParameterType.Float ? param.defaultFloat : (object)null },
                    { "defaultInt", param.type == AnimatorControllerParameterType.Int ? param.defaultInt : (object)null }
                });
            }
            
            var extras = new Dictionary<string, object>
            {
                { "layers", layers },
                { "parameters", parameters },
                { "stateCount", totalStateCount },
                { "transitionCount", totalTransitionCount }
            };
            
            return ToolUtils.CreateSuccessResponse($"Retrieved Animator Controller info: {layers.Count} layer(s), {parameters.Count} parameter(s)", extras);
        }
    }
}
