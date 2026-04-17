using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    public class AssignRenderTextureTool : ITool
    {
        public string Name => "assign_render_texture";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "Camera";

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

            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(assetPath);
            if (rt == null)
            {
                return ToolUtils.CreateErrorResponse($"RenderTexture not found at '{assetPath}'");
            }

            if (componentType.Equals("Camera", StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Camera camera = obj.GetComponent<UnityEngine.Camera>();
                if (camera == null) return ToolUtils.CreateErrorResponse($"No Camera found on '{gameObjectPath}'");
                Undo.RecordObject(camera, "Assign RenderTexture");
                camera.targetTexture = rt;
            }
            else if (componentType.Equals("RawImage", StringComparison.OrdinalIgnoreCase))
            {
                RawImage rawImage = obj.GetComponent<RawImage>();
                if (rawImage == null) return ToolUtils.CreateErrorResponse($"No RawImage found on '{gameObjectPath}'");
                Undo.RecordObject(rawImage, "Assign RenderTexture");
                rawImage.texture = rt;
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Unsupported componentType '{componentType}'");
            }

            return ToolUtils.CreateSuccessResponse($"Assigned RenderTexture to '{gameObjectPath}'");
        }
    }
}
#endif
