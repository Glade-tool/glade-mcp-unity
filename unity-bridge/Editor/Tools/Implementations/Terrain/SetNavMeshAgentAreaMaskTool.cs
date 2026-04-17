using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetNavMeshAgentAreaMaskTool : ITool
    {
        public string Name => "set_navmesh_agent_area_mask";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            var agent = obj.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a NavMeshAgent component");
            }

            // Record object for undo before modifying properties
            Undo.RecordObject(agent, $"Set NavMeshAgent Area Mask: {gameObjectPath}");

            int areaMask = -1; // All areas by default

            if (args.ContainsKey("areaMask"))
            {
                if (args["areaMask"] is int i) areaMask = i;
                else if (int.TryParse(args["areaMask"].ToString(), out int mask))
                    areaMask = mask;
            }
            else if (args.ContainsKey("areaNames"))
            {
                // Build mask from area names
                if (args["areaNames"] is System.Collections.IEnumerable enumerable)
                {
                    var areaNames = new List<string>();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                        {
                            areaNames.Add(item.ToString());
                        }
                    }
                    areaMask = NavMesh.GetAreaFromName(areaNames[0]);
                    for (int i = 1; i < areaNames.Count; i++)
                    {
                        int area = NavMesh.GetAreaFromName(areaNames[i]);
                        if (area >= 0)
                        {
                            areaMask |= (1 << area);
                        }
                    }
                }
            }

            agent.areaMask = areaMask;

            var extras = new Dictionary<string, object>
            {
                { "areaMask", areaMask }
            };

            return ToolUtils.CreateSuccessResponse($"Set area mask for NavMeshAgent on '{gameObjectPath}'", extras);
        }
    }
}
