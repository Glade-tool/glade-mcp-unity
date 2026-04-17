using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetNavMeshAreaCostTool : ITool
    {
        public string Name => "set_navmesh_area_cost";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("areaIndex"))
            {
                return ToolUtils.CreateErrorResponse("areaIndex is required");
            }

            if (!args.ContainsKey("cost"))
            {
                return ToolUtils.CreateErrorResponse("cost is required");
            }

            int areaIndex = 0;
            if (args["areaIndex"] is int i) areaIndex = i;
            else if (!int.TryParse(args["areaIndex"].ToString(), out areaIndex))
            {
                return ToolUtils.CreateErrorResponse("areaIndex must be a valid integer");
            }

            if (areaIndex < 0 || areaIndex >= 32)
            {
                return ToolUtils.CreateErrorResponse("areaIndex must be between 0 and 31");
            }

            float cost = 1.0f;
            if (args["cost"] is float f) cost = f;
            else if (args["cost"] is double d) cost = (float)d;
            else if (!float.TryParse(args["cost"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out cost))
            {
                return ToolUtils.CreateErrorResponse("cost must be a valid number");
            }

            if (cost < 0.01f || cost > 1000.0f)
            {
                return ToolUtils.CreateErrorResponse("cost must be between 0.01 and 1000.0");
            }

            string areaName = GetAreaName(areaIndex);
            NavMesh.SetAreaCost(areaIndex, cost);

            var extras = new Dictionary<string, object>
            {
                { "areaIndex", areaIndex },
                { "areaName", areaName ?? "Unknown" },
                { "cost", cost }
            };

            return ToolUtils.CreateSuccessResponse($"Set cost for NavMesh area '{areaName}' (index {areaIndex}) to {cost}", extras);
        }

        private string GetAreaName(int areaIndex)
        {
            // Try to get area name using reflection (Unity internal API)
            try
            {
                var navMeshAreaType = System.Type.GetType("UnityEditorInternal.NavMeshArea, UnityEditor");
                if (navMeshAreaType != null)
                {
                    var getAreaNameMethod = navMeshAreaType.GetMethod("GetAreaName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (getAreaNameMethod != null)
                    {
                        var result = getAreaNameMethod.Invoke(null, new object[] { areaIndex });
                        return result?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                // Fallback: return standard area names
            }

            // Standard Unity NavMesh area names
            string[] standardNames = {
                "Walkable", "Not Walkable", "Jump", "", "", "", "", "",
                "", "", "", "", "", "", "", "",
                "", "", "", "", "", "", "", "",
                "", "", "", "", "", "", "", ""
            };

            if (areaIndex >= 0 && areaIndex < standardNames.Length)
            {
                return standardNames[areaIndex];
            }

            return $"Area {areaIndex}";
        }
    }
}
