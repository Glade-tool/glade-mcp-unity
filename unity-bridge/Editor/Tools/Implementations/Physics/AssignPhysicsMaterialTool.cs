using System;
using System.Collections.Generic;
using System.Linq;
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

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse(
                    $"GameObject '{gameObjectPath}' not found. Use find_game_objects to locate it.");
            }

            var mat = ToolUtils.LoadAssetAtPathCaseInsensitive<PhysicsMaterial>(materialPath);
            if (mat == null)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(materialPath);
                var similar = new List<string>();
                if (!string.IsNullOrEmpty(fileName))
                {
                    string[] guids = AssetDatabase.FindAssets($"{fileName} t:PhysicsMaterial");
                    foreach (var guid in guids)
                    {
                        string p = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(p) && !p.StartsWith("Packages/"))
                        {
                            similar.Add(p);
                            if (similar.Count >= 10) break;
                        }
                    }
                }
                var extras = new Dictionary<string, object>();
                if (similar.Count > 0)
                {
                    extras["similarPhysicsMaterials"] = similar;
                    extras["hint"] = "Retry with one of similarPhysicsMaterials, or create one via create_physics_material.";
                }
                else
                {
                    extras["hint"] = "Use create_physics_material first. Path must be Assets-relative with .physicMaterial extension.";
                }
                return ToolUtils.CreateErrorResponse(
                    $"PhysicsMaterial not found at '{materialPath}'",
                    extras);
            }

            Collider[] colliders = obj.GetComponents<Collider>();
            if (colliders.Length == 0)
            {
                var present = obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .Take(15)
                    .ToList();
                var childColliders = obj.GetComponentsInChildren<Collider>(true)
                    .Where(c => c != null && c.gameObject != obj)
                    .Select(c => ToolUtils.GetGameObjectPath(c.gameObject))
                    .Distinct()
                    .Take(10)
                    .ToList();
                var extras = new Dictionary<string, object>
                {
                    ["componentsPresent"] = present,
                };
                if (childColliders.Count > 0)
                {
                    extras["childColliders"] = childColliders;
                    extras["hint"] = "This GameObject has no Collider, but a child does. Retarget gameObjectPath to one of childColliders.";
                }
                else
                {
                    extras["hint"] = "Add a collider first via create_collider.";
                }
                return ToolUtils.CreateErrorResponse(
                    $"No Collider found on '{gameObjectPath}'",
                    extras);
            }

            var assignedColliders = new List<string>();
            var skippedDisabled = new List<string>();
            foreach (var collider in colliders)
            {
                if (collider == null) continue;
                if (!collider.enabled)
                {
                    skippedDisabled.Add(collider.GetType().Name);
                    continue;
                }
                Undo.RecordObject(collider, $"Assign PhysicsMaterial: {gameObjectPath}");
                collider.sharedMaterial = mat;
                EditorUtility.SetDirty(collider);
                assignedColliders.Add(collider.GetType().Name);
            }

            if (assignedColliders.Count == 0)
            {
                var extras = new Dictionary<string, object>
                {
                    ["skippedDisabled"] = skippedDisabled,
                    ["hint"] = "All colliders on this GameObject were disabled. Enable one or retarget gameObjectPath.",
                };
                return ToolUtils.CreateErrorResponse(
                    $"No enabled Collider on '{gameObjectPath}' (found {skippedDisabled.Count} disabled)",
                    extras);
            }

            var responseExtras = new Dictionary<string, object>
            {
                ["materialName"] = mat.name,
                ["materialPath"] = AssetDatabase.GetAssetPath(mat),
                ["assignedToColliders"] = assignedColliders,
                ["colliderCount"] = assignedColliders.Count,
            };
            if (skippedDisabled.Count > 0)
            {
                responseExtras["skippedDisabled"] = skippedDisabled;
            }

            string message = $"Assigned PhysicsMaterial '{mat.name}' to {assignedColliders.Count} collider(s) on '{gameObjectPath}'";
            if (assignedColliders.Count > 1)
            {
                message += $" ({string.Join(", ", assignedColliders)})";
            }

            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }
    }
}
