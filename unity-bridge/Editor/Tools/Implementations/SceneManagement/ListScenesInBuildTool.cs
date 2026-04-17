using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.SceneManagement
{
    public class ListScenesInBuildTool : ITool
    {
        public string Name => "list_scenes_in_build";

        public string Execute(Dictionary<string, object> args)
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = scenes.Select(s => s.path).ToList();
            var result = new Dictionary<string, object>
            {
                ["count"] = paths.Count,
                ["paths"] = paths
            };
            return ToolUtils.SerializeDictToJson(result);
        }
    }
}
