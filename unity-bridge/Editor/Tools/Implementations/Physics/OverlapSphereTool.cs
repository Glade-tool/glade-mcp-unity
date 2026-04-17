using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class OverlapSphereTool : ITool
    {
        public string Name => "overlap_sphere";

        public string Execute(Dictionary<string, object> args)
        {
            string centerStr = args.ContainsKey("center") ? args["center"].ToString() : "";
            if (string.IsNullOrEmpty(centerStr))
                return ToolUtils.CreateErrorResponse("center is required (format: 'x,y,z')");

            if (!TryParseVector3(centerStr, out Vector3 center))
                return ToolUtils.CreateErrorResponse($"Invalid center format: '{centerStr}'. Use 'x,y,z'");

            if (!args.ContainsKey("radius"))
                return ToolUtils.CreateErrorResponse("radius is required");
            if (!float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float radius))
                return ToolUtils.CreateErrorResponse("Invalid radius value");

            int layerMask = UnityEngine.Physics.DefaultRaycastLayers;
            if (args.ContainsKey("layerMask") && int.TryParse(args["layerMask"].ToString(), out int lm))
                layerMask = lm;

            Collider[] colliders = UnityEngine.Physics.OverlapSphere(center, radius, layerMask);

            if (colliders.Length == 0)
            {
                return ToolUtils.CreateSuccessResponse("No colliders found in sphere",
                    new Dictionary<string, object> { { "count", 0 } });
            }

            var hitLines = new List<string>();
            foreach (var col in colliders)
            {
                string path = ToolUtils.GetGameObjectPath(col.gameObject);
                hitLines.Add($"{path} ({col.GetType().Name})");
            }

            var extras = new Dictionary<string, object>
            {
                { "count", colliders.Length },
                { "colliders", string.Join("; ", hitLines) }
            };
            return ToolUtils.CreateSuccessResponse($"Found {colliders.Length} collider(s) in sphere", extras);
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
    }
}
