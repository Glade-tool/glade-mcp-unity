using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to set GameObject properties (active, layer, tag) on prefab assets.
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class SetPrefabGameObjectPropertyTool : ITool
    {
        public string Name => "set_prefab_gameobject_property";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string objectPath = args.ContainsKey("objectPath") ? args["objectPath"].ToString() : "";
            
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

            // Find target object (root or child)
            UnityEngine.GameObject targetObject = prefabAsset;
            if (!string.IsNullOrEmpty(objectPath))
            {
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

            // Use SerializedObject to edit prefab asset
            SerializedObject serializedObject = new SerializedObject(targetObject);
            bool hasChanges = false;

            // Handle active state
            if (args.ContainsKey("active"))
            {
                bool active = false;
                if (bool.TryParse(args["active"].ToString(), out bool parsedBool))
                    active = parsedBool;
                else if (args["active"] is bool b)
                    active = b;
                else
                    return ToolUtils.CreateErrorResponse("active must be a boolean value");

                SerializedProperty activeProperty = serializedObject.FindProperty("m_IsActive");
                if (activeProperty != null)
                {
                    activeProperty.boolValue = active;
                    hasChanges = true;
                }
            }

            // Handle layer
            if (args.ContainsKey("layer"))
            {
                int layer = 0;
                if (int.TryParse(args["layer"].ToString(), out int parsedLayer))
                    layer = parsedLayer;
                else
                    return ToolUtils.CreateErrorResponse("layer must be an integer");

                SerializedProperty layerProperty = serializedObject.FindProperty("m_Layer");
                if (layerProperty != null)
                {
                    layerProperty.intValue = layer;
                    hasChanges = true;
                }
            }

            // Handle tag
            if (args.ContainsKey("tag"))
            {
                string tag = args["tag"].ToString();
                try
                {
                    // Verify tag exists
                    targetObject.CompareTag(tag);
                }
                catch
                {
                    return ToolUtils.CreateErrorResponse($"Tag '{tag}' does not exist. Add it in Edit > Project Settings > Tags and Layers.");
                }

                SerializedProperty tagProperty = serializedObject.FindProperty("m_TagString");
                if (tagProperty != null)
                {
                    tagProperty.stringValue = tag;
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                return ToolUtils.CreateErrorResponse("No properties provided. Specify at least one of: active, layer, tag");
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["objectPath"] = objectPath ?? prefabAsset.name
            };

            return ToolUtils.CreateSuccessResponse($"Updated GameObject properties on '{targetObject.name}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
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
