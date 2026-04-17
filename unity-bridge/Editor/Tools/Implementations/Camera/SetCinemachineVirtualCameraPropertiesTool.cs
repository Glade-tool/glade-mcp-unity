#if GLADE_CINEMACHINE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    /// <summary>
    /// Sets properties on a CinemachineVirtualCamera component including priority, follow target, look at target, and other properties.
    /// </summary>
    public class SetCinemachineVirtualCameraPropertiesTool : ITool
    {
        public string Name => "set_cinemachine_virtual_camera_properties";

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
            
            // Record for undo BEFORE modifying
            Undo.RecordObject(vcam, $"Set Cinemachine Virtual Camera Properties: {gameObjectPath}");
            
            // Set priority
            if (args.ContainsKey("priority"))
            {
                var priorityProperty = cinemachineType.GetProperty("Priority");
                if (priorityProperty != null)
                {
                    int priority = 10;
                    if (args["priority"] is int i) priority = i;
                    else if (args["priority"] is float f) priority = (int)f;
                    else int.TryParse(args["priority"].ToString(), out priority);
                    
                    priorityProperty.SetValue(vcam, priority);
                }
            }
            
            // Set Follow target
            if (args.ContainsKey("followTarget"))
            {
                var followProperty = cinemachineType.GetProperty("Follow");
                if (followProperty != null)
                {
                    string followPath = args["followTarget"].ToString();
                    if (string.IsNullOrEmpty(followPath))
                    {
                        followProperty.SetValue(vcam, null);
                    }
                    else
                    {
                        UnityEngine.GameObject followObj = ToolUtils.FindGameObjectByPath(followPath);
                        if (followObj != null)
                        {
                            followProperty.SetValue(vcam, followObj.transform);
                        }
                    }
                }
            }
            
            // Set Look At target
            if (args.ContainsKey("lookAtTarget"))
            {
                var lookAtProperty = cinemachineType.GetProperty("LookAt");
                if (lookAtProperty != null)
                {
                    string lookAtPath = args["lookAtTarget"].ToString();
                    if (string.IsNullOrEmpty(lookAtPath))
                    {
                        lookAtProperty.SetValue(vcam, null);
                    }
                    else
                    {
                        UnityEngine.GameObject lookAtObj = ToolUtils.FindGameObjectByPath(lookAtPath);
                        if (lookAtObj != null)
                        {
                            lookAtProperty.SetValue(vcam, lookAtObj.transform);
                        }
                    }
                }
            }
            
            // Set enabled state
            if (args.ContainsKey("enabled"))
            {
                var enabledProperty = cinemachineType.GetProperty("enabled");
                if (enabledProperty != null)
                {
                    bool enabled = true;
                    if (args["enabled"] is bool b) enabled = b;
                    else bool.TryParse(args["enabled"].ToString(), out enabled);
                    
                    enabledProperty.SetValue(vcam, enabled);
                }
            }
            
            return ToolUtils.CreateSuccessResponse($"Updated Cinemachine Virtual Camera properties on '{gameObjectPath}'");
        }
    }
}
#endif
