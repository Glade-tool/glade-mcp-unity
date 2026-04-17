using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class SetColliderPropertiesTool : ITool
    {
        public string Name => "set_collider_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            Collider collider = obj.GetComponent<Collider>();
            if (collider == null)
                return ToolUtils.CreateErrorResponse($"No Collider found on '{gameObjectPath}'");

            // Check conflicts
            var conflictInfo = ToolUtils.CheckColliderConflicts(obj);
            bool hasCharacterController = conflictInfo.ContainsKey("hasCharacterController") && conflictInfo["hasCharacterController"] is bool hasCC && hasCC;
            var existingColliders = conflictInfo.ContainsKey("existingColliders") ? conflictInfo["existingColliders"] as List<string> : new List<string>();
            bool hasConflicts = conflictInfo.ContainsKey("isConflicted") && conflictInfo["isConflicted"] is bool c && c;

            Undo.RecordObject(collider, "Set Collider Properties");

            // Delegate to the appropriate handler
            string typeKey = collider.GetType().Name.Replace("Collider", "");
            if (ColliderHandlerRegistry.TryGet(typeKey, out var handler))
                handler.ApplyArgs(collider, args);

            // Build response
            var responseExtras = new Dictionary<string, object>
            {
                { "colliderType", collider.GetType().Name }
            };

            if (hasConflicts)
            {
                responseExtras["warnings"] = conflictInfo["warnings"];
                if (hasCharacterController) responseExtras["hasCharacterController"] = true;
                if (existingColliders.Count > 1) responseExtras["otherColliders"] = existingColliders;
            }

            string message = $"Updated collider properties on '{gameObjectPath}'";
            if (hasConflicts) message += ". WARNING: Conflicts detected - see warnings in response.";

            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }
    }
}
