using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Transform
{
    public class SetLocalTransformTool : ITool
    {
        public string Name => "set_local_transform";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            string operation = args.ContainsKey("operation") ? args["operation"]?.ToString() : "set";

            if (args.ContainsKey("position"))
            {
                var pos = ToolUtils.ParseVector3(args["position"].ToString());
                if (operation == "add")
                    obj.transform.localPosition += pos;
                else if (operation == "multiply")
                {
                    var current = obj.transform.localPosition;
                    obj.transform.localPosition = new Vector3(current.x * pos.x, current.y * pos.y, current.z * pos.z);
                }
                else
                    obj.transform.localPosition = pos;
            }

            if (args.ContainsKey("rotation"))
            {
                var rot = ToolUtils.ParseVector3(args["rotation"].ToString());
                var eulerRot = Quaternion.Euler(rot);
                if (operation == "add")
                    obj.transform.localRotation *= eulerRot;
                else if (operation == "multiply")
                {
                    var current = obj.transform.localRotation.eulerAngles;
                    var newRot = new Vector3(current.x * rot.x, current.y * rot.y, current.z * rot.z);
                    obj.transform.localRotation = Quaternion.Euler(newRot);
                }
                else
                    obj.transform.localRotation = eulerRot;
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

            Undo.RecordObject(obj.transform, $"Set Local Transform: {gameObjectPath}");
            return ToolUtils.CreateSuccessResponse($"Updated local transform for '{gameObjectPath}' (operation: {operation})");
        }
    }
}
