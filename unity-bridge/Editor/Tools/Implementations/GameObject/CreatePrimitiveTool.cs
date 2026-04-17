using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class CreatePrimitiveTool : ITool
    {
        public string Name => "create_primitive";

        public string Execute(Dictionary<string, object> args)
        {
            string primitiveType = args.ContainsKey("primitiveType") ? args["primitiveType"]?.ToString() : "Cube";
            string name = args.ContainsKey("name") ? args["name"]?.ToString() : primitiveType;

            PrimitiveType type = primitiveType switch
            {
                "Cube" => PrimitiveType.Cube,
                "Sphere" => PrimitiveType.Sphere,
                "Capsule" => PrimitiveType.Capsule,
                "Cylinder" => PrimitiveType.Cylinder,
                "Plane" => PrimitiveType.Plane,
                "Quad" => PrimitiveType.Quad,
                _ => PrimitiveType.Cube
            };

            UnityEngine.GameObject obj = UnityEngine.GameObject.CreatePrimitive(type);
            obj.name = name;

            if (args.ContainsKey("parent") && args["parent"] != null)
            {
                string parentPath = args["parent"].ToString();
                UnityEngine.GameObject parent = ToolUtils.FindGameObjectByPath(parentPath);
                if (parent != null)
                    obj.transform.SetParent(parent.transform);
            }

            Undo.RegisterCreatedObjectUndo(obj, $"Create {primitiveType}: {name}");

            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", obj.name }
            };
            return ToolUtils.CreateSuccessResponse($"Created {primitiveType} named '{name}'", extras);
        }
    }
}
