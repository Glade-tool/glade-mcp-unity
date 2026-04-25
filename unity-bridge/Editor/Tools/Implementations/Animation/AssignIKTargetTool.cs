using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AssignIKTargetTool : ITool
    {
        public string Name => "assign_ik_target";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string targetPath = args.ContainsKey("targetPath") ? args["targetPath"].ToString() : "";
            string ikGoalStr = args.ContainsKey("ikGoal") ? args["ikGoal"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            if (string.IsNullOrEmpty(targetPath))
                return ToolUtils.CreateErrorResponse("targetPath is required");
            
            if (string.IsNullOrEmpty(ikGoalStr))
                return ToolUtils.CreateErrorResponse("ikGoal is required");
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            UnityEngine.GameObject targetObj = ToolUtils.FindGameObjectByPath(targetPath);
            if (targetObj == null)
                return ToolUtils.CreateErrorResponse($"Target GameObject '{targetPath}' not found");
            
            // Find IKController component. If the GameObject has no Animator at all,
            // this is almost certainly a misroute (model meant set_game_object_parent
            // for "attach X to Y" / "parent X under Y") — surface that explicitly so
            // the retry doesn't go down the rabbit hole of creating an IKController.
            var ikController = obj.GetComponent("IKController");
            if (ikController == null)
            {
                bool hasAnimator = obj.GetComponent<UnityEngine.Animator>() != null;
                if (!hasAnimator)
                {
                    return ToolUtils.CreateErrorResponse(
                        $"'{gameObjectPath}' has no Animator, so it cannot host an IKController. " +
                        $"If you intended to make '{targetPath}' a child of '{gameObjectPath}' " +
                        $"(reparenting / 'attach to' / 'put on top of'), call set_game_object_parent " +
                        $"with gameObjectPath='{targetPath}' and parentPath='{gameObjectPath}' instead.");
                }
                return ToolUtils.CreateErrorResponse($"IKController component not found on '{gameObjectPath}'. Create it first using create_ik_controller_script and add_component.");
            }
            
            // Set the target field using reflection
            AvatarIKGoal goal = IKUtils.ParseIKGoal(ikGoalStr);
            string fieldName = IKUtils.GetIKGoalFieldName(goal);
            
            if (string.IsNullOrEmpty(fieldName))
            {
                // Check if it's a hint goal (elbow/knee)
                if (IKUtils.IsHintGoal(ikGoalStr))
                {
                    fieldName = IKUtils.GetIKHintTargetFieldName(ikGoalStr);
                }
                
                if (string.IsNullOrEmpty(fieldName))
                {
                    return ToolUtils.CreateErrorResponse($"Invalid IK goal: {ikGoalStr}");
                }
            }
            
            var field = ikController.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                Undo.RecordObject(ikController, $"Assign IK Target: {ikGoalStr}");
                field.SetValue(ikController, targetObj.transform);
                EditorUtility.SetDirty(ikController);
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Field '{fieldName}' not found on IKController component");
            }
            
            var extras = new Dictionary<string, object>
            {
                { "ikGoal", ikGoalStr },
                { "targetPath", targetPath },
                { "gameObjectPath", gameObjectPath } // For revert system
            };
            
            return ToolUtils.CreateSuccessResponse($"Assigned '{targetPath}' as {ikGoalStr} IK target", extras);
        }
    }
}
