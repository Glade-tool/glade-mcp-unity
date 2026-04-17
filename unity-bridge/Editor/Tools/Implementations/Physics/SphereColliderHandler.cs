using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Handles SphereCollider creation, alignment, property read/write.
    /// TypeKey: "sphere"
    /// </summary>
    public class SphereColliderHandler : IColliderHandler
    {
        public string TypeKey => "sphere";

        public bool AlreadyExists(UnityEngine.GameObject obj) => obj.GetComponent<SphereCollider>() != null;

        public Collider AddComponent(UnityEngine.GameObject obj) => Undo.AddComponent<SphereCollider>(obj);

        public void ApplyArgs(Collider collider, Dictionary<string, object> args)
        {
            if (collider is not SphereCollider sphere) return;

            if (args.ContainsKey("isTrigger"))
                collider.isTrigger = ToolUtils.ParseBool(args["isTrigger"]);

            if (args.ContainsKey("center") && !string.IsNullOrWhiteSpace(args["center"]?.ToString()))
                sphere.center = ToolUtils.ParseVector3(args["center"].ToString());

            if (args.ContainsKey("radius") && args["radius"] != null &&
                float.TryParse(args["radius"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float r) && r > 0.01f)
                sphere.radius = r;
        }

        public void ApplyAutoAlign(Collider collider, Bounds bounds)
        {
            if (collider is not SphereCollider sphere) return;
            sphere.center = bounds.center;
            sphere.radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
        }

        public Dictionary<string, object> ReadProperties(Collider collider)
        {
            var props = new Dictionary<string, object>();
            if (collider is not SphereCollider sphere) return props;
            props["center"] = $"{sphere.center.x},{sphere.center.y},{sphere.center.z}";
            props["radius"] = sphere.radius;
            return props;
        }
    }
}
