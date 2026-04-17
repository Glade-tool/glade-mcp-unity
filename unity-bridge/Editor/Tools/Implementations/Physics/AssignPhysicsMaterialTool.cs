using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class AssignPhysicsMaterialTool : ITool
    {
        public string Name => "assign_physics_material";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";

            if (string.IsNullOrEmpty(gameObjectPath) || string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath and materialPath are required");
            }

            if (!materialPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                materialPath = "Assets/" + materialPath;

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
            if (mat == null)
            {
                return ToolUtils.CreateErrorResponse($"PhysicMaterial not found at '{materialPath}'");
            }

            Collider[] colliders = obj.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                return ToolUtils.CreateErrorResponse($"No Collider found on '{gameObjectPath}'");
            }

            // Apply material to all colliders
            var assignedColliders = new List<string>();
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled)
                {
                    Undo.RecordObject(collider, $"Assign PhysicMaterial: {gameObjectPath}");
                    collider.sharedMaterial = mat;
                    assignedColliders.Add(collider.GetType().Name);
                }
            }

            // Build response with information about which colliders received the material
            var responseExtras = new Dictionary<string, object>
            {
                { "materialName", mat.name },
                { "assignedToColliders", assignedColliders },
                { "colliderCount", assignedColliders.Count }
            };

            if (assignedColliders.Count > 1)
            {
                responseExtras["info"] = $"Material assigned to {assignedColliders.Count} colliders on this GameObject.";
            }

            string message = $"Assigned PhysicMaterial '{mat.name}' to {assignedColliders.Count} collider(s) on '{gameObjectPath}'";
            if (assignedColliders.Count > 1)
            {
                message += $" ({string.Join(", ", assignedColliders)})";
            }

            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }
    }
}
