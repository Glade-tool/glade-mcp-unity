using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class GetUiEventHandlersTool : ITool
    {
        public string Name => "get_ui_event_handlers";

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

            var handlers = new List<Dictionary<string, object>>();

            if (obj.TryGetComponent<Button>(out var button))
            {
                int count = button.onClick.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    handlers.Add(new Dictionary<string, object>
                    {
                        {"eventType", "onClick"},
                        {"targetGameObject", button.onClick.GetPersistentTarget(i)?.ToString() ?? ""},
                        {"methodName", button.onClick.GetPersistentMethodName(i) ?? ""}
                    });
                }
            }
            if (obj.TryGetComponent<Toggle>(out var toggle))
            {
                int count = toggle.onValueChanged.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    handlers.Add(new Dictionary<string, object>
                    {
                        {"eventType", "onValueChanged"},
                        {"targetGameObject", toggle.onValueChanged.GetPersistentTarget(i)?.ToString() ?? ""},
                        {"methodName", toggle.onValueChanged.GetPersistentMethodName(i) ?? ""}
                    });
                }
            }
            if (obj.TryGetComponent<Slider>(out var slider))
            {
                int count = slider.onValueChanged.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    handlers.Add(new Dictionary<string, object>
                    {
                        {"eventType", "onValueChanged"},
                        {"targetGameObject", slider.onValueChanged.GetPersistentTarget(i)?.ToString() ?? ""},
                        {"methodName", slider.onValueChanged.GetPersistentMethodName(i) ?? ""}
                    });
                }
            }
            if (obj.TryGetComponent<InputField>(out var inputField))
            {
                int countEndEdit = inputField.onEndEdit.GetPersistentEventCount();
                for (int i = 0; i < countEndEdit; i++)
                {
                    handlers.Add(new Dictionary<string, object>
                    {
                        {"eventType", "onEndEdit"},
                        {"targetGameObject", inputField.onEndEdit.GetPersistentTarget(i)?.ToString() ?? ""},
                        {"methodName", inputField.onEndEdit.GetPersistentMethodName(i) ?? ""}
                    });
                }
                int countSubmit = inputField.onSubmit.GetPersistentEventCount();
                for (int i = 0; i < countSubmit; i++)
                {
                    handlers.Add(new Dictionary<string, object>
                    {
                        {"eventType", "onSubmit"},
                        {"targetGameObject", inputField.onSubmit.GetPersistentTarget(i)?.ToString() ?? ""},
                        {"methodName", inputField.onSubmit.GetPersistentMethodName(i) ?? ""}
                    });
                }
            }
            if (obj.TryGetComponent<Dropdown>(out var dropdown))
            {
                int count = dropdown.onValueChanged.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    handlers.Add(new Dictionary<string, object>
                    {
                        {"eventType", "onValueChangedInt"},
                        {"targetGameObject", dropdown.onValueChanged.GetPersistentTarget(i)?.ToString() ?? ""},
                        {"methodName", dropdown.onValueChanged.GetPersistentMethodName(i) ?? ""}
                    });
                }
            }

            var sb = new StringBuilder();
            sb.Append("{\"success\":true,\"gameObjectPath\":\"");
            sb.Append(ToolUtils.EscapeJsonString(gameObjectPath));
            sb.Append("\",\"handlers\":[");
            for (int i = 0; i < handlers.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"eventType\":\"{ToolUtils.EscapeJsonString(handlers[i]["eventType"].ToString())}\",");
                sb.Append($"\"targetGameObject\":\"{ToolUtils.EscapeJsonString(handlers[i]["targetGameObject"].ToString())}\",");
                sb.Append($"\"methodName\":\"{ToolUtils.EscapeJsonString(handlers[i]["methodName"].ToString())}\"");
                sb.Append("}");
            }
            sb.Append($"],\"count\":{handlers.Count}}}");
            return sb.ToString();
        }
    }
}
#endif
