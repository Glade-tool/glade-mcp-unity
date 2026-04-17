using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class SetGameObjectPropertyTool : ITool
    {
        public string Name => "set_game_object_property";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string propertyName = args.ContainsKey("propertyName") ? args["propertyName"].ToString() : "";
            string valueStr = args.ContainsKey("value") ? args["value"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return ToolUtils.CreateErrorResponse("propertyName is required");
            }
            
            if (string.IsNullOrEmpty(valueStr))
            {
                return ToolUtils.CreateErrorResponse("value is required");
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            // Use reflection to set the property
            var prop = typeof(UnityEngine.GameObject).GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop == null || !prop.CanWrite)
            {
                return ToolUtils.CreateErrorResponse($"Property '{propertyName}' not found or not writable on GameObject");
            }
            
            try
            {
                object convertedValue = ToolUtils.ConvertValueToPropertyType(valueStr, prop.PropertyType);
                Undo.RecordObject(obj, $"Set Property: {gameObjectPath}.{propertyName}");
                prop.SetValue(obj, convertedValue);
                
                return ToolUtils.CreateSuccessResponse($"Set property '{propertyName}' on '{gameObjectPath}' to '{valueStr}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to set property: {e.Message}");
            }
        }
    }
}
