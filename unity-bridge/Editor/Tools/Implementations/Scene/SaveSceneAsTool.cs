using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Scene
{
    public class SaveSceneAsTool : ITool
    {
        public string Name => "save_scene_as";

        public string Execute(Dictionary<string, object> args)
        {
            string scenePath = args.ContainsKey("scenePath") ? args["scenePath"]?.ToString() : "";
            if (string.IsNullOrEmpty(scenePath))
                return ToolUtils.CreateErrorResponse("scenePath is required");

            if (!scenePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                scenePath = "Assets/" + scenePath;
            if (!scenePath.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
                scenePath += ".unity";

            string dir = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                ToolUtils.EnsureAssetFolder(dir);

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return ToolUtils.CreateErrorResponse("No active scene to save");

            bool success = EditorSceneManager.SaveScene(scene, scenePath);
            if (!success)
                return ToolUtils.CreateErrorResponse($"Failed to save scene as '{scenePath}'");

            var extras = new Dictionary<string, object> { { "scenePath", scenePath } };
            return ToolUtils.CreateSuccessResponse($"Saved scene as '{scenePath}'", extras);
        }
    }
}
