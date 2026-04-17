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
    public class CreateNavMeshLinkTool : ITool
    {
        public string Name => "create_navmesh_link";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("startPosition"))
            {
                return ToolUtils.CreateErrorResponse("startPosition is required (as 'x,y,z')");
            }

            if (!args.ContainsKey("endPosition"))
            {
                return ToolUtils.CreateErrorResponse("endPosition is required (as 'x,y,z')");
            }

            Vector3 startPos = ToolUtils.ParseVector3(args["startPosition"].ToString());
            Vector3 endPos = ToolUtils.ParseVector3(args["endPosition"].ToString());

            string name = args.ContainsKey("name") ? args["name"].ToString() : "NavMeshLink";
            if (string.IsNullOrEmpty(name))
            {
                name = "NavMeshLink";
            }

            UnityEngine.GameObject linkObj = new UnityEngine.GameObject(name);
            linkObj.transform.position = (startPos + endPos) * 0.5f; // Center position
            Undo.RegisterCreatedObjectUndo(linkObj, $"Create NavMeshLink: {name}");

            var link = Undo.AddComponent<NavMeshLink>(linkObj);
            link.startPoint = startPos;
            link.endPoint = endPos;

            bool bidirectional = true;
            if (args.ContainsKey("bidirectional"))
            {
                if (args["bidirectional"] is bool b) bidirectional = b;
                else bool.TryParse(args["bidirectional"].ToString(), out bidirectional);
            }
            link.bidirectional = bidirectional;

            bool activated = true;
            if (args.ContainsKey("activated"))
            {
                if (args["activated"] is bool b) activated = b;
                else bool.TryParse(args["activated"].ToString(), out activated);
            }
            link.activated = activated;

            int area = 0;
            if (args.ContainsKey("area"))
            {
                if (args["area"] is int i) area = i;
                else if (!int.TryParse(args["area"].ToString(), out area))
                    area = 0;
            }
            link.area = area;

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

            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(linkObj) },
                { "startPosition", $"{startPos.x},{startPos.y},{startPos.z}" },
                { "endPosition", $"{endPos.x},{endPos.y},{endPos.z}" },
                { "bidirectional", bidirectional },
                { "activated", activated }
            };

            return ToolUtils.CreateSuccessResponse($"Created NavMeshLink '{name}'", extras);
        }
    }
}
#endif
