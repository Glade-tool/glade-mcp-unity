using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateBlendTree2DTool : ITool
    {
        public string Name => "create_blend_tree_2d";

        public string Execute(Dictionary<string, object> args)
        {
            string controllerPath = args.ContainsKey("controllerPath") ? args["controllerPath"].ToString() : "";
            string stateName = args.ContainsKey("stateName") ? args["stateName"].ToString() : "";
            string parameterX = args.ContainsKey("parameterX") ? args["parameterX"].ToString() : "";
            string parameterY = args.ContainsKey("parameterY") ? args["parameterY"].ToString() : "";
            string blendType = args.ContainsKey("blendType") ? args["blendType"].ToString() : "SimpleDirectional2D";
            
            if (string.IsNullOrEmpty(controllerPath))
                return ToolUtils.CreateErrorResponse("controllerPath is required");
            if (string.IsNullOrEmpty(stateName))
                return ToolUtils.CreateErrorResponse("stateName is required");
            if (string.IsNullOrEmpty(parameterX))
                return ToolUtils.CreateErrorResponse("parameterX is required");
            if (string.IsNullOrEmpty(parameterY))
                return ToolUtils.CreateErrorResponse("parameterY is required");
            if (!args.ContainsKey("motions"))
                return ToolUtils.CreateErrorResponse("motions is required");
            
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
            
            if (layerIndex >= controller.layers.Length)
                return ToolUtils.CreateErrorResponse($"Layer index {layerIndex} out of range");
            
            // Ensure parameters exist
            bool xExists = false, yExists = false;
            foreach (var p in controller.parameters)
            {
                if (p.name == parameterX && p.type == AnimatorControllerParameterType.Float) xExists = true;
                if (p.name == parameterY && p.type == AnimatorControllerParameterType.Float) yExists = true;
            }
            if (!xExists) controller.AddParameter(parameterX, AnimatorControllerParameterType.Float);
            if (!yExists) controller.AddParameter(parameterY, AnimatorControllerParameterType.Float);
            
            var stateMachine = controller.layers[layerIndex].stateMachine;
            
            BlendTree blendTree;
            var state = controller.CreateBlendTreeInController(stateName, out blendTree, layerIndex);
            
            // Set blend type
            blendTree.blendType = blendType.ToLower() switch
            {
                "simpledirectional2d" => BlendTreeType.SimpleDirectional2D,
                "freeformdirectional2d" => BlendTreeType.FreeformDirectional2D,
                "freeformcartesian2d" => BlendTreeType.FreeformCartesian2D,
                _ => BlendTreeType.SimpleDirectional2D
            };
            blendTree.blendParameter = parameterX;
            blendTree.blendParameterY = parameterY;
            
            // Add motions
            var motionsObj = args["motions"];
            int motionCount = 0;

            // Re-hydrate JSON-array strings so motions works whether it arrives
            // already-typed or string-encoded (e.g. via batch_execute).
            if (motionsObj is string motionsJson && ToolUtils.TryParseJsonArrayToList(motionsJson, out var parsedMotions))
                motionsObj = parsedMotions;

            var skippedMotions = new List<Dictionary<string, object>>();
            int requestedMotions = 0;
            if (motionsObj is List<object> motionList)
            {
                foreach (var motionObj in motionList)
                {
                    if (!(motionObj is Dictionary<string, object> motion))
                    {
                        skippedMotions.Add(new Dictionary<string, object> { { "reason", "motion entry is not an object" } });
                        continue;
                    }
                    requestedMotions++;

                    string clipPath = motion.ContainsKey("clipPath") ? motion["clipPath"].ToString() : "";
                    float posX = 0f, posY = 0f;
                    if (motion.ContainsKey("positionX"))
                    {
                        if (motion["positionX"] is float f) posX = f;
                        else float.TryParse(motion["positionX"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out posX);
                    }
                    if (motion.ContainsKey("positionY"))
                    {
                        if (motion["positionY"] is float f) posY = f;
                        else float.TryParse(motion["positionY"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out posY);
                    }

                    if (string.IsNullOrEmpty(clipPath))
                    {
                        skippedMotions.Add(new Dictionary<string, object> { { "clipPath", clipPath }, { "reason", "clipPath is required" } });
                        continue;
                    }

                    if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        clipPath = "Assets/" + clipPath;

                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip == null)
                    {
                        skippedMotions.Add(new Dictionary<string, object> { { "clipPath", clipPath }, { "reason", "AnimationClip not found at clipPath" } });
                        continue;
                    }

                    blendTree.AddChild(clip, new Vector2(posX, posY));
                    motionCount++;
                }
            }

            bool isDefault = true;
            if (args.ContainsKey("isDefault"))
            {
                if (args["isDefault"] is bool b) isDefault = b;
                else bool.TryParse(args["isDefault"].ToString(), out isDefault);
            }
            if (isDefault) stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                { "stateName", stateName },
                { "motionCount", motionCount },
                { "blendType", blendType },
                { "skippedCount", skippedMotions.Count }
            };
            if (skippedMotions.Count > 0)
                extras["skippedMotions"] = skippedMotions;

            if (requestedMotions > 0 && motionCount == 0)
                return ToolUtils.CreateErrorResponse(
                    $"Created 2D BlendTree '{stateName}' but 0 of {requestedMotions} motion(s) applied. See skippedMotions for per-entry reasons.",
                    extras);

            return ToolUtils.CreateSuccessResponse($"Created 2D BlendTree '{stateName}' with {motionCount} motion(s)", extras);
        }
    }
}
