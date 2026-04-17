#if GLADE_CINEMACHINE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    /// <summary>
    /// Creates a new GameObject with a CinemachineVirtualCamera component.
    /// </summary>
    public class CreateCinemachineVirtualCameraTool : ITool
    {
        public string Name => "create_cinemachine_virtual_camera";

        public string Execute(Dictionary<string, object> args)
        {
            string name = args.ContainsKey("name") ? args["name"]?.ToString() : "CM vcam";
            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"]?.ToString() : null;
            
            // Create GameObject
            UnityEngine.GameObject obj = new UnityEngine.GameObject(name);
            
            // Set parent if provided
            if (!string.IsNullOrEmpty(parentPath))
            {
                UnityEngine.GameObject parent = ToolUtils.FindGameObjectByPath(parentPath);
                if (parent != null)
                    obj.transform.SetParent(parent.transform);
            }
            
            // Add CinemachineVirtualCamera component using reflection
            var cinemachineType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
            if (cinemachineType == null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return ToolUtils.CreateErrorResponse("Cinemachine package is not installed or not available");
            }
            
            var vcam = Undo.AddComponent(obj, cinemachineType);
            
            // Set position if provided
            if (args.ContainsKey("position"))
            {
                UnityEngine.Vector3 pos = ToolUtils.ParseVector3(args["position"].ToString());
                obj.transform.position = pos;
            }
            
            // Set priority if provided
            if (args.ContainsKey("priority"))
            {
                var priorityProperty = cinemachineType.GetProperty("Priority");
                if (priorityProperty != null)
                {
                    int priority = 10;
                    if (args["priority"] is int i) priority = i;
                    else if (args["priority"] is float f) priority = (int)f;
                    else int.TryParse(args["priority"].ToString(), out priority);
                    
                    Undo.RecordObject(vcam, $"Set Cinemachine Priority: {name}");
                    priorityProperty.SetValue(vcam, priority);
                }
            }
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(obj, $"Create Cinemachine Virtual Camera: {name}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(obj) }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Cinemachine Virtual Camera named '{name}'", extras);
        }
    }
}
#endif
