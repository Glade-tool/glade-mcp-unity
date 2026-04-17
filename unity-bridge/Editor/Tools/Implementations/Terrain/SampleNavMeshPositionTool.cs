using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SampleNavMeshPositionTool : ITool
    {
        public string Name => "sample_navmesh_position";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("position"))
            {
                return ToolUtils.CreateErrorResponse("position is required (as 'x,y,z')");
            }

            Vector3 position = ToolUtils.ParseVector3(args["position"].ToString());

            float maxDistance = 4.0f;
            if (args.ContainsKey("maxDistance"))
            {
                if (args["maxDistance"] is float f) maxDistance = f;
                else if (!float.TryParse(args["maxDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out maxDistance))
                    maxDistance = 4.0f;
            }

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

            NavMeshHit hit;
            bool found = NavMesh.SamplePosition(position, out hit, maxDistance, areaMask);

            var extras = new Dictionary<string, object>
            {
                { "found", found }
            };

            if (found)
            {
                extras["sampledPosition"] = $"{hit.position.x},{hit.position.y},{hit.position.z}";
                extras["distance"] = hit.distance;
                extras["mask"] = hit.mask;
            }
            else
            {
                extras["sampledPosition"] = "";
                extras["distance"] = 0f;
                extras["mask"] = 0;
            }

            return ToolUtils.CreateSuccessResponse($"Sampled NavMesh position", extras);
        }
    }
}
