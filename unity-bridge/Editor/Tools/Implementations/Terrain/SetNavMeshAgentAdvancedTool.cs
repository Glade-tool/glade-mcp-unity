using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetNavMeshAgentAdvancedTool : ITool
    {
        public string Name => "set_navmesh_agent_advanced";

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
            Undo.RecordObject(agent, $"Set NavMeshAgent Advanced Properties: {gameObjectPath}");

            if (args.ContainsKey("areaMask"))
            {
                if (args["areaMask"] is int i) agent.areaMask = i;
                else if (int.TryParse(args["areaMask"].ToString(), out int areaMask))
                    agent.areaMask = areaMask;
            }

            if (args.ContainsKey("agentTypeID"))
            {
                if (int.TryParse(args["agentTypeID"].ToString(), out int agentTypeID))
                    agent.agentTypeID = agentTypeID;
            }

            if (args.ContainsKey("obstacleAvoidanceType"))
            {
                if (int.TryParse(args["obstacleAvoidanceType"].ToString(), out int avoidanceType))
                {
                    if (avoidanceType >= 0 && avoidanceType <= 3)
                        agent.obstacleAvoidanceType = (ObstacleAvoidanceType)avoidanceType;
                }
            }

            if (args.ContainsKey("avoidancePriority"))
            {
                if (int.TryParse(args["avoidancePriority"].ToString(), out int priority))
                {
                    if (priority >= 0 && priority <= 99)
                        agent.avoidancePriority = priority;
                }
            }

            if (args.ContainsKey("autoBraking"))
            {
                if (args["autoBraking"] is bool b)
                {
                    agent.autoBraking = b;
                }
                else if (bool.TryParse(args["autoBraking"].ToString(), out bool autoBraking))
                {
                    agent.autoBraking = autoBraking;
                }
            }

            if (args.ContainsKey("autoRepath"))
            {
                if (args["autoRepath"] is bool b)
                {
                    agent.autoRepath = b;
                }
                else if (bool.TryParse(args["autoRepath"].ToString(), out bool autoRepath))
                {
                    agent.autoRepath = autoRepath;
                }
            }

            if (args.ContainsKey("autoTraverseOffMeshLink"))
            {
                if (args["autoTraverseOffMeshLink"] is bool b)
                {
                    agent.autoTraverseOffMeshLink = b;
                }
                else if (bool.TryParse(args["autoTraverseOffMeshLink"].ToString(), out bool autoTraverseOffMeshLink))
                {
                    agent.autoTraverseOffMeshLink = autoTraverseOffMeshLink;
                }
            }

            if (args.ContainsKey("baseOffset"))
            {
                if (float.TryParse(args["baseOffset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float offset))
                    agent.baseOffset = offset;
            }

            if (args.ContainsKey("updatePosition"))
            {
                if (args["updatePosition"] is bool b)
                {
                    agent.updatePosition = b;
                }
                else if (bool.TryParse(args["updatePosition"].ToString(), out bool updatePosition))
                {
                    agent.updatePosition = updatePosition;
                }
            }

            if (args.ContainsKey("updateRotation"))
            {
                if (args["updateRotation"] is bool b)
                {
                    agent.updateRotation = b;
                }
                else if (bool.TryParse(args["updateRotation"].ToString(), out bool updateRotation))
                {
                    agent.updateRotation = updateRotation;
                }
            }

            if (args.ContainsKey("updateUpAxis"))
            {
                if (args["updateUpAxis"] is bool b)
                {
                    agent.updateUpAxis = b;
                }
                else if (bool.TryParse(args["updateUpAxis"].ToString(), out bool updateUpAxis))
                {
                    agent.updateUpAxis = updateUpAxis;
                }
            }

            return ToolUtils.CreateSuccessResponse($"Updated advanced NavMeshAgent properties on '{gameObjectPath}'");
        }
    }
}
