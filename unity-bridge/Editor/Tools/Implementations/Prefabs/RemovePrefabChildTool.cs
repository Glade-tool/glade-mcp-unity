using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to remove/delete child objects from prefab assets.
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class RemovePrefabChildTool : ITool
    {
        public string Name => "remove_prefab_child";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string childPath = args.ContainsKey("childPath") ? args["childPath"].ToString() : "";
            if (string.IsNullOrEmpty(childPath))
                return ToolUtils.CreateErrorResponse("childPath is required");

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

            // Find child object to remove
            string[] segments = childPath.Split('/');
            UnityEngine.Transform current = prefabAsset.transform;
            UnityEngine.Transform targetTransform = null;

            // If childPath is just the root name, check if it's the root itself
            if (segments.Length == 1 && string.Equals(segments[0], prefabAsset.name, StringComparison.OrdinalIgnoreCase))
            {
                return ToolUtils.CreateErrorResponse("Cannot remove root object of prefab. Use delete_asset to delete the entire prefab.");
            }

            // Navigate to the child
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrEmpty(segments[i])) continue;
                
                UnityEngine.Transform child = FindChildByName(current, segments[i]);
                if (child == null)
                    return ToolUtils.CreateErrorResponse($"Child object '{childPath}' not found in prefab hierarchy");
                
                // If this is the last segment, this is our target
                if (i == segments.Length - 1)
                {
                    targetTransform = child;
                    break;
                }
                
                current = child;
            }

            if (targetTransform == null)
                return ToolUtils.CreateErrorResponse($"Child object '{childPath}' not found in prefab hierarchy");

            string childName = targetTransform.name;

            // Remove the child
            Undo.DestroyObjectImmediate(targetTransform.gameObject);

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["childPath"] = childPath,
                ["removedChild"] = childName
            };

            return ToolUtils.CreateSuccessResponse($"Removed child '{childName}' from prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
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
