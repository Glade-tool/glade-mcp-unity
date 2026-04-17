using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Handles BoxCollider creation, alignment, property read/write.
    /// TypeKey: "box"
    /// </summary>
    public class BoxColliderHandler : IColliderHandler
    {
        public string TypeKey => "box";

        public bool AlreadyExists(UnityEngine.GameObject obj) => obj.GetComponent<BoxCollider>() != null;

        public Collider AddComponent(UnityEngine.GameObject obj) => Undo.AddComponent<BoxCollider>(obj);

        public void ApplyArgs(Collider collider, Dictionary<string, object> args)
        {
            if (collider is not BoxCollider box) return;

            if (args.ContainsKey("isTrigger"))
                collider.isTrigger = ToolUtils.ParseBool(args["isTrigger"]);

            if (args.ContainsKey("center") && !string.IsNullOrWhiteSpace(args["center"]?.ToString()))
                box.center = ToolUtils.ParseVector3(args["center"].ToString());

            if (args.ContainsKey("size"))
            {
                string sizeStr = args["size"]?.ToString();
                if (!string.IsNullOrWhiteSpace(sizeStr))
                {
                    Vector3 size = ToolUtils.ParseVector3(sizeStr);
                    if (size.magnitude > 0.01f) box.size = size;
                }
            }
        }

        public void ApplyAutoAlign(Collider collider, Bounds bounds)
        {
            if (collider is not BoxCollider box) return;
            box.center = bounds.center;
            box.size   = bounds.size;
        }

        public Dictionary<string, object> ReadProperties(Collider collider)
        {
            var props = new Dictionary<string, object>();
            if (collider is not BoxCollider box) return props;
            props["center"] = $"{box.center.x},{box.center.y},{box.center.z}";
            props["size"]   = $"{box.size.x},{box.size.y},{box.size.z}";
            return props;
        }
    }
}
