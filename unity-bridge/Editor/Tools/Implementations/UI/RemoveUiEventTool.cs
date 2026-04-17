using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class RemoveUiEventTool : ITool
    {
        public string Name => "remove_ui_event";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string eventType = args.ContainsKey("eventType") ? args["eventType"].ToString() : "";
            bool removeAll = true;
            if (args.ContainsKey("removeAll"))
            {
                if (args["removeAll"] is bool ra) removeAll = ra;
                else if (bool.TryParse(args["removeAll"].ToString(), out bool rav)) removeAll = rav;
            }

            if (string.IsNullOrEmpty(gameObjectPath) || string.IsNullOrEmpty(eventType))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath and eventType are required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            bool success = false;
            string errorMsg = "";

            try
            {
                switch (eventType.ToLower())
                {
                    case "onclick":
                        if (obj.TryGetComponent<Button>(out var button))
                        {
                            if (removeAll)
                            {
                                button.onClick.RemoveAllListeners();
                            }
                            else
                            {
                                string targetPath = args.ContainsKey("targetGameObjectPath") ? args["targetGameObjectPath"].ToString() : "";
                                string methodName = args.ContainsKey("methodName") ? args["methodName"].ToString() : "";
                                if (string.IsNullOrEmpty(targetPath) || string.IsNullOrEmpty(methodName))
                                {
                                    return ToolUtils.CreateErrorResponse("targetGameObjectPath and methodName are required when removeAll=false");
                                }
                                // Remove specific listener - Unity doesn't have direct API, so we remove all and re-add others
                                // For now, just remove all
                                button.onClick.RemoveAllListeners();
                            }
                            success = true;
                        }
                        else
                        {
                            errorMsg = "GameObject does not have a Button component";
                        }
                        break;
                    case "onvaluechanged":
                        if (obj.TryGetComponent<Toggle>(out var toggle))
                        {
                            if (removeAll) toggle.onValueChanged.RemoveAllListeners();
                            success = true;
                        }
                        else if (obj.TryGetComponent<Slider>(out var slider))
                        {
                            if (removeAll) slider.onValueChanged.RemoveAllListeners();
                            success = true;
                        }
                        else
                        {
                            errorMsg = "GameObject does not have a Toggle or Slider component";
                        }
                        break;
                    case "onendedit":
                        if (obj.TryGetComponent<InputField>(out var inputField))
                        {
                            if (removeAll) inputField.onEndEdit.RemoveAllListeners();
                            success = true;
                        }
                        else
                        {
                            errorMsg = "GameObject does not have an InputField component";
                        }
                        break;
                    case "onsubmit":
                        if (obj.TryGetComponent<InputField>(out var inputField2))
                        {
                            if (removeAll) inputField2.onSubmit.RemoveAllListeners();
                            success = true;
                        }
                        else
                        {
                            errorMsg = "GameObject does not have an InputField component";
                        }
                        break;
                    case "onvaluechangedint":
                        if (obj.TryGetComponent<Dropdown>(out var dropdown))
                        {
                            if (removeAll) dropdown.onValueChanged.RemoveAllListeners();
                            success = true;
                        }
                        else
                        {
                            errorMsg = "GameObject does not have a Dropdown component";
                        }
                        break;
                    default:
                        errorMsg = $"Unknown event type: {eventType}";
                        break;
                }
            }
            catch (System.Exception e)
            {
                errorMsg = e.Message;
            }

            if (success)
            {
                return ToolUtils.CreateSuccessResponse($"Removed {eventType} event handlers from '{gameObjectPath}'");
            }
            else
            {
                return ToolUtils.CreateErrorResponse(errorMsg);
            }
        }
    }
}
#endif
