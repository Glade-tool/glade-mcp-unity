using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class SphereCastTool : ITool
    {
        public string Name => "sphere_cast";

        public string Execute(Dictionary<string, object> args)
        {
            string originStr = args.ContainsKey("origin") ? args["origin"].ToString() : "";
            string directionStr = args.ContainsKey("direction") ? args["direction"].ToString() : "";

            if (string.IsNullOrEmpty(originStr))
                return ToolUtils.CreateErrorResponse("origin is required (format: 'x,y,z')");
            if (string.IsNullOrEmpty(directionStr))
                return ToolUtils.CreateErrorResponse("direction is required (format: 'x,y,z')");

            if (!TryParseVector3(originStr, out Vector3 origin))
                return ToolUtils.CreateErrorResponse($"Invalid origin format: '{originStr}'. Use 'x,y,z'");
            if (!TryParseVector3(directionStr, out Vector3 direction))
                return ToolUtils.CreateErrorResponse($"Invalid direction format: '{directionStr}'. Use 'x,y,z'");

            if (!args.ContainsKey("radius"))
                return ToolUtils.CreateErrorResponse("radius is required");
            if (!float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float radius))
                return ToolUtils.CreateErrorResponse("Invalid radius value");

            float maxDistance = Mathf.Infinity;
            if (args.ContainsKey("maxDistance") && float.TryParse(args["maxDistance"].ToString(),
                System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float md))
                maxDistance = md;

            int layerMask = UnityEngine.Physics.DefaultRaycastLayers;
            if (args.ContainsKey("layerMask") && int.TryParse(args["layerMask"].ToString(), out int lm))
                layerMask = lm;

            bool all = false;
            if (args.ContainsKey("all") && bool.TryParse(args["all"].ToString(), out bool a))
                all = a;

            if (all)
            {
                RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(origin, radius, direction.normalized, maxDistance, layerMask);
                if (hits.Length == 0)
                    return ToolUtils.CreateSuccessResponse("SphereCastAll missed",
                        new Dictionary<string, object> { { "hit", false }, { "hitCount", 0 } });

                System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
                var hitLines = new List<string>();
                foreach (var hit in hits)
                {
                    string path = ToolUtils.GetGameObjectPath(hit.collider.gameObject);
                    hitLines.Add($"{path} at dist={System.Math.Round(hit.distance, 4)}");
                }

                return ToolUtils.CreateSuccessResponse($"SphereCastAll hit {hits.Length} object(s)",
                    new Dictionary<string, object>
                    {
                        { "hit", true },
                        { "hitCount", hits.Length },
                        { "hits", string.Join("; ", hitLines) }
                    });
            }

            if (UnityEngine.Physics.SphereCast(origin, radius, direction.normalized, out RaycastHit singleHit, maxDistance, layerMask))
            {
                var extras = new Dictionary<string, object>
                {
                    { "hit", true },
                    { "gameObject", ToolUtils.GetGameObjectPath(singleHit.collider.gameObject) },
                    { "point", FormatVector3(singleHit.point) },
                    { "normal", FormatVector3(singleHit.normal) },
                    { "distance", System.Math.Round(singleHit.distance, 4) },
                    { "colliderType", singleHit.collider.GetType().Name }
                };
                return ToolUtils.CreateSuccessResponse("SphereCast hit", extras);
            }

            return ToolUtils.CreateSuccessResponse("SphereCast missed",
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
