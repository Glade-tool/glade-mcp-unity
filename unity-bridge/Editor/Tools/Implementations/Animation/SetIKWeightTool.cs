using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetIKWeightTool : ITool
    {
        public string Name => "set_ik_weight";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string ikGoalStr = args.ContainsKey("ikGoal") ? args["ikGoal"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            if (string.IsNullOrEmpty(ikGoalStr))
                return ToolUtils.CreateErrorResponse("ikGoal is required");
            
            float positionWeight = 0f;
            float rotationWeight = 0f;
            
            if (args.ContainsKey("positionWeight"))
            {
                if (args["positionWeight"] is float f) positionWeight = f;
                else float.TryParse(args["positionWeight"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out positionWeight);
            }
            else
            {
                return ToolUtils.CreateErrorResponse("positionWeight is required");
            }
            
            if (args.ContainsKey("rotationWeight"))
            {
                if (args["rotationWeight"] is float f) rotationWeight = f;
                else float.TryParse(args["rotationWeight"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rotationWeight);
            }
            else
            {
                return ToolUtils.CreateErrorResponse("rotationWeight is required");
            }
            
            float hintWeight = 0f;
            if (args.ContainsKey("hintWeight"))
            {
                if (args["hintWeight"] is float f) hintWeight = f;
                else float.TryParse(args["hintWeight"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out hintWeight);
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            // Find or create IKController
            var ikController = obj.GetComponent("IKController");
            if (ikController == null)
            {
                return ToolUtils.CreateErrorResponse($"IKController component not found on '{gameObjectPath}'. Create it first using create_ik_controller_script and add_component.");
            }
            
            // Set weight field
            AvatarIKGoal goal = IKUtils.ParseIKGoal(ikGoalStr);
            string weightFieldName = IKUtils.GetIKWeightFieldName(goal);
            
            Undo.RecordObject(ikController, $"Set IK Weight: {ikGoalStr}");
            
            // Handle hint weights for elbows/knees
            if (IKUtils.IsHintGoal(ikGoalStr) && args.ContainsKey("hintWeight"))
            {
                string hintWeightFieldName = IKUtils.GetIKHintWeightFieldName(ikGoalStr);
                if (!string.IsNullOrEmpty(hintWeightFieldName))
                {
                    var hintField = ikController.GetType().GetField(hintWeightFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (hintField != null)
                    {
                        hintField.SetValue(ikController, hintWeight);
                    }
                }
            }
            
            if (string.IsNullOrEmpty(weightFieldName))
            {
                // Check if it's a hint goal (elbow/knee) - these don't have main weight fields
                if (IKUtils.IsHintGoal(ikGoalStr))
                {
                    // For hint goals, we only set hint weight (already done above)
                    EditorUtility.SetDirty(ikController);
                    var extras = new Dictionary<string, object>
                    {
                        { "ikGoal", ikGoalStr },
                        { "hintWeight", hintWeight }
                    };
                    return ToolUtils.CreateSuccessResponse($"Set IK hint weight for {ikGoalStr}: {hintWeight}", extras);
                }
                return ToolUtils.CreateErrorResponse($"Invalid IK goal: {ikGoalStr}");
            }
            
            var weightField = ikController.GetType().GetField(weightFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (weightField != null)
            {
                // Use positionWeight as the main weight (both position and rotation use same weight in our template)
                weightField.SetValue(ikController, positionWeight);
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Field '{weightFieldName}' not found on IKController component");
            }
            
            EditorUtility.SetDirty(ikController);
            
            var responseExtras = new Dictionary<string, object>
            {
                { "ikGoal", ikGoalStr },
                { "positionWeight", positionWeight },
                { "rotationWeight", rotationWeight },
                { "gameObjectPath", gameObjectPath } // For revert system
            };
            
            if (args.ContainsKey("hintWeight"))
            {
                responseExtras["hintWeight"] = hintWeight;
            }
            
            return ToolUtils.CreateSuccessResponse($"Set IK weights for {ikGoalStr}: position={positionWeight}, rotation={rotationWeight}", responseExtras);
        }
    }
}
