using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class CreateColliderTool : ITool
    {
        public string Name => "create_collider";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string colliderType   = args.ContainsKey("colliderType")   ? args["colliderType"].ToString()   : null;
            bool autoAlign = !args.ContainsKey("autoAlign") ||
                             (args["autoAlign"] is bool b ? b :
                              bool.TryParse(args["autoAlign"]?.ToString(), out bool v) && v);

            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            // Resolve handler — if colliderType is null/empty, auto-detect from mesh shape
            string resolvedType = colliderType;
            if (string.IsNullOrEmpty(resolvedType))
            {
                var suggestion = ToolUtils.SuggestPrimitiveColliderType(obj);
                resolvedType = suggestion.ContainsKey("suggestedType") ? suggestion["suggestedType"].ToString() : "Box";
            }

            // For "convex" we mark the mesh convex via args, reuse mesh handler
            bool forceConvex = string.Equals(resolvedType, "convex", System.StringComparison.OrdinalIgnoreCase);
            if (forceConvex && !args.ContainsKey("convex"))
            {
                args = new Dictionary<string, object>(args) { ["convex"] = true };
            }

            if (!ColliderHandlerRegistry.TryGet(resolvedType, out var handler))
                return ToolUtils.CreateErrorResponse($"Unknown collider type '{resolvedType}'. Supported: Box, Sphere, Capsule, Mesh, Convex, Wheel, Terrain.");

            // Check for conflicts
            var conflictInfo = ToolUtils.CheckColliderConflicts(obj);
            bool hasConflicts = conflictInfo.ContainsKey("isConflicted") && conflictInfo["isConflicted"] is bool conflicted && conflicted;
            bool hasCharacterController = conflictInfo.ContainsKey("hasCharacterController") && conflictInfo["hasCharacterController"] is bool hasCC && hasCC;
            var existingColliders = conflictInfo.ContainsKey("existingColliders") ? conflictInfo["existingColliders"] as List<string> : new List<string>();

            // Prevent duplicate
            if (handler.AlreadyExists(obj))
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' already has a {resolvedType} collider. Use set_collider_properties to modify it instead.");

            // Add the collider
            Collider collider = handler.AddComponent(obj);
            if (collider == null)
                return ToolUtils.CreateErrorResponse($"Failed to create {resolvedType} collider on '{gameObjectPath}'");

            // Auto-align unless user provided explicit size parameters
            bool hasExplicitSize = HasMeaningfulSizeParameter(args);
            if (autoAlign && !hasExplicitSize)
            {
                Bounds meshBounds = ToolUtils.GetMeshBounds(obj);
                if (meshBounds.size.magnitude > 0.01f)
                    handler.ApplyAutoAlign(collider, meshBounds);
                else
                    handler.ApplyAutoAlign(collider, new Bounds(Vector3.zero, Vector3.one));
            }

            // Apply explicit args (overrides auto-align for any provided values)
            handler.ApplyArgs(collider, args);

            // Build response
            var responseExtras = new Dictionary<string, object>
            {
                { "colliderType", collider.GetType().Name },
                { "autoAligned",  autoAlign && !hasExplicitSize }
            };

            if (hasConflicts)
            {
                responseExtras["warnings"] = conflictInfo["warnings"];
                responseExtras["hasCharacterController"] = hasCharacterController;
                if (existingColliders.Count > 0)
                    responseExtras["existingColliders"] = existingColliders;
            }

            string message = $"Added {collider.GetType().Name} to '{gameObjectPath}'";
            if (autoAlign && !hasExplicitSize) message += " (auto-aligned with mesh bounds)";
            if (hasConflicts)                  message += ". WARNING: Conflicts detected - see warnings in response.";

            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }

        /// <summary>
        /// Returns true if the args contain meaningful (non-zero) size, radius, height, or center values.
        /// Used to decide whether to run auto-alignment.
        /// </summary>
        private static bool HasMeaningfulSizeParameter(Dictionary<string, object> args)
        {
            if (args.ContainsKey("size"))
            {
                string s = args["size"]?.ToString();
                if (!string.IsNullOrWhiteSpace(s) && ToolUtils.ParseVector3(s).magnitude > 0.01f) return true;
            }
            if (args.ContainsKey("radius") && args["radius"] != null &&
                float.TryParse(args["radius"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float r) && r > 0.01f) return true;
            if (args.ContainsKey("height") && args["height"] != null &&
                float.TryParse(args["height"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float h) && h > 0.01f) return true;
            if (args.ContainsKey("center"))
            {
                string c = args["center"]?.ToString();
                if (!string.IsNullOrWhiteSpace(c) && ToolUtils.ParseVector3(c).magnitude > 0.01f) return true;
            }
            return false;
        }
    }
}
