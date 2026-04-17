using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class GetAnimatorLayerInfoTool : ITool
    {
        public string Name => "get_animator_layer_info";

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
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range. Controller has {controller.layers.Length} layer(s).");
            
            var layer = controller.layers[layerIndex];
            
            // Get avatar mask path
            string avatarMaskPath = null;
            if (layer.avatarMask != null)
            {
                avatarMaskPath = AssetDatabase.GetAssetPath(layer.avatarMask);
            }
            
            // Convert blending mode to string
            string blendingModeStr = layer.blendingMode.ToString();
            
            var extras = new Dictionary<string, object>
            {
                { "layerIndex", layerIndex },
                { "name", layer.name },
                { "defaultWeight", layer.defaultWeight },
                { "blendingMode", blendingModeStr },
                { "avatarMaskPath", avatarMaskPath },
                { "syncedLayerIndex", layer.syncedLayerIndex },
                { "iKPass", layer.iKPass },
                { "syncedLayerAffectsTiming", layer.syncedLayerAffectsTiming }
            };
            
            return ToolUtils.CreateSuccessResponse($"Retrieved layer info for layer {layerIndex} ('{layer.name}')", extras);
        }
    }
}
