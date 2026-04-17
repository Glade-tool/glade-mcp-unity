using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class DistributeObjectsTool : ITool
    {
        public string Name => "distribute_objects";

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
                return ToolUtils.CreateErrorResponse("Need at least 2 objects to distribute. Provide gameObjectPaths or select objects.");
            }
            
            // Get all objects and sort by position on axis
            var objects = new List<UnityEngine.GameObject>();
            foreach (var path in paths)
            {
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null) objects.Add(obj);
            }
            
            if (objects.Count < 2)
            {
                return ToolUtils.CreateErrorResponse("Need at least 2 valid objects to distribute.");
            }
            
            // Sort by axis position
            objects.Sort((a, b) => ToolUtils.GetAxisValue(a.transform.position, axis).CompareTo(ToolUtils.GetAxisValue(b.transform.position, axis)));
            
            float spacing = 0f;
            bool hasFixedSpacing = false;
            if (args.ContainsKey("spacing"))
            {
                if (args["spacing"] is float f) spacing = f;
                else float.TryParse(args["spacing"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out spacing);
                hasFixedSpacing = true;
            }
            
            float startPos = ToolUtils.GetAxisValue(objects[0].transform.position, axis);
            if (args.ContainsKey("startPosition"))
            {
                if (args["startPosition"] is float f) startPos = f;
                else float.TryParse(args["startPosition"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out startPos);
            }
            
            // Calculate spacing if not fixed
            if (!hasFixedSpacing)
            {
                float endPos = ToolUtils.GetAxisValue(objects[objects.Count - 1].transform.position, axis);
                spacing = (endPos - startPos) / (objects.Count - 1);
            }
            
            // Apply distribution
            for (int i = 0; i < objects.Count; i++)
            {
                Undo.RecordObject(objects[i].transform, $"Distribute: {objects[i].name}");
                Vector3 pos = objects[i].transform.position;
                pos = ToolUtils.SetAxisValue(pos, axis, startPos + spacing * i);
                objects[i].transform.position = pos;
            }
            
            var extras = new Dictionary<string, object>
            {
                { "distributedCount", objects.Count },
                { "axis", axis },
                { "spacing", spacing }
            };
            
            return ToolUtils.CreateSuccessResponse($"Distributed {objects.Count} object(s) on {axis.ToUpper()} axis with spacing {spacing:F2}", extras);
        }
    }
}
