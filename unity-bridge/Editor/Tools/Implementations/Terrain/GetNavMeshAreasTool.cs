using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class GetNavMeshAreasTool : ITool
    {
        public string Name => "get_navmesh_areas";

        public string Execute(Dictionary<string, object> args)
        {
            var areas = new List<Dictionary<string, object>>();
            
            // Get all area names (Unity supports up to 32 areas, indexed 0-31)
            // Try to get area names using reflection or return index/cost only
            for (int i = 0; i < 32; i++)
            {
                float cost = NavMesh.GetAreaCost(i);
                string areaName = GetAreaName(i);
                
                // Only add areas that have been configured (non-zero cost or have a name)
                if (!string.IsNullOrEmpty(areaName) || cost != 1.0f)
                {
                    areas.Add(new Dictionary<string, object>
                    {
                        { "index", i },
                        { "name", areaName ?? $"Area {i}" },
                        { "cost", cost }
                    });
                }
            }

            var extras = new Dictionary<string, object>
            {
                { "areas", areas },
                { "count", areas.Count }
            };

            return ToolUtils.CreateSuccessResponse($"Retrieved {areas.Count} NavMesh areas", extras);
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

            return "";
        }
    }
}
