using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Gets detailed information about a Rigidbody component including mass, drag, gravity, kinematic state, and other properties.
    /// </summary>
    public class GetRigidbodyPropertiesTool : ITool
    {
        public string Name => "get_rigidbody_properties";

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
            
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a Rigidbody component");
            }
            
            // READ ONLY - No Undo needed
            var properties = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["mass"] = rb.mass,
                ["drag"] = rb.linearDamping,
                ["angularDrag"] = rb.angularDamping,
                ["useGravity"] = rb.useGravity,
                ["isKinematic"] = rb.isKinematic,
                ["velocity"] = $"{rb.linearVelocity.x},{rb.linearVelocity.y},{rb.linearVelocity.z}",
                ["angularVelocity"] = $"{rb.angularVelocity.x},{rb.angularVelocity.y},{rb.angularVelocity.z}"
            };
            
            // Check for conflicts
            CharacterController charController = obj.GetComponent<CharacterController>();
            if (charController != null && charController.enabled)
            {
                properties["hasCharacterController"] = true;
                properties["warning"] = "GameObject has CharacterController. Rigidbody and CharacterController should NOT be on the same GameObject.";
            }
            
            Collider[] colliders = obj.GetComponents<Collider>();
            properties["hasColliders"] = colliders.Length > 0;
            if (colliders.Length > 0)
            {
                var colliderTypes = new List<string>();
                foreach (var col in colliders)
                {
                    colliderTypes.Add(col.GetType().Name);
                }
                properties["colliderTypes"] = colliderTypes;
            }
            
            string message = $"Retrieved rigidbody properties for '{gameObjectPath}': Mass={rb.mass}, UseGravity={rb.useGravity}, IsKinematic={rb.isKinematic}";
            
            return ToolUtils.CreateSuccessResponse(message, properties);
        }
    }
}
