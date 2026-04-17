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
    public class CreateNavMeshObstacleTool : ITool
    {
        public string Name => "create_navmesh_obstacle";

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
            if (obstacle != null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' already has a NavMeshObstacle component. Use set_navmesh_obstacle_properties to modify it instead.");
            }

            obstacle = Undo.AddComponent<UnityEngine.AI.NavMeshObstacle>(obj);

            // Set shape
            string shape = "Capsule";
            if (args.ContainsKey("shape"))
            {
                shape = args["shape"].ToString();
            }

            if (System.Enum.TryParse(shape, true, out UnityEngine.AI.NavMeshObstacleShape obstacleShape))
            {
                obstacle.shape = obstacleShape;
            }

            // Set carve properties
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

            // Set size based on shape
            if (obstacle.shape == UnityEngine.AI.NavMeshObstacleShape.Capsule)
            {
                if (args.ContainsKey("radius"))
                {
                    if (float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius))
                        obstacle.radius = radius;
                }
                if (args.ContainsKey("height"))
                {
                    if (float.TryParse(args["height"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float height))
                        obstacle.height = height;
                }
            }
            else if (obstacle.shape == UnityEngine.AI.NavMeshObstacleShape.Box)
            {
                if (args.ContainsKey("size"))
                {
                    Vector3 size = ToolUtils.ParseVector3(args["size"].ToString());
                    obstacle.size = size;
                }
            }

            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(obj) },
                { "shape", obstacle.shape.ToString() }
            };

            return ToolUtils.CreateSuccessResponse($"Created NavMeshObstacle on '{gameObjectPath}'", extras);
        }
    }
}
#endif
