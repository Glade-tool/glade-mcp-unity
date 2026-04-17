using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    public class SetCameraPropertiesTool : ITool
    {
        public string Name => "set_camera_properties";

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
                return ToolUtils.CreateErrorResponse($"No Camera found on '{gameObjectPath}'");
            }

            // Record camera for undo BEFORE modifying properties
            Undo.RecordObject(camera, $"Set Camera Properties: {gameObjectPath}");

            ApplyCameraProperties(camera, args);
            return ToolUtils.CreateSuccessResponse($"Updated Camera on '{gameObjectPath}'");
        }

        private static void ApplyCameraProperties(UnityEngine.Camera camera, Dictionary<string, object> args)
        {
            if (args.ContainsKey("fieldOfView") && float.TryParse(args["fieldOfView"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fov)) camera.fieldOfView = fov;
            if (args.ContainsKey("orthographic") && bool.TryParse(args["orthographic"].ToString(), out bool ortho)) camera.orthographic = ortho;
            if (args.ContainsKey("nearClip") && float.TryParse(args["nearClip"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float nearClip)) camera.nearClipPlane = nearClip;
            if (args.ContainsKey("farClip") && float.TryParse(args["farClip"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float farClip)) camera.farClipPlane = farClip;
            if (args.ContainsKey("clearFlags") && System.Enum.TryParse(args["clearFlags"].ToString(), true, out UnityEngine.CameraClearFlags flags)) camera.clearFlags = flags;
            if (args.ContainsKey("backgroundColor")) camera.backgroundColor = ToolUtils.ParseColor(args["backgroundColor"].ToString());
            
            // HDR and MSAA settings
            if (args.ContainsKey("allowHDR"))
            {
                bool allowHDR = false;
                if (args["allowHDR"] is bool b) allowHDR = b;
                else bool.TryParse(args["allowHDR"].ToString(), out allowHDR);
                camera.allowHDR = allowHDR;
            }
            
            if (args.ContainsKey("allowMSAA"))
            {
                bool allowMSAA = false;
                if (args["allowMSAA"] is bool b) allowMSAA = b;
                else bool.TryParse(args["allowMSAA"].ToString(), out allowMSAA);
                camera.allowMSAA = allowMSAA;
            }
            
            // Note: Exposure/tonemapping settings are typically handled via Volume system in URP/HDRP
            // These are not directly accessible on the Camera component
            // If exposure is needed in the future, we would need to access the Volume system
        }
    }
}
