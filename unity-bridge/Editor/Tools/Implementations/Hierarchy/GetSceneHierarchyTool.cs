using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class GetSceneHierarchyTool : ITool
    {
        public string Name => "get_scene_hierarchy";

        public string Execute(Dictionary<string, object> args)
        {
            bool includeInactive = true;
            if (args.ContainsKey("includeInactive"))
            {
                if (args["includeInactive"] is bool b) includeInactive = b;
                else bool.TryParse(args["includeInactive"].ToString(), out includeInactive);
            }

            int maxDepth = -1;
            if (args.ContainsKey("maxDepth"))
            {
                if (args["maxDepth"] is int i) maxDepth = i;
                else if (args["maxDepth"] is float f) maxDepth = (int)f;
                else int.TryParse(args["maxDepth"].ToString(), out maxDepth);
            }

            bool rootOnly = false;
            if (args.ContainsKey("rootOnly"))
            {
                if (args["rootOnly"] is bool b) rootOnly = b;
                else bool.TryParse(args["rootOnly"].ToString(), out rootOnly);
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var paths = new List<string>();

            foreach (var root in roots)
            {
                if (!includeInactive && !root.activeInHierarchy) continue;
                if (rootOnly)
                {
                    paths.Add(ToolUtils.GetGameObjectPath(root));
                }
                else
                {
                    ToolUtils.CollectHierarchyPaths(root, paths, includeInactive, maxDepth, 0);
                }
            }

            var result = new Dictionary<string, object>
            {
                ["count"] = paths.Count,
                ["paths"] = paths
            };

            return ToolUtils.SerializeDictToJson(result);
        }
    }
}
