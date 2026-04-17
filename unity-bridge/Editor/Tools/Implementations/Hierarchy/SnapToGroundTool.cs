using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class SnapToGroundTool : ITool
    {
        public string Name => "snap_to_ground";

        public string Execute(Dictionary<string, object> args)
        {
            var paths = ToolUtils.GetPathsFromArgsOrSelection(args, "gameObjectPaths");
            if (paths.Count == 0)
            {
                return ToolUtils.CreateErrorResponse("No objects to snap. Provide gameObjectPaths or select objects.");
            }
            
            float offset = 0f;
            if (args.ContainsKey("offset"))
            {
                if (args["offset"] is float f) offset = f;
                else float.TryParse(args["offset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out offset);
            }
            
            float maxDistance = 1000f;
            if (args.ContainsKey("maxDistance"))
            {
                if (args["maxDistance"] is float f) maxDistance = f;
                else float.TryParse(args["maxDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out maxDistance);
            }
            
            int layerMask = -1; // Everything
            if (args.ContainsKey("layerMask"))
            {
                string maskStr = args["layerMask"].ToString();
                if (maskStr.ToLower() != "everything")
                {
                    int layer = LayerMask.NameToLayer(maskStr);
                    if (layer != -1)
                        layerMask = 1 << layer;
                }
            }
            
            int snappedCount = 0;
            var snapped = new List<string>();
            
            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj == null) continue;
                
                // Raycast down from object
                Vector3 origin = obj.transform.position + Vector3.up * 0.1f; // Slight offset up
                if (UnityEngine.Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, layerMask))
                {
                    Undo.RecordObject(obj.transform, $"Snap to Ground: {obj.name}");
                    
                    // Account for object bounds
                    Bounds bounds = ToolUtils.GetObjectBounds(obj);
                    float bottomOffset = obj.transform.position.y - bounds.min.y;
                    
                    obj.transform.position = new Vector3(
                        obj.transform.position.x,
                        hit.point.y + bottomOffset + offset,
                        obj.transform.position.z
                    );
                    
                    snappedCount++;
                    snapped.Add(path);
                }
            }
            
            var extras = new Dictionary<string, object>
            {
                { "snappedCount", snappedCount }
            };
            
            return ToolUtils.CreateSuccessResponse($"Snapped {snappedCount} of {paths.Count} object(s) to ground", extras);
        }
    }
}
