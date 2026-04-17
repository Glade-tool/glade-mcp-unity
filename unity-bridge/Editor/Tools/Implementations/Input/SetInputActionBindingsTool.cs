using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

#if GLADE_INPUT_SYSTEM
using UnityEngine.InputSystem;
namespace GladeAgenticAI.Core.Tools.Implementations.Input
{
    public class SetInputActionBindingsTool : ITool
    {
        public string Name => "set_input_action_bindings";

        public string Execute(Dictionary<string, object> args)
        {
            // Check if the project uses the new Input System
            #if !ENABLE_INPUT_SYSTEM
            return ToolUtils.CreateErrorResponse("Cannot modify InputActionAsset: The project is not using the new Input System. The new Input System package (com.unity.inputsystem) must be installed and enabled in Project Settings > Player > Active Input Handling.");
            #else
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (asset == null)
            {
                return ToolUtils.CreateErrorResponse($"InputActionAsset not found at '{assetPath}'");
            }

            if (!args.ContainsKey("maps"))
            {
                return ToolUtils.CreateErrorResponse("maps is required");
            }

            bool replaceBindings = true;
            if (args.ContainsKey("replaceBindings"))
            {
                if (args["replaceBindings"] is bool b) replaceBindings = b;
                else bool.TryParse(args["replaceBindings"].ToString(), out replaceBindings);
            }

            if (args["maps"] is List<object> mapsList)
            {
                foreach (var mapObj in mapsList)
                {
                    if (mapObj is Dictionary<string, object> mapDict)
                    {
                        string mapName = mapDict.ContainsKey("name") ? mapDict["name"].ToString() : "";
                        if (string.IsNullOrEmpty(mapName)) continue;

                        InputActionMap map = asset.FindActionMap(mapName, true) ?? asset.AddActionMap(mapName);
                        if (mapDict.ContainsKey("actions") && mapDict["actions"] is List<object> actionsList)
                        {
                            foreach (var actionObj in actionsList)
                            {
                                if (actionObj is Dictionary<string, object> actionDict)
                                {
                                    string actionName = actionDict.ContainsKey("name") ? actionDict["name"].ToString() : "";
                                    string actionType = actionDict.ContainsKey("type") ? actionDict["type"].ToString() : "Value";
                                    if (string.IsNullOrEmpty(actionName)) continue;

                                    InputAction action = map.FindAction(actionName, true);
                                    if (action == null)
                                    {
                                        action = map.AddAction(actionName, ToolUtils.ParseInputActionType(actionType));
                                    }

                                    if (replaceBindings)
                                    {
                                        InputActionSetupExtensions.RemoveAction(asset, actionName);
                                        action = map.AddAction(actionName, ToolUtils.ParseInputActionType(actionType));
                                    }

                                    if (actionDict.ContainsKey("bindings") && actionDict["bindings"] is List<object> bindingsList)
                                    {
                                        foreach (var bindObj in bindingsList)
                                        {
                                            if (bindObj is Dictionary<string, object> bindDict)
                                            {
                                                string path = bindDict.ContainsKey("path") ? bindDict["path"].ToString() : "";
                                                if (string.IsNullOrEmpty(path)) continue;
                                                var binding = action.AddBinding(path);
                                                if (bindDict.ContainsKey("interactions")) binding.WithInteractions(bindDict["interactions"].ToString());
                                                if (bindDict.ContainsKey("processors")) binding.WithProcessors(bindDict["processors"].ToString());
                                                if (bindDict.ContainsKey("groups")) binding.WithGroups(bindDict["groups"].ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            
            return ToolUtils.CreateSuccessResponse("Updated InputActionAsset bindings");
            #endif
        }
    }
}
#endif
