using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class GetSceneHierarchyTool : ITool
    {
        public string Name => "get_scene_hierarchy";

        // Default cap matches the AI schema. BFS + this cap keep large scenes
        // (Microverse terrain, prefab spam) from producing multi-MB HTTP payloads.
        // The backend formatter caps the model-facing slice at _MAX_HIERARCHY_PATHS.
        private const int DefaultMaxResults = 200;

        public string Execute(Dictionary<string, object> args)
        {
            bool includeInactive = ParseBoolArg(args, "includeInactive", true);
            int maxDepth = ParseIntArg(args, "maxDepth", -1);
            bool rootOnly = ParseBoolArg(args, "rootOnly", false);
            int maxResults = ParseIntArg(args, "maxResults", DefaultMaxResults);

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            // BFS ensures balanced coverage across roots instead of burning the budget
            // on the first deep subtree (mirrors UnityContextGatherer.GetSceneHierarchy).
            var queue = new Queue<(GameObject go, int depth, string parentPath)>();
            foreach (var root in roots)
            {
                if (!includeInactive && !root.activeInHierarchy) continue;
                queue.Enqueue((root, 0, null));
            }

            var paths = new List<string>();
            int totalSeen = 0;
            bool capped = maxResults >= 0;

            while (queue.Count > 0)
            {
                var (go, depth, parentPath) = queue.Dequeue();
                string fullPath = parentPath != null ? parentPath + "/" + go.name : go.name;

                totalSeen++;
                if (!capped || paths.Count < maxResults)
                    paths.Add(fullPath);

                if (rootOnly) continue;
                if (maxDepth >= 0 && depth + 1 > maxDepth) continue;

                for (int i = 0; i < go.transform.childCount; i++)
                {
                    var child = go.transform.GetChild(i).gameObject;
                    if (!includeInactive && !child.activeInHierarchy) continue;
                    queue.Enqueue((child, depth + 1, fullPath));
                }
            }

            bool truncated = capped && totalSeen > maxResults;

            var result = new Dictionary<string, object>
            {
                ["count"] = totalSeen,
                ["paths"] = paths,
                ["truncated"] = truncated,
            };
            if (truncated)
            {
                result["note"] = $"Hierarchy capped at {maxResults} of {totalSeen} paths. " +
                    "Use find_game_objects (nameContains/hasComponent/tag) or list_children " +
                    "for specific subtrees instead of raising maxResults.";
            }

            return ToolUtils.SerializeDictToJson(result);
        }

        private static bool ParseBoolArg(Dictionary<string, object> args, string key, bool fallback)
        {
            if (!args.ContainsKey(key) || args[key] == null) return fallback;
            if (args[key] is bool b) return b;
            return bool.TryParse(args[key].ToString(), out bool parsed) ? parsed : fallback;
        }

        private static int ParseIntArg(Dictionary<string, object> args, string key, int fallback)
        {
            if (!args.ContainsKey(key) || args[key] == null) return fallback;
            if (args[key] is int i) return i;
            if (args[key] is float f) return (int)f;
            if (args[key] is double d) return (int)d;
            return int.TryParse(args[key].ToString(), out int parsed) ? parsed : fallback;
        }
    }
}
