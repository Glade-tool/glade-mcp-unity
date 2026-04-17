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
    public class SetUiPropertiesTool : ITool
    {
        public string Name => "set_ui_properties";

        public string Execute(Dictionary<string, object> args)
        {
            // CRITICAL: All UI actions require TextMeshPro. Check before doing anything.
            var tmpCheck = UIHelpers.EnsureTMPForUIActions();
            if (!tmpCheck.IsAvailable)
            {
                // TMP not available - user must install it explicitly first
                return ToolUtils.CreateErrorResponse(tmpCheck.Message);
            }

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

            Undo.RecordObject(obj, $"Set UI Properties: {gameObjectPath}");

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                if (args.ContainsKey("size")) rect.sizeDelta = ToolUtils.ParseVector2(args["size"].ToString());
                if (args.ContainsKey("anchoredPosition")) rect.anchoredPosition = ToolUtils.ParseVector2(args["anchoredPosition"].ToString());
                if (args.ContainsKey("anchorMin")) rect.anchorMin = ToolUtils.ParseVector2(args["anchorMin"].ToString());
                if (args.ContainsKey("anchorMax")) rect.anchorMax = ToolUtils.ParseVector2(args["anchorMax"].ToString());
                if (args.ContainsKey("pivot")) rect.pivot = ToolUtils.ParseVector2(args["pivot"].ToString());
            }

            // Only TMP text components are supported - no regular Text components
            var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var tmpComponent = obj.GetComponent(tmpType);
                if (tmpComponent != null)
                {
                    UIHelpers.TrySetTmpTextProperties(tmpComponent, args);
                }
            }
            if (obj.TryGetComponent<Image>(out var image))
            {
                if (args.ContainsKey("color")) image.color = ToolUtils.ParseColor(args["color"].ToString());
                if (args.ContainsKey("spritePath"))
                {
                    string spritePath = args["spritePath"].ToString();
                    if (!spritePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        spritePath = "Assets/" + spritePath;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite != null) image.sprite = sprite;
                }
                // Image fill properties for health bars and filled UI elements
                // If fillAmount is set, ensure type is Filled (unless explicitly set otherwise)
                float fillAmountValue = 0f;
                bool hasFillAmount = args.ContainsKey("fillAmount") && float.TryParse(args["fillAmount"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out fillAmountValue);
                // Accept both "imageType" (new) and "type" (legacy) parameter names
                string imgTypeKey = args.ContainsKey("imageType") ? "imageType" : "type";
                if (args.ContainsKey(imgTypeKey) && System.Enum.TryParse<Image.Type>(args[imgTypeKey].ToString(), true, out var imageType))
                    image.type = imageType;
                else if (hasFillAmount)
                    image.type = Image.Type.Filled; // Auto-set to Filled if fillAmount is used
                else if (image.type == Image.Type.Tiled)
                    image.type = Image.Type.Simple; // Default to Simple to avoid tiled/repeated images
                
                if (hasFillAmount)
                    image.fillAmount = Mathf.Clamp01(fillAmountValue);
                if (args.ContainsKey("fillMethod") && System.Enum.TryParse<Image.FillMethod>(args["fillMethod"].ToString(), true, out var fillMethod))
                    image.fillMethod = fillMethod;
                if (args.ContainsKey("fillOrigin") && int.TryParse(args["fillOrigin"].ToString(), out int fillOrigin))
                    image.fillOrigin = fillOrigin;
                if (args.ContainsKey("preserveAspect"))
                {
                    if (args["preserveAspect"] is bool pa) image.preserveAspect = pa;
                    else if (bool.TryParse(args["preserveAspect"].ToString(), out bool pav)) image.preserveAspect = pav;
                }
                if (args.ContainsKey("raycastTarget"))
                {
                    if (args["raycastTarget"] is bool rt) image.raycastTarget = rt;
                    else if (bool.TryParse(args["raycastTarget"].ToString(), out bool rtv)) image.raycastTarget = rtv;
                }
            }
            if (obj.TryGetComponent<Button>(out var button))
            {
                if (args.ContainsKey("interactable"))
                {
                    if (args["interactable"] is bool b) button.interactable = b;
                    else if (bool.TryParse(args["interactable"].ToString(), out bool v)) button.interactable = v;
                }
            }
            if (obj.TryGetComponent<Slider>(out var slider))
            {
                if (args.ContainsKey("minValue") && float.TryParse(args["minValue"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float min)) slider.minValue = min;
                if (args.ContainsKey("maxValue") && float.TryParse(args["maxValue"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float max)) slider.maxValue = max;
                if (args.ContainsKey("value") && float.TryParse(args["value"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float val)) slider.value = val;
            }
            if (obj.TryGetComponent<Toggle>(out var toggle))
            {
                if (args.ContainsKey("isOn"))
                {
                    if (args["isOn"] is bool b) toggle.isOn = b;
                    else if (bool.TryParse(args["isOn"].ToString(), out bool v)) toggle.isOn = v;
                }
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
            }
            // Only TMP_Dropdown is supported - regular Dropdown is not handled
            // TMP_Dropdown
            var tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
            if (tmpDropdownType != null)
            {
                var tmpDropdown = obj.GetComponent(tmpDropdownType);
                if (tmpDropdown != null)
                {
                    if (args.ContainsKey("options") && args["options"] is System.Collections.IList tmpOptionsList)
                    {
                        var optionsProp = tmpDropdownType.GetProperty("options");
                        if (optionsProp != null)
                        {
                            var optionsList = optionsProp.GetValue(tmpDropdown);
                            var clearMethod = optionsList.GetType().GetMethod("Clear");
                            clearMethod?.Invoke(optionsList, null);
                            var addMethod = optionsList.GetType().GetMethod("Add");
                            foreach (var opt in tmpOptionsList)
                            {
                                var optionData = System.Activator.CreateInstance(optionsList.GetType().GetGenericArguments()[0]);
                                var textProp = optionData.GetType().GetProperty("text");
                                textProp?.SetValue(optionData, opt.ToString(), null);
                                addMethod?.Invoke(optionsList, new[] { optionData });
                            }
                        }
                    }
                    if (args.ContainsKey("value") && int.TryParse(args["value"].ToString(), out int tmpDropdownValue))
                    {
                        var valueProp = tmpDropdownType.GetProperty("value");
                        valueProp?.SetValue(tmpDropdown, tmpDropdownValue, null);
                    }
                }
            }
            // Only TMP_InputField is supported - regular InputField is not handled
            // TMP_InputField
            var tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (tmpInputFieldType != null)
            {
                var tmpInputField = obj.GetComponent(tmpInputFieldType);
                if (tmpInputField != null)
                {
                    if (args.ContainsKey("placeholder") && !string.IsNullOrEmpty(args["placeholder"].ToString()))
                    {
                        var placeholderProp = tmpInputFieldType.GetProperty("placeholder");
                        var placeholder = placeholderProp?.GetValue(tmpInputField);
                        if (placeholder != null)
                        {
                            var textProp = placeholder.GetType().GetProperty("text");
                            textProp?.SetValue(placeholder, args["placeholder"].ToString(), null);
                        }
                    }
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
                    if (args.ContainsKey("lineType"))
                    {
                        var lineTypeProp = tmpInputFieldType.GetProperty("lineType");
                        if (lineTypeProp != null)
                        {
                            var lineTypeEnum = System.Enum.Parse(lineTypeProp.PropertyType, args["lineType"].ToString(), true);
                            lineTypeProp.SetValue(tmpInputField, lineTypeEnum, null);
                        }
                    }
                }
            }
            if (obj.TryGetComponent<ScrollRect>(out var scrollRect))
            {
                if (args.ContainsKey("horizontal"))
                {
                    if (args["horizontal"] is bool h) scrollRect.horizontal = h;
                    else if (bool.TryParse(args["horizontal"].ToString(), out bool hv)) scrollRect.horizontal = hv;
                }
                if (args.ContainsKey("vertical"))
                {
                    if (args["vertical"] is bool v) scrollRect.vertical = v;
                    else if (bool.TryParse(args["vertical"].ToString(), out bool vv)) scrollRect.vertical = vv;
                }
                if (args.ContainsKey("movementType") && System.Enum.TryParse<ScrollRect.MovementType>(args["movementType"].ToString(), true, out var movementType))
                    scrollRect.movementType = movementType;
                if (args.ContainsKey("contentPath") && !string.IsNullOrEmpty(args["contentPath"].ToString()))
                {
                    var contentObj = ToolUtils.FindGameObjectByPath(args["contentPath"].ToString());
                    if (contentObj != null) scrollRect.content = contentObj.GetComponent<RectTransform>();
                }
                if (args.ContainsKey("viewportPath") && !string.IsNullOrEmpty(args["viewportPath"].ToString()))
                {
                    var viewportObj = ToolUtils.FindGameObjectByPath(args["viewportPath"].ToString());
                    if (viewportObj != null) scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
                }
            }
            if (obj.TryGetComponent<Scrollbar>(out var scrollbar))
            {
                if (args.ContainsKey("direction") && System.Enum.TryParse<Scrollbar.Direction>(args["direction"].ToString(), true, out var direction))
                    scrollbar.direction = direction;
                if (args.ContainsKey("value") && float.TryParse(args["value"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float scrollbarValue))
                    scrollbar.value = scrollbarValue;
                if (args.ContainsKey("size") && float.TryParse(args["size"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float scrollbarSize))
                    scrollbar.size = scrollbarSize;
                if (args.ContainsKey("numberOfSteps") && int.TryParse(args["numberOfSteps"].ToString(), out int steps))
                    scrollbar.numberOfSteps = steps;
            }
            if (obj.TryGetComponent<RawImage>(out var rawImage))
            {
                if (args.ContainsKey("color")) rawImage.color = ToolUtils.ParseColor(args["color"].ToString());
                if (args.ContainsKey("texturePath") && !string.IsNullOrEmpty(args["texturePath"].ToString()))
                {
                    string texturePath = args["texturePath"].ToString();
                    if (!texturePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        texturePath = "Assets/" + texturePath;
                    var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
                    if (texture != null) rawImage.texture = texture;
                }
                if (args.ContainsKey("uvRect") && !string.IsNullOrEmpty(args["uvRect"].ToString()))
                {
                    var uvParts = args["uvRect"].ToString().Split(',');
                    if (uvParts.Length >= 4)
                    {
                        rawImage.uvRect = new Rect(
                            float.Parse(uvParts[0].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(uvParts[1].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(uvParts[2].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(uvParts[3].Trim(), CultureInfo.InvariantCulture)
                        );
                    }
                }
            }
            if (obj.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                if (args.ContainsKey("alpha") && float.TryParse(args["alpha"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float alpha))
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
            }
            // Layout Groups - delegate to SetLayoutGroupPropertiesTool logic
            if (obj.TryGetComponent<HorizontalLayoutGroup>(out var hLayout))
            {
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
            if (obj.TryGetComponent<VerticalLayoutGroup>(out var vLayout))
            {
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
            if (obj.TryGetComponent<GridLayoutGroup>(out var gridLayout))
            {
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

            return ToolUtils.CreateSuccessResponse($"Updated UI properties on '{gameObjectPath}'");
        }
    }
}
#endif
