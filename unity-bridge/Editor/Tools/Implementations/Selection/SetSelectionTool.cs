using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Selection
{
    public class SetSelectionTool : ITool
    {
        public string Name => "set_selection";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("paths"))
                return ToolUtils.CreateErrorResponse("paths is required");

            bool addToSelection = false;
            if (args.ContainsKey("addToSelection"))
            {
                if (args["addToSelection"] is bool b) addToSelection = b;
                else bool.TryParse(args["addToSelection"]?.ToString(), out addToSelection);
            }

            var pathsObj = args["paths"];
            var paths = new List<string>();
            if (pathsObj is List<object> pathList)
            {
                foreach (var p in pathList)
                    paths.Add(p?.ToString() ?? "");
            }
            else if (pathsObj is string pathStr)
            {
                if (pathStr.StartsWith("["))
                {
                    pathStr = pathStr.Trim('[', ']');
                    foreach (var p in pathStr.Split(','))
                        paths.Add(p.Trim().Trim('"'));
                }
                else
                {
                    paths.Add(pathStr);
                }
            }

            var objects = new List<UnityEngine.Object>();
            if (addToSelection)
                objects.AddRange(UnityEditor.Selection.objects);

            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null)
                    objects.Add(obj);
            }

            UnityEditor.Selection.objects = objects.ToArray();
            return ToolUtils.CreateSuccessResponse($"Selected {objects.Count} object(s)");
        }
    }
}
