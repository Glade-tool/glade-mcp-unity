using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class CreateGameObjectTool : ITool
    {
        public string Name => "create_game_object";

        public string Execute(Dictionary<string, object> args)
        {
            string name = args.ContainsKey("name") ? args["name"]?.ToString() : "GameObject";
            UnityEngine.GameObject obj = new UnityEngine.GameObject(name);

            if (args.ContainsKey("parent") && args["parent"] != null)
            {
                string parentPath = args["parent"].ToString();
                UnityEngine.GameObject parent = ToolUtils.FindGameObjectByPath(parentPath);
                if (parent != null)
                    obj.transform.SetParent(parent.transform);
            }

            Undo.RegisterCreatedObjectUndo(obj, $"Create GameObject: {name}");

            var extras = new Dictionary<string, object> { { "gameObjectPath", obj.name } };
            return ToolUtils.CreateSuccessResponse($"Created GameObject named '{name}'", extras);
        }
    }
}
