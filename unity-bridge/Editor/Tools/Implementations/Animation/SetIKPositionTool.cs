using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetIKPositionTool : ITool
    {
        public string Name => "set_ik_position";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string ikGoalStr = args.ContainsKey("ikGoal") ? args["ikGoal"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            if (string.IsNullOrEmpty(ikGoalStr))
                return ToolUtils.CreateErrorResponse("ikGoal is required");
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            // Find IKController component to get target transform
            var ikController = obj.GetComponent("IKController");
            if (ikController == null)
            {
                return ToolUtils.CreateErrorResponse($"IKController component not found on '{gameObjectPath}'. Create it first using create_ik_controller_script and add_component.");
            }
            
            AvatarIKGoal goal = IKUtils.ParseIKGoal(ikGoalStr);
            string targetFieldName = IKUtils.GetIKGoalFieldName(goal);
            
            // Check if it's a hint goal (elbow/knee)
            if (string.IsNullOrEmpty(targetFieldName) && IKUtils.IsHintGoal(ikGoalStr))
            {
                targetFieldName = IKUtils.GetIKHintTargetFieldName(ikGoalStr);
            }
            
            if (string.IsNullOrEmpty(targetFieldName))
            {
                return ToolUtils.CreateErrorResponse($"Invalid IK goal: {ikGoalStr}");
            }
            
            var targetField = ikController.GetType().GetField(targetFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            UnityEngine.Transform target = targetField?.GetValue(ikController) as UnityEngine.Transform;
            
            if (target == null)
            {
                return ToolUtils.CreateErrorResponse($"IK target for {ikGoalStr} not assigned. Use assign_ik_target first.");
            }
            
            bool useWorldSpace = true;
            if (args.ContainsKey("useWorldSpace"))
            {
                if (args["useWorldSpace"] is bool b) useWorldSpace = b;
                else bool.TryParse(args["useWorldSpace"].ToString(), out useWorldSpace);
            }
            
            Undo.RecordObject(target, $"Set IK Position: {ikGoalStr}");
            
            if (args.ContainsKey("position"))
            {
                Vector3 position = ToolUtils.ParseVector3(args["position"].ToString());
                if (useWorldSpace)
                    target.position = position;
                else
                    target.localPosition = position;
            }
            
            if (args.ContainsKey("rotation"))
            {
                Vector3 rotation = ToolUtils.ParseVector3(args["rotation"].ToString());
                Quaternion rot = Quaternion.Euler(rotation);
                if (useWorldSpace)
                    target.rotation = rot;
                else
                    target.localRotation = rot;
            }
            
            EditorUtility.SetDirty(target);
            
            var extras = new Dictionary<string, object>
            {
                { "ikGoal", ikGoalStr },
                { "targetPath", GetGameObjectPath(target.gameObject) },
                { "gameObjectPath", gameObjectPath } // For revert system
            };
            
            if (args.ContainsKey("position"))
                extras["position"] = args["position"].ToString();
            if (args.ContainsKey("rotation"))
                extras["rotation"] = args["rotation"].ToString();
            
            return ToolUtils.CreateSuccessResponse($"Set IK position/rotation for {ikGoalStr}", extras);
        }
        
        private string GetGameObjectPath(UnityEngine.GameObject obj)
        {
            string path = obj.name;
            UnityEngine.Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
