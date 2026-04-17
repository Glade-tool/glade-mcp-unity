using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class CreateCharacterControllerTool : ITool
    {
        public string Name => "create_character_controller";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            bool autoAlign = !args.ContainsKey("autoAlign") || (args["autoAlign"] is bool b ? b : bool.TryParse(args["autoAlign"]?.ToString(), out bool v) && v);

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            // Check for existing CharacterController
            CharacterController existing = obj.GetComponent<CharacterController>();
            if (existing != null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' already has a CharacterController. Use set_character_controller_properties to modify it instead.");
            }

            // Check for conflicts with other colliders
            var conflictInfo = ToolUtils.CheckColliderConflicts(obj);
            bool hasColliders = conflictInfo.ContainsKey("hasColliders") && conflictInfo["hasColliders"] is bool hasCol && hasCol;
            var existingColliders = conflictInfo.ContainsKey("existingColliders") ? conflictInfo["existingColliders"] as List<string> : new List<string>();
            var warnings = conflictInfo.ContainsKey("warnings") ? conflictInfo["warnings"] as List<string> : new List<string>();

            // Auto-remove redundant colliders (CharacterController has its own built-in collider)
            var removedColliders = new List<string>();
            bool autoRemoveColliders = !args.ContainsKey("keepExistingColliders") || !(args["keepExistingColliders"] is bool keep && keep);

            if (hasColliders && autoRemoveColliders)
            {
                Collider[] colliders = obj.GetComponents<Collider>();
                foreach (var collider in colliders)
                {
                    if (collider != null)
                    {
                        removedColliders.Add(collider.GetType().Name);
                        Undo.DestroyObjectImmediate(collider);
                    }
                }
                
                if (removedColliders.Count > 0)
                {
                    warnings.Add($"INFO: Removed {removedColliders.Count} redundant collider(s): {string.Join(", ", removedColliders)}. CharacterController includes its own built-in collider.");
                }
            }
            else if (hasColliders && !autoRemoveColliders)
            {
                warnings.Add($"WARNING: GameObject has {existingColliders.Count} existing collider(s): {string.Join(", ", existingColliders)}. CharacterController includes its own collider - multiple colliders may cause conflicts.");
            }

            // Create CharacterController
            CharacterController charController = Undo.AddComponent<CharacterController>(obj);
            if (charController == null)
            {
                return ToolUtils.CreateErrorResponse($"Failed to create CharacterController on '{gameObjectPath}'");
            }

            // Auto-align with mesh bounds if requested and no explicit size provided
            bool hasExplicitSize = args.ContainsKey("radius") || args.ContainsKey("height") || args.ContainsKey("center");
            if (autoAlign && !hasExplicitSize)
            {
                Bounds meshBounds = ToolUtils.GetMeshBounds(obj);
                if (meshBounds.size.magnitude > 0.01f) // Only if we found actual mesh bounds
                {
                    Vector3 size = meshBounds.size;
                    // CharacterController uses radius (X/Z) and height (Y)
                    float radius = Mathf.Max(size.x, size.z) * 0.5f;
                    float height = size.y;
                    float centerY = meshBounds.center.y;

                    charController.radius = radius;
                    charController.height = height;
                    charController.center = new Vector3(0, centerY, 0);
                }
            }

            // Apply explicit properties if provided
            ApplyCharacterControllerProperties(charController, args);

            // Build response with conflict information
            var responseExtras = new Dictionary<string, object>
            {
                { "autoAligned", autoAlign && !hasExplicitSize },
                { "radius", charController.radius },
                { "height", charController.height },
                { "center", $"{charController.center.x},{charController.center.y},{charController.center.z}" }
            };

            if (removedColliders.Count > 0)
            {
                responseExtras["removedColliders"] = removedColliders;
                responseExtras["autoRemovedColliders"] = true;
            }

            if (warnings.Count > 0)
            {
                responseExtras["warnings"] = warnings;
            }

            if (hasColliders && !autoRemoveColliders)
            {
                responseExtras["existingColliders"] = existingColliders;
            }

            string message = $"Added CharacterController to '{gameObjectPath}'";
            if (autoAlign && !hasExplicitSize)
                message += " (auto-aligned with mesh bounds)";
            if (removedColliders.Count > 0)
                message += $" (removed {removedColliders.Count} redundant collider(s))";
            if (hasColliders && !autoRemoveColliders)
                message += ". WARNING: Existing colliders detected - see warnings in response.";

            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }

        private static void ApplyCharacterControllerProperties(CharacterController charController, Dictionary<string, object> args)
        {
            if (args.ContainsKey("radius") && float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius))
                charController.radius = radius;
            if (args.ContainsKey("height") && float.TryParse(args["height"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float height))
                charController.height = height;
            if (args.ContainsKey("center"))
                charController.center = ToolUtils.ParseVector3(args["center"].ToString());
            if (args.ContainsKey("slopeLimit") && float.TryParse(args["slopeLimit"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float slopeLimit))
                charController.slopeLimit = slopeLimit;
            if (args.ContainsKey("stepOffset") && float.TryParse(args["stepOffset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float stepOffset))
                charController.stepOffset = stepOffset;
            if (args.ContainsKey("skinWidth") && float.TryParse(args["skinWidth"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float skinWidth))
                charController.skinWidth = skinWidth;
            if (args.ContainsKey("minMoveDistance") && float.TryParse(args["minMoveDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float minMoveDistance))
                charController.minMoveDistance = minMoveDistance;

            EnsureValidCharacterController(charController);
        }

        /// <summary>
        /// Ensures CharacterController has valid values so it stays active and avoids
        /// "Step Offset must be positive" and "Move called on inactive controller" errors.
        /// </summary>
        private static void EnsureValidCharacterController(CharacterController cc)
        {
            if (cc.radius <= 0f) cc.radius = 0.5f;
            float minHeight = cc.radius * 2f;
            if (cc.height < minHeight) cc.height = Mathf.Max(2f, minHeight);
            if (cc.stepOffset <= 0f) cc.stepOffset = 0.3f;
            if (cc.skinWidth <= 0f) cc.skinWidth = 0.08f;
            if (cc.center == Vector3.zero) cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
        }
    }
}
