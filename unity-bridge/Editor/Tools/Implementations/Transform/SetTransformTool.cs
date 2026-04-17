using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Transform
{
    public class SetTransformTool : ITool
    {
        public string Name => "set_transform";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            // Capture previous state BEFORE modification
            var prevPos = obj.transform.position;
            var prevRot = obj.transform.rotation.eulerAngles;
            var prevScale = obj.transform.localScale;

            string operation = args.ContainsKey("operation") ? args["operation"]?.ToString() : "set";

            if (args.ContainsKey("position"))
            {
                var pos = ToolUtils.ParseVector3(args["position"].ToString());
                if (operation == "add")
                    obj.transform.position += pos;
                else if (operation == "multiply")
                {
                    var current = obj.transform.position;
                    obj.transform.position = new Vector3(current.x * pos.x, current.y * pos.y, current.z * pos.z);
                }
                else
                    obj.transform.position = pos;
            }

            if (args.ContainsKey("rotation"))
            {
                var rot = ToolUtils.ParseVector3(args["rotation"].ToString());
                var eulerRot = Quaternion.Euler(rot);
                if (operation == "add")
                    obj.transform.rotation *= eulerRot;
                else if (operation == "multiply")
                {
                    var current = obj.transform.rotation.eulerAngles;
                    var newRot = new Vector3(current.x * rot.x, current.y * rot.y, current.z * rot.z);
                    obj.transform.rotation = Quaternion.Euler(newRot);
                }
                else
                    obj.transform.rotation = eulerRot;
            }

            if (args.ContainsKey("scale"))
            {
                var scale = ToolUtils.ParseVector3(args["scale"].ToString());
                if (operation == "add")
                    obj.transform.localScale += scale;
                else if (operation == "multiply")
                {
                    var current = obj.transform.localScale;
                    obj.transform.localScale = new Vector3(current.x * scale.x, current.y * scale.y, current.z * scale.z);
                }
                else
                    obj.transform.localScale = scale;
            }

            // Return previous state in response for revert capability
            var extras = new Dictionary<string, object>
            {
                ["previousState"] = new Dictionary<string, object>
                {
                    ["position"] = $"{prevPos.x},{prevPos.y},{prevPos.z}",
                    ["rotation"] = $"{prevRot.x},{prevRot.y},{prevRot.z}",
                    ["scale"] = $"{prevScale.x},{prevScale.y},{prevScale.z}",
                    ["isLocal"] = false
                }
            };

            return ToolUtils.CreateSuccessResponse($"Updated transform for '{gameObjectPath}' (operation: {operation})", extras);
        }
    }
}
