using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class CalculateNavMeshPathTool : ITool
    {
        public string Name => "calculate_navmesh_path";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("startPosition"))
            {
                return ToolUtils.CreateErrorResponse("startPosition is required (as 'x,y,z')");
            }

            if (!args.ContainsKey("endPosition"))
            {
                return ToolUtils.CreateErrorResponse("endPosition is required (as 'x,y,z')");
            }

            Vector3 startPos = ToolUtils.ParseVector3(args["startPosition"].ToString());
            Vector3 endPos = ToolUtils.ParseVector3(args["endPosition"].ToString());

            int agentTypeID = 0;
            if (args.ContainsKey("agentTypeID"))
            {
                if (args["agentTypeID"] is int i) agentTypeID = i;
                else if (!int.TryParse(args["agentTypeID"].ToString(), out agentTypeID))
                    agentTypeID = 0;
            }

            int areaMask = -1; // All areas
            if (args.ContainsKey("areaMask"))
            {
                if (args["areaMask"] is int i) areaMask = i;
                else if (!int.TryParse(args["areaMask"].ToString(), out areaMask))
                    areaMask = -1;
            }

            NavMeshPath path = new NavMeshPath();
            bool pathFound = NavMesh.CalculatePath(startPos, endPos, areaMask, path);

            var corners = new List<string>();
            float pathLength = 0f;

            if (pathFound && path.status == NavMeshPathStatus.PathComplete)
            {
                foreach (var corner in path.corners)
                {
                    corners.Add($"{corner.x},{corner.y},{corner.z}");
                }

                // Calculate path length
                if (path.corners.Length > 1)
                {
                    for (int i = 1; i < path.corners.Length; i++)
                    {
                        pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                    }
                }
            }

            var extras = new Dictionary<string, object>
            {
                { "pathExists", pathFound },
                { "pathStatus", path.status.ToString() },
                { "pathLength", pathLength },
                { "cornerCount", path.corners.Length },
                { "corners", corners }
            };

            return ToolUtils.CreateSuccessResponse($"Calculated NavMesh path", extras);
        }
    }
}
