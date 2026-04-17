using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetIKTargetInfoTool : ITool
    {
        public string Name => "get_ik_target_info";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            
            var ikTargets = new List<Dictionary<string, object>>();
            
            // Find IKController component
            var ikController = obj.GetComponent("IKController");
            if (ikController != null)
            {
                var targets = new Dictionary<string, string>
                {
                    { "LeftHand", "leftHandTarget" },
                    { "RightHand", "rightHandTarget" },
                    { "LeftFoot", "leftFootTarget" },
                    { "RightFoot", "rightFootTarget" },
                    { "LeftElbow", "leftElbowTarget" },
                    { "RightElbow", "rightElbowTarget" },
                    { "LeftKnee", "leftKneeTarget" },
                    { "RightKnee", "rightKneeTarget" }
                };
                
                foreach (var kvp in targets)
                {
                    var field = ikController.GetType().GetField(kvp.Value, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        UnityEngine.Transform target = field.GetValue(ikController) as UnityEngine.Transform;
                        if (target != null)
                        {
                            ikTargets.Add(new Dictionary<string, object>
                            {
                                { "ikGoal", kvp.Key },
                                { "targetPath", GetGameObjectPath(target.gameObject) },
                                { "position", $"{target.position.x},{target.position.y},{target.position.z}" },
                                { "rotation", $"{target.rotation.eulerAngles.x},{target.rotation.eulerAngles.y},{target.rotation.eulerAngles.z}" }
                            });
                        }
                    }
                }
            }
            
            var extras = new Dictionary<string, object>
            {
                { "ikTargets", ikTargets },
                { "hasIKController", ikController != null },
                { "targetCount", ikTargets.Count }
            };
            
            return ToolUtils.CreateSuccessResponse($"Found {ikTargets.Count} IK target(s)", extras);
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
