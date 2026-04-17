using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class SetCanvasGroupPropertiesTool : ITool
    {
        public string Name => "set_canvasgroup_properties";

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

            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a CanvasGroup component");
            }

            Undo.RecordObject(canvasGroup, $"Set CanvasGroup Properties: {gameObjectPath}");

            if (args.ContainsKey("alpha") && float.TryParse(args["alpha"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float alpha))
                canvasGroup.alpha = alpha;
            if (args.ContainsKey("interactable"))
            {
                if (args["interactable"] is bool i) canvasGroup.interactable = i;
                else if (bool.TryParse(args["interactable"].ToString(), out bool iv)) canvasGroup.interactable = iv;
            }
            if (args.ContainsKey("blocksRaycasts"))
            {
                if (args["blocksRaycasts"] is bool br) canvasGroup.blocksRaycasts = br;
                else if (bool.TryParse(args["blocksRaycasts"].ToString(), out bool brv)) canvasGroup.blocksRaycasts = brv;
            }
            if (args.ContainsKey("ignoreParentGroups"))
            {
                if (args["ignoreParentGroups"] is bool ipg) canvasGroup.ignoreParentGroups = ipg;
                else if (bool.TryParse(args["ignoreParentGroups"].ToString(), out bool ipgv)) canvasGroup.ignoreParentGroups = ipgv;
            }

            return ToolUtils.CreateSuccessResponse($"Updated CanvasGroup properties on '{gameObjectPath}'");
        }
    }
}
#endif
