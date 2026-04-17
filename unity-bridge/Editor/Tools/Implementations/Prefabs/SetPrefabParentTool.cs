using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to reparent objects within prefab assets.
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class SetPrefabParentTool : ITool
    {
        public string Name => "set_prefab_parent";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string childPath = args.ContainsKey("childPath") ? args["childPath"].ToString() : "";
            if (string.IsNullOrEmpty(childPath))
                return ToolUtils.CreateErrorResponse("childPath is required");

            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"].ToString() : "";

            bool worldPositionStays = true;
            if (args.ContainsKey("worldPositionStays"))
            {
                if (bool.TryParse(args["worldPositionStays"].ToString(), out bool parsedBool))
                    worldPositionStays = parsedBool;
                else if (args["worldPositionStays"] is bool b)
                    worldPositionStays = b;
            }

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

            // Find child object to reparent
            UnityEngine.Transform childTransform = FindObjectInPrefab(prefabAsset.transform, childPath);
            if (childTransform == null)
                return ToolUtils.CreateErrorResponse($"Child object '{childPath}' not found in prefab hierarchy");

            // Find new parent (null means root)
            UnityEngine.Transform newParent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                newParent = FindObjectInPrefab(prefabAsset.transform, parentPath);
                if (newParent == null)
                    return ToolUtils.CreateErrorResponse($"Parent object '{parentPath}' not found in prefab hierarchy");
            }

            // Prevent reparenting to self or descendants
            if (newParent != null)
            {
                UnityEngine.Transform check = newParent;
                while (check != null)
                {
                    if (check == childTransform)
                        return ToolUtils.CreateErrorResponse("Cannot reparent object to itself or its descendants");
                    check = check.parent;
                }
            }

            // Use SerializedObject to edit prefab asset
            SerializedObject serializedTransform = new SerializedObject(childTransform);
            SerializedProperty parentProperty = serializedTransform.FindProperty("m_Father");
            
            if (parentProperty != null && newParent != null)
            {
                // Set parent reference
                parentProperty.objectReferenceValue = newParent;
            }
            else if (parentProperty != null && newParent == null)
            {
                // Remove parent (set to root)
                parentProperty.objectReferenceValue = null;
            }

            // Also update the transform's parent directly
            childTransform.SetParent(newParent, worldPositionStays);

            // Apply changes
            serializedTransform.ApplyModifiedProperties();

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            string parentName = newParent != null ? newParent.name : "root";

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["childPath"] = childPath,
                ["parentPath"] = parentPath ?? "root",
                ["worldPositionStays"] = worldPositionStays
            };

            return ToolUtils.CreateSuccessResponse($"Reparented '{childTransform.name}' to '{parentName}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
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
