using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Components
{
    public class RemoveComponentTool : ITool
    {
        public string Name => "remove_component";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            
            if (string.IsNullOrEmpty(componentType))
            {
                return ToolUtils.CreateErrorResponse("componentType is required");
            }
            
            UnityEngine.GameObject obj = string.IsNullOrEmpty(gameObjectPath) ? null : ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            // Try to find component type
            System.Type type = System.Type.GetType(componentType);
            if (type == null)
            {
                type = System.Type.GetType($"UnityEngine.{componentType}");
            }
            if (type == null)
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(componentType);
                    if (type != null) break;
                    type = assembly.GetType($"UnityEngine.{componentType}");
                    if (type != null) break;
                }
            }
            
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found");
            }
            
            Component comp = obj.GetComponent(type);
            if (comp == null)
            {
                return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on '{gameObjectPath}'");
            }
            
            Undo.DestroyObjectImmediate(comp);
            
            return ToolUtils.CreateSuccessResponse($"Removed component '{componentType}' from '{gameObjectPath}'");
        }
    }
}
