using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class RenameGameObjectTool : ITool
    {
        public string Name => "rename_game_object";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            string newName = args.ContainsKey("newName") ? args["newName"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            if (string.IsNullOrEmpty(newName))
                return ToolUtils.CreateErrorResponse("newName is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            string oldName = obj.name;
            Undo.RecordObject(obj, $"Rename {oldName} to {newName}");
            obj.name = newName;

            var extras = new Dictionary<string, object>
            {
                { "oldName", oldName },
                { "newName", newName }
            };
            return ToolUtils.CreateSuccessResponse($"Renamed '{oldName}' to '{newName}'", extras);
        }
    }
}
