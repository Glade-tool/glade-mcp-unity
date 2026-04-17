using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if GLADE_AI_NAVIGATION
using Unity.AI.Navigation;
#endif
using GladeAgenticAI.Core.Tools;

#if GLADE_AI_NAVIGATION
namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetNavMeshObstaclePropertiesTool : ITool
    {
        public string Name => "set_navmesh_obstacle_properties";

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

            var obstacle = obj.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a NavMeshObstacle component");
            }

            // Record object for undo before modifying properties
            Undo.RecordObject(obstacle, $"Set NavMeshObstacle Properties: {gameObjectPath}");

            if (args.ContainsKey("carve"))
            {
                if (args["carve"] is bool b) obstacle.carveOnlyStationary = !b;
                else if (bool.TryParse(args["carve"].ToString(), out bool carve))
                    obstacle.carveOnlyStationary = !carve;
            }

            if (args.ContainsKey("carveOnlyStationary"))
            {
                if (args["carveOnlyStationary"] is bool b)
                {
                    obstacle.carveOnlyStationary = b;
                }
                else if (bool.TryParse(args["carveOnlyStationary"].ToString(), out bool carveOnlyStationary))
                {
                    obstacle.carveOnlyStationary = carveOnlyStationary;
                }
            }

            if (args.ContainsKey("moveThreshold"))
            {
                if (float.TryParse(args["moveThreshold"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float threshold))
                    obstacle.carvingMoveThreshold = threshold;
            }

            if (args.ContainsKey("timeToStationary"))
            {
                if (float.TryParse(args["timeToStationary"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float time))
                    obstacle.carvingTimeToStationary = time;
            }

            if (args.ContainsKey("radius") && obstacle.shape == UnityEngine.AI.NavMeshObstacleShape.Capsule)
            {
                if (float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius))
                    obstacle.radius = radius;
            }

            if (args.ContainsKey("height") && obstacle.shape == UnityEngine.AI.NavMeshObstacleShape.Capsule)
            {
                if (float.TryParse(args["height"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float height))
                    obstacle.height = height;
            }

            if (args.ContainsKey("size") && obstacle.shape == UnityEngine.AI.NavMeshObstacleShape.Box)
            {
                Vector3 size = ToolUtils.ParseVector3(args["size"].ToString());
                obstacle.size = size;
            }

            return ToolUtils.CreateSuccessResponse($"Updated NavMeshObstacle properties on '{gameObjectPath}'");
        }
    }
}
#endif
