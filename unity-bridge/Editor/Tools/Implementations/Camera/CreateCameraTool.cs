using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    public class CreateCameraTool : ITool
    {
        public string Name => "create_camera";

        public string Execute(Dictionary<string, object> args)
        {
            string name = args.ContainsKey("name") ? args["name"].ToString() : "Camera";
            UnityEngine.GameObject cameraObj = new UnityEngine.GameObject(name);
            UnityEngine.Camera camera = Undo.AddComponent<UnityEngine.Camera>(cameraObj);

            if (args.ContainsKey("position"))
                cameraObj.transform.position = ToolUtils.ParseVector3(args["position"].ToString());
            if (args.ContainsKey("rotation"))
                cameraObj.transform.rotation = Quaternion.Euler(ToolUtils.ParseVector3(args["rotation"].ToString()));

            ApplyCameraProperties(camera, args);

            if (args.ContainsKey("tagMain") && args["tagMain"].ToString().ToLower() == "true")
            {
                cameraObj.tag = "MainCamera";
            }

            Undo.RegisterCreatedObjectUndo(cameraObj, $"Create Camera: {name}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(cameraObj) }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Camera '{name}'", extras);
        }

        private static void ApplyCameraProperties(UnityEngine.Camera camera, Dictionary<string, object> args)
        {
            if (args.ContainsKey("fieldOfView") && float.TryParse(args["fieldOfView"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fov)) camera.fieldOfView = fov;
            if (args.ContainsKey("orthographic") && bool.TryParse(args["orthographic"].ToString(), out bool ortho)) camera.orthographic = ortho;
            if (args.ContainsKey("nearClip") && float.TryParse(args["nearClip"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float nearClip)) camera.nearClipPlane = nearClip;
            if (args.ContainsKey("farClip") && float.TryParse(args["farClip"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float farClip)) camera.farClipPlane = farClip;
            if (args.ContainsKey("clearFlags") && System.Enum.TryParse(args["clearFlags"].ToString(), true, out UnityEngine.CameraClearFlags flags)) camera.clearFlags = flags;
            if (args.ContainsKey("backgroundColor")) camera.backgroundColor = ToolUtils.ParseColor(args["backgroundColor"].ToString());
        }
    }
}
