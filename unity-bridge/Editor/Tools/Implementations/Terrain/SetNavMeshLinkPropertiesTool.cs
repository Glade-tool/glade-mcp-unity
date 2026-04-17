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
    public class SetNavMeshLinkPropertiesTool : ITool
    {
        public string Name => "set_navmesh_link_properties";

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

            var link = obj.GetComponent<NavMeshLink>();
            if (link == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a NavMeshLink component");
            }

            // Record object for undo before modifying properties
            Undo.RecordObject(link, $"Set NavMeshLink Properties: {gameObjectPath}");

            if (args.ContainsKey("startPosition"))
            {
                Vector3 startPos = ToolUtils.ParseVector3(args["startPosition"].ToString());
                link.startPoint = startPos;
            }

            if (args.ContainsKey("endPosition"))
            {
                Vector3 endPos = ToolUtils.ParseVector3(args["endPosition"].ToString());
                link.endPoint = endPos;
            }

            if (args.ContainsKey("bidirectional"))
            {
                if (args["bidirectional"] is bool b)
                {
                    link.bidirectional = b;
                }
                else if (bool.TryParse(args["bidirectional"].ToString(), out bool bidirectional))
                {
                    link.bidirectional = bidirectional;
                }
            }

            if (args.ContainsKey("activated"))
            {
                if (args["activated"] is bool b)
                {
                    link.activated = b;
                }
                else if (bool.TryParse(args["activated"].ToString(), out bool activated))
                {
                    link.activated = activated;
                }
            }

            if (args.ContainsKey("area"))
            {
                if (args["area"] is int i) link.area = i;
                else if (int.TryParse(args["area"].ToString(), out int area))
                    link.area = area;
            }

            if (args.ContainsKey("costModifier"))
            {
                if (float.TryParse(args["costModifier"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float cost))
                    link.costModifier = cost;
            }

            if (args.ContainsKey("width"))
            {
                if (float.TryParse(args["width"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float width))
                    link.width = width;
            }

            return ToolUtils.CreateSuccessResponse($"Updated NavMeshLink properties on '{gameObjectPath}'");
        }
    }
}
#endif
