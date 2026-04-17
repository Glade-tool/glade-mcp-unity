using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    /// <summary>
    /// Gets detailed information about a Camera component including field of view, clipping planes, HDR settings, and other properties.
    /// </summary>
    public class GetCameraPropertiesTool : ITool
    {
        public string Name => "get_camera_properties";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            UnityEngine.Camera camera = obj.GetComponent<UnityEngine.Camera>();
            if (camera == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a Camera component");
            }
            
            // READ ONLY - No Undo needed
            var properties = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath,
                ["fieldOfView"] = camera.fieldOfView,
                ["orthographic"] = camera.orthographic,
                ["orthographicSize"] = camera.orthographicSize,
                ["nearClipPlane"] = camera.nearClipPlane,
                ["farClipPlane"] = camera.farClipPlane,
                ["clearFlags"] = camera.clearFlags.ToString(),
                ["backgroundColor"] = $"{camera.backgroundColor.r},{camera.backgroundColor.g},{camera.backgroundColor.b},{camera.backgroundColor.a}",
                ["allowHDR"] = camera.allowHDR,
                ["allowMSAA"] = camera.allowMSAA,
                ["depth"] = camera.depth,
                ["renderingPath"] = camera.renderingPath.ToString(),
                ["targetTexture"] = camera.targetTexture != null ? "Set" : "None",
                ["cullingMask"] = camera.cullingMask,
                ["enabled"] = camera.enabled
            };
            
            // Note: Exposure and tonemapping settings are typically handled via Volume system in URP/HDRP
            // These are not directly accessible on the Camera component
            // If needed in the future, we would need to access the Volume system
            
            string message = $"Retrieved camera properties for '{gameObjectPath}': FOV={camera.fieldOfView}, HDR={camera.allowHDR}, MSAA={camera.allowMSAA}";
            
            return ToolUtils.CreateSuccessResponse(message, properties);
        }
    }
}
