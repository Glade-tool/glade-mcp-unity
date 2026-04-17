using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Scene
{
    public class OpenSceneTool : ITool
    {
        public string Name => "open_scene";

        public string Execute(Dictionary<string, object> args)
        {
            string scenePath = args.ContainsKey("scenePath") ? args["scenePath"]?.ToString() : "";
            if (string.IsNullOrEmpty(scenePath))
                return ToolUtils.CreateErrorResponse("scenePath is required");

            if (!scenePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                scenePath = "Assets/" + scenePath;
            if (!scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                scenePath += ".unity";

            if (!File.Exists(scenePath))
                return ToolUtils.CreateErrorResponse($"Scene not found at '{scenePath}'");

            string modeStr = args.ContainsKey("mode") ? args["mode"]?.ToString().ToLower() : "single";
            var mode = modeStr == "additive" ? OpenSceneMode.Additive : OpenSceneMode.Single;

            if (SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            var scene = EditorSceneManager.OpenScene(scenePath, mode);
            if (!scene.IsValid())
                return ToolUtils.CreateErrorResponse($"Failed to open scene at '{scenePath}'");

            var extras = new Dictionary<string, object> { { "scenePath", scenePath } };
            return ToolUtils.CreateSuccessResponse($"Opened scene '{scene.name}'", extras);
        }
    }
}
