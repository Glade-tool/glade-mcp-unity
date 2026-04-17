using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class SetRigidbodyPropertiesTool : ITool
    {
        public string Name => "set_rigidbody_properties";

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
                return ToolUtils.CreateErrorResponse($"No Rigidbody found on '{gameObjectPath}'");
            }

            // Check for CharacterController conflict
            CharacterController charController = obj.GetComponent<CharacterController>();
            bool hasCharacterController = charController != null && charController.enabled;
            
            var warnings = new List<string>();
            if (hasCharacterController)
            {
                warnings.Add("WARNING: GameObject has CharacterController. Rigidbody and CharacterController should NOT be on the same GameObject - they handle physics differently.");
            }

            // Check for colliders
            Collider[] colliders = obj.GetComponents<Collider>();
            bool hasColliders = colliders.Length > 0;

            Undo.RecordObject(rb, "Set Rigidbody Properties");
            ApplyRigidbodyProperties(rb, args);

            // Build response with conflict information
            var responseExtras = new Dictionary<string, object>
            {
                { "mass", rb.mass },
                { "useGravity", rb.useGravity },
                { "isKinematic", rb.isKinematic },
                { "hasColliders", hasColliders }
            };

            if (warnings.Count > 0)
            {
                responseExtras["warnings"] = warnings;
                if (hasCharacterController)
                    responseExtras["hasCharacterController"] = true;
            }

            string message = $"Updated Rigidbody on '{gameObjectPath}'";
            if (warnings.Count > 0)
                message += ". WARNING: Conflicts detected - see warnings in response.";

            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }

        private static void ApplyRigidbodyProperties(Rigidbody rb, Dictionary<string, object> args)
        {
            if (args.ContainsKey("mass") && float.TryParse(args["mass"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float mass)) rb.mass = mass;
            if (args.ContainsKey("drag") && float.TryParse(args["drag"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float drag)) rb.linearDamping = drag;
            if (args.ContainsKey("angularDrag") && float.TryParse(args["angularDrag"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float angularDrag)) rb.angularDamping = angularDrag;
            if (args.ContainsKey("useGravity") && bool.TryParse(args["useGravity"].ToString(), out bool gravity)) rb.useGravity = gravity;
            if (args.ContainsKey("isKinematic") && bool.TryParse(args["isKinematic"].ToString(), out bool isKinematic)) rb.isKinematic = isKinematic;
        }
    }
}
