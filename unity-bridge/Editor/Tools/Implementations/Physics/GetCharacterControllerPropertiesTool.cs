using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Gets detailed information about a CharacterController component including radius, height, center, slope limit, and other properties.
    /// </summary>
    public class GetCharacterControllerPropertiesTool : ITool
    {
        public string Name => "get_character_controller_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            CharacterController charController = obj.GetComponent<CharacterController>();
            if (charController == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a CharacterController component");
            }
            
            // READ ONLY - No Undo needed
            var properties = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["radius"] = charController.radius,
                ["height"] = charController.height,
                ["center"] = $"{charController.center.x},{charController.center.y},{charController.center.z}",
                ["slopeLimit"] = charController.slopeLimit,
                ["stepOffset"] = charController.stepOffset,
                ["skinWidth"] = charController.skinWidth,
                ["minMoveDistance"] = charController.minMoveDistance,
                ["enabled"] = charController.enabled
            };
            
            // Check for conflicts
            var conflictInfo = ToolUtils.CheckColliderConflicts(obj);
            bool hasColliders = conflictInfo.ContainsKey("hasColliders") && conflictInfo["hasColliders"] is bool hasCol && hasCol;
            var existingColliders = conflictInfo.ContainsKey("existingColliders") ? conflictInfo["existingColliders"] as List<string> : new List<string>();
            
            if (hasColliders && existingColliders.Count > 1) // More than just CharacterController
            {
                properties["hasOtherColliders"] = true;
                properties["otherColliders"] = existingColliders;
                properties["warning"] = $"GameObject has {existingColliders.Count} collider(s) in addition to CharacterController. This may cause conflicts.";
            }
            
            string message = $"Retrieved character controller properties for '{gameObjectPath}': Radius={charController.radius}, Height={charController.height}";
            
            return ToolUtils.CreateSuccessResponse(message, properties);
        }
    }
}
