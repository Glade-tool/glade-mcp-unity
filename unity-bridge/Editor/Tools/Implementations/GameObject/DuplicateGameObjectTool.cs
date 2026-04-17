using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class DuplicateGameObjectTool : ITool
    {
        public string Name => "duplicate_game_object";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");

            UnityEngine.GameObject original = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (original == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            string newName = args.ContainsKey("newName") ? args["newName"]?.ToString() : "";
            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"]?.ToString() : "";
            int count = 1;
            if (args.ContainsKey("count"))
            {
                if (args["count"] is int c) count = c;
                else if (args["count"] is float f) count = (int)f;
                else int.TryParse(args["count"]?.ToString(), out count);
            }
            count = Mathf.Clamp(count, 1, 100);

            UnityEngine.Transform newParent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                UnityEngine.GameObject parentObj = ToolUtils.FindGameObjectByPath(parentPath);
                if (parentObj != null)
                    newParent = parentObj.transform;
            }

            var created = new List<string>();
            for (int i = 0; i < count; i++)
            {
                UnityEngine.GameObject duplicate = (UnityEngine.GameObject)Object.Instantiate(original);
                if (!string.IsNullOrEmpty(newName))
                    duplicate.name = count > 1 ? $"{newName} ({i + 1})" : newName;
                else
                    duplicate.name = original.name + (count > 1 ? $" ({i + 1})" : " (Copy)");

                if (newParent != null)
                    duplicate.transform.SetParent(newParent);
                else
                    duplicate.transform.SetParent(original.transform.parent);

                Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {original.name}");
                created.Add(ToolUtils.GetGameObjectPath(duplicate));
            }

            return ToolUtils.BuildStringArrayResultWithCount("created", created, $"Duplicated {created.Count} object(s)");
        }
    }
}
