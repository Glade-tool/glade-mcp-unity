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
    public class ClearNavMeshTool : ITool
    {
        public string Name => "clear_navmesh";

        public string Execute(Dictionary<string, object> args)
        {
            if (args.ContainsKey("gameObjectPath"))
            {
                // Clear specific NavMeshSurface
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

                surface.RemoveData();
                return ToolUtils.CreateSuccessResponse($"Cleared NavMesh data from surface '{gameObjectPath}'");
            }
            else
            {
                // Clear all NavMesh data
                UnityEngine.AI.NavMesh.RemoveAllNavMeshData();
                return ToolUtils.CreateSuccessResponse("Cleared all NavMesh data");
            }
        }
    }
}
#endif
