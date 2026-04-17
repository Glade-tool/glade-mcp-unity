using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if GLADE_AI_NAVIGATION
using Unity.AI.Navigation;
#endif
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

#if GLADE_AI_NAVIGATION
namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class GetNavMeshInfoTool : ITool
    {
        public string Name => "get_navmesh_info";

        public string Execute(Dictionary<string, object> args)
        {
            var info = new Dictionary<string, object>();

            if (args.ContainsKey("gameObjectPath"))
            {
                // Get info for specific NavMeshSurface
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

                info["surfacePath"] = ToolUtils.GetGameObjectPath(obj);
                info["hasData"] = surface.navMeshData != null;
                
                if (surface.navMeshData != null)
                {
                    var bounds = surface.navMeshData.sourceBounds;
                    info["bounds"] = new Dictionary<string, object>
                    {
                        { "center", $"{bounds.center.x},{bounds.center.y},{bounds.center.z}" },
                        { "size", $"{bounds.size.x},{bounds.size.y},{bounds.size.z}" },
                        { "min", $"{bounds.min.x},{bounds.min.y},{bounds.min.z}" },
                        { "max", $"{bounds.max.x},{bounds.max.y},{bounds.max.z}" }
                    };
                }
            }
            else
            {
                // Get general NavMesh info
                var surfaces = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
                var agents = UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

                info["surfaceCount"] = surfaces.Length;
                info["agentCount"] = agents.Length;
                info["hasNavMeshData"] = UnityEngine.AI.NavMesh.GetSettingsCount() > 0;

                var surfaceInfo = new List<Dictionary<string, object>>();
                foreach (var surface in surfaces)
                {
                    surfaceInfo.Add(new Dictionary<string, object>
                    {
                        { "path", ToolUtils.GetGameObjectPath(surface.gameObject) },
                        { "hasData", surface.navMeshData != null }
                    });
                }
                info["surfaces"] = surfaceInfo;
            }

            return ToolUtils.CreateSuccessResponse("Retrieved NavMesh information", info);
        }
    }
}
#endif
