using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class FindUiElementsByTypeTool : ITool
    {
        public string Name => "find_ui_elements_by_type";

        public string Execute(Dictionary<string, object> args)
        {
            string elementType = args.ContainsKey("elementType") ? args["elementType"].ToString() : "";
            if (string.IsNullOrEmpty(elementType))
            {
                return ToolUtils.CreateErrorResponse("elementType is required");
            }

            string canvasPath = args.ContainsKey("canvasPath") ? args["canvasPath"].ToString() : "";
            Canvas filterCanvas = null;
            if (!string.IsNullOrEmpty(canvasPath))
            {
                var canvasObj = ToolUtils.FindGameObjectByPath(canvasPath);
                if (canvasObj != null) filterCanvas = canvasObj.GetComponent<Canvas>();
            }

            var results = new List<string>();
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            switch (elementType.ToLower())
            {
                case "button":
                    var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
                    foreach (var btn in buttons)
                    {
                        if (btn.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(btn.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(btn.gameObject));
                    }
                    break;
                case "text":
                    var texts = Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
                    foreach (var txt in texts)
                    {
                        if (txt.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(txt.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(txt.gameObject));
                    }
                    break;
                case "image":
                    var images = Object.FindObjectsByType<Image>(FindObjectsSortMode.None);
                    foreach (var img in images)
                    {
                        if (img.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(img.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(img.gameObject));
                    }
                    break;
                case "slider":
                    var sliders = Object.FindObjectsByType<Slider>(FindObjectsSortMode.None);
                    foreach (var sld in sliders)
                    {
                        if (sld.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(sld.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(sld.gameObject));
                    }
                    break;
                case "toggle":
                    var toggles = Object.FindObjectsByType<Toggle>(FindObjectsSortMode.None);
                    foreach (var tgl in toggles)
                    {
                        if (tgl.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(tgl.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(tgl.gameObject));
                    }
                    break;
                case "dropdown":
                    var dropdowns = Object.FindObjectsByType<Dropdown>(FindObjectsSortMode.None);
                    foreach (var dd in dropdowns)
                    {
                        if (dd.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(dd.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(dd.gameObject));
                    }
                    break;
                case "tmp_dropdown":
                    var tmpDropdownType = System.Type.GetType("TMPro.TMP_Dropdown, Unity.TextMeshPro");
                    if (tmpDropdownType != null)
                    {
                        var tmpDropdowns = Object.FindObjectsByType(tmpDropdownType, FindObjectsSortMode.None);
                        foreach (Component dd in tmpDropdowns)
                        {
                            if (dd.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(dd.gameObject, filterCanvas)))
                                results.Add(ToolUtils.GetGameObjectPath(dd.gameObject));
                        }
                    }
                    break;
                case "inputfield":
                    var inputFields = Object.FindObjectsByType<InputField>(FindObjectsSortMode.None);
                    foreach (var ifield in inputFields)
                    {
                        if (ifield.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(ifield.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(ifield.gameObject));
                    }
                    break;
                case "tmp_inputfield":
                    var tmpInputFieldType = System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
                    if (tmpInputFieldType != null)
                    {
                        var tmpInputFields = Object.FindObjectsByType(tmpInputFieldType, FindObjectsSortMode.None);
                        foreach (Component ifield in tmpInputFields)
                        {
                            if (ifield.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(ifield.gameObject, filterCanvas)))
                                results.Add(ToolUtils.GetGameObjectPath(ifield.gameObject));
                        }
                    }
                    break;
                case "scrollrect":
                    var scrollRects = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None);
                    foreach (var sr in scrollRects)
                    {
                        if (sr.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(sr.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(sr.gameObject));
                    }
                    break;
                case "scrollbar":
                    var scrollbars = Object.FindObjectsByType<Scrollbar>(FindObjectsSortMode.None);
                    foreach (var scrollbar in scrollbars)
                    {
                        if (scrollbar.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(scrollbar.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(scrollbar.gameObject));
                    }
                    break;
                case "rawimage":
                    var rawImages = Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None);
                    foreach (var ri in rawImages)
                    {
                        if (ri.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(ri.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(ri.gameObject));
                    }
                    break;
                case "canvasgroup":
                    var canvasGroups = Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None);
                    foreach (var cg in canvasGroups)
                    {
                        if (cg.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(cg.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(cg.gameObject));
                    }
                    break;
                case "horizontallayoutgroup":
                    var hLayouts = Object.FindObjectsByType<HorizontalLayoutGroup>(FindObjectsSortMode.None);
                    foreach (var hlg in hLayouts)
                    {
                        if (hlg.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(hlg.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(hlg.gameObject));
                    }
                    break;
                case "verticallayoutgroup":
                    var vLayouts = Object.FindObjectsByType<VerticalLayoutGroup>(FindObjectsSortMode.None);
                    foreach (var vlg in vLayouts)
                    {
                        if (vlg.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(vlg.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(vlg.gameObject));
                    }
                    break;
                case "gridlayoutgroup":
                    var gridLayouts = Object.FindObjectsByType<GridLayoutGroup>(FindObjectsSortMode.None);
                    foreach (var glg in gridLayouts)
                    {
                        if (glg.gameObject.scene == activeScene && (filterCanvas == null || IsInCanvas(glg.gameObject, filterCanvas)))
                            results.Add(ToolUtils.GetGameObjectPath(glg.gameObject));
                    }
                    break;
                case "canvas":
                    var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    foreach (var canvas in canvases)
                    {
                        if (canvas.gameObject.scene == activeScene)
                            results.Add(ToolUtils.GetGameObjectPath(canvas.gameObject));
                    }
                    break;
                case "eventsystem":
                    var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
                    foreach (var es in eventSystems)
                    {
                        if (es.gameObject.scene == activeScene)
                            results.Add(ToolUtils.GetGameObjectPath(es.gameObject));
                    }
                    break;
            }

            var sb = new StringBuilder();
            sb.Append("{\"success\":true,\"elementType\":\"");
            sb.Append(ToolUtils.EscapeJsonString(elementType));
            sb.Append("\",\"paths\":[");
            for (int i = 0; i < results.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"{ToolUtils.EscapeJsonString(results[i])}\"");
            }
            sb.Append($"],\"count\":{results.Count}}}");
            return sb.ToString();
        }

        private static bool IsInCanvas(UnityEngine.GameObject obj, Canvas canvas)
        {
            UnityEngine.Transform current = obj.transform;
            while (current != null)
            {
                if (current.GetComponent<Canvas>() == canvas)
                    return true;
                current = current.parent;
            }
            return false;
        }
    }
}
#endif
