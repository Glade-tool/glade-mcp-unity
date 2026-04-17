using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.SceneManagement
{
    public class CreateSceneTool : ITool
    {
        public string Name => "create_scene";

        public string Execute(Dictionary<string, object> args)
        {
            string scenePath = args.ContainsKey("scenePath") ? args["scenePath"].ToString() : "";
            NewSceneSetup setup = NewSceneSetup.DefaultGameObjects;
            NewSceneMode mode = NewSceneMode.Single;

            if (args.ContainsKey("setup") && System.Enum.TryParse(args["setup"].ToString(), true, out NewSceneSetup setupVal))
                setup = setupVal;
            if (args.ContainsKey("mode") && System.Enum.TryParse(args["mode"].ToString(), true, out NewSceneMode modeVal))
                mode = modeVal;

            var scene = EditorSceneManager.NewScene(setup, mode);

            if (!string.IsNullOrEmpty(scenePath))
            {
                if (!scenePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    scenePath = "Assets/" + scenePath;
                string dir = System.IO.Path.GetDirectoryName(scenePath);
                if (!UnityEditor.AssetDatabase.IsValidFolder(dir))
                {
                    ToolUtils.EnsureAssetFolder(dir);
                }
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            var extras = new Dictionary<string, object>
            {
                { "scenePath", scene.path }
            };
            return ToolUtils.CreateSuccessResponse("Created scene", extras);
        }
    }
}
