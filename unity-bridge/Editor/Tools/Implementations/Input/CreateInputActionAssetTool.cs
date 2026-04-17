using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

#if GLADE_INPUT_SYSTEM
using UnityEngine.InputSystem;
namespace GladeAgenticAI.Core.Tools.Implementations.Input
{
    public class CreateInputActionAssetTool : ITool
    {
        public string Name => "create_input_action_asset";

        public string Execute(Dictionary<string, object> args)
        {
            // Check if the project uses the new Input System
            #if !ENABLE_INPUT_SYSTEM
            return ToolUtils.CreateErrorResponse("Cannot create InputActionAsset: The project is not using the new Input System. The new Input System package (com.unity.inputsystem) must be installed and enabled in Project Settings > Player > Active Input Handling.");
            #else
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            string dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                { "assetPath", assetPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created InputActionAsset at '{assetPath}'", extras);
            #endif
        }
    }
}
#endif
