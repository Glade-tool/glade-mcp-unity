using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class SetUiEventTool : ITool
    {
        public string Name => "set_ui_event";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string eventType = args.ContainsKey("eventType") ? args["eventType"].ToString() : "";
            string targetGameObjectPath = args.ContainsKey("targetGameObjectPath") ? args["targetGameObjectPath"].ToString() : "";
            string methodName = args.ContainsKey("methodName") ? args["methodName"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";

            if (string.IsNullOrEmpty(gameObjectPath) || string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(targetGameObjectPath) || string.IsNullOrEmpty(methodName))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath, eventType, targetGameObjectPath, and methodName are required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            UnityEngine.GameObject targetObj = ToolUtils.FindGameObjectByPath(targetGameObjectPath);
            if (targetObj == null)
            {
                return ToolUtils.CreateErrorResponse($"Target GameObject '{targetGameObjectPath}' not found");
            }

            Component targetComponent = null;
            if (!string.IsNullOrEmpty(componentType))
            {
                targetComponent = targetObj.GetComponent(componentType);
                if (targetComponent == null)
                {
                    return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on target GameObject");
                }
            }
            else
            {
                // Find component with the method
                var components = targetObj.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp.GetType().GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
                    {
                        targetComponent = comp;
                        break;
                    }
                }
                if (targetComponent == null)
                {
                    return ToolUtils.CreateErrorResponse($"Method '{methodName}' not found on target GameObject");
                }
            }

            // Verify method exists
            var method = targetComponent.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                return ToolUtils.CreateErrorResponse($"Method '{methodName}' not found on component '{targetComponent.GetType().Name}'");
            }

            // Helper to add persistent listener using SerializedProperty (most reliable method)
            bool AddListenerToEvent(Component component, string propertyPath, Component target, string methodName)
            {
                try
                {
                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty eventProperty = serializedObject.FindProperty(propertyPath);
                    
                    if (eventProperty != null)
                    {
                        // Get the current listener count
                        int listenerCount = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize;
                        
                        // Add a new listener entry
                        eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize = listenerCount + 1;
                        var newCall = eventProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").GetArrayElementAtIndex(listenerCount);
                        
                        // Set the target object
                        newCall.FindPropertyRelative("m_Target").objectReferenceValue = target;
                        
                        // Set the method name
                        newCall.FindPropertyRelative("m_MethodName").stringValue = methodName;
                        
                        // Set the mode (RuntimeOnly is default)
                        newCall.FindPropertyRelative("m_Mode").enumValueIndex = 2; // RuntimeOnly = 2
                        
                        // Apply changes
                        serializedObject.ApplyModifiedProperties();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to add listener: {ex.Message}");
                }
                return false;
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
                            success = AddListenerToEvent(button, "m_OnClick", targetComponent, methodName);
                            if (!success) errorMsg = "Failed to add listener to Button.onClick";
                        }
                        else
                        {
                            errorMsg = "GameObject does not have a Button component";
                        }
                        break;
                    case "onvaluechanged":
                        if (obj.TryGetComponent<Toggle>(out var toggle))
                        {
                            success = AddListenerToEvent(toggle, "m_OnValueChanged", targetComponent, methodName);
                            if (!success) errorMsg = "Failed to add listener to Toggle.onValueChanged";
                        }
                        else if (obj.TryGetComponent<Slider>(out var slider))
                        {
                            success = AddListenerToEvent(slider, "m_OnValueChanged", targetComponent, methodName);
                            if (!success) errorMsg = "Failed to add listener to Slider.onValueChanged";
                        }
                        else
                        {
                            errorMsg = "GameObject does not have a Toggle or Slider component";
                        }
                        break;
                    case "onendedit":
                        if (obj.TryGetComponent<InputField>(out var inputField))
                        {
                            success = AddListenerToEvent(inputField, "m_OnEndEdit", targetComponent, methodName);
                            if (!success) errorMsg = "Failed to add listener to InputField.onEndEdit";
                        }
                        else
                        {
                            errorMsg = "GameObject does not have an InputField component";
                        }
                        break;
                    case "onsubmit":
                        if (obj.TryGetComponent<InputField>(out var inputField2))
                        {
                            success = AddListenerToEvent(inputField2, "m_OnSubmit", targetComponent, methodName);
                            if (!success) errorMsg = "Failed to add listener to InputField.onSubmit";
                        }
                        else
                        {
                            errorMsg = "GameObject does not have an InputField component";
                        }
                        break;
                    case "onvaluechangedint":
                        if (obj.TryGetComponent<Dropdown>(out var dropdown))
                        {
                            success = AddListenerToEvent(dropdown, "m_OnValueChanged", targetComponent, methodName);
                            if (!success) errorMsg = "Failed to add listener to Dropdown.onValueChanged";
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
            catch (Exception e)
            {
                errorMsg = e.Message;
            }

            if (success)
            {
                return ToolUtils.CreateSuccessResponse($"Wired {eventType} event to {methodName} on {targetGameObjectPath}");
            }
            else
            {
                return ToolUtils.CreateErrorResponse(errorMsg);
            }
        }
    }
}
#endif
