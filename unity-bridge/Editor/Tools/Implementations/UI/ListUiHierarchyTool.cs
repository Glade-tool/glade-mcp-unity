using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class ListUiHierarchyTool : ITool
    {
        public string Name => "list_ui_hierarchy";

        public string Execute(Dictionary<string, object> args)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var sb = new StringBuilder();
            sb.Append("{\"success\":true,\"canvases\":[");

            int canvasIndex = 0;
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
                    continue;

                if (canvasIndex > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"name\":\"{ToolUtils.EscapeJsonString(canvas.gameObject.name)}\",");
                sb.Append($"\"path\":\"{ToolUtils.EscapeJsonString(ToolUtils.GetGameObjectPath(canvas.gameObject))}\",");
                sb.Append($"\"renderMode\":\"{canvas.renderMode}\",");
                sb.Append($"\"active\":{(canvas.gameObject.activeSelf ? "true" : "false")},");
                sb.Append("\"children\":[");
                CollectUiChildrenJson(canvas.gameObject.transform, sb, 0);
                sb.Append("]}");
                canvasIndex++;
            }

            sb.Append($"],\"count\":{canvasIndex},\"message\":\"Found {canvasIndex} Canvas object(s)\"}}");

            if (canvasIndex == 0)
            {
                return "{\"success\":true,\"canvases\":[],\"count\":0,\"message\":\"No Canvas objects found in the scene\"}";
            }

            return sb.ToString();
        }

        private static void CollectUiChildrenJson(UnityEngine.Transform parent, StringBuilder sb, int depth)
        {
            if (depth > 10) return; // Prevent infinite recursion

            bool firstChild = true;
            foreach (UnityEngine.Transform child in parent)
            {
                // Only include objects with RectTransform (UI elements)
                if (child.GetComponent<RectTransform>() == null)
                    continue;

                if (!firstChild) sb.Append(",");
                firstChild = false;

                sb.Append("{");
                sb.Append($"\"name\":\"{ToolUtils.EscapeJsonString(child.name)}\",");
                sb.Append($"\"path\":\"{ToolUtils.EscapeJsonString(ToolUtils.GetGameObjectPath(child.gameObject))}\",");
                sb.Append($"\"active\":{(child.gameObject.activeSelf ? "true" : "false")},");

                // Identify UI component types
                var componentTypes = new List<string>();
                if (child.GetComponent<Text>() != null) componentTypes.Add("Text");
                if (child.GetComponent<Image>() != null) componentTypes.Add("Image");
                if (child.GetComponent<Button>() != null) componentTypes.Add("Button");
                if (child.GetComponent<Slider>() != null) componentTypes.Add("Slider");
                if (child.GetComponent<Toggle>() != null) componentTypes.Add("Toggle");
                if (child.GetComponent<Dropdown>() != null) componentTypes.Add("Dropdown");
                if (child.GetComponent<InputField>() != null) componentTypes.Add("InputField");
                if (child.GetComponent<Scrollbar>() != null) componentTypes.Add("Scrollbar");
                if (child.GetComponent<ScrollRect>() != null) componentTypes.Add("ScrollRect");
                if (child.GetComponent<Canvas>() != null) componentTypes.Add("Canvas");
                if (child.GetComponent<CanvasGroup>() != null) componentTypes.Add("CanvasGroup");
                if (child.GetComponent<LayoutGroup>() != null) componentTypes.Add("LayoutGroup");
                if (child.GetComponent<HorizontalLayoutGroup>() != null) componentTypes.Add("HorizontalLayoutGroup");
                if (child.GetComponent<VerticalLayoutGroup>() != null) componentTypes.Add("VerticalLayoutGroup");
                if (child.GetComponent<GridLayoutGroup>() != null) componentTypes.Add("GridLayoutGroup");
                if (child.GetComponent<ContentSizeFitter>() != null) componentTypes.Add("ContentSizeFitter");
                if (child.GetComponent<AspectRatioFitter>() != null) componentTypes.Add("AspectRatioFitter");

                // Check for TextMeshPro components
                var tmpTextType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                if (tmpTextType != null && child.GetComponent(tmpTextType) != null)
                {
                    componentTypes.Add("TextMeshProUGUI");
                }
                
                var tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
                if (tmpInputFieldType != null && child.GetComponent(tmpInputFieldType) != null)
                {
                    componentTypes.Add("TMP_InputField");
                }
                
                var tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
                if (tmpDropdownType != null && child.GetComponent(tmpDropdownType) != null)
                {
                    componentTypes.Add("TMP_Dropdown");
                }

                if (componentTypes.Count == 0)
                {
                    componentTypes.Add("Panel"); // Default for UI elements without specific components
                }

                sb.Append("\"componentTypes\":[");
                for (int i = 0; i < componentTypes.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"\"{ToolUtils.EscapeJsonString(componentTypes[i])}\"");
                }
                sb.Append("]");

                // Recursively get children
                var childSb = new StringBuilder();
                CollectUiChildrenJson(child, childSb, depth + 1);
                if (childSb.Length > 0)
                {
                    sb.Append(",\"children\":[");
                    sb.Append(childSb);
                    sb.Append("]");
                }

                sb.Append("}");
            }
        }
    }
}
#endif
