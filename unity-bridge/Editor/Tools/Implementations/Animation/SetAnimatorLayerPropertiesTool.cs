using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class SetAnimatorLayerPropertiesTool : ITool
    {
        public string Name => "set_animator_layer_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            
            if (!controllerPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                controllerPath = "Assets/" + controllerPath;
            
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return ToolUtils.CreateErrorResponse($"Animator Controller not found at '{controllerPath}'");
            
            int layerIndex = 0;
            if (args.ContainsKey("layerIndex"))
            {
                if (args["layerIndex"] is int i) layerIndex = i;
                else if (args["layerIndex"] is float f) layerIndex = (int)f;
                else int.TryParse(args["layerIndex"].ToString(), out layerIndex);
            }
            else if (args.ContainsKey("layerName"))
            {
                string layerName = args["layerName"].ToString();
                layerIndex = -1;
                for (int i = 0; i < controller.layers.Length; i++)
                {
                    if (controller.layers[i].name == layerName)
                    {
                        layerIndex = i;
                        break;
                    }
                }
                if (layerIndex < 0)
                    return ToolUtils.CreateErrorResponse($"Layer '{layerName}' not found");
            }
            
            if (layerIndex >= controller.layers.Length)
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range");
            
            var layers = controller.layers;
            
            if (args.ContainsKey("weight"))
            {
                float weight = 1f;
                if (args["weight"] is float f) weight = f;
                else float.TryParse(args["weight"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out weight);
                layers[layerIndex].defaultWeight = weight;
            }
            
            if (args.ContainsKey("blendingMode"))
            {
                string blendingMode = args["blendingMode"].ToString();
                layers[layerIndex].blendingMode = blendingMode.ToLower() switch
                {
                    "additive" => AnimatorLayerBlendingMode.Additive,
                    _ => AnimatorLayerBlendingMode.Override
                };
            }
            
            if (args.ContainsKey("avatarMaskPath"))
            {
                string maskPath = args["avatarMaskPath"].ToString();
                if (string.IsNullOrEmpty(maskPath))
                {
                    layers[layerIndex].avatarMask = null;
                }
                else
                {
                    if (!maskPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        maskPath = "Assets/" + maskPath;
                    UnityEngine.AvatarMask mask = AssetDatabase.LoadAssetAtPath<UnityEngine.AvatarMask>(maskPath);
                    if (mask != null)
                        layers[layerIndex].avatarMask = mask;
                }
            }
            
            if (args.ContainsKey("syncedLayerIndex"))
            {
                int syncIndex = -1;
                if (args["syncedLayerIndex"] is int i) syncIndex = i;
                else if (args["syncedLayerIndex"] is float f) syncIndex = (int)f;
                else int.TryParse(args["syncedLayerIndex"].ToString(), out syncIndex);
                layers[layerIndex].syncedLayerIndex = syncIndex;
            }
            
            controller.layers = layers;
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "layerIndex", layerIndex }
            };
            
            return ToolUtils.CreateSuccessResponse($"Updated layer properties for layer {layerIndex}", extras);
        }
    }
}
