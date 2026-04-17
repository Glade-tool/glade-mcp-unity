using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class LinecastTool : ITool
    {
        public string Name => "linecast";

        public string Execute(Dictionary<string, object> args)
        {
            string startStr = args.ContainsKey("start") ? args["start"].ToString() : "";
            string endStr = args.ContainsKey("end") ? args["end"].ToString() : "";

            if (string.IsNullOrEmpty(startStr))
                return ToolUtils.CreateErrorResponse("start is required (format: 'x,y,z')");
            if (string.IsNullOrEmpty(endStr))
                return ToolUtils.CreateErrorResponse("end is required (format: 'x,y,z')");

            if (!TryParseVector3(startStr, out Vector3 start))
                return ToolUtils.CreateErrorResponse($"Invalid start format: '{startStr}'. Use 'x,y,z'");
            if (!TryParseVector3(endStr, out Vector3 end))
                return ToolUtils.CreateErrorResponse($"Invalid end format: '{endStr}'. Use 'x,y,z'");

            int layerMask = UnityEngine.Physics.DefaultRaycastLayers;
            if (args.ContainsKey("layerMask") && int.TryParse(args["layerMask"].ToString(), out int lm))
                layerMask = lm;

            if (UnityEngine.Physics.Linecast(start, end, out RaycastHit hit, layerMask))
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
                return ToolUtils.CreateSuccessResponse("Linecast hit", extras);
            }

            return ToolUtils.CreateSuccessResponse("Linecast — no obstruction between start and end",
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
