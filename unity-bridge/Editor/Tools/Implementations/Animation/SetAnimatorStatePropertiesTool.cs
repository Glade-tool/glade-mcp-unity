using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimatorStatePropertiesTool : ITool
    {
        public string Name => "set_animator_state_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required");
            
            // Ensure path starts with Assets/
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            // Load controller
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
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range. Controller has {controller.layers.Length} layer(s).");
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            // Find state by name
            AnimatorState state = null;
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == stateName)
                {
                    state = childState.state;
                    break;
                }
            }
            
            if (state == null)
                return ToolUtils.CreateErrorResponse($"State '{stateName}' not found in layer {layerIndex}");
            
            bool modified = false;
            
            // Record for undo
            Undo.RecordObject(state, $"Set Animator State Properties: {stateName}");
            
            // Modify speed
            if (args.ContainsKey("speed"))
            {
                float speed = 1f;
                if (args["speed"] is float f) speed = f;
                else float.TryParse(args["speed"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out speed);
                
                state.speed = speed;
                modified = true;
            }
            
            // Modify mirror
            if (args.ContainsKey("mirror"))
            {
                bool mirror = false;
                if (args["mirror"] is bool b) mirror = b;
                else bool.TryParse(args["mirror"].ToString(), out mirror);
                
                state.mirror = mirror;
                modified = true;
            }
            
            // Modify cycleOffset
            if (args.ContainsKey("cycleOffset"))
            {
                float cycleOffset = 0f;
                if (args["cycleOffset"] is float f) cycleOffset = f;
                else float.TryParse(args["cycleOffset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cycleOffset);
                
                state.cycleOffset = cycleOffset;
                modified = true;
            }
            
            // Modify writeDefaultValues
            if (args.ContainsKey("writeDefaultValues"))
            {
                bool writeDefaultValues = false;
                if (args["writeDefaultValues"] is bool b) writeDefaultValues = b;
                else bool.TryParse(args["writeDefaultValues"].ToString(), out writeDefaultValues);
                state.writeDefaultValues = writeDefaultValues;
                modified = true;
            }
            
            // Modify iKOnFeet
            if (args.ContainsKey("iKOnFeet"))
            {
                bool iKOnFeet = false;
                if (args["iKOnFeet"] is bool b) iKOnFeet = b;
                else bool.TryParse(args["iKOnFeet"].ToString(), out iKOnFeet);
                state.iKOnFeet = iKOnFeet;
                modified = true;
            }
            
            // Modify tag
            if (args.ContainsKey("tag"))
            {
                state.tag = args["tag"].ToString();
                modified = true;
            }
            
            // Modify motionPath
            if (args.ContainsKey("motionPath"))
            {
                string motionPath = args["motionPath"].ToString();
                if (!motionPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    motionPath = "Assets/" + motionPath;
                
                Motion motion = AssetDatabase.LoadAssetAtPath<Motion>(motionPath);
                if (motion != null)
                {
                    state.motion = motion;
                    modified = true;
                }
            }
            
            // Modify timeParameter
            if (args.ContainsKey("timeParameter"))
            {
                state.timeParameter = args["timeParameter"].ToString();
                modified = true;
            }
            
            // Modify timeParameterActive
            if (args.ContainsKey("timeParameterActive"))
            {
                bool timeParameterActive = false;
                if (args["timeParameterActive"] is bool b) timeParameterActive = b;
                else bool.TryParse(args["timeParameterActive"].ToString(), out timeParameterActive);
                state.timeParameterActive = timeParameterActive;
                modified = true;
            }
            
            if (modified)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            
            var extras = new Dictionary<string, object>
            {
                { "modified", modified }
            };
            
            return ToolUtils.CreateSuccessResponse(modified ? $"Modified state '{stateName}' properties" : $"No changes made to state '{stateName}'", extras);
        }
    }
}
