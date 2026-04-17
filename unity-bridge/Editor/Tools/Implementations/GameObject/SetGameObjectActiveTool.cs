using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class SetGameObjectActiveTool : ITool
    {
        public string Name => "set_game_object_active";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");

            if (!args.ContainsKey("active"))
                return ToolUtils.CreateErrorResponse("active is required");

            bool active = false;
            if (bool.TryParse(args["active"].ToString(), out bool parsedBool))
                active = parsedBool;
            else if (args["active"] is bool b)
                active = b;
            else
                return ToolUtils.CreateErrorResponse("active must be a boolean value");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            Undo.RecordObject(obj, $"Set Active: {gameObjectPath}");
            obj.SetActive(active);
            return ToolUtils.CreateSuccessResponse($"Set GameObject '{gameObjectPath}' active to {active.ToString().ToLower()}");
        }
    }
}
