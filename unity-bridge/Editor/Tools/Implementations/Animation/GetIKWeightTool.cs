using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetIKWeightTool : ITool
    {
        public string Name => "get_ik_weight";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            string specificGoal = args.ContainsKey("ikGoal") ? args["ikGoal"].ToString() : null;
            
            var ikController = obj.GetComponent("IKController");
            if (ikController == null)
            {
                return ToolUtils.CreateErrorResponse($"IKController component not found on '{gameObjectPath}'");
            }
            
            var results = new List<Dictionary<string, object>>();
            var goals = new[] { "LeftHand", "RightHand", "LeftFoot", "RightFoot", "LeftElbow", "RightElbow", "LeftKnee", "RightKnee" };
            
            foreach (string goalStr in goals)
            {
                if (specificGoal != null && !goalStr.Equals(specificGoal, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                
                AvatarIKGoal goal;
                string weightFieldName;
                string targetFieldName;
                
                if (IKUtils.IsHintGoal(goalStr))
                {
                    // For hint goals, get hint weight and target
                    weightFieldName = IKUtils.GetIKHintWeightFieldName(goalStr);
                    targetFieldName = IKUtils.GetIKHintTargetFieldName(goalStr);
                    goal = IKUtils.ParseIKGoal(goalStr); // Still parse to get the base goal
                }
                else
                {
                    goal = IKUtils.ParseIKGoal(goalStr);
                    weightFieldName = IKUtils.GetIKWeightFieldName(goal);
                    targetFieldName = IKUtils.GetIKGoalFieldName(goal);
                }
                
                float weight = 0f;
                if (!string.IsNullOrEmpty(weightFieldName))
                {
                    var weightField = ikController.GetType().GetField(weightFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (weightField != null)
                    {
                        weight = (float)(weightField.GetValue(ikController) ?? 0f);
                    }
                }
                
                UnityEngine.Transform target = null;
                if (!string.IsNullOrEmpty(targetFieldName))
                {
                    var targetField = ikController.GetType().GetField(targetFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (targetField != null)
                    {
                        target = targetField.GetValue(ikController) as UnityEngine.Transform;
                    }
                }
                
                var result = new Dictionary<string, object>
                {
                    { "ikGoal", goalStr },
                    { "hasTarget", target != null }
                };
                
                if (IKUtils.IsHintGoal(goalStr))
                {
                    result["hintWeight"] = weight;
                }
                else
                {
                    result["positionWeight"] = weight;
                    result["rotationWeight"] = weight;
                }
                
                if (target != null)
                {
                    result["targetPath"] = GetGameObjectPath(target.gameObject);
                    result["position"] = $"{target.position.x},{target.position.y},{target.position.z}";
                    result["rotation"] = $"{target.rotation.eulerAngles.x},{target.rotation.eulerAngles.y},{target.rotation.eulerAngles.z}";
                }
                
                results.Add(result);
            }
            
            var extras = new Dictionary<string, object>
            {
                { "ikWeights", results }
            };
            
            return ToolUtils.CreateSuccessResponse($"Retrieved IK weight info for {results.Count} body part(s)", extras);
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
