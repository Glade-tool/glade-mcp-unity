using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to set any property on a prefab asset's objects (root or children).
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class SetPrefabPropertyTool : ITool
    {
        public string Name => "set_prefab_property";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string objectPath = args.ContainsKey("objectPath") ? args["objectPath"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            string propertyName = args.ContainsKey("propertyName") ? args["propertyName"].ToString() : "";
            object propertyValue = args.ContainsKey("propertyValue") ? args["propertyValue"] : null;

            if (string.IsNullOrEmpty(componentType))
                return ToolUtils.CreateErrorResponse("componentType is required");
            if (string.IsNullOrEmpty(propertyName))
                return ToolUtils.CreateErrorResponse("propertyName is required");
            if (propertyValue == null)
                return ToolUtils.CreateErrorResponse("propertyValue is required");

            // Normalize path
            if (!prefabPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                prefabPath = "Assets/" + prefabPath;

            // Load prefab asset
            UnityEngine.GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                // Try case-insensitive search
                prefabAsset = ToolUtils.LoadAssetAtPathCaseInsensitive<UnityEngine.GameObject>(prefabPath);
                if (prefabAsset == null)
                    return ToolUtils.CreateErrorResponse($"Prefab not found at '{prefabPath}'");
            }

            // Find target object (root or child)
            UnityEngine.GameObject targetObject = prefabAsset;
            if (!string.IsNullOrEmpty(objectPath))
            {
                // objectPath is relative to prefab root (e.g., "Model" or "Model/Child")
                string[] segments = objectPath.Split('/');
                UnityEngine.Transform current = prefabAsset.transform;
                foreach (string segment in segments)
                {
                    if (string.IsNullOrEmpty(segment)) continue;
                    UnityEngine.Transform child = FindChildByName(current, segment);
                    if (child == null)
                        return ToolUtils.CreateErrorResponse($"Object '{objectPath}' not found in prefab hierarchy");
                    current = child;
                }
                targetObject = current.gameObject;
            }

            // Find component
            System.Type componentTypeObj = ToolUtils.FindComponentType(componentType);
            if (componentTypeObj == null)
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found");

            Component component = targetObject.GetComponent(componentTypeObj);
            if (component == null)
            {
                // List available components for better error message
                var availableComponents = targetObject.GetComponents<Component>().Select(c => c.GetType().Name).ToArray();
                return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on '{targetObject.name}'. Available components: {string.Join(", ", availableComponents)}");
            }

            // Use SerializedObject to edit prefab asset
            SerializedObject serializedComponent = new SerializedObject(component);
            SerializedProperty property = serializedComponent.FindProperty(propertyName);

            if (property == null)
            {
                // Try to find by display name (case-insensitive)
                SerializedProperty iterator = serializedComponent.GetIterator();
                bool found = false;
                while (iterator.NextVisible(true))
                {
                    if (string.Equals(iterator.name, propertyName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(iterator.displayName, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        property = iterator.Copy();
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return ToolUtils.CreateErrorResponse($"Property '{propertyName}' not found on component '{componentType}'. Use get_component_inspector_properties to list available properties.");
                }
            }

            // Convert and set value
            try
            {
                object convertedValue;
                if (property.propertyType == SerializedPropertyType.Enum)
                {
                    // For enums, pass the string value directly - SetSerializedPropertyValue will handle parsing
                    convertedValue = propertyValue.ToString();
                }
                else
                {
                    convertedValue = ToolUtils.ConvertValueToPropertyType(propertyValue.ToString(), GetPropertyType(property));
                }
                
                // Set property value based on type
                SetSerializedPropertyValue(property, convertedValue);
                
                // Apply changes
                serializedComponent.ApplyModifiedProperties();

                // Mark prefab as dirty and save
                EditorUtility.SetDirty(prefabAsset);
                AssetDatabase.SaveAssets();

                var extras = new Dictionary<string, object>
                {
                    ["prefabPath"] = prefabPath,
                    ["objectPath"] = objectPath ?? prefabAsset.name,
                    ["componentType"] = componentType,
                    ["propertyName"] = propertyName
                };

                return ToolUtils.CreateSuccessResponse($"Set property '{propertyName}' on '{componentType}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
            }
            catch (Exception ex)
            {
                return ToolUtils.CreateErrorResponse($"Failed to set property: {ex.Message}");
            }
        }

        private UnityEngine.Transform FindChildByName(UnityEngine.Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                UnityEngine.Transform child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                    return child;
                
                // Recursive search
                UnityEngine.Transform found = FindChildByName(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private System.Type GetPropertyType(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return typeof(int);
                case SerializedPropertyType.Boolean:
                    return typeof(bool);
                case SerializedPropertyType.Float:
                    return typeof(float);
                case SerializedPropertyType.String:
                    return typeof(string);
                case SerializedPropertyType.Color:
                    return typeof(Color);
                case SerializedPropertyType.Vector2:
                    return typeof(Vector2);
                case SerializedPropertyType.Vector3:
                    return typeof(Vector3);
                case SerializedPropertyType.Vector4:
                    return typeof(Vector4);
                case SerializedPropertyType.Quaternion:
                    return typeof(Quaternion);
                case SerializedPropertyType.ObjectReference:
                    return typeof(UnityEngine.Object);
                default:
                    return typeof(string);
            }
        }

        private void SetSerializedPropertyValue(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = value.ToString();
                    break;
                case SerializedPropertyType.Color:
                    if (value is Color color)
                        prop.colorValue = color;
                    else
                        prop.colorValue = ToolUtils.ParseColor(value.ToString());
                    break;
                case SerializedPropertyType.Vector2:
                    if (value is Vector2 v2)
                        prop.vector2Value = v2;
                    else
                        prop.vector2Value = ToolUtils.ParseVector2(value.ToString());
                    break;
                case SerializedPropertyType.Vector3:
                    if (value is Vector3 v3)
                        prop.vector3Value = v3;
                    else
                        prop.vector3Value = ToolUtils.ParseVector3(value.ToString());
                    break;
                case SerializedPropertyType.Vector4:
                    if (value is Vector4 v4)
                        prop.vector4Value = v4;
                    else
                        prop.vector4Value = ToolUtils.ParseVector4(value.ToString());
                    break;
                case SerializedPropertyType.Quaternion:
                    if (value is Quaternion q)
                        prop.quaternionValue = q;
                    else
                    {
                        var euler = ToolUtils.ParseVector3(value.ToString());
                        prop.quaternionValue = Quaternion.Euler(euler);
                    }
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (value is UnityEngine.Object obj)
                        prop.objectReferenceValue = obj;
                    else
                    {
                        // Try to load asset from path
                        string assetPath = value.ToString();
                        UnityEngine.Object asset = ToolUtils.LoadAssetAtPathCaseInsensitive<UnityEngine.Object>(assetPath);
                        prop.objectReferenceValue = asset;
                    }
                    break;
                case SerializedPropertyType.Enum:
                    if (value is int intVal)
                        prop.enumValueIndex = intVal;
                    else
                    {
                        // Try to parse enum by name
                        string enumName = value.ToString();
                        string[] enumNames = prop.enumNames;
                        for (int i = 0; i < enumNames.Length; i++)
                        {
                            if (string.Equals(enumNames[i], enumName, StringComparison.OrdinalIgnoreCase))
                            {
                                prop.enumValueIndex = i;
                                return;
                            }
                        }
                        throw new ArgumentException($"Enum value '{enumName}' not found");
                    }
                    break;
                default:
                    throw new ArgumentException($"Unsupported property type: {prop.propertyType}");
            }
        }
    }
}
