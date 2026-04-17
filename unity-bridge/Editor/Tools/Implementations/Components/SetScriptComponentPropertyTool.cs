using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Components
{
    public class SetScriptComponentPropertyTool : ITool
    {
        public string Name => "set_script_component_property";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string scriptName = args.ContainsKey("scriptName") ? args["scriptName"].ToString() : "";
            string propertyName = args.ContainsKey("propertyName") ? args["propertyName"].ToString() : "";
            string valueStr = args.ContainsKey("value") ? args["value"].ToString() : "";
            
            // Check for append mode (defaults to false for backward compatibility)
            bool appendToList = false;
            if (args.ContainsKey("appendToList"))
            {
                if (args["appendToList"] is bool b) appendToList = b;
                else bool.TryParse(args["appendToList"]?.ToString(), out appendToList);
            }
            
            if (string.IsNullOrEmpty(scriptName))
            {
                return ToolUtils.CreateErrorResponse("scriptName is required");
            }
            
            if (string.IsNullOrEmpty(propertyName))
            {
                return ToolUtils.CreateErrorResponse("propertyName is required");
            }
            
            if (string.IsNullOrEmpty(valueStr))
            {
                return ToolUtils.CreateErrorResponse("value is required");
            }
            
            UnityEngine.GameObject obj = string.IsNullOrEmpty(gameObjectPath) ? null : ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            // Find the script component by name (more flexible than requiring exact type name)
            Component scriptComponent = null;
            System.Type componentType = null;
            
            // Try to find component by script name
            Component[] allComponents = obj.GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                
                System.Type compType = comp.GetType();
                string typeName = compType.Name;
                
                // Check if the script name matches (case-insensitive, or partial match)
                if (string.Equals(typeName, scriptName, StringComparison.OrdinalIgnoreCase) ||
                    typeName.Contains(scriptName, StringComparison.OrdinalIgnoreCase) ||
                    scriptName.Contains(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    scriptComponent = comp;
                    componentType = compType;
                    break;
                }
            }
            
            // If not found by name, try using ToolUtils.FindComponentType for exact lookup
            if (scriptComponent == null)
            {
                componentType = ToolUtils.FindComponentType(scriptName);
                if (componentType != null)
                {
                    scriptComponent = obj.GetComponent(componentType);
                }
            }
            
            if (scriptComponent == null || componentType == null)
            {
                // List available script components for helpful error
                var scriptComponents = allComponents
                    .Where(c => c != null && c.GetType().Namespace != "UnityEngine")
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .ToList();
                
                string availableList = scriptComponents.Count > 0
                    ? $" Available script components on '{gameObjectPath}': {string.Join(", ", scriptComponents)}"
                    : $" No script components found on '{gameObjectPath}'. Use add_component to add the script first.";
                
                return ToolUtils.CreateErrorResponse($"Script component '{scriptName}' not found on '{gameObjectPath}'.{availableList}");
            }
            
            // Use reflection to set the property or field
            var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            
            // Try property first
            var prop = componentType.GetProperty(propertyName, bindingFlags);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    // Check if this is a list/array and we want to append
                    if (appendToList && IsListOrArrayType(prop.PropertyType))
                    {
                        return AppendToListProperty(scriptComponent, propertyName, valueStr, prop.PropertyType, gameObjectPath, componentType.Name);
                    }
                    
                    object convertedValue = ToolUtils.ConvertValueToPropertyType(valueStr, prop.PropertyType);
                    Undo.RecordObject(scriptComponent, $"Set Property: {gameObjectPath}.{componentType.Name}.{propertyName}");
                    prop.SetValue(scriptComponent, convertedValue);
                    EditorUtility.SetDirty(scriptComponent);
                    
                    return ToolUtils.CreateSuccessResponse($"Set property '{propertyName}' on script component '{componentType.Name}' of '{gameObjectPath}' to '{valueStr}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set property: {e.Message}");
                }
            }
            
            // Try field if property not found (common for serialized fields in scripts)
            var field = componentType.GetField(propertyName, bindingFlags);
            if (field != null && !field.IsLiteral && !field.IsInitOnly)
            {
                try
                {
                    // Check if this is a list/array and we want to append
                    if (appendToList && IsListOrArrayType(field.FieldType))
                    {
                        return AppendToListField(scriptComponent, propertyName, valueStr, field.FieldType, gameObjectPath, componentType.Name);
                    }
                    
                    object convertedValue = ToolUtils.ConvertValueToPropertyType(valueStr, field.FieldType);
                    Undo.RecordObject(scriptComponent, $"Set Field: {gameObjectPath}.{componentType.Name}.{propertyName}");
                    field.SetValue(scriptComponent, convertedValue);
                    EditorUtility.SetDirty(scriptComponent);
                    
                    return ToolUtils.CreateSuccessResponse($"Set field '{propertyName}' on script component '{componentType.Name}' of '{gameObjectPath}' to '{valueStr}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set field: {e.Message}");
                }
            }
            
            // List available properties/fields for helpful error message
            var availableProperties = componentType.GetProperties(bindingFlags)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToList();
            var availableFields = componentType.GetFields(bindingFlags)
                .Where(f => !f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.Name)
                .ToList();
            var availableNames = availableProperties.Concat(availableFields).Distinct().ToList();
            
            string availablePropertiesList = availableNames.Count > 0
                ? $" Available properties/fields on '{componentType.Name}': {string.Join(", ", availableNames)}"
                : " No writable properties or fields found.";
            
            return ToolUtils.CreateErrorResponse($"Property or field '{propertyName}' not found or not writable on script component '{componentType.Name}'.{availablePropertiesList}");
        }
        
        /// <summary>
        /// Checks if a type is a List&lt;T&gt; or array type.
        /// </summary>
        private static bool IsListOrArrayType(System.Type type)
        {
            if (type == null) return false;
            
            // Check for arrays
            if (type.IsArray) return true;
            
            // Check for List<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return true;
            
            // Check for IList<T> (some collections implement this)
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(System.Collections.Generic.IList<>) || 
                    genericDef == typeof(System.Collections.Generic.ICollection<>))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the element type of a List&lt;T&gt; or array.
        /// </summary>
        private static System.Type GetElementType(System.Type listType)
        {
            if (listType == null) return null;
            
            if (listType.IsArray)
                return listType.GetElementType();
            
            if (listType.IsGenericType)
            {
                var genericArgs = listType.GetGenericArguments();
                if (genericArgs.Length > 0)
                    return genericArgs[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Safely appends items to a list property using SerializedProperty (supports undo/redo).
        /// </summary>
        private string AppendToListProperty(UnityEngine.Object target, string propertyName, string valueStr, System.Type listType, string gameObjectPath, string componentName)
        {
            try
            {
                SerializedObject serializedObject = new SerializedObject(target);
                SerializedProperty listProperty = serializedObject.FindProperty(propertyName);
                
                if (listProperty == null || !listProperty.isArray)
                {
                    return ToolUtils.CreateErrorResponse($"Property '{propertyName}' is not a serialized list/array. Cannot append.");
                }
                
                System.Type elementType = GetElementType(listType);
                if (elementType == null)
                {
                    return ToolUtils.CreateErrorResponse($"Could not determine element type for list '{propertyName}'");
                }
                
                // Parse the value - could be a single item or JSON array
                List<object> itemsToAdd = ParseListValue(valueStr, elementType);
                
                if (itemsToAdd.Count == 0)
                {
                    return ToolUtils.CreateErrorResponse($"No valid items found in value to append to list '{propertyName}'");
                }
                
                Undo.RecordObject(target, $"Append to List: {gameObjectPath}.{componentName}.{propertyName}");
                
                // Append each item
                int startIndex = listProperty.arraySize;
                listProperty.arraySize = startIndex + itemsToAdd.Count;
                
                for (int i = 0; i < itemsToAdd.Count; i++)
                {
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(startIndex + i);
                    SetSerializedPropertyValue(element, itemsToAdd[i], elementType);
                }
                
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                
                return ToolUtils.CreateSuccessResponse($"Appended {itemsToAdd.Count} item(s) to list '{propertyName}' on script component '{componentName}' of '{gameObjectPath}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to append to list property: {e.Message}");
            }
        }
        
        /// <summary>
        /// Safely appends items to a list field using SerializedProperty (supports undo/redo).
        /// </summary>
        private string AppendToListField(UnityEngine.Object target, string fieldName, string valueStr, System.Type listType, string gameObjectPath, string componentName)
        {
            try
            {
                SerializedObject serializedObject = new SerializedObject(target);
                SerializedProperty listProperty = serializedObject.FindProperty(fieldName);
                
                if (listProperty == null || !listProperty.isArray)
                {
                    return ToolUtils.CreateErrorResponse($"Field '{fieldName}' is not a serialized list/array. Cannot append.");
                }
                
                System.Type elementType = GetElementType(listType);
                if (elementType == null)
                {
                    return ToolUtils.CreateErrorResponse($"Could not determine element type for list '{fieldName}'");
                }
                
                // Parse the value - could be a single item or JSON array
                List<object> itemsToAdd = ParseListValue(valueStr, elementType);
                
                if (itemsToAdd.Count == 0)
                {
                    return ToolUtils.CreateErrorResponse($"No valid items found in value to append to list '{fieldName}'");
                }
                
                Undo.RecordObject(target, $"Append to List: {gameObjectPath}.{componentName}.{fieldName}");
                
                // Append each item
                int startIndex = listProperty.arraySize;
                listProperty.arraySize = startIndex + itemsToAdd.Count;
                
                for (int i = 0; i < itemsToAdd.Count; i++)
                {
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(startIndex + i);
                    SetSerializedPropertyValue(element, itemsToAdd[i], elementType);
                }
                
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                
                return ToolUtils.CreateSuccessResponse($"Appended {itemsToAdd.Count} item(s) to list '{fieldName}' on script component '{componentName}' of '{gameObjectPath}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to append to list field: {e.Message}");
            }
        }
        
        /// <summary>
        /// Parses a value string that could be a single item or JSON array into a list of objects.
        /// </summary>
        private List<object> ParseListValue(string valueStr, System.Type elementType)
        {
            var result = new List<object>();
            
            // Try to parse as JSON array first
            valueStr = valueStr.Trim();
            if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
            {
                // It's a JSON array - parse it
                string inner = valueStr.Substring(1, valueStr.Length - 2).Trim();
                if (!string.IsNullOrEmpty(inner))
                {
                    string[] items = inner.Split(',');
                    foreach (var item in items)
                    {
                        string trimmed = item.Trim().Trim('"', '\'');
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            try
                            {
                                object converted = ToolUtils.ConvertValueToPropertyType(trimmed, elementType);
                                result.Add(converted);
                            }
                            catch
                            {
                                // Skip invalid items
                            }
                        }
                    }
                }
            }
            else
            {
                // Single item - convert and add
                try
                {
                    object converted = ToolUtils.ConvertValueToPropertyType(valueStr, elementType);
                    result.Add(converted);
                }
                catch
                {
                    // If conversion fails, try treating as comma-separated
                    string[] items = valueStr.Split(',');
                    foreach (var item in items)
                    {
                        string trimmed = item.Trim().Trim('"', '\'');
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            try
                            {
                                object converted = ToolUtils.ConvertValueToPropertyType(trimmed, elementType);
                                result.Add(converted);
                            }
                            catch
                            {
                                // Skip invalid items
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Sets a value on a SerializedProperty based on the element type.
        /// </summary>
        private void SetSerializedPropertyValue(SerializedProperty prop, object value, System.Type elementType)
        {
            if (value == null)
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    prop.objectReferenceValue = null;
                }
                return;
            }
            
            // Handle different property types
            if (elementType == typeof(int) || elementType == typeof(short) || elementType == typeof(byte))
            {
                if (value is int intVal)
                    prop.intValue = intVal;
                else if (int.TryParse(value.ToString(), out int parsed))
                    prop.intValue = parsed;
            }
            else if (elementType == typeof(float) || elementType == typeof(double))
            {
                if (value is float floatVal)
                    prop.floatValue = floatVal;
                else if (value is double doubleVal)
                    prop.floatValue = (float)doubleVal;
                else if (float.TryParse(value.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                    prop.floatValue = parsed;
            }
            else if (elementType == typeof(bool))
            {
                if (value is bool boolVal)
                    prop.boolValue = boolVal;
                else if (bool.TryParse(value.ToString(), out bool parsed))
                    prop.boolValue = parsed;
            }
            else if (elementType == typeof(string))
            {
                prop.stringValue = value.ToString();
            }
            else if (elementType == typeof(Vector2))
            {
                if (value is Vector2 vec2)
                    prop.vector2Value = vec2;
            }
            else if (elementType == typeof(Vector3))
            {
                if (value is Vector3 vec3)
                    prop.vector3Value = vec3;
            }
            else if (elementType == typeof(Vector4))
            {
                if (value is Vector4 vec4)
                    prop.vector4Value = vec4;
            }
            else if (elementType == typeof(Color))
            {
                if (value is Color color)
                    prop.colorValue = color;
            }
            else if (elementType.IsEnum)
            {
                // SerializedProperty.enumValueIndex is the index in the enum array, not the enum's underlying value
                // We need to find the index by matching the enum name
                string valueStr = value.ToString().Trim();
                
                // First, try to use SerializedProperty's enumNames array (most reliable)
                if (prop.enumNames != null && prop.enumNames.Length > 0)
                {
                    for (int i = 0; i < prop.enumNames.Length; i++)
                    {
                        if (string.Equals(prop.enumNames[i], valueStr, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            return;
                        }
                    }
                }
                
                // Fallback: try parsing as enum and finding its index
                try
                {
                    object enumValue = Enum.Parse(elementType, valueStr, true);
                    string enumName = Enum.GetName(elementType, enumValue);
                    if (!string.IsNullOrEmpty(enumName))
                    {
                        // Find the index of this enum name
                        string[] enumNames = Enum.GetNames(elementType);
                        for (int i = 0; i < enumNames.Length; i++)
                        {
                            if (string.Equals(enumNames[i], enumName, StringComparison.OrdinalIgnoreCase))
                            {
                                prop.enumValueIndex = i;
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    // If parsing fails, try direct name matching as last resort
                    string[] enumNames = Enum.GetNames(elementType);
                    for (int i = 0; i < enumNames.Length; i++)
                    {
                        if (string.Equals(enumNames[i], valueStr, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = i;
                            return;
                        }
                    }
                }
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                if (value is UnityEngine.Object obj)
                    prop.objectReferenceValue = obj;
            }
        }
    }
}
