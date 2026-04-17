using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class SetLayoutGroupPropertiesTool : ITool
    {
        public string Name => "set_layout_group_properties";

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

            Undo.RecordObject(obj, $"Set Layout Group Properties: {gameObjectPath}");

            if (obj.TryGetComponent<HorizontalLayoutGroup>(out var hLayout))
            {
                if (args.ContainsKey("spacing") && float.TryParse(args["spacing"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float hSpacing))
                    hLayout.spacing = hSpacing;
                if (args.ContainsKey("padding") && !string.IsNullOrEmpty(args["padding"].ToString()))
                {
                    var paddingParts = args["padding"].ToString().Split(',');
                    if (paddingParts.Length >= 4)
                    {
                        hLayout.padding = new RectOffset(
                            int.Parse(paddingParts[0].Trim()),
                            int.Parse(paddingParts[1].Trim()),
                            int.Parse(paddingParts[2].Trim()),
                            int.Parse(paddingParts[3].Trim())
                        );
                    }
                }
                if (args.ContainsKey("childAlignment") && System.Enum.TryParse<TextAnchor>(args["childAlignment"].ToString(), true, out var hAlignment))
                    hLayout.childAlignment = hAlignment;
                if (args.ContainsKey("childControlWidth"))
                {
                    if (args["childControlWidth"] is bool ccw) hLayout.childControlWidth = ccw;
                    else if (bool.TryParse(args["childControlWidth"].ToString(), out bool ccwv)) hLayout.childControlWidth = ccwv;
                }
                if (args.ContainsKey("childControlHeight"))
                {
                    if (args["childControlHeight"] is bool cch) hLayout.childControlHeight = cch;
                    else if (bool.TryParse(args["childControlHeight"].ToString(), out bool cchv)) hLayout.childControlHeight = cchv;
                }
                if (args.ContainsKey("childForceExpandWidth"))
                {
                    if (args["childForceExpandWidth"] is bool cfew) hLayout.childForceExpandWidth = cfew;
                    else if (bool.TryParse(args["childForceExpandWidth"].ToString(), out bool cfewv)) hLayout.childForceExpandWidth = cfewv;
                }
                if (args.ContainsKey("childForceExpandHeight"))
                {
                    if (args["childForceExpandHeight"] is bool cfeh) hLayout.childForceExpandHeight = cfeh;
                    else if (bool.TryParse(args["childForceExpandHeight"].ToString(), out bool cfehv)) hLayout.childForceExpandHeight = cfehv;
                }
            }
            else if (obj.TryGetComponent<VerticalLayoutGroup>(out var vLayout))
            {
                if (args.ContainsKey("spacing") && float.TryParse(args["spacing"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vSpacing))
                    vLayout.spacing = vSpacing;
                if (args.ContainsKey("padding") && !string.IsNullOrEmpty(args["padding"].ToString()))
                {
                    var paddingParts = args["padding"].ToString().Split(',');
                    if (paddingParts.Length >= 4)
                    {
                        vLayout.padding = new RectOffset(
                            int.Parse(paddingParts[0].Trim()),
                            int.Parse(paddingParts[1].Trim()),
                            int.Parse(paddingParts[2].Trim()),
                            int.Parse(paddingParts[3].Trim())
                        );
                    }
                }
                if (args.ContainsKey("childAlignment") && System.Enum.TryParse<TextAnchor>(args["childAlignment"].ToString(), true, out var vAlignment))
                    vLayout.childAlignment = vAlignment;
                if (args.ContainsKey("childControlWidth"))
                {
                    if (args["childControlWidth"] is bool ccw) vLayout.childControlWidth = ccw;
                    else if (bool.TryParse(args["childControlWidth"].ToString(), out bool ccwv)) vLayout.childControlWidth = ccwv;
                }
                if (args.ContainsKey("childControlHeight"))
                {
                    if (args["childControlHeight"] is bool cch) vLayout.childControlHeight = cch;
                    else if (bool.TryParse(args["childControlHeight"].ToString(), out bool cchv)) vLayout.childControlHeight = cchv;
                }
                if (args.ContainsKey("childForceExpandWidth"))
                {
                    if (args["childForceExpandWidth"] is bool cfew) vLayout.childForceExpandWidth = cfew;
                    else if (bool.TryParse(args["childForceExpandWidth"].ToString(), out bool cfewv)) vLayout.childForceExpandWidth = cfewv;
                }
                if (args.ContainsKey("childForceExpandHeight"))
                {
                    if (args["childForceExpandHeight"] is bool cfeh) vLayout.childForceExpandHeight = cfeh;
                    else if (bool.TryParse(args["childForceExpandHeight"].ToString(), out bool cfehv)) vLayout.childForceExpandHeight = cfehv;
                }
            }
            else if (obj.TryGetComponent<GridLayoutGroup>(out var gridLayout))
            {
                if (args.ContainsKey("spacing") && !string.IsNullOrEmpty(args["spacing"].ToString()))
                {
                    var spacingParts = args["spacing"].ToString().Split(',');
                    if (spacingParts.Length >= 2)
                    {
                        gridLayout.spacing = new Vector2(
                            float.Parse(spacingParts[0].Trim()),
                            float.Parse(spacingParts[1].Trim())
                        );
                    }
                }
                if (args.ContainsKey("cellSize") && !string.IsNullOrEmpty(args["cellSize"].ToString()))
                {
                    var cellParts = args["cellSize"].ToString().Split(',');
                    if (cellParts.Length >= 2)
                    {
                        gridLayout.cellSize = new Vector2(
                            float.Parse(cellParts[0].Trim()),
                            float.Parse(cellParts[1].Trim())
                        );
                    }
                }
                if (args.ContainsKey("padding") && !string.IsNullOrEmpty(args["padding"].ToString()))
                {
                    var paddingParts = args["padding"].ToString().Split(',');
                    if (paddingParts.Length >= 4)
                    {
                        gridLayout.padding = new RectOffset(
                            int.Parse(paddingParts[0].Trim()),
                            int.Parse(paddingParts[1].Trim()),
                            int.Parse(paddingParts[2].Trim()),
                            int.Parse(paddingParts[3].Trim())
                        );
                    }
                }
                if (args.ContainsKey("childAlignment") && System.Enum.TryParse<TextAnchor>(args["childAlignment"].ToString(), true, out var gAlignment))
                    gridLayout.childAlignment = gAlignment;
                if (args.ContainsKey("startCorner") && System.Enum.TryParse<GridLayoutGroup.Corner>(args["startCorner"].ToString(), true, out var startCorner))
                    gridLayout.startCorner = startCorner;
                if (args.ContainsKey("startAxis") && System.Enum.TryParse<GridLayoutGroup.Axis>(args["startAxis"].ToString(), true, out var startAxis))
                    gridLayout.startAxis = startAxis;
                if (args.ContainsKey("constraint") && System.Enum.TryParse<GridLayoutGroup.Constraint>(args["constraint"].ToString(), true, out var constraint))
                    gridLayout.constraint = constraint;
                if (args.ContainsKey("constraintCount") && int.TryParse(args["constraintCount"].ToString(), out int constraintCount))
                    gridLayout.constraintCount = constraintCount;
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a LayoutGroup component");
            }

            return ToolUtils.CreateSuccessResponse($"Updated layout group properties on '{gameObjectPath}'");
        }
    }
}
#endif
