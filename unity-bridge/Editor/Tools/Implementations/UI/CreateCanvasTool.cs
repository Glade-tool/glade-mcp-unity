using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    public class CreateCanvasTool : ITool
    {
        public string Name => "create_canvas";

        public string Execute(Dictionary<string, object> args)
        {
            // CRITICAL: All UI actions require TextMeshPro. Check before doing anything.
            var tmpCheck = UIHelpers.EnsureTMPForUIActions();
            if (!tmpCheck.IsAvailable)
            {
                // TMP not available - user must install it explicitly first
                return ToolUtils.CreateErrorResponse(tmpCheck.Message);
            }

            string name = args.ContainsKey("name") ? args["name"].ToString() : "Canvas";
            string renderModeStr = args.ContainsKey("renderMode") ? args["renderMode"].ToString() : "ScreenSpaceOverlay";
            string cameraPath = args.ContainsKey("cameraPath") ? args["cameraPath"].ToString() : "";

            UnityEngine.GameObject canvasObj = new UnityEngine.GameObject(name);
            Canvas canvas = Undo.AddComponent<Canvas>(canvasObj);
            Undo.AddComponent<CanvasScaler>(canvasObj);
            Undo.AddComponent<GraphicRaycaster>(canvasObj);

            if (System.Enum.TryParse(renderModeStr, true, out RenderMode mode))
            {
                canvas.renderMode = mode;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && !string.IsNullOrEmpty(cameraPath))
            {
                UnityEngine.GameObject camObj = ToolUtils.FindGameObjectByPath(cameraPath);
                if (camObj != null)
                {
                    UnityEngine.Camera cam = camObj.GetComponent<UnityEngine.Camera>();
                    if (cam != null) canvas.worldCamera = cam;
                }
            }

            Undo.RegisterCreatedObjectUndo(canvasObj, $"Create Canvas: {name}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(canvasObj) }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Canvas '{name}'", extras);
        }
    }
}
#endif
