using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Selection
{
    public class GetSelectionTool : ITool
    {
        public string Name => "get_selection";

        public string Execute(Dictionary<string, object> args)
        {
            var selected = UnityEditor.Selection.gameObjects;
            var paths = new List<string>();
            foreach (var obj in selected)
            {
                if (obj != null)
                    paths.Add(ToolUtils.GetGameObjectPath(obj));
            }
            return ToolUtils.BuildStringArrayResultWithCount("selected", paths, $"{paths.Count} object(s) selected");
        }
    }
}
