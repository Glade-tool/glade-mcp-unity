using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Scene
{
    public class SaveSceneTool : ITool
    {
        public string Name => "save_scene";

        public string Execute(Dictionary<string, object> args)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return ToolUtils.CreateErrorResponse("No active scene to save");

            bool success = EditorSceneManager.SaveScene(scene);
            if (!success)
                return ToolUtils.CreateErrorResponse($"Failed to save scene '{scene.name}'");

            var extras = new Dictionary<string, object> { { "scenePath", scene.path } };
            return ToolUtils.CreateSuccessResponse($"Saved scene '{scene.name}'", extras);
        }
    }
}
