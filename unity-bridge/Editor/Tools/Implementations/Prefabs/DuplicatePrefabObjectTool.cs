using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to duplicate objects within prefab assets.
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class DuplicatePrefabObjectTool : ITool
    {
        public string Name => "duplicate_prefab_object";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string objectPath = args.ContainsKey("objectPath") ? args["objectPath"].ToString() : "";
            if (string.IsNullOrEmpty(objectPath))
                return ToolUtils.CreateErrorResponse("objectPath is required");

            string newName = args.ContainsKey("newName") ? args["newName"].ToString() : "";
            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"].ToString() : "";

            int count = 1;
            if (args.ContainsKey("count"))
            {
                if (args["count"] is int c) count = c;
                else if (args["count"] is float f) count = (int)f;
                else int.TryParse(args["count"]?.ToString(), out count);
            }
            count = Mathf.Clamp(count, 1, 100);

            // Normalize path
            if (!prefabPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                prefabPath = "Assets/" + prefabPath;

            // Load prefab asset
            UnityEngine.GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                prefabAsset = ToolUtils.LoadAssetAtPathCaseInsensitive<UnityEngine.GameObject>(prefabPath);
                if (prefabAsset == null)
                    return ToolUtils.CreateErrorResponse($"Prefab not found at '{prefabPath}'");
            }

            // Find source object to duplicate
            UnityEngine.Transform sourceTransform = FindObjectInPrefab(prefabAsset.transform, objectPath);
            if (sourceTransform == null)
                return ToolUtils.CreateErrorResponse($"Object '{objectPath}' not found in prefab hierarchy");

            // Find parent for duplicates
            UnityEngine.Transform parentTransform = prefabAsset.transform;
            if (!string.IsNullOrEmpty(parentPath))
            {
                parentTransform = FindObjectInPrefab(prefabAsset.transform, parentPath);
                if (parentTransform == null)
                    return ToolUtils.CreateErrorResponse($"Parent object '{parentPath}' not found in prefab hierarchy");
            }

            var created = new List<string>();

            // Duplicate the object
            for (int i = 0; i < count; i++)
            {
                UnityEngine.GameObject duplicate = InstantiatePrefabObject(sourceTransform.gameObject, parentTransform);
                
                // Set name
                if (!string.IsNullOrEmpty(newName))
                {
                    if (count > 1)
                        duplicate.name = $"{newName} ({i + 1})";
                    else
                        duplicate.name = newName;
                }
                else
                {
                    // Auto-generate name if not provided
                    string baseName = sourceTransform.name;
                    if (count > 1)
                        duplicate.name = $"{baseName} ({i + 1})";
                    else
                        duplicate.name = $"{baseName} (1)";
                }

                created.Add(duplicate.name);
            }

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["objectPath"] = objectPath,
                ["parentPath"] = parentPath ?? prefabAsset.name,
                ["count"] = count,
                ["created"] = created
            };

            string message = count == 1 
                ? $"Duplicated '{sourceTransform.name}' in prefab '{prefabPath}' as '{created[0]}'. Changes apply to all future instantiations."
                : $"Duplicated '{sourceTransform.name}' {count} times in prefab '{prefabPath}'. Changes apply to all future instantiations.";

            return ToolUtils.CreateSuccessResponse(message, extras);
        }

        private UnityEngine.GameObject InstantiatePrefabObject(UnityEngine.GameObject source, UnityEngine.Transform parent)
        {
            // Create a deep copy of the GameObject and all its children
            UnityEngine.GameObject duplicate = new UnityEngine.GameObject(source.name);
            
            // Copy Transform
            duplicate.transform.SetParent(parent, false);
            duplicate.transform.localPosition = source.transform.localPosition;
            duplicate.transform.localRotation = source.transform.localRotation;
            duplicate.transform.localScale = source.transform.localScale;

            // Copy GameObject properties
            duplicate.SetActive(source.activeSelf);
            duplicate.layer = source.layer;
            duplicate.tag = source.tag;

            // Copy all components
            Component[] components = source.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp is UnityEngine.Transform) continue; // Skip Transform, already handled

                System.Type compType = comp.GetType();
                Component newComp = duplicate.AddComponent(compType);

                // Copy component properties using SerializedObject
                CopyComponentProperties(comp, newComp);
            }

            // Recursively copy children
            for (int i = 0; i < source.transform.childCount; i++)
            {
                UnityEngine.Transform child = source.transform.GetChild(i);
                InstantiatePrefabObject(child.gameObject, duplicate.transform);
            }

            return duplicate;
        }

        private void CopyComponentProperties(Component source, Component target)
        {
            SerializedObject sourceSO = new SerializedObject(source);
            SerializedObject targetSO = new SerializedObject(target);

            SerializedProperty iterator = sourceSO.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                
                // Skip script reference and other read-only properties
                if (iterator.propertyPath == "m_Script" || iterator.propertyPath.StartsWith("m_Component"))
                    continue;

                SerializedProperty targetProp = targetSO.FindProperty(iterator.propertyPath);
                if (targetProp != null && targetProp.propertyType == iterator.propertyType)
                {
                    CopySerializedProperty(iterator, targetProp);
                }
            }

            targetSO.ApplyModifiedProperties();
        }

        private void CopySerializedProperty(SerializedProperty source, SerializedProperty target)
        {
            switch (source.propertyType)
            {
                case SerializedPropertyType.Integer:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    target.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    target.floatValue = source.floatValue;
                    break;
                case SerializedPropertyType.String:
                    target.stringValue = source.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    target.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.Vector2:
                    target.vector2Value = source.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    target.vector3Value = source.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    target.vector4Value = source.vector4Value;
                    break;
                case SerializedPropertyType.Quaternion:
                    target.quaternionValue = source.quaternionValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    target.objectReferenceValue = source.objectReferenceValue;
                    break;
                case SerializedPropertyType.Enum:
                    target.enumValueIndex = source.enumValueIndex;
                    break;
                case SerializedPropertyType.ArraySize:
                    target.arraySize = source.arraySize;
                    break;
                default:
                    // For complex types, try to copy recursively
                    if (source.hasChildren)
                    {
                        // Copy all child properties recursively
                        SerializedProperty sourceIterator = source.Copy();
                        SerializedProperty targetIterator = target.Copy();
                        bool enterChildren = true;
                        while (sourceIterator.NextVisible(enterChildren) && targetIterator.NextVisible(enterChildren))
                        {
                            enterChildren = false;
                            if (sourceIterator.propertyType == targetIterator.propertyType)
                            {
                                CopySerializedProperty(sourceIterator, targetIterator);
                            }
                        }
                    }
                    break;
            }
        }

        private UnityEngine.Transform FindObjectInPrefab(UnityEngine.Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            string[] segments = path.Split('/');
            UnityEngine.Transform current = root;

            foreach (string segment in segments)
            {
                if (string.IsNullOrEmpty(segment)) continue;
                
                UnityEngine.Transform child = FindChildByName(current, segment);
                if (child == null)
                    return null;
                
                current = child;
            }

            return current;
        }

        private UnityEngine.Transform FindChildByName(UnityEngine.Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                UnityEngine.Transform child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                    return child;
                
                UnityEngine.Transform found = FindChildByName(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
