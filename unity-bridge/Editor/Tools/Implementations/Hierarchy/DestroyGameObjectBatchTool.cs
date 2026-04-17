using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class DestroyGameObjectBatchTool : ITool
    {
        public string Name => "destroy_game_object_batch";

        public string Execute(Dictionary<string, object> args)
        {
            var paths = ToolUtils.GetPathsFromArgsOrSelection(args, "gameObjectPaths");
            if (paths.Count == 0)
            {
                return ToolUtils.CreateErrorResponse("No objects to destroy. Provide gameObjectPaths or select objects.");
            }

            int destroyed = 0;
            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                    destroyed++;
                }
            }

            var extras = new Dictionary<string, object>
            {
                { "destroyed", destroyed }
            };
            return ToolUtils.CreateSuccessResponse($"Destroyed {destroyed} object(s)", extras);
        }
    }
}
