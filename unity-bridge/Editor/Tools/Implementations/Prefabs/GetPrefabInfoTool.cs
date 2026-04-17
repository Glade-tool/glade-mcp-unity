using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to get information about a prefab asset, including hierarchy, components, and transform properties.
    /// </summary>
    public class GetPrefabInfoTool : ITool
    {
        public string Name => "get_prefab_info";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

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

            var info = new Dictionary<string, object>();

            // Basic info
            info["prefabPath"] = prefabPath;
            info["name"] = prefabAsset.name;
            info["active"] = prefabAsset.activeSelf;

            // Transform info
            UnityEngine.Transform rootTransform = prefabAsset.transform;
            info["transform"] = new Dictionary<string, object>
            {
                ["position"] = $"{rootTransform.localPosition.x},{rootTransform.localPosition.y},{rootTransform.localPosition.z}",
                ["rotation"] = $"{rootTransform.localRotation.eulerAngles.x},{rootTransform.localRotation.eulerAngles.y},{rootTransform.localRotation.eulerAngles.z}",
                ["scale"] = $"{rootTransform.localScale.x},{rootTransform.localScale.y},{rootTransform.localScale.z}"
            };

            // Components
            var components = new List<string>();
            Component[] allComponents = prefabAsset.GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                if (comp != null)
                    components.Add(comp.GetType().Name);
            }
            info["components"] = components;

            // Hierarchy
            var hierarchy = new List<string>();
            CollectHierarchy(prefabAsset.transform, hierarchy, "");
            info["hierarchy"] = hierarchy;

            // Child count
            info["childCount"] = rootTransform.childCount;

            var extras = new Dictionary<string, object>
            {
                ["info"] = info
            };

            return ToolUtils.CreateSuccessResponse($"Prefab info for '{prefabPath}'", extras);
        }

        private void CollectHierarchy(UnityEngine.Transform transform, List<string> hierarchy, string prefix)
        {
            string name = prefix + transform.name;
            hierarchy.Add(name);

            for (int i = 0; i < transform.childCount; i++)
            {
                CollectHierarchy(transform.GetChild(i), hierarchy, name + "/");
            }
        }
    }
}
