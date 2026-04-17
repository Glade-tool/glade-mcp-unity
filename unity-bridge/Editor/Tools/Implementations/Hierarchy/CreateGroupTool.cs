using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class CreateGroupTool : ITool
    {
        public string Name => "create_group";

        public string Execute(Dictionary<string, object> args)
        {
            string groupName = args.ContainsKey("groupName") ? args["groupName"].ToString() : "Group";
            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"].ToString() : "";

            var paths = ToolUtils.GetPathsFromArgsOrSelection(args, "gameObjectPaths");
            UnityEngine.Transform parent = null;

            if (!string.IsNullOrEmpty(parentPath))
            {
                UnityEngine.GameObject parentObj = ToolUtils.FindGameObjectByPath(parentPath);
                if (parentObj == null)
                {
                    return ToolUtils.CreateErrorResponse($"Parent GameObject '{parentPath}' not found");
                }
                parent = parentObj.transform;
            }

            bool centerPivot = true;
            if (args.ContainsKey("centerPivot"))
            {
                if (args["centerPivot"] is bool b) centerPivot = b;
                else bool.TryParse(args["centerPivot"].ToString(), out centerPivot);
            }

            var objects = new List<UnityEngine.GameObject>();
            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null) objects.Add(obj);
            }

            Vector3 center = Vector3.zero;
            if (centerPivot && objects.Count > 0)
            {
                Bounds combinedBounds = ToolUtils.GetObjectBounds(objects[0]);
                foreach (var obj in objects)
                {
                    combinedBounds.Encapsulate(ToolUtils.GetObjectBounds(obj));
                }
                center = combinedBounds.center;
            }

            UnityEngine.GameObject group = new UnityEngine.GameObject(groupName);
            group.transform.position = center;
            if (parent != null) group.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(group, $"Create Group: {groupName}");

            foreach (var obj in objects)
            {
                Undo.SetTransformParent(obj.transform, group.transform, $"Group {obj.name}");
            }

            var extras = new Dictionary<string, object>
            {
                { "groupPath", ToolUtils.GetGameObjectPath(group) },
                { "childCount", objects.Count }
            };
            return ToolUtils.CreateSuccessResponse($"Created group '{groupName}'", extras);
        }
    }
}
