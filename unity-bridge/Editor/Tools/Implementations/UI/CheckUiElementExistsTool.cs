using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class CheckUiElementExistsTool : ITool
    {
        public string Name => "check_ui_element_exists";

        public string Execute(Dictionary<string, object> args)
        {
            string elementPath = args.ContainsKey("elementPath") ? args["elementPath"].ToString() : "";
            if (string.IsNullOrEmpty(elementPath))
            {
                return ToolUtils.CreateErrorResponse("elementPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(elementPath);
            bool exists = obj != null;
            bool hasRectTransform = false;
            if (exists)
            {
                hasRectTransform = obj.GetComponent<RectTransform>() != null;
            }

            var extras = new Dictionary<string, object>
            {
                { "exists", exists },
                { "hasRectTransform", hasRectTransform },
                { "path", elementPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"UI element exists: {exists}", extras);
        }
    }
}
#endif
