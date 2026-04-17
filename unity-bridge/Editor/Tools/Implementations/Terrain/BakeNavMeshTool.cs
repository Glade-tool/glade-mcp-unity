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
    public class BakeNavMeshTool : ITool
    {
        public string Name => "bake_navmesh";

        public string Execute(Dictionary<string, object> args)
        {
            List<NavMeshSurface> surfaces = new List<NavMeshSurface>();
            if (args.ContainsKey("gameObjectPath"))
            {
                string path = args["gameObjectPath"].ToString();
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj == null) return ToolUtils.CreateErrorResponse($"GameObject '{path}' not found");
                var surface = obj.GetComponent<NavMeshSurface>();
                if (surface != null) surfaces.Add(surface);
            }
            else
            {
                surfaces.AddRange(UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None));
            }

            int baked = 0;
            foreach (var surface in surfaces)
            {
                surface.BuildNavMesh();
                baked++;
            }

            var extras = new Dictionary<string, object>
            {
                { "baked", baked }
            };
            
            return ToolUtils.CreateSuccessResponse($"Baked {baked} NavMeshSurface(s)", extras);
        }
    }
}
#endif
