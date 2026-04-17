using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateIKTargetTool : ITool
    {
        public string Name => "create_ik_target";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string targetName = args.ContainsKey("targetName") ? args["targetName"].ToString() : "";
            string ikGoalStr = args.ContainsKey("ikGoal") ? args["ikGoal"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            if (string.IsNullOrEmpty(targetName))
                return ToolUtils.CreateErrorResponse("targetName is required");
            
            if (string.IsNullOrEmpty(ikGoalStr))
                return ToolUtils.CreateErrorResponse("ikGoal is required");
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            Animator animator = obj.GetComponent<Animator>();
            if (animator == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' must have an Animator component");
            
            // Create IK target GameObject
            UnityEngine.GameObject targetObj = new UnityEngine.GameObject(targetName);
            
            // Register for undo immediately after creation (before modifications)
            Undo.RegisterCreatedObjectUndo(targetObj, $"Create IK Target: {targetName}");
            
            // Set parent if specified (using proper Undo method)
            if (args.ContainsKey("parentPath"))
            {
                string parentPath = args["parentPath"].ToString();
                UnityEngine.GameObject parent = ToolUtils.FindGameObjectByPath(parentPath);
                if (parent != null)
                {
                    Undo.SetTransformParent(targetObj.transform, parent.transform, $"Set Parent: {targetName}");
                }
            }
            else
            {
                // Default: parent to the animator GameObject
                Undo.SetTransformParent(targetObj.transform, obj.transform, $"Set Parent: {targetName}");
            }
            
            // Record transform before modifying position/rotation
            Undo.RecordObject(targetObj.transform, $"Set IK Target Transform: {targetName}");
            
            // Set initial position
            Vector3 position = Vector3.zero;
            bool positionSet = false;
            if (args.ContainsKey("position"))
            {
                position = ToolUtils.ParseVector3(args["position"].ToString());
                targetObj.transform.position = position;
                positionSet = true;
            }
            
            // Set initial rotation
            if (args.ContainsKey("rotation"))
            {
                Vector3 rotation = ToolUtils.ParseVector3(args["rotation"].ToString());
                targetObj.transform.rotation = Quaternion.Euler(rotation);
            }
            
            // If position not set, default to bone location
            if (!positionSet)
            {
                AvatarIKGoal goal = IKUtils.ParseIKGoal(ikGoalStr);
                UnityEngine.Transform bone = GetBoneTransform(animator, goal);
                if (bone != null)
                {
                    targetObj.transform.position = bone.position;
                    targetObj.transform.rotation = bone.rotation;
                }
            }
            
            string targetPath = GetGameObjectPath(targetObj);
            var extras = new Dictionary<string, object>
            {
                { "targetPath", targetPath },
                { "gameObjectPath", targetPath }, // For revert system
                { "name", targetName }, // For revert system
                { "ikGoal", ikGoalStr }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created IK target '{targetName}' for {ikGoalStr}", extras);
        }
        
        private UnityEngine.Transform GetBoneTransform(Animator animator, AvatarIKGoal goal)
        {
            HumanBodyBones bone = HumanBodyBones.LeftHand;
            switch (goal)
            {
                case AvatarIKGoal.LeftHand:
                    bone = HumanBodyBones.LeftHand; break;
                case AvatarIKGoal.RightHand:
                    bone = HumanBodyBones.RightHand; break;
                case AvatarIKGoal.LeftFoot:
                    bone = HumanBodyBones.LeftFoot; break;
                case AvatarIKGoal.RightFoot:
                    bone = HumanBodyBones.RightFoot; break;
            }
            return animator.GetBoneTransform(bone);
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
