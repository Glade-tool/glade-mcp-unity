using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Handles CapsuleCollider creation, alignment, property read/write.
    /// Auto-alignment detects the dominant axis (X/Y/Z) so horizontal objects get the right orientation.
    /// TypeKey: "capsule"
    /// </summary>
    public class CapsuleColliderHandler : IColliderHandler
    {
        public string TypeKey => "capsule";

        public bool AlreadyExists(UnityEngine.GameObject obj) => obj.GetComponent<CapsuleCollider>() != null;

        public Collider AddComponent(UnityEngine.GameObject obj) => Undo.AddComponent<CapsuleCollider>(obj);

        public void ApplyArgs(Collider collider, Dictionary<string, object> args)
        {
            if (collider is not CapsuleCollider capsule) return;

            if (args.ContainsKey("isTrigger"))
                collider.isTrigger = ToolUtils.ParseBool(args["isTrigger"]);

            if (args.ContainsKey("center") && !string.IsNullOrWhiteSpace(args["center"]?.ToString()))
                capsule.center = ToolUtils.ParseVector3(args["center"].ToString());

            if (args.ContainsKey("radius") && args["radius"] != null &&
                float.TryParse(args["radius"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float r) && r > 0.01f)
                capsule.radius = r;

            if (args.ContainsKey("height") && args["height"] != null &&
                float.TryParse(args["height"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float h) && h > 0.01f)
                capsule.height = h;

            // direction: 0=X, 1=Y, 2=Z
            if (args.ContainsKey("direction") && args["direction"] != null &&
                int.TryParse(args["direction"].ToString(), out int dir) && dir >= 0 && dir <= 2)
                capsule.direction = dir;
        }

        public void ApplyAutoAlign(Collider collider, Bounds bounds)
        {
            if (collider is not CapsuleCollider capsule) return;

            Vector3 size = bounds.size;
            // Detect dominant axis
            int direction = 1; // Y default
            if (size.x >= size.y && size.x >= size.z)      direction = 0;
            else if (size.z >= size.x && size.z >= size.y) direction = 2;

            float height, radius;
            if (direction == 0)      { height = size.x; radius = Mathf.Max(size.y, size.z) * 0.5f; }
            else if (direction == 2) { height = size.z; radius = Mathf.Max(size.x, size.y) * 0.5f; }
            else                     { height = size.y; radius = Mathf.Max(size.x, size.z) * 0.5f; }

            capsule.center    = bounds.center;
            capsule.radius    = radius;
            capsule.height    = height;
            capsule.direction = direction;
        }

        public Dictionary<string, object> ReadProperties(Collider collider)
        {
            var props = new Dictionary<string, object>();
            if (collider is not CapsuleCollider capsule) return props;
            props["center"]    = $"{capsule.center.x},{capsule.center.y},{capsule.center.z}";
            props["radius"]    = capsule.radius;
            props["height"]    = capsule.height;
            props["direction"] = capsule.direction; // 0=X, 1=Y, 2=Z
            return props;
        }
    }
}
