using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class RaycastTool : ITool
    {
        public string Name => "raycast";

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
                return ExecuteRaycastAll(origin, direction, maxDistance, layerMask);

            if (UnityEngine.Physics.Raycast(origin, direction.normalized, out RaycastHit hit, maxDistance, layerMask))
            {
                var extras = new Dictionary<string, object>
                {
                    { "hit", true },
                    { "gameObject", ToolUtils.GetGameObjectPath(hit.collider.gameObject) },
                    { "point", FormatVector3(hit.point) },
                    { "normal", FormatVector3(hit.normal) },
                    { "distance", System.Math.Round(hit.distance, 4) },
                    { "colliderName", hit.collider.name },
                    { "colliderType", hit.collider.GetType().Name }
                };
                return ToolUtils.CreateSuccessResponse("Raycast hit", extras);
            }

            return ToolUtils.CreateSuccessResponse("Raycast missed — no object hit",
                new Dictionary<string, object> { { "hit", false } });
        }

        private string ExecuteRaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            RaycastHit[] hits = UnityEngine.Physics.RaycastAll(origin, direction.normalized, maxDistance, layerMask);
            if (hits.Length == 0)
                return ToolUtils.CreateSuccessResponse("RaycastAll missed — no objects hit",
                    new Dictionary<string, object> { { "hit", false }, { "hitCount", 0 } });

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            var hitLines = new List<string>();
            foreach (var hit in hits)
            {
                string path = ToolUtils.GetGameObjectPath(hit.collider.gameObject);
                hitLines.Add($"{path} at {FormatVector3(hit.point)} (dist={System.Math.Round(hit.distance, 4)})");
            }

            var extras = new Dictionary<string, object>
            {
                { "hit", true },
                { "hitCount", hits.Length },
                { "hits", string.Join("; ", hitLines) }
            };
            return ToolUtils.CreateSuccessResponse($"RaycastAll hit {hits.Length} object(s)", extras);
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
