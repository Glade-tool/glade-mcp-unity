#if GLADE_CINEMACHINE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    /// <summary>
    /// Gets detailed information about a CinemachineVirtualCamera component including priority, follow target, look at target, and other properties.
    /// </summary>
    public class GetCinemachineVirtualCameraPropertiesTool : ITool
    {
        public string Name => "get_cinemachine_virtual_camera_properties";

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
            
            var cinemachineType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
            if (cinemachineType == null)
            {
                return ToolUtils.CreateErrorResponse("Cinemachine package is not installed or not available");
            }
            
            var vcam = obj.GetComponent(cinemachineType);
            if (vcam == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a CinemachineVirtualCamera component");
            }
            
            // READ ONLY - No Undo needed
            var properties = new Dictionary<string, object>
            {
                ["gameObjectPath"] = gameObjectPath
            };
            
            // Get priority
            var priorityProperty = cinemachineType.GetProperty("Priority");
            if (priorityProperty != null)
            {
                properties["priority"] = priorityProperty.GetValue(vcam);
            }
            
            // Get Follow target
            var followProperty = cinemachineType.GetProperty("Follow");
            if (followProperty != null)
            {
                var followTarget = followProperty.GetValue(vcam) as UnityEngine.Transform;
                properties["followTarget"] = followTarget != null ? ToolUtils.GetGameObjectPath(followTarget.gameObject) : null;
            }
            
            // Get Look At target
            var lookAtProperty = cinemachineType.GetProperty("LookAt");
            if (lookAtProperty != null)
            {
                var lookAtTarget = lookAtProperty.GetValue(vcam) as UnityEngine.Transform;
                properties["lookAtTarget"] = lookAtTarget != null ? ToolUtils.GetGameObjectPath(lookAtTarget.gameObject) : null;
            }
            
            // Get enabled state
            var enabledProperty = cinemachineType.GetProperty("enabled");
            if (enabledProperty != null)
            {
                properties["enabled"] = enabledProperty.GetValue(vcam);
            }
            
            string message = $"Retrieved Cinemachine Virtual Camera properties for '{gameObjectPath}'";
            
            return ToolUtils.CreateSuccessResponse(message, properties);
        }
    }
}
#endif
