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
    public class CreateNavMeshSurfaceTool : ITool
    {
        public string Name => "create_navmesh_surface";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            UnityEngine.GameObject obj = null;
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                obj = new UnityEngine.GameObject("NavMeshSurface");
                Undo.RegisterCreatedObjectUndo(obj, "Create NavMeshSurface");
            }
            else
            {
                obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
                if (obj == null)
                {
                    return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
                }
            }

            NavMeshSurface surface = obj.GetComponent<NavMeshSurface>() ?? Undo.AddComponent<NavMeshSurface>(obj);
            if (args.ContainsKey("collectObjects"))
            {
                if (System.Enum.TryParse(args["collectObjects"].ToString(), true, out CollectObjects collect))
                    surface.collectObjects = collect;
            }
            if (args.ContainsKey("layerMask"))
            {
                surface.layerMask = ToolUtils.ParseLayerMask(args["layerMask"]);
            }
            if (args.ContainsKey("useGeometry"))
            {
                if (System.Enum.TryParse(args["useGeometry"].ToString(), true, out UnityEngine.AI.NavMeshCollectGeometry geom))
                    surface.useGeometry = geom;
            }

            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(obj) }
            };
            
            return ToolUtils.CreateSuccessResponse("Created/updated NavMeshSurface", extras);
        }
    }
}
#endif
