using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to rename objects in prefab assets (root or children).
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class RenamePrefabObjectTool : ITool
    {
        public string Name => "rename_prefab_object";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string objectPath = args.ContainsKey("objectPath") ? args["objectPath"].ToString() : "";
            string newName = args.ContainsKey("newName") ? args["newName"].ToString() : "";
            
            if (string.IsNullOrEmpty(newName))
                return ToolUtils.CreateErrorResponse("newName is required");

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

            string oldName = targetObject.name;

            // Use SerializedObject to edit prefab asset
            SerializedObject serializedObject = new SerializedObject(targetObject);
            SerializedProperty nameProperty = serializedObject.FindProperty("m_Name");
            if (nameProperty != null)
            {
                nameProperty.stringValue = newName;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                // Fallback to direct assignment
                targetObject.name = newName;
            }

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["objectPath"] = objectPath ?? prefabAsset.name,
                ["oldName"] = oldName,
                ["newName"] = newName
            };

            return ToolUtils.CreateSuccessResponse($"Renamed '{oldName}' to '{newName}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
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
