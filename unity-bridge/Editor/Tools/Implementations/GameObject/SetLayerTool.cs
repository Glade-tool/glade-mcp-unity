using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class SetLayerTool : ITool
    {
        public string Name => "set_layer";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            string layer = args.ContainsKey("layer") ? args["layer"]?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            if (string.IsNullOrEmpty(layer))
                return ToolUtils.CreateErrorResponse("layer is required");

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            int layerIndex = LayerMask.NameToLayer(layer);
            if (layerIndex == -1)
            {
                if (!int.TryParse(layer, out layerIndex) || layerIndex < 0 || layerIndex > 31)
                    return ToolUtils.CreateErrorResponse($"Invalid layer '{layer}'");
            }

            bool recursive = false;
            if (args.ContainsKey("recursive"))
            {
                if (args["recursive"] is bool b) recursive = b;
                else bool.TryParse(args["recursive"]?.ToString(), out recursive);
            }

            Undo.RecordObject(obj, $"Set Layer: {gameObjectPath}");
            obj.layer = layerIndex;
            int count = 1;
            if (recursive)
                count += SetLayerRecursive(obj.transform, layerIndex);

            string layerName = LayerMask.LayerToName(layerIndex);
            var extras = new Dictionary<string, object>
            {
                { "layer", layerName },
                { "layerIndex", layerIndex },
                { "objectsChanged", count }
            };
            return ToolUtils.CreateSuccessResponse($"Set layer to '{layerName}' on {count} object(s)", extras);
        }

        private static int SetLayerRecursive(UnityEngine.Transform parent, int layer)
        {
            int count = 0;
            foreach (UnityEngine.Transform child in parent)
            {
                Undo.RecordObject(child.gameObject, "Set Layer");
                child.gameObject.layer = layer;
                count++;
                count += SetLayerRecursive(child, layer);
            }
            return count;
        }
    }
}
