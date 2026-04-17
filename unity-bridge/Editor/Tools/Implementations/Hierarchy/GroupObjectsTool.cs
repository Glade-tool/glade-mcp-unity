using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class GroupObjectsTool : ITool
    {
        public string Name => "group_objects";

        public string Execute(Dictionary<string, object> args)
        {
            var paths = ToolUtils.GetPathsFromArgsOrSelection(args, "gameObjectPaths");
            if (paths.Count == 0)
            {
                return ToolUtils.CreateErrorResponse("No objects to group. Provide gameObjectPaths or select objects.");
            }
            
            string groupName = args.ContainsKey("groupName") ? args["groupName"].ToString() : "Group";
            
            bool centerPivot = true;
            if (args.ContainsKey("centerPivot"))
            {
                if (args["centerPivot"] is bool b) centerPivot = b;
                else bool.TryParse(args["centerPivot"].ToString(), out centerPivot);
            }
            
            // Get all objects
            var objects = new List<UnityEngine.GameObject>();
            UnityEngine.Transform commonParent = null;
            bool firstParent = true;
            
            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null)
                {
                    objects.Add(obj);
                    if (firstParent)
                    {
                        commonParent = obj.transform.parent;
                        firstParent = false;
                    }
                }
            }
            
            if (objects.Count == 0)
            {
                return ToolUtils.CreateErrorResponse("No valid objects found to group.");
            }
            
            // Calculate center position
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
            
            // Create group
            UnityEngine.GameObject group = new UnityEngine.GameObject(groupName);
            group.transform.position = center;
            group.transform.SetParent(commonParent);
            
            Undo.RegisterCreatedObjectUndo(group, $"Create Group: {groupName}");
            
            // Parent all objects to group
            foreach (var obj in objects)
            {
                Undo.SetTransformParent(obj.transform, group.transform, $"Group {obj.name}");
            }
            
            var extras = new Dictionary<string, object>
            {
                { "groupPath", ToolUtils.GetGameObjectPath(group) },
                { "childCount", objects.Count }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created group '{groupName}' with {objects.Count} child(ren)", extras);
        }
    }
}
