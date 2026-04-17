using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class AlignObjectsTool : ITool
    {
        public string Name => "align_objects";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("axis"))
            {
                return ToolUtils.CreateErrorResponse("axis is required");
            }
            
            string axis = args["axis"].ToString().ToLower();
            if (axis != "x" && axis != "y" && axis != "z")
            {
                return ToolUtils.CreateErrorResponse("axis must be 'x', 'y', or 'z'");
            }
            
            var paths = ToolUtils.GetPathsFromArgsOrSelection(args, "gameObjectPaths");
            if (paths.Count < 2)
            {
                return ToolUtils.CreateErrorResponse("Need at least 2 objects to align. Provide gameObjectPaths or select objects.");
            }
            
            string alignTo = args.ContainsKey("alignTo") ? args["alignTo"].ToString().ToLower() : "first";
            string targetPath = args.ContainsKey("targetPath") ? args["targetPath"].ToString() : "";
            
            // Get all objects
            var objects = new List<UnityEngine.GameObject>();
            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null) objects.Add(obj);
            }
            
            if (objects.Count < 2)
            {
                return ToolUtils.CreateErrorResponse("Need at least 2 valid objects to align.");
            }
            
            // Determine target position
            float targetValue = 0f;
            
            if (!string.IsNullOrEmpty(targetPath))
            {
                UnityEngine.GameObject target = ToolUtils.FindGameObjectByPath(targetPath);
                if (target != null)
                    targetValue = ToolUtils.GetAxisValue(target.transform.position, axis);
            }
            else if (alignTo == "first")
            {
                targetValue = ToolUtils.GetAxisValue(objects[0].transform.position, axis);
            }
            else
            {
                // Calculate bounds of all objects
                float min = float.MaxValue, max = float.MinValue;
                foreach (var obj in objects)
                {
                    float val = ToolUtils.GetAxisValue(obj.transform.position, axis);
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
                
                if (alignTo == "min") targetValue = min;
                else if (alignTo == "max") targetValue = max;
                else if (alignTo == "center") targetValue = (min + max) / 2f;
            }
            
            // Apply alignment
            int alignedCount = 0;
            foreach (var obj in objects)
            {
                Undo.RecordObject(obj.transform, $"Align: {obj.name}");
                Vector3 pos = obj.transform.position;
                pos = ToolUtils.SetAxisValue(pos, axis, targetValue);
                obj.transform.position = pos;
                alignedCount++;
            }
            
            var extras = new Dictionary<string, object>
            {
                { "alignedCount", alignedCount },
                { "axis", axis },
                { "alignTo", alignTo }
            };
            
            return ToolUtils.CreateSuccessResponse($"Aligned {alignedCount} object(s) on {axis.ToUpper()} axis", extras);
        }
    }
}
