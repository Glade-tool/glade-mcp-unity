using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Handles WheelCollider creation, alignment, property read/write.
    /// WheelCollider is used for vehicle wheel physics and requires a Rigidbody on the same or parent GameObject.
    /// Note: WheelCollider does not have isTrigger — that property is ignored.
    /// TypeKey: "wheel"
    /// </summary>
    public class WheelColliderHandler : IColliderHandler
    {
        public string TypeKey => "wheel";

        public bool AlreadyExists(UnityEngine.GameObject obj) => obj.GetComponent<WheelCollider>() != null;

        public Collider AddComponent(UnityEngine.GameObject obj) => Undo.AddComponent<WheelCollider>(obj);

        public void ApplyArgs(Collider collider, Dictionary<string, object> args)
        {
            if (collider is not WheelCollider wheel) return;

            if (args.ContainsKey("radius") && args["radius"] != null &&
                float.TryParse(args["radius"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float r) && r > 0.001f)
                wheel.radius = r;

            if (args.ContainsKey("suspensionDistance") && args["suspensionDistance"] != null &&
                float.TryParse(args["suspensionDistance"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float sd) && sd >= 0f)
                wheel.suspensionDistance = sd;

            if (args.ContainsKey("wheelMass") && args["wheelMass"] != null &&
                float.TryParse(args["wheelMass"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float mass) && mass > 0f)
                wheel.mass = mass;

            if (args.ContainsKey("forwardFriction") && args["forwardFriction"] != null &&
                float.TryParse(args["forwardFriction"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float ff))
            {
                var friction = wheel.forwardFriction;
                friction.stiffness = ff;
                wheel.forwardFriction = friction;
            }

            if (args.ContainsKey("sidewaysFriction") && args["sidewaysFriction"] != null &&
                float.TryParse(args["sidewaysFriction"].ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float sf))
            {
                var friction = wheel.sidewaysFriction;
                friction.stiffness = sf;
                wheel.sidewaysFriction = friction;
            }
        }

        public void ApplyAutoAlign(Collider collider, Bounds bounds)
        {
            if (collider is not WheelCollider wheel) return;
            // Estimate wheel radius from the smaller of X and Z extents (wheel cross-section)
            float estimatedRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);
            if (estimatedRadius > 0.001f) wheel.radius = estimatedRadius;
            // Suspension distance defaults to ~10% of wheel radius
            if (wheel.suspensionDistance < 0.001f) wheel.suspensionDistance = estimatedRadius * 0.1f;
        }

        public Dictionary<string, object> ReadProperties(Collider collider)
        {
            var props = new Dictionary<string, object>();
            if (collider is not WheelCollider wheel) return props;
            props["radius"]             = wheel.radius;
            props["suspensionDistance"] = wheel.suspensionDistance;
            props["wheelMass"]          = wheel.mass;
            props["forwardFriction"]    = wheel.forwardFriction.stiffness;
            props["sidewaysFriction"]   = wheel.sidewaysFriction.stiffness;
            props["motorTorque"]        = wheel.motorTorque;
            props["brakeTorque"]        = wheel.brakeTorque;
            props["steerAngle"]         = wheel.steerAngle;
            props["isGrounded"]         = wheel.isGrounded;
            return props;
        }
    }
}
