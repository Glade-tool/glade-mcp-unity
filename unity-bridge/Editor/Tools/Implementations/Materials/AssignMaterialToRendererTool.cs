using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    public class AssignMaterialToRendererTool : ITool
    {
        public string Name => "assign_material_to_renderer";

        public string Execute(Dictionary<string, object> args)
        {
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";
            if (string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("materialPath is required");
            }

            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            // Material lookup with case-insensitive + filename-only fallback
            Material mat = ToolUtils.LoadAssetAtPathCaseInsensitive<Material>(materialPath);
            if (mat == null)
            {
                var similar = FindSimilarMaterials(materialPath, 10);
                var extras = new Dictionary<string, object>();
                if (similar.Count > 0)
                {
                    extras["similarMaterials"] = similar;
                    extras["hint"] = "Retry with one of similarMaterials, or use list_materials to discover the right path.";
                }
                else
                {
                    extras["hint"] = "Use list_materials or create_material first. Path must be Assets-relative.";
                }
                return ToolUtils.CreateErrorResponse(
                    $"Material not found at '{materialPath}'",
                    extras);
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse(
                    $"GameObject '{gameObjectPath}' not found. Use find_game_objects or get_scene_hierarchy to locate it.");
            }

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                var present = obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .Take(15)
                    .ToList();
                var childRenderers = obj.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r != null && r.gameObject != obj)
                    .Select(r => ToolUtils.GetGameObjectPath(r.gameObject))
                    .Distinct()
                    .Take(10)
                    .ToList();
                var extras = new Dictionary<string, object>
                {
                    ["componentsPresent"] = present,
                };
                if (childRenderers.Count > 0)
                {
                    extras["childRenderers"] = childRenderers;
                    extras["hint"] = "This GameObject has no Renderer, but a child does. Retarget gameObjectPath to one of childRenderers.";
                }
                else
                {
                    extras["hint"] = "Add a MeshRenderer/SpriteRenderer first via add_component.";
                }
                return ToolUtils.CreateErrorResponse(
                    $"GameObject '{gameObjectPath}' has no Renderer component",
                    extras);
            }

            int slot = 0;
            if (args.ContainsKey("materialSlot"))
            {
                int.TryParse(args["materialSlot"].ToString(), out slot);
            }

            Material[] currentMaterials = renderer.sharedMaterials;
            int slotCount = currentMaterials != null ? currentMaterials.Length : 0;
            if (slot < 0)
            {
                return ToolUtils.CreateErrorResponse(
                    $"materialSlot must be >= 0 (got {slot}). Renderer has {slotCount} slots.");
            }

            // Capture previous state BEFORE mutation (for revert)
            string prevMaterialPath = null;
            if (currentMaterials != null && slot < currentMaterials.Length && currentMaterials[slot] != null)
            {
                prevMaterialPath = AssetDatabase.GetAssetPath(currentMaterials[slot]);
            }

            Undo.RecordObject(renderer, $"Assign Material: {gameObjectPath}");

            Material[] materials = renderer.sharedMaterials;
            bool slotExtended = false;
            if (materials.Length <= slot)
            {
                slotExtended = true;
                Material[] newMaterials = new Material[slot + 1];
                Array.Copy(materials, newMaterials, materials.Length);
                materials = newMaterials;
            }
            materials[slot] = mat;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);

            var responseExtras = new Dictionary<string, object>
            {
                ["materialPath"] = AssetDatabase.GetAssetPath(mat),
                ["gameObjectPath"] = gameObjectPath,
                ["rendererType"] = renderer.GetType().Name,
                ["slot"] = slot,
                ["slotCount"] = materials.Length,
                ["previousState"] = new Dictionary<string, object>
                {
                    ["materialPath"] = prevMaterialPath ?? "",
                    ["materialSlot"] = slot,
                    ["slotCount"] = slotCount,
                },
            };
            if (slotExtended)
            {
                responseExtras["slotExtended"] = true;
                responseExtras["note"] = $"Extended material slot array from {slotCount} to {materials.Length}.";
            }

            return ToolUtils.CreateSuccessResponse(
                $"Assigned material '{mat.name}' to '{gameObjectPath}' (slot {slot})",
                responseExtras);
        }

        /// <summary>
        /// Find materials whose path might be what the caller meant.
        /// Strategy: (1) Unity's built-in FindAssets filter (prefix/substring), then
        /// (2) a fuzzy pass over all Materials ranked by normalized edit-distance,
        /// so typos like "PlayMaterial"→"PlayerMaterial" or "GladeTestMatt"→
        /// "GladeTestMat" surface in the error response.
        /// </summary>
        private static List<string> FindSimilarMaterials(string materialPath, int maxResults)
        {
            var results = new List<string>();
            string target = System.IO.Path.GetFileNameWithoutExtension(materialPath) ?? "";
            if (string.IsNullOrEmpty(target)) return results;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Consider(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath) || assetPath.StartsWith("Packages/")) return;
                if (seen.Add(assetPath)) results.Add(assetPath);
            }

            // Phase 1: Unity's indexed search (fast; catches prefix + obvious matches)
            foreach (var guid in AssetDatabase.FindAssets($"{target} t:Material"))
            {
                Consider(AssetDatabase.GUIDToAssetPath(guid));
                if (results.Count >= maxResults) return results;
            }

            // Phase 2: fuzzy pass over ALL Materials — catches typos that Unity's
            // index misses. Capped at 500 candidates to bound the cost on huge projects.
            var allGuids = AssetDatabase.FindAssets("t:Material");
            int scanned = 0;
            var scored = new List<(string path, int score)>();
            foreach (var guid in allGuids)
            {
                if (scanned++ >= 500) break;
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(p) || p.StartsWith("Packages/")) continue;
                string name = System.IO.Path.GetFileNameWithoutExtension(p);
                int distance = LevenshteinCapped(target, name, 4);
                if (distance <= 3 && !seen.Contains(p))
                {
                    scored.Add((p, distance));
                }
            }
            scored.Sort((a, b) => a.score.CompareTo(b.score));
            foreach (var (path, _) in scored)
            {
                Consider(path);
                if (results.Count >= maxResults) break;
            }
            return results;
        }

        /// <summary>
        /// Bounded Levenshtein — bails out when the minimum possible distance
        /// exceeds <paramref name="maxDistance"/> so long mismatched names cost O(1).
        /// Case-insensitive. Returns maxDistance+1 when bailed.
        /// </summary>
        private static int LevenshteinCapped(string a, string b, int maxDistance)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;
            if (Math.Abs(a.Length - b.Length) > maxDistance) return maxDistance + 1;

            string lowA = a.ToLowerInvariant();
            string lowB = b.ToLowerInvariant();
            int[] prev = new int[lowB.Length + 1];
            int[] curr = new int[lowB.Length + 1];
            for (int j = 0; j <= lowB.Length; j++) prev[j] = j;

            for (int i = 1; i <= lowA.Length; i++)
            {
                curr[0] = i;
                int rowMin = curr[0];
                for (int j = 1; j <= lowB.Length; j++)
                {
                    int cost = (lowA[i - 1] == lowB[j - 1]) ? 0 : 1;
                    curr[j] = Math.Min(
                        Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + cost);
                    if (curr[j] < rowMin) rowMin = curr[j];
                }
                if (rowMin > maxDistance) return maxDistance + 1;
                (prev, curr) = (curr, prev);
            }
            return prev[lowB.Length];
        }
    }
}
