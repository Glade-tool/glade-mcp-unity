using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class SetGameObjectParentTool : ITool
    {
        public string Name => "set_game_object_parent";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            bool worldPositionStays = true;
            if (args.ContainsKey("worldPositionStays"))
            {
                if (bool.TryParse(args["worldPositionStays"].ToString(), out bool parsedBool))
                    worldPositionStays = parsedBool;
                else if (args["worldPositionStays"] is bool b)
                    worldPositionStays = b;
            }

            UnityEngine.Transform newParent = null;
            if (args.ContainsKey("parentPath") && args["parentPath"] != null)
            {
                string parentPath = args["parentPath"].ToString();
                if (!string.IsNullOrEmpty(parentPath))
                {
                    UnityEngine.GameObject parentObj = ToolUtils.FindGameObjectByPath(parentPath);
                    if (parentObj != null)
                        newParent = parentObj.transform;
                    else
                        return ToolUtils.CreateErrorResponse($"Parent GameObject '{parentPath}' not found");
                }
            }

            Undo.RecordObject(obj.transform, $"Set Parent: {gameObjectPath}");
            obj.transform.SetParent(newParent, worldPositionStays);
            string parentName = newParent != null ? newParent.name : "root";
            return ToolUtils.CreateSuccessResponse($"Set parent of '{gameObjectPath}' to '{parentName}'");
        }
    }
}
