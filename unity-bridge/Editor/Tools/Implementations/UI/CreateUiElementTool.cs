using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class CreateUiElementTool : ITool
    {
        public string Name => "create_ui_element";

        public string Execute(Dictionary<string, object> args)
        {
            // CRITICAL: All UI actions require TextMeshPro. Check before doing anything.
            var tmpCheck = UIHelpers.EnsureTMPForUIActions();
            if (!tmpCheck.IsAvailable)
            {
                // TMP not available - user must install it explicitly first
                return ToolUtils.CreateErrorResponse(tmpCheck.Message);
            }

            string elementType = args.ContainsKey("elementType") ? args["elementType"].ToString() : "Panel";
            string name = args.ContainsKey("name") ? args["name"].ToString() : elementType;
            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"].ToString() : "";

            UnityEngine.GameObject parentObj = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentObj = ToolUtils.FindGameObjectByPath(parentPath);
            }
            if (parentObj == null)
            {
                var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                parentObj = canvas != null ? canvas.gameObject : null;
            }

            UnityEngine.GameObject obj = new UnityEngine.GameObject(name, typeof(RectTransform));
            if (parentObj != null)
            {
                obj.transform.SetParent(parentObj.transform, false);
            }

            switch (elementType.ToLower())
            {
                case "panel":
                    // Panel: Image component (color should be specified for visibility)
                    var panelImage = obj.AddComponent<Image>();
                    if (args.ContainsKey("color"))
                    {
                        panelImage.color = ToolUtils.ParseColor(args["color"].ToString());
                    }
                    panelImage.type = Image.Type.Simple;
                    break;
                case "text":
                case "tmp":
                case "tmp_text":
                case "textmeshpro":
                case "textmeshprougui":
                    // All text elements use TMP - no fallback to regular Text
                    if (!UIHelpers.TryAddTmpText(obj, args))
                    {
                        return ToolUtils.CreateErrorResponse("Failed to create TextMeshPro component. TextMeshPro is required for all UI actions in this agent and is the only text rendering system supported. If installation was just started, please wait for Unity to finish and manually click 'Import TMP Essentials' when prompted.");
                    }
                    break;
                case "image":
                    var image = obj.AddComponent<Image>();
                    if (args.ContainsKey("color"))
                    {
                        image.color = ToolUtils.ParseColor(args["color"].ToString());
                    }
                    // Default to Simple image type (not Tiled) to avoid repeated images unless explicitly set
                    image.type = Image.Type.Simple;
                    break;
                case "button":
                    var buttonImage = obj.AddComponent<Image>();
                    if (args.ContainsKey("color"))
                    {
                        buttonImage.color = ToolUtils.ParseColor(args["color"].ToString());
                    }
                    buttonImage.type = Image.Type.Simple;
                    var button = obj.AddComponent<Button>();
                    var textObj = new UnityEngine.GameObject("Text", typeof(RectTransform));
                    textObj.transform.SetParent(obj.transform, false);
                    if (!UIHelpers.TryAddTmpText(textObj, args))
                    {
                        return ToolUtils.CreateErrorResponse("Failed to create TextMeshPro component for button text. TextMeshPro is required for all UI actions in this agent and is the only text rendering system supported. If installation was just started, please wait for Unity to finish and manually click 'Import TMP Essentials' when prompted.");
                    }
                    // Set alignment for button text
                    var tmpComponent = textObj.GetComponent(System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro"));
                    if (tmpComponent != null && !args.ContainsKey("alignment"))
                    {
                        var alignmentProp = tmpComponent.GetType().GetProperty("alignment");
                        if (alignmentProp != null)
                        {
                            var alignmentType = alignmentProp.PropertyType;
                            var centerValue = System.Enum.Parse(alignmentType, "Center", true);
                            alignmentProp.SetValue(tmpComponent, centerValue, null);
                        }
                    }
                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    break;
                case "slider":
                    obj.AddComponent<Image>();
                    var slider = obj.AddComponent<Slider>();
                    slider.minValue = 0f;
                    slider.maxValue = 1f;
                    slider.value = 0.5f;
                    break;
                case "toggle":
                    obj.AddComponent<Image>();
                    var toggle = obj.AddComponent<Toggle>();
                    toggle.isOn = args.ContainsKey("isOn") && bool.TryParse(args["isOn"].ToString(), out bool isOn) ? isOn : false;
                    if (args.ContainsKey("toggleGroupPath") && !string.IsNullOrEmpty(args["toggleGroupPath"].ToString()))
                    {
                        var toggleGroupObj = ToolUtils.FindGameObjectByPath(args["toggleGroupPath"].ToString());
                        if (toggleGroupObj != null)
                        {
                            var toggleGroup = toggleGroupObj.GetComponent<ToggleGroup>();
                            if (toggleGroup == null) toggleGroup = toggleGroupObj.AddComponent<ToggleGroup>();
                            toggle.group = toggleGroup;
                        }
                    }
                    // Create label child for toggle
                    var toggleLabelObj = new UnityEngine.GameObject("Label", typeof(RectTransform));
                    toggleLabelObj.transform.SetParent(obj.transform, false);
                    if (!UIHelpers.TryAddTmpText(toggleLabelObj, args))
                    {
                        return ToolUtils.CreateErrorResponse("Failed to create TextMeshPro component for toggle label. TextMeshPro is required for all UI actions in this agent and is the only text rendering system supported. If installation was just started, please wait for Unity to finish and manually click 'Import TMP Essentials' when prompted.");
                    }
                    RectTransform toggleLabelRect = toggleLabelObj.GetComponent<RectTransform>();
                    toggleLabelRect.anchorMin = Vector2.zero;
                    toggleLabelRect.anchorMax = Vector2.one;
                    toggleLabelRect.offsetMin = Vector2.zero;
                    toggleLabelRect.offsetMax = Vector2.zero;
                    break;
                case "dropdown":
                case "tmp_dropdown":
                    // All dropdowns use TMP - TMP already checked at start of Execute()
                    var tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
                    if (tmpDropdownType == null || !UIHelpers.IsTextMeshProAvailable())
                    {
                        return ToolUtils.CreateErrorResponse("TMP_Dropdown requires TextMeshPro. TextMeshPro is required for all UI actions in this agent and is the only text rendering system supported. Please ensure TMP is installed and Essential Resources are imported.");
                    }
                    
                    obj.AddComponent<Image>();
                    var tmpDropdown = obj.AddComponent(tmpDropdownType);
                    if (args.ContainsKey("options") && args["options"] is System.Collections.IList tmpOptionsList)
                    {
                        var optionsProp = tmpDropdownType.GetProperty("options");
                        if (optionsProp != null)
                        {
                            var tmpOptionsListValue = optionsProp.GetValue(tmpDropdown);
                            var clearMethod = tmpOptionsListValue.GetType().GetMethod("Clear");
                            clearMethod?.Invoke(tmpOptionsListValue, null);
                            var addMethod = tmpOptionsListValue.GetType().GetMethod("Add");
                            foreach (var opt in tmpOptionsList)
                            {
                                var optionData = System.Activator.CreateInstance(tmpOptionsListValue.GetType().GetGenericArguments()[0]);
                                var textProp = optionData.GetType().GetProperty("text");
                                textProp?.SetValue(optionData, opt.ToString(), null);
                                addMethod?.Invoke(tmpOptionsListValue, new[] { optionData });
                            }
                        }
                    }
                    break;
                case "inputfield":
                case "tmp_inputfield":
                    // All input fields use TMP - TMP already checked at start of Execute()
                    var tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
                    if (tmpInputFieldType == null || !UIHelpers.IsTextMeshProAvailable())
                    {
                        return ToolUtils.CreateErrorResponse("TMP_InputField requires TextMeshPro. TextMeshPro is required for all UI actions in this agent and is the only text rendering system supported. Please ensure TMP is installed and Essential Resources are imported.");
                    }
                    
                    obj.AddComponent<Image>();
                    var tmpInputField = obj.AddComponent(tmpInputFieldType);
                    // Set placeholder
                    if (args.ContainsKey("placeholder"))
                    {
                        var placeholderObj = new UnityEngine.GameObject("Placeholder", typeof(RectTransform));
                        placeholderObj.transform.SetParent(obj.transform, false);
                        var tmpPlaceholderType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                        if (tmpPlaceholderType != null)
                        {
                            var tmpPlaceholder = placeholderObj.AddComponent(tmpPlaceholderType);
                            var textProp = tmpPlaceholderType.GetProperty("text");
                            textProp?.SetValue(tmpPlaceholder, args["placeholder"].ToString(), null);
                            var colorProp = tmpPlaceholderType.GetProperty("color");
                            colorProp?.SetValue(tmpPlaceholder, new Color(0.2f, 0.2f, 0.2f, 0.5f), null);
                            var placeholderProp = tmpInputFieldType.GetProperty("placeholder");
                            placeholderProp?.SetValue(tmpInputField, tmpPlaceholder, null);
                        }
                        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
                        placeholderRect.anchorMin = Vector2.zero;
                        placeholderRect.anchorMax = Vector2.one;
                        placeholderRect.offsetMin = Vector2.zero;
                        placeholderRect.offsetMax = Vector2.zero;
                    }
                    // Set text component
                    var textObj2 = new UnityEngine.GameObject("Text", typeof(RectTransform));
                    textObj2.transform.SetParent(obj.transform, false);
                    var tmpTextType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                    if (tmpTextType != null)
                    {
                        var tmpText = textObj2.AddComponent(tmpTextType);
                        var textComponentProp = tmpInputFieldType.GetProperty("textComponent");
                        textComponentProp?.SetValue(tmpInputField, tmpText, null);
                    }
                    RectTransform inputTextRect = textObj2.GetComponent<RectTransform>();
                    inputTextRect.anchorMin = Vector2.zero;
                    inputTextRect.anchorMax = Vector2.one;
                    inputTextRect.offsetMin = Vector2.zero;
                    inputTextRect.offsetMax = Vector2.zero;
                    if (args.ContainsKey("contentType"))
                    {
                        var contentTypeProp = tmpInputFieldType.GetProperty("contentType");
                        if (contentTypeProp != null)
                        {
                            var contentTypeEnum = System.Enum.Parse(contentTypeProp.PropertyType, args["contentType"].ToString(), true);
                            contentTypeProp.SetValue(tmpInputField, contentTypeEnum, null);
                        }
                    }
                    if (args.ContainsKey("characterLimit") && int.TryParse(args["characterLimit"].ToString(), out int tmpCharLimit))
                    {
                        var charLimitProp = tmpInputFieldType.GetProperty("characterLimit");
                        charLimitProp?.SetValue(tmpInputField, tmpCharLimit, null);
                    }
                    break;
                case "scrollview":
                case "scrollrect":
                    obj.AddComponent<Image>();
                    var scrollRect = obj.AddComponent<ScrollRect>();
                    scrollRect.horizontal = args.ContainsKey("horizontal") && bool.TryParse(args["horizontal"].ToString(), out bool h) ? h : true;
                    scrollRect.vertical = args.ContainsKey("vertical") && bool.TryParse(args["vertical"].ToString(), out bool v) ? v : true;
                    // Create Viewport child
                    var viewportObj = new UnityEngine.GameObject("Viewport", typeof(RectTransform));
                    viewportObj.transform.SetParent(obj.transform, false);
                    var viewportMask = viewportObj.AddComponent<Mask>();
                    viewportObj.AddComponent<Image>();
                    scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
                    // Create Content child
                    var contentObj = new UnityEngine.GameObject("Content", typeof(RectTransform));
                    contentObj.transform.SetParent(viewportObj.transform, false);
                    scrollRect.content = contentObj.GetComponent<RectTransform>();
                    // Create Scrollbar Horizontal
                    var hScrollbarObj = new UnityEngine.GameObject("Scrollbar Horizontal", typeof(RectTransform));
                    hScrollbarObj.transform.SetParent(obj.transform, false);
                    var hScrollbar = hScrollbarObj.AddComponent<Scrollbar>();
                    hScrollbar.direction = Scrollbar.Direction.LeftToRight;
                    scrollRect.horizontalScrollbar = hScrollbar;
                    // Create Scrollbar Vertical
                    var vScrollbarObj = new UnityEngine.GameObject("Scrollbar Vertical", typeof(RectTransform));
                    vScrollbarObj.transform.SetParent(obj.transform, false);
                    var vScrollbar = vScrollbarObj.AddComponent<Scrollbar>();
                    vScrollbar.direction = Scrollbar.Direction.BottomToTop;
                    scrollRect.verticalScrollbar = vScrollbar;
                    break;
                case "scrollbar":
                    obj.AddComponent<Image>();
                    var scrollbar = obj.AddComponent<Scrollbar>();
                    if (args.ContainsKey("direction") && System.Enum.TryParse<Scrollbar.Direction>(args["direction"].ToString(), true, out var dir))
                        scrollbar.direction = dir;
                    scrollbar.value = 1f;
                    scrollbar.size = 0.2f;
                    break;
                case "rawimage":
                    var rawImage = obj.AddComponent<RawImage>();
                    if (args.ContainsKey("texturePath") && !string.IsNullOrEmpty(args["texturePath"].ToString()))
                    {
                        string texturePath = args["texturePath"].ToString();
                        if (!texturePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                            texturePath = "Assets/" + texturePath;
                        var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                        if (texture != null) rawImage.texture = texture;
                    }
                    if (args.ContainsKey("color")) rawImage.color = ToolUtils.ParseColor(args["color"].ToString());
                    break;
                case "canvasgroup":
                    var canvasGroup = obj.AddComponent<CanvasGroup>();
                    if (args.ContainsKey("alpha") && float.TryParse(args["alpha"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float alpha))
                        canvasGroup.alpha = alpha;
                    else
                        canvasGroup.alpha = 1f;
                    if (args.ContainsKey("interactable") && bool.TryParse(args["interactable"].ToString(), out bool interactable))
                        canvasGroup.interactable = interactable;
                    else
                        canvasGroup.interactable = true;
                    if (args.ContainsKey("blocksRaycasts") && bool.TryParse(args["blocksRaycasts"].ToString(), out bool blocks))
                        canvasGroup.blocksRaycasts = blocks;
                    else
                        canvasGroup.blocksRaycasts = true;
                    break;
                case "horizontallayoutgroup":
                    var hLayout = obj.AddComponent<HorizontalLayoutGroup>();
                    if (args.ContainsKey("spacing") && float.TryParse(args["spacing"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float hSpacing))
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
                    break;
                case "verticallayoutgroup":
                    var vLayout = obj.AddComponent<VerticalLayoutGroup>();
                    if (args.ContainsKey("spacing") && float.TryParse(args["spacing"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float vSpacing))
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
                    break;
                case "gridlayoutgroup":
                    var gridLayout = obj.AddComponent<GridLayoutGroup>();
                    if (args.ContainsKey("spacing") && !string.IsNullOrEmpty(args["spacing"].ToString()))
                    {
                        var spacingParts = args["spacing"].ToString().Split(',');
                        if (spacingParts.Length >= 2)
                        {
                            gridLayout.spacing = new Vector2(
                                float.Parse(spacingParts[0].Trim(), CultureInfo.InvariantCulture),
                                float.Parse(spacingParts[1].Trim(), CultureInfo.InvariantCulture)
                            );
                        }
                    }
                    if (args.ContainsKey("cellSize") && !string.IsNullOrEmpty(args["cellSize"].ToString()))
                    {
                        var cellParts = args["cellSize"].ToString().Split(',');
                        if (cellParts.Length >= 2)
                        {
                            gridLayout.cellSize = new Vector2(
                                float.Parse(cellParts[0].Trim(), CultureInfo.InvariantCulture),
                                float.Parse(cellParts[1].Trim(), CultureInfo.InvariantCulture)
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
                    break;
                case "mask":
                    obj.AddComponent<Image>();
                    obj.AddComponent<Mask>();
                    break;
                case "rectmask2d":
                    obj.AddComponent<RectMask2D>();
                    break;
                default:
                    // Default case: Panel (Image component)
                    var defaultImage = obj.AddComponent<Image>();
                    if (args.ContainsKey("color"))
                    {
                        defaultImage.color = ToolUtils.ParseColor(args["color"].ToString());
                    }
                    defaultImage.type = Image.Type.Simple;
                    break;
            }

            var rect = obj.GetComponent<RectTransform>();
            if (args.ContainsKey("size"))
            {
                rect.sizeDelta = ToolUtils.ParseVector2(args["size"].ToString());
            }
            if (args.ContainsKey("anchoredPosition"))
            {
                rect.anchoredPosition = ToolUtils.ParseVector2(args["anchoredPosition"].ToString());
            }

            Undo.RegisterCreatedObjectUndo(obj, $"Create UI Element: {name}");
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(obj) }
            };
            return ToolUtils.CreateSuccessResponse($"Created UI element '{name}'", extras);
        }
    }
}
#endif
