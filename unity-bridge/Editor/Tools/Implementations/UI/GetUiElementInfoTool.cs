using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class GetUiElementInfoTool : ITool
    {
        public string Name => "get_ui_element_info";

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

            var sb = new StringBuilder();
            sb.Append("{\"success\":true,");
            sb.Append($"\"gameObjectPath\":\"{ToolUtils.EscapeJsonString(gameObjectPath)}\",");

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                sb.Append("\"rectTransform\":{");
                sb.Append($"\"sizeDelta\":\"{rect.sizeDelta.x},{rect.sizeDelta.y}\",");
                sb.Append($"\"anchoredPosition\":\"{rect.anchoredPosition.x},{rect.anchoredPosition.y}\",");
                sb.Append($"\"anchorMin\":\"{rect.anchorMin.x},{rect.anchorMin.y}\",");
                sb.Append($"\"anchorMax\":\"{rect.anchorMax.x},{rect.anchorMax.y}\",");
                sb.Append($"\"pivot\":\"{rect.pivot.x},{rect.pivot.y}\"");
                sb.Append("},");
            }

            var componentTypes = new List<string>();
            var properties = new Dictionary<string, object>();

            if (obj.TryGetComponent<Button>(out var button))
            {
                componentTypes.Add("Button");
                properties["interactable"] = button.interactable;
                properties["onClickCount"] = button.onClick.GetPersistentEventCount();
            }
            if (obj.TryGetComponent<Text>(out var text))
            {
                componentTypes.Add("Text");
                properties["text"] = text.text;
                properties["fontSize"] = text.fontSize;
                properties["color"] = $"{text.color.r},{text.color.g},{text.color.b},{text.color.a}";
            }
            if (obj.TryGetComponent<Image>(out var image))
            {
                componentTypes.Add("Image");
                properties["color"] = $"{image.color.r},{image.color.g},{image.color.b},{image.color.a}";
                properties["hasSprite"] = image.sprite != null;
            }
            if (obj.TryGetComponent<Slider>(out var slider))
            {
                componentTypes.Add("Slider");
                properties["minValue"] = slider.minValue;
                properties["maxValue"] = slider.maxValue;
                properties["value"] = slider.value;
            }
            if (obj.TryGetComponent<Toggle>(out var toggle))
            {
                componentTypes.Add("Toggle");
                properties["isOn"] = toggle.isOn;
                properties["hasGroup"] = toggle.group != null;
                properties["onValueChangedCount"] = toggle.onValueChanged.GetPersistentEventCount();
            }
            if (obj.TryGetComponent<Dropdown>(out var dropdown))
            {
                componentTypes.Add("Dropdown");
                properties["value"] = dropdown.value;
                var options = new List<string>();
                foreach (var opt in dropdown.options)
                {
                    options.Add(opt.text);
                }
                properties["options"] = options;
                properties["optionsCount"] = dropdown.options.Count;
            }
            if (obj.TryGetComponent<InputField>(out var inputField))
            {
                componentTypes.Add("InputField");
                properties["text"] = inputField.text;
                properties["characterLimit"] = inputField.characterLimit;
                properties["contentType"] = inputField.contentType.ToString();
                properties["lineType"] = inputField.lineType.ToString();
            }
            if (obj.TryGetComponent<ScrollRect>(out var scrollRect))
            {
                componentTypes.Add("ScrollRect");
                properties["horizontal"] = scrollRect.horizontal;
                properties["vertical"] = scrollRect.vertical;
                properties["hasContent"] = scrollRect.content != null;
                properties["hasViewport"] = scrollRect.viewport != null;
            }
            if (obj.TryGetComponent<Scrollbar>(out var scrollbar))
            {
                componentTypes.Add("Scrollbar");
                properties["direction"] = scrollbar.direction.ToString();
                properties["value"] = scrollbar.value;
                properties["size"] = scrollbar.size;
                properties["numberOfSteps"] = scrollbar.numberOfSteps;
            }
            if (obj.TryGetComponent<RawImage>(out var rawImage))
            {
                componentTypes.Add("RawImage");
                properties["hasTexture"] = rawImage.texture != null;
                properties["color"] = $"{rawImage.color.r},{rawImage.color.g},{rawImage.color.b},{rawImage.color.a}";
            }
            if (obj.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                componentTypes.Add("CanvasGroup");
                properties["alpha"] = canvasGroup.alpha;
                properties["interactable"] = canvasGroup.interactable;
                properties["blocksRaycasts"] = canvasGroup.blocksRaycasts;
                properties["ignoreParentGroups"] = canvasGroup.ignoreParentGroups;
            }
            if (obj.TryGetComponent<HorizontalLayoutGroup>(out var hLayout))
            {
                componentTypes.Add("HorizontalLayoutGroup");
                properties["spacing"] = hLayout.spacing;
            }
            if (obj.TryGetComponent<VerticalLayoutGroup>(out var vLayout))
            {
                componentTypes.Add("VerticalLayoutGroup");
                properties["spacing"] = vLayout.spacing;
            }
            if (obj.TryGetComponent<GridLayoutGroup>(out var gridLayout))
            {
                componentTypes.Add("GridLayoutGroup");
                properties["cellSize"] = $"{gridLayout.cellSize.x},{gridLayout.cellSize.y}";
                properties["spacing"] = $"{gridLayout.spacing.x},{gridLayout.spacing.y}";
            }

            // Check for TMP components
            var tmpTextType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpTextType != null)
            {
                var tmpComponent = obj.GetComponent(tmpTextType);
                if (tmpComponent != null)
                {
                    componentTypes.Add("TextMeshProUGUI");
                    var textProp = tmpTextType.GetProperty("text");
                    if (textProp != null) properties["text"] = textProp.GetValue(tmpComponent)?.ToString() ?? "";
                    var sizeProp = tmpTextType.GetProperty("fontSize");
                    if (sizeProp != null) properties["fontSize"] = sizeProp.GetValue(tmpComponent);
                }
            }

            var tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
            if (tmpDropdownType != null)
            {
                var tmpDropdown = obj.GetComponent(tmpDropdownType);
                if (tmpDropdown != null)
                {
                    componentTypes.Add("TMP_Dropdown");
                    var valueProp = tmpDropdownType.GetProperty("value");
                    if (valueProp != null) properties["value"] = valueProp.GetValue(tmpDropdown);
                }
            }

            var tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputFieldType != null)
            {
                var tmpInputField = obj.GetComponent(tmpInputFieldType);
                if (tmpInputField != null)
                {
                    componentTypes.Add("TMP_InputField");
                    var textProp = tmpInputFieldType.GetProperty("text");
                    if (textProp != null) properties["text"] = textProp.GetValue(tmpInputField)?.ToString() ?? "";
                }
            }

            sb.Append("\"componentTypes\":[");
            for (int i = 0; i < componentTypes.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"{ToolUtils.EscapeJsonString(componentTypes[i])}\"");
            }
            sb.Append("],");

            sb.Append("\"properties\":{");
            bool firstProp = true;
            foreach (var prop in properties)
            {
                if (!firstProp) sb.Append(",");
                firstProp = false;
                if (prop.Value is string str)
                    sb.Append($"\"{prop.Key}\":\"{ToolUtils.EscapeJsonString(str)}\"");
                else if (prop.Value is bool b)
                    sb.Append($"\"{prop.Key}\":{(b ? "true" : "false")}");
                else if (prop.Value is System.Collections.IList list)
                {
                    sb.Append($"\"{prop.Key}\":[");
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append($"\"{ToolUtils.EscapeJsonString(list[i].ToString())}\"");
                    }
                    sb.Append("]");
                }
                else
                    sb.Append($"\"{prop.Key}\":{prop.Value}");
            }
            sb.Append("}");

            // Get child UI elements
            var children = new List<string>();
            foreach (UnityEngine.Transform child in obj.transform)
            {
                if (child.GetComponent<RectTransform>() != null)
                {
                    children.Add(ToolUtils.GetGameObjectPath(child.gameObject));
                }
            }
            sb.Append(",\"children\":[");
            for (int i = 0; i < children.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"{ToolUtils.EscapeJsonString(children[i])}\"");
            }
            sb.Append("]");

            sb.Append("}");
            return sb.ToString();
        }
    }
}
#endif
