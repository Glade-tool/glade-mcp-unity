using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

#if GLADE_INPUT_SYSTEM
using UnityEngine.InputSystem;
namespace GladeAgenticAI.Core.Tools.Implementations.Input
{
    public class AssignInputActionsTool : ITool
    {
        public string Name => "assign_input_actions";

        public string Execute(Dictionary<string, object> args)
        {
            // Check if the project uses the new Input System
            #if !ENABLE_INPUT_SYSTEM
            return ToolUtils.CreateErrorResponse("Cannot assign InputActionAsset: The project is not using the new Input System. The new Input System package (com.unity.inputsystem) must be installed and enabled in Project Settings > Player > Active Input Handling.");
            #else
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";

            if (string.IsNullOrEmpty(gameObjectPath) || string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath and assetPath are required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (asset == null)
            {
                return ToolUtils.CreateErrorResponse($"InputActionAsset not found at '{assetPath}'");
            }

            PlayerInput playerInput = obj.GetComponent<PlayerInput>() ?? Undo.AddComponent<PlayerInput>(obj);
            playerInput.actions = asset;
            EditorUtility.SetDirty(playerInput);

            return ToolUtils.CreateSuccessResponse($"Assigned InputActionAsset to '{gameObjectPath}'");
            #endif
        }
    }
}
#endif
