using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Components
{
    public class SetObjectReferenceTool : ITool
    {
        public string Name => "set_object_reference";

        public string Execute(Dictionary<string, object> args)
        {
            string targetGameObjectPath = args.ContainsKey("targetGameObject") ? args["targetGameObject"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            string fieldName = args.ContainsKey("fieldName") ? args["fieldName"].ToString() : "";
            string sourceGameObjectPath = args.ContainsKey("sourceGameObject") ? args["sourceGameObject"].ToString() : "";
            string sourceType = args.ContainsKey("sourceType") ? args["sourceType"].ToString() : "Transform";
            
            if (string.IsNullOrEmpty(targetGameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("targetGameObject is required");
            }
            
            if (string.IsNullOrEmpty(componentType))
            {
                return ToolUtils.CreateErrorResponse("componentType is required");
            }
            
            if (string.IsNullOrEmpty(fieldName))
            {
                return ToolUtils.CreateErrorResponse("fieldName is required");
            }
            
            if (string.IsNullOrEmpty(sourceGameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("sourceGameObject is required");
            }
            
            // Find target GameObject
            UnityEngine.GameObject targetObj = ToolUtils.FindGameObjectByPath(targetGameObjectPath);
            if (targetObj == null)
            {
                return ToolUtils.CreateErrorResponse($"Target GameObject '{targetGameObjectPath}' not found");
            }
            
            // Find source GameObject
            UnityEngine.GameObject sourceObj = ToolUtils.FindGameObjectByPath(sourceGameObjectPath);
            if (sourceObj == null)
            {
                return ToolUtils.CreateErrorResponse($"Source GameObject '{sourceGameObjectPath}' not found");
            }
            
            // Find component type on target
            System.Type compType = ToolUtils.FindComponentType(componentType);
            if (compType == null)
            {
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found");
            }
            
            Component targetComp = targetObj.GetComponent(compType);
            if (targetComp == null)
            {
                return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on '{targetGameObjectPath}'");
            }
            
            // Determine what to assign from source
            UnityEngine.Object sourceReference = null;
            string assignedType = sourceType;
            
            if (string.IsNullOrEmpty(sourceType) || sourceType.Equals("Transform", StringComparison.OrdinalIgnoreCase))
            {
                sourceReference = sourceObj.transform;
                assignedType = "Transform";
            }
            else if (sourceType.Equals("GameObject", StringComparison.OrdinalIgnoreCase))
            {
                sourceReference = sourceObj;
                assignedType = "GameObject";
            }
            else
            {
                // Try to find a component of the specified type on the source object
                System.Type sourceCompType = ToolUtils.FindComponentType(sourceType);
                if (sourceCompType != null)
                {
                    Component sourceComp = sourceObj.GetComponent(sourceCompType);
                    if (sourceComp != null)
                    {
                        sourceReference = sourceComp;
                        assignedType = sourceType;
                    }
                    else
                    {
                        return ToolUtils.CreateErrorResponse($"Component '{sourceType}' not found on source GameObject '{sourceGameObjectPath}'");
                    }
                }
                else
                {
                    // Default to Transform if source type not recognized
                    sourceReference = sourceObj.transform;
                    assignedType = "Transform";
                }
            }
            
            // Find the field or property on the target component
            var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            // Try field first (most common for serialized fields)
            var field = compType.GetField(fieldName, bindingFlags);
            if (field != null)
            {
                // Check if the types are compatible
                if (!field.FieldType.IsAssignableFrom(sourceReference.GetType()))
                {
                    return ToolUtils.CreateErrorResponse($"Field '{fieldName}' is of type '{field.FieldType.Name}' but source is '{sourceReference.GetType().Name}'. They are not compatible.");
                }
                
                try
                {
                    Undo.RecordObject(targetComp, $"Set Reference: {targetGameObjectPath}.{componentType}.{fieldName}");
                    field.SetValue(targetComp, sourceReference);
                    EditorUtility.SetDirty(targetComp);
                    
                    return ToolUtils.CreateSuccessResponse($"Set field '{fieldName}' on '{componentType}' of '{targetGameObjectPath}' to reference {assignedType} of '{sourceGameObjectPath}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set field: {e.Message}");
                }
            }
            
            // Try property
            var prop = compType.GetProperty(fieldName, bindingFlags);
            if (prop != null && prop.CanWrite)
            {
                // Check if the types are compatible
                if (!prop.PropertyType.IsAssignableFrom(sourceReference.GetType()))
                {
                    return ToolUtils.CreateErrorResponse($"Property '{fieldName}' is of type '{prop.PropertyType.Name}' but source is '{sourceReference.GetType().Name}'. They are not compatible.");
                }
                
                try
                {
                    Undo.RecordObject(targetComp, $"Set Reference: {targetGameObjectPath}.{componentType}.{fieldName}");
                    prop.SetValue(targetComp, sourceReference);
                    EditorUtility.SetDirty(targetComp);
                    
                    return ToolUtils.CreateSuccessResponse($"Set property '{fieldName}' on '{componentType}' of '{targetGameObjectPath}' to reference {assignedType} of '{sourceGameObjectPath}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set property: {e.Message}");
                }
            }
            
            var availableFields = string.Join(", ", compType.GetFields(bindingFlags).Select(f => f.Name));
            return ToolUtils.CreateErrorResponse($"Field or property '{fieldName}' not found on '{componentType}'. Available fields: {availableFields}");
        }
    }
}
