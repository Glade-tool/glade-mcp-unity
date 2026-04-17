using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class DestroyGameObjectTool : ITool
    {
        public string Name => "destroy_game_object";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("path") ? args["path"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("path is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            Undo.DestroyObjectImmediate(obj);
            return ToolUtils.CreateSuccessResponse($"Destroyed GameObject '{gameObjectPath}'");
        }
    }
}
