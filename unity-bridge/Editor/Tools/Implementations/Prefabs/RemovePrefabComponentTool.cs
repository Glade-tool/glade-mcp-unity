using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to remove components from prefab assets (root or children).
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class RemovePrefabComponentTool : ITool
    {
        public string Name => "remove_prefab_component";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string objectPath = args.ContainsKey("objectPath") ? args["objectPath"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            
            if (string.IsNullOrEmpty(componentType))
                return ToolUtils.CreateErrorResponse("componentType is required");

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

            // Find component type
            System.Type type = ToolUtils.FindComponentType(componentType);
            
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found");
            }
            
            Component comp = targetObject.GetComponent(type);
            if (comp == null)
            {
                return ToolUtils.CreateErrorResponse($"Component '{componentType}' not found on '{targetObject.name}' in prefab '{prefabPath}'");
            }
            
            // Remove component
            Undo.DestroyObjectImmediate(comp);
            
            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["objectPath"] = objectPath ?? prefabAsset.name,
                ["componentType"] = componentType
            };

            return ToolUtils.CreateSuccessResponse($"Removed component '{componentType}' from '{targetObject.name}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
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
