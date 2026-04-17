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
    public class GetNavMeshBoundsTool : ITool
    {
        public string Name => "get_navmesh_bounds";

        public string Execute(Dictionary<string, object> args)
        {
            if (args.ContainsKey("gameObjectPath"))
            {
                // Get bounds for specific NavMeshSurface
                string gameObjectPath = args["gameObjectPath"].ToString();
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
                if (obj == null)
                {
                    return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
                }

                var surface = obj.GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a NavMeshSurface component");
                }

                if (surface.navMeshData == null)
                {
                    return ToolUtils.CreateErrorResponse($"NavMeshSurface '{gameObjectPath}' has no baked NavMesh data");
                }

                var bounds = surface.navMeshData.sourceBounds;
                var extras = new Dictionary<string, object>
                {
                    { "center", $"{bounds.center.x},{bounds.center.y},{bounds.center.z}" },
                    { "size", $"{bounds.size.x},{bounds.size.y},{bounds.size.z}" },
                    { "min", $"{bounds.min.x},{bounds.min.y},{bounds.min.z}" },
                    { "max", $"{bounds.max.x},{bounds.max.y},{bounds.max.z}" }
                };

                return ToolUtils.CreateSuccessResponse($"Retrieved NavMesh bounds for '{gameObjectPath}'", extras);
            }
            else
            {
                // Get combined bounds from all surfaces
                var surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
                if (surfaces.Length == 0)
                {
                    return ToolUtils.CreateErrorResponse("No NavMeshSurface components found in scene");
                }

                Bounds? combinedBounds = null;
                foreach (var surface in surfaces)
                {
                    if (surface.navMeshData != null)
                    {
                        var bounds = surface.navMeshData.sourceBounds;
                        if (combinedBounds == null)
                        {
                            combinedBounds = bounds;
                        }
                        else
                        {
                            combinedBounds.Value.Encapsulate(bounds);
                        }
                    }
                }

                if (combinedBounds == null)
                {
                    return ToolUtils.CreateErrorResponse("No baked NavMesh data found in any surface");
                }

                var extras = new Dictionary<string, object>
                {
                    { "center", $"{combinedBounds.Value.center.x},{combinedBounds.Value.center.y},{combinedBounds.Value.center.z}" },
                    { "size", $"{combinedBounds.Value.size.x},{combinedBounds.Value.size.y},{combinedBounds.Value.size.z}" },
                    { "min", $"{combinedBounds.Value.min.x},{combinedBounds.Value.min.y},{combinedBounds.Value.min.z}" },
                    { "max", $"{combinedBounds.Value.max.x},{combinedBounds.Value.max.y},{combinedBounds.Value.max.z}" },
                    { "surfaceCount", surfaces.Length }
                };

                return ToolUtils.CreateSuccessResponse("Retrieved combined NavMesh bounds", extras);
            }
        }
    }
}
#endif
