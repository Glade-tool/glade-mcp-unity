using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class BoxCastTool : ITool
    {
        public string Name => "box_cast";

        public string Execute(Dictionary<string, object> args)
        {
            string centerStr = args.ContainsKey("center") ? args["center"].ToString() : "";
            string halfExtentsStr = args.ContainsKey("halfExtents") ? args["halfExtents"].ToString() : "";
            string directionStr = args.ContainsKey("direction") ? args["direction"].ToString() : "";

            if (string.IsNullOrEmpty(centerStr))
                return ToolUtils.CreateErrorResponse("center is required (format: 'x,y,z')");
            if (string.IsNullOrEmpty(halfExtentsStr))
                return ToolUtils.CreateErrorResponse("halfExtents is required (format: 'x,y,z')");
            if (string.IsNullOrEmpty(directionStr))
                return ToolUtils.CreateErrorResponse("direction is required (format: 'x,y,z')");

            if (!TryParseVector3(centerStr, out Vector3 center))
                return ToolUtils.CreateErrorResponse($"Invalid center format. Use 'x,y,z'");
            if (!TryParseVector3(halfExtentsStr, out Vector3 halfExtents))
                return ToolUtils.CreateErrorResponse($"Invalid halfExtents format. Use 'x,y,z'");
            if (!TryParseVector3(directionStr, out Vector3 direction))
                return ToolUtils.CreateErrorResponse($"Invalid direction format. Use 'x,y,z'");

            Quaternion orientation = Quaternion.identity;
            if (args.ContainsKey("orientation"))
            {
                string oriStr = args["orientation"].ToString();
                if (TryParseVector3(oriStr, out Vector3 euler))
                    orientation = Quaternion.Euler(euler);
            }

            float maxDistance = Mathf.Infinity;
            if (args.ContainsKey("maxDistance") && float.TryParse(args["maxDistance"].ToString(),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float md))
                maxDistance = md;

            int layerMask = UnityEngine.Physics.DefaultRaycastLayers;
            if (args.ContainsKey("layerMask") && int.TryParse(args["layerMask"].ToString(), out int lm))
                layerMask = lm;

            if (UnityEngine.Physics.BoxCast(center, halfExtents, direction.normalized, out RaycastHit hit, orientation, maxDistance, layerMask))
            {
                var extras = new Dictionary<string, object>
                {
                    { "hit", true },
                    { "gameObject", ToolUtils.GetGameObjectPath(hit.collider.gameObject) },
                    { "point", FormatVector3(hit.point) },
                    { "normal", FormatVector3(hit.normal) },
                    { "distance", System.Math.Round(hit.distance, 4) },
                    { "colliderType", hit.collider.GetType().Name }
                };
                return ToolUtils.CreateSuccessResponse("BoxCast hit", extras);
            }

            return ToolUtils.CreateSuccessResponse("BoxCast missed",
                new Dictionary<string, object> { { "hit", false } });
        }

        private static bool TryParseVector3(string s, out Vector3 v)
        {
            v = Vector3.zero;
            var parts = s.Split(',');
            if (parts.Length != 3) return false;
            if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)) return false;
            if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y)) return false;
            if (!float.TryParse(parts[2].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) return false;
            v = new Vector3(x, y, z);
            return true;
        }

        private static string FormatVector3(Vector3 v)
        {
            return $"{v.x:F4},{v.y:F4},{v.z:F4}";
        }
    }
}
