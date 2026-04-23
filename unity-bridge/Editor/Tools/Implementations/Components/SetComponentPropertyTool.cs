using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Components
{
    public class SetComponentPropertyTool : ITool
    {
        public string Name => "set_component_property";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            string propertyName = args.ContainsKey("propertyName") ? args["propertyName"].ToString() : "";
            string valueStr = args.ContainsKey("value") ? args["value"].ToString() : "";
            
            // Check for append mode (defaults to false for backward compatibility)
            bool appendToList = false;
            if (args.ContainsKey("appendToList"))
            {
                if (args["appendToList"] is bool b) appendToList = b;
                else bool.TryParse(args["appendToList"]?.ToString(), out appendToList);
            }
            
            if (string.IsNullOrEmpty(componentType))
            {
                return ToolUtils.CreateErrorResponse("componentType is required");
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
            
            // GameObject is not a Component in Unity; tag and layer are properties on the GameObject itself.
            // Handle them here so set_component_property(gameObjectPath, "GameObject", "tag", "Player") works.
            if (string.Equals(componentType, "GameObject", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(propertyName, "tag", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Undo.RecordObject(obj, $"Set Tag: {gameObjectPath}");
                        obj.tag = valueStr;
                        return ToolUtils.CreateSuccessResponse($"Set property 'tag' on GameObject '{gameObjectPath}' to '{valueStr}'");
                    }
                    catch (UnityException)
                    {
                        return ToolUtils.CreateErrorResponse($"Tag '{valueStr}' does not exist. Add it in Edit > Project Settings > Tags and Layers, or use set_tag.");
                    }
                }
                if (string.Equals(propertyName, "layer", StringComparison.OrdinalIgnoreCase))
                {
                    int layer;
                    if (int.TryParse(valueStr, out layer) && layer >= 0 && layer <= 31)
                    {
                        Undo.RecordObject(obj, $"Set Layer: {gameObjectPath}");
                        obj.layer = layer;
                        return ToolUtils.CreateSuccessResponse($"Set property 'layer' on GameObject '{gameObjectPath}' to '{valueStr}'");
                    }
                    // Try by layer name
                    int layerByName = LayerMask.NameToLayer(valueStr);
                    if (layerByName >= 0)
                    {
                        Undo.RecordObject(obj, $"Set Layer: {gameObjectPath}");
                        obj.layer = layerByName;
                        return ToolUtils.CreateSuccessResponse($"Set property 'layer' on GameObject '{gameObjectPath}' to '{valueStr}'");
                    }
                    return ToolUtils.CreateErrorResponse($"Layer '{valueStr}' is not valid. Use a layer index (0-31) or a layer name from Edit > Project Settings > Tags and Layers.");
                }
                return ToolUtils.CreateErrorResponse($"Component type 'GameObject' only supports properties 'tag' and 'layer'. For tag use set_component_property with propertyName 'tag' or use set_tag.");
            }
            
            // Try to find component type using the robust lookup method
            // NOTE: Unity scripts are compiled into assemblies; until compilation + domain reload completes,
            // the type will not exist and cannot be found.
            System.Type type = ToolUtils.FindComponentType(componentType);
            
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                // Check if Unity is currently compiling
                if (EditorApplication.isCompiling)
                {
                    var extras = new Dictionary<string, object>
                    {
                        { "requiresCompilation", true },
                        { "isCompiling", true }
                    };
                    return ToolUtils.CreateErrorResponse(
                        $"Component type '{componentType}' not found. Unity is currently compiling. Please wait for compilation to finish, then try again.",
                        extras);
                }
                
                // Check if there's a script file on disk that might contain this class
                // This helps the AI understand that the script needs to compile first
                string[] possiblePaths = new string[]
                {
                    $"Assets/Scripts/{componentType}.cs",
                    $"Assets/{componentType}.cs",
                    $"Assets/Scripts/Player/{componentType}.cs",
                    $"Assets/Scripts/Game/{componentType}.cs"
                };
                
                string foundScriptPath = null;
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        foundScriptPath = path;
                        break;
                    }
                }
                
                // Also search all .cs files for the class name
                if (foundScriptPath == null)
                {
                    string[] allScripts = AssetDatabase.FindAssets("t:MonoScript");
                    foreach (var guid in allScripts)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (path.EndsWith($"{componentType}.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            foundScriptPath = path;
                            break;
                        }
                    }
                }
                
                if (foundScriptPath != null)
                {
                    // Script file exists but type not compiled yet
                    var extras = new Dictionary<string, object>
                    {
                        { "scriptExists", true },
                        { "scriptPath", foundScriptPath },
                        { "requiresCompilation", true }
                    };
                    return ToolUtils.CreateErrorResponse(
                        $"Component type '{componentType}' not found. Script file exists at '{foundScriptPath}' but is not yet compiled. Wait for Unity to finish compiling scripts, then try again.",
                        extras);
                }
                
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found. No matching script file found in the project.");
            }
            
            Component comp = obj.GetComponent(type);
            if (comp == null)
            {
                return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on '{gameObjectPath}'");
            }
            
            // Resolve property name. The model may pass any of:
            //   - C# property/field name ("mass", "receiveShadows")       → reflection
            //   - Case-variant ("Mass", "ReceiveShadows")                  → reflection w/ IgnoreCase
            //   - Serialized internal name ("m_Mass", "m_ReceiveShadows")  → SerializedObject.FindProperty
            // The last path matters for built-in Unity C++ components (MeshRenderer, Rigidbody)
            // whose m_* fields aren't exposed to reflection — this is what
            // get_component_inspector_properties returns as `internalName`.
            var bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var bindingFlagsIgnoreCase = bindingFlags | System.Reflection.BindingFlags.IgnoreCase;

            var prop = type.GetProperty(propertyName, bindingFlags)
                    ?? type.GetProperty(propertyName, bindingFlagsIgnoreCase);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    // Check if this is a list/array and we want to append
                    if (appendToList && IsListOrArrayType(prop.PropertyType))
                    {
                        return AppendToListProperty(comp, prop.Name, valueStr, prop.PropertyType, gameObjectPath, componentType);
                    }

                    object convertedValue = ToolUtils.ConvertValueToPropertyType(valueStr, prop.PropertyType);
                    Undo.RecordObject(comp, $"Set Property: {gameObjectPath}.{componentType}.{prop.Name}");
                    prop.SetValue(comp, convertedValue);
                    EditorUtility.SetDirty(comp);

                    return ToolUtils.CreateSuccessResponse($"Set property '{prop.Name}' on '{componentType}' of '{gameObjectPath}' to '{valueStr}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set property: {e.Message}");
                }
            }

            // Try field if property not found (some components may have serialized fields)
            var field = type.GetField(propertyName, bindingFlags)
                    ?? type.GetField(propertyName, bindingFlagsIgnoreCase);
            if (field != null && !field.IsLiteral && !field.IsInitOnly)
            {
                try
                {
                    // Check if this is a list/array and we want to append
                    if (appendToList && IsListOrArrayType(field.FieldType))
                    {
                        return AppendToListField(comp, field.Name, valueStr, field.FieldType, gameObjectPath, componentType);
                    }

                    object convertedValue = ToolUtils.ConvertValueToPropertyType(valueStr, field.FieldType);
                    Undo.RecordObject(comp, $"Set Field: {gameObjectPath}.{componentType}.{field.Name}");
                    field.SetValue(comp, convertedValue);
                    EditorUtility.SetDirty(comp);

                    return ToolUtils.CreateSuccessResponse($"Set field '{field.Name}' on '{componentType}' of '{gameObjectPath}' to '{valueStr}'");
                }
                catch (Exception e)
                {
                    return ToolUtils.CreateErrorResponse($"Failed to set field: {e.Message}");
                }
            }

            // SerializedObject fallback — the path that rescues m_* internal names on
            // built-in C++ components. Also covers MonoBehaviours whose [SerializeField]
            // private fields aren't findable via reflection on the derived type.
            var serializedResult = TrySetViaSerializedObject(
                comp, propertyName, valueStr, appendToList, gameObjectPath, componentType);
            if (serializedResult != null) return serializedResult;

            // List available properties/fields for helpful error message
            var availableProperties = type.GetProperties(bindingFlags)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToList();
            var availableFields = type.GetFields(bindingFlags)
                .Where(f => !f.IsLiteral && !f.IsInitOnly)
                .Select(f => f.Name)
                .ToList();
            var availableNames = availableProperties.Concat(availableFields).Distinct().ToList();

            // Also surface serialized-property names so the model can retry with one that exists.
            var serializedNames = new List<string>();
            try
            {
                var so = new SerializedObject(comp);
                var iter = so.GetIterator();
                bool enterChildren = true;
                while (iter.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iter.name != "m_Script") serializedNames.Add(iter.name);
                }
            }
            catch { /* surface whatever we have */ }

            var availableList = availableNames.Count > 0
                ? $" Reflection-visible on '{componentType}': {string.Join(", ", availableNames)}."
                : "";
            var serializedList = serializedNames.Count > 0
                ? $" Serialized on '{componentType}': {string.Join(", ", serializedNames)}."
                : "";

            return ToolUtils.CreateErrorResponse(
                $"Property or field '{propertyName}' not found or not writable on '{componentType}'.{availableList}{serializedList}");
        }

        /// <summary>
        /// Fallback setter that routes through SerializedObject/SerializedProperty.
        /// Resolves m_* internal names on built-in C++ components (MeshRenderer,
        /// Rigidbody, etc.) and serialized private fields on MonoBehaviours that
        /// reflection on the derived type can't see. Returns a response string on
        /// hit, or null to let the caller surface the "not found" error.
        /// </summary>
        private string TrySetViaSerializedObject(
            Component comp,
            string propertyName,
            string valueStr,
            bool appendToList,
            string gameObjectPath,
            string componentType)
        {
            try
            {
                var so = new SerializedObject(comp);
                // Exact match first; then case-insensitive walk of the top-level iterator.
                var spExact = so.FindProperty(propertyName);
                SerializedProperty sp = spExact;
                if (sp == null)
                {
                    var iter = so.GetIterator();
                    bool enterChildren = true;
                    while (iter.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (string.Equals(iter.name, propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            sp = so.FindProperty(iter.propertyPath);
                            break;
                        }
                    }
                }
                if (sp == null) return null;

                if (appendToList && sp.isArray)
                {
                    // Reuse the existing list-append machinery by reconstructing a
                    // stub element type from the SerializedProperty. For simplicity
                    // we defer to the reflection-based path when possible; otherwise
                    // grow the array and write the raw string.
                    sp.arraySize += 1;
                    var element = sp.GetArrayElementAtIndex(sp.arraySize - 1);
                    ApplySerializedValue(element, valueStr);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(comp);
                    return ToolUtils.CreateSuccessResponse(
                        $"Appended 1 item to serialized list '{sp.name}' on '{componentType}' of '{gameObjectPath}'");
                }

                Undo.RecordObject(comp, $"Set Serialized: {gameObjectPath}.{componentType}.{sp.name}");
                if (!ApplySerializedValue(sp, valueStr))
                {
                    return ToolUtils.CreateErrorResponse(
                        $"Could not convert value '{valueStr}' to serialized property '{sp.name}' " +
                        $"(type {sp.propertyType}) on '{componentType}'.");
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
                return ToolUtils.CreateSuccessResponse(
                    $"Set serialized property '{sp.name}' on '{componentType}' of '{gameObjectPath}' to '{valueStr}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to set via SerializedObject: {e.Message}");
            }
        }

        /// <summary>Apply a string value to a SerializedProperty using its declared type.</summary>
        private static bool ApplySerializedValue(SerializedProperty sp, string valueStr)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    if (bool.TryParse(valueStr, out var bv)) { sp.boolValue = bv; return true; }
                    return false;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Character:
                    if (int.TryParse(valueStr, out var iv)) { sp.intValue = iv; return true; }
                    return false;
                case SerializedPropertyType.Float:
                    if (float.TryParse(valueStr,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var fv))
                    { sp.floatValue = fv; return true; }
                    return false;
                case SerializedPropertyType.String:
                    sp.stringValue = valueStr; return true;
                case SerializedPropertyType.Enum:
                {
                    // Accept either the enum name ("Off") or its integer index ("0").
                    if (int.TryParse(valueStr, out var enumIdx)
                        && enumIdx >= 0 && sp.enumNames != null && enumIdx < sp.enumNames.Length)
                    {
                        sp.enumValueIndex = enumIdx;
                        return true;
                    }
                    if (sp.enumNames != null)
                    {
                        for (int i = 0; i < sp.enumNames.Length; i++)
                        {
                            if (string.Equals(sp.enumNames[i], valueStr, StringComparison.OrdinalIgnoreCase))
                            {
                                sp.enumValueIndex = i;
                                return true;
                            }
                        }
                    }
                    return false;
                }
                case SerializedPropertyType.Color:
                    try { sp.colorValue = ToolUtils.ParseColor(valueStr); return true; } catch { return false; }
                case SerializedPropertyType.Vector2:
                {
                    try { var v = ToolUtils.ParseVector3(valueStr); sp.vector2Value = new Vector2(v.x, v.y); return true; } catch { return false; }
                }
                case SerializedPropertyType.Vector3:
                    try { sp.vector3Value = ToolUtils.ParseVector3(valueStr); return true; } catch { return false; }
                case SerializedPropertyType.Vector4:
                {
                    try
                    {
                        var v = ToolUtils.ParseVector3(valueStr);
                        sp.vector4Value = new Vector4(v.x, v.y, v.z, 0f);
                        return true;
                    }
                    catch { return false; }
                }
                case SerializedPropertyType.Quaternion:
                {
                    try
                    {
                        var euler = ToolUtils.ParseVector3(valueStr);
                        sp.quaternionValue = Quaternion.Euler(euler);
                        return true;
                    }
                    catch { return false; }
                }
                case SerializedPropertyType.ObjectReference:
                {
                    var trimmed = valueStr?.Trim().Trim('"') ?? "";
                    if (string.IsNullOrEmpty(trimmed)) { sp.objectReferenceValue = null; return true; }
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(trimmed);
                    if (asset != null) { sp.objectReferenceValue = asset; return true; }
                    var go = ToolUtils.FindGameObjectByPath(trimmed);
                    if (go != null) { sp.objectReferenceValue = go; return true; }
                    return false;
                }
                default:
                    return false;
            }
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
        private string AppendToListProperty(UnityEngine.Object target, string propertyName, string valueStr, System.Type listType, string gameObjectPath, string componentType)
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
                
                Undo.RecordObject(target, $"Append to List: {gameObjectPath}.{componentType}.{propertyName}");
                
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
                
                return ToolUtils.CreateSuccessResponse($"Appended {itemsToAdd.Count} item(s) to list '{propertyName}' on component '{componentType}' of '{gameObjectPath}'");
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to append to list property: {e.Message}");
            }
        }
        
        /// <summary>
        /// Safely appends items to a list field using SerializedProperty (supports undo/redo).
        /// </summary>
        private string AppendToListField(UnityEngine.Object target, string fieldName, string valueStr, System.Type listType, string gameObjectPath, string componentType)
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
                
                Undo.RecordObject(target, $"Append to List: {gameObjectPath}.{componentType}.{fieldName}");
                
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
                
                return ToolUtils.CreateSuccessResponse($"Appended {itemsToAdd.Count} item(s) to list '{fieldName}' on component '{componentType}' of '{gameObjectPath}'");
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
