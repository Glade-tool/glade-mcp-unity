using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class SetTagTool : ITool
    {
        public string Name => "set_tag";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            string tag = args.ContainsKey("tag") ? args["tag"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            if (string.IsNullOrEmpty(tag))
                return ToolUtils.CreateErrorResponse("tag is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            try
            {
                obj.CompareTag(tag);
            }
            catch
            {
                return ToolUtils.CreateErrorResponse($"Tag '{tag}' does not exist. Add it in Edit > Project Settings > Tags and Layers.");
            }

            string oldTag = obj.tag;
            Undo.RecordObject(obj, $"Set Tag: {gameObjectPath}");
            obj.tag = tag;

            var extras = new Dictionary<string, object>
            {
                { "oldTag", oldTag },
                { "newTag", tag }
            };
            return ToolUtils.CreateSuccessResponse($"Set tag to '{tag}' on '{gameObjectPath}'", extras);
        }
    }
}
