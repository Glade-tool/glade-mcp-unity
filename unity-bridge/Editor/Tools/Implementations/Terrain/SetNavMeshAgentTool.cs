using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetNavMeshAgentTool : ITool
    {
        public string Name => "set_navmesh_agent";

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

            bool wasNewAgent = obj.GetComponent<NavMeshAgent>() == null;
            var agent = obj.GetComponent<NavMeshAgent>() ?? Undo.AddComponent<NavMeshAgent>(obj);
            
            // Ensure GameObject and agent are active and enabled for proper initialization
            if (!obj.activeSelf)
            {
                obj.SetActive(true);
            }
            if (!agent.enabled)
            {
                agent.enabled = true;
            }
            
            // Record object for undo before modifying properties
            Undo.RecordObject(agent, $"Set NavMeshAgent Properties: {gameObjectPath}");
            
            if (args.ContainsKey("radius") && float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius)) agent.radius = radius;
            if (args.ContainsKey("height") && float.TryParse(args["height"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float height)) agent.height = height;
            if (args.ContainsKey("speed") && float.TryParse(args["speed"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float speed)) agent.speed = speed;
            if (args.ContainsKey("acceleration") && float.TryParse(args["acceleration"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float accel)) agent.acceleration = accel;
            if (args.ContainsKey("angularSpeed") && float.TryParse(args["angularSpeed"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float angular)) agent.angularSpeed = angular;
            if (args.ContainsKey("stoppingDistance") && float.TryParse(args["stoppingDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float stop)) agent.stoppingDistance = stop;

            // Place agent on NavMesh using editor-safe operations only
            // Note: We don't check isOnNavMesh or use Warp() as these are runtime-only
            // We just sample a valid position and set the transform directly
            // The agent will be recognized on NavMesh at runtime
            
            // Record transform for undo before positioning
            Undo.RecordObject(agent.transform, $"Place NavMeshAgent on NavMesh: {gameObjectPath}");
            
            // Try to find a valid position on NavMesh
            NavMeshHit hit;
            Vector3 currentPosition = agent.transform.position;
            Vector3 sampledPosition = Vector3.zero;
            bool foundValidPosition = false;
            
            // Use the agent's area mask for sampling
            int areaMask = agent.areaMask;
            
            // Strategy: Try multiple positions with increasing search radius
            Vector3[] testPositions = new Vector3[]
            {
                currentPosition,
                new Vector3(currentPosition.x, 0, currentPosition.z),
                Vector3.zero,
                new Vector3(0, 0.1f, 0),
                new Vector3(0, 0.5f, 0),
            };
            
            float[] searchRadii = new float[] { 1.0f, 5.0f, 10.0f, 50.0f, 100.0f };
            
            // Try with agent's area mask first
            foreach (Vector3 testPos in testPositions)
            {
                foreach (float searchRadius in searchRadii)
                {
                    if (NavMesh.SamplePosition(testPos, out hit, searchRadius, areaMask))
                    {
                        sampledPosition = hit.position;
                        foundValidPosition = true;
                        break;
                    }
                }
                if (foundValidPosition) break;
            }
            
            // If not found with agent's area mask, try AllAreas
            if (!foundValidPosition)
            {
                areaMask = NavMesh.AllAreas;
                foreach (Vector3 testPos in testPositions)
                {
                    foreach (float searchRadius in searchRadii)
                    {
                        if (NavMesh.SamplePosition(testPos, out hit, searchRadius, areaMask))
                        {
                            sampledPosition = hit.position;
                            foundValidPosition = true;
                            break;
                        }
                    }
                    if (foundValidPosition) break;
                }
            }
            
            if (foundValidPosition)
            {
                // Editor-safe: Just set the transform position directly
                // The agent will be recognized on NavMesh at runtime
                agent.transform.position = sampledPosition;
            }
            else
            {
                // No NavMesh found
                var extras = new Dictionary<string, object>
                {
                    { "warning", "NavMeshAgent created but could not find a valid NavMesh position. Ensure NavMesh is baked before creating agents." },
                    { "position", $"{currentPosition.x},{currentPosition.y},{currentPosition.z}" },
                    { "suggestion", "Bake the NavMesh first using bake_navmesh tool, then create the agent." }
                };
                return ToolUtils.CreateSuccessResponse($"Updated NavMeshAgent on '{gameObjectPath}' (NavMesh not found - agent position not adjusted)", extras);
            }

            var responseExtras = new Dictionary<string, object>
            {
                { "position", $"{agent.transform.position.x},{agent.transform.position.y},{agent.transform.position.z}" },
                { "note", "Agent position set on NavMesh. The agent will be recognized as on NavMesh at runtime. Editor-only: isOnNavMesh check is not reliable in edit mode." }
            };

            string message = wasNewAgent 
                ? $"Created and positioned NavMeshAgent on '{gameObjectPath}'" 
                : $"Updated NavMeshAgent on '{gameObjectPath}'";
            
            return ToolUtils.CreateSuccessResponse(message, responseExtras);
        }
    }
}
