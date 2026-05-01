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

            // Re-hydrate JSON-array strings at every nesting level so this works
            // whether args arrive already-typed or string-encoded (e.g. via batch_execute).
            var mapsObj = args["maps"];
            if (mapsObj is string mapsJson && ToolUtils.TryParseJsonArrayToList(mapsJson, out var parsedMaps))
                mapsObj = parsedMaps;
            if (!(mapsObj is List<object> mapsList))
                return ToolUtils.CreateErrorResponse("maps must be an array of action maps");

            int mapsUpdated = 0;
            int actionsUpdated = 0;
            int bindingsAdded = 0;

            foreach (var mapObj in mapsList)
            {
                if (!(mapObj is Dictionary<string, object> mapDict)) continue;

                string mapName = mapDict.ContainsKey("name") ? mapDict["name"].ToString() : "";
                if (string.IsNullOrEmpty(mapName)) continue;

                // Default throwIfNotFound:false returns null so the ?? fallback fires.
                InputActionMap map = asset.FindActionMap(mapName) ?? asset.AddActionMap(mapName);
                mapsUpdated++;

                if (!mapDict.ContainsKey("actions")) continue;
                var actionsObj = mapDict["actions"];
                if (actionsObj is string actionsJson && ToolUtils.TryParseJsonArrayToList(actionsJson, out var parsedActions))
                    actionsObj = parsedActions;
                if (!(actionsObj is List<object> actionsList)) continue;

                foreach (var actionObj in actionsList)
                {
                    if (!(actionObj is Dictionary<string, object> actionDict)) continue;

                    string actionName = actionDict.ContainsKey("name") ? actionDict["name"].ToString() : "";
                    string actionType = actionDict.ContainsKey("type") ? actionDict["type"].ToString() : "Value";
                    if (string.IsNullOrEmpty(actionName)) continue;

                    InputAction action = map.FindAction(actionName);
                    if (action == null)
                    {
                        action = map.AddAction(actionName, ToolUtils.ParseInputActionType(actionType));
                    }

                    if (replaceBindings)
                    {
                        InputActionSetupExtensions.RemoveAction(asset, actionName);
                        action = map.AddAction(actionName, ToolUtils.ParseInputActionType(actionType));
                    }
                    actionsUpdated++;

                    if (!actionDict.ContainsKey("bindings")) continue;
                    var bindingsObj = actionDict["bindings"];
                    if (bindingsObj is string bindingsJson && ToolUtils.TryParseJsonArrayToList(bindingsJson, out var parsedBindings))
                        bindingsObj = parsedBindings;
                    if (!(bindingsObj is List<object> bindingsList)) continue;

                    foreach (var bindObj in bindingsList)
                    {
                        if (!(bindObj is Dictionary<string, object> bindDict)) continue;
                        string path = bindDict.ContainsKey("path") ? bindDict["path"].ToString() : "";
                        if (string.IsNullOrEmpty(path)) continue;
                        var binding = action.AddBinding(path);
                        if (bindDict.ContainsKey("interactions")) binding.WithInteractions(bindDict["interactions"].ToString());
                        if (bindDict.ContainsKey("processors")) binding.WithProcessors(bindDict["processors"].ToString());
                        if (bindDict.ContainsKey("groups")) binding.WithGroups(bindDict["groups"].ToString());
                        bindingsAdded++;
                    }
                }
            }

            // .inputactions assets persist via JSON — SetDirty + SaveAssets is not
            // enough; explicitly re-serialize the asset and reimport.
            try
            {
                string json = asset.ToJson();
                System.IO.File.WriteAllText(assetPath, json);
                AssetDatabase.ImportAsset(assetPath);
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse(
                    $"Applied {mapsUpdated} map(s)/{actionsUpdated} action(s) in memory but failed to persist to '{assetPath}': {e.Message}");
            }

            var extras = new Dictionary<string, object>
            {
                { "mapsUpdated", mapsUpdated },
                { "actionsUpdated", actionsUpdated },
                { "bindingsAdded", bindingsAdded }
            };
            return ToolUtils.CreateSuccessResponse(
                $"Updated InputActionAsset: {mapsUpdated} map(s), {actionsUpdated} action(s), {bindingsAdded} binding(s)",
                extras);
            #endif
        }
    }
}
#endif
