using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class ListChildrenTool : ITool
    {
        public string Name => "list_children";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            bool recursive = false;
            if (args.ContainsKey("recursive"))
            {
                if (args["recursive"] is bool b) recursive = b;
                else bool.TryParse(args["recursive"]?.ToString(), out recursive);
            }
            bool includeInactive = true;
            if (args.ContainsKey("includeInactive"))
            {
                if (args["includeInactive"] is bool b) includeInactive = b;
                else bool.TryParse(args["includeInactive"]?.ToString(), out includeInactive);
            }

            var children = new List<string>();
            if (recursive)
                GetChildrenRecursive(obj.transform, children, includeInactive);
            else
            {
                foreach (UnityEngine.Transform child in obj.transform)
                {
                    if (includeInactive || child.gameObject.activeSelf)
                        children.Add(ToolUtils.GetGameObjectPath(child.gameObject));
                }
            }

            return ToolUtils.BuildStringArrayResultWithCount("children", children, $"Found {children.Count} child(ren)");
        }

        private static void GetChildrenRecursive(UnityEngine.Transform parent, List<string> results, bool includeInactive)
        {
            foreach (UnityEngine.Transform child in parent)
            {
                if (includeInactive || child.gameObject.activeSelf)
                {
                    results.Add(ToolUtils.GetGameObjectPath(child.gameObject));
                    GetChildrenRecursive(child, results, includeInactive);
                }
            }
        }
    }
}
