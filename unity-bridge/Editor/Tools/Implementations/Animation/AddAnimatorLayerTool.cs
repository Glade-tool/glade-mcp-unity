using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class AddAnimatorLayerTool : ITool
    {
        public string Name => "add_animator_layer";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string layerName = args.ContainsKey("layerName") ? args["layerName"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(layerName))
                return ToolUtils.CreateErrorResponse("layerName is required");
            
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            // Check if layer already exists
            foreach (var layer in controller.layers)
            {
                if (layer.name == layerName)
                    return ToolUtils.CreateErrorResponse($"Layer '{layerName}' already exists");
            }
            
            float defaultWeight = 1f;
            if (args.ContainsKey("defaultWeight"))
            {
                if (args["defaultWeight"] is float f) defaultWeight = f;
                else float.TryParse(args["defaultWeight"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out defaultWeight);
            }
            
            string blendingMode = args.ContainsKey("blendingMode") ? args["blendingMode"].ToString() : "Override";
            AnimatorLayerBlendingMode mode = blendingMode.ToLower() switch
            {
                "additive" => AnimatorLayerBlendingMode.Additive,
                _ => AnimatorLayerBlendingMode.Override
            };
            
            controller.AddLayer(layerName);
            
            // Get the newly added layer and configure it
            var layers = controller.layers;
            int newLayerIndex = layers.Length - 1;
            layers[newLayerIndex].defaultWeight = defaultWeight;
            layers[newLayerIndex].blendingMode = mode;
            
            // Apply avatar mask if specified
            if (args.ContainsKey("avatarMaskPath"))
            {
                string maskPath = args["avatarMaskPath"].ToString();
                if (!maskPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    maskPath = "Assets/" + maskPath;
                UnityEngine.AvatarMask mask = AssetDatabase.LoadAssetAtPath<UnityEngine.AvatarMask>(maskPath);
                if (mask != null)
                    layers[newLayerIndex].avatarMask = mask;
            }
            
            controller.layers = layers;
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "layerName", layerName },
                { "layerIndex", newLayerIndex }
            };
            
            return ToolUtils.CreateSuccessResponse($"Added layer '{layerName}' at index {newLayerIndex}", extras);
        }
    }
}
