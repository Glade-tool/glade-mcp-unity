using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to add child objects to prefab assets.
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class AddPrefabChildTool : ITool
    {
        public string Name => "add_prefab_child";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            string parentPath = args.ContainsKey("parentPath") ? args["parentPath"].ToString() : "";
            string childName = args.ContainsKey("childName") ? args["childName"].ToString() : "GameObject";
            string primitiveType = args.ContainsKey("primitiveType") ? args["primitiveType"].ToString() : "";

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

            // Find parent object (root or child)
            UnityEngine.GameObject parentObject = prefabAsset;
            if (!string.IsNullOrEmpty(parentPath))
            {
                string[] segments = parentPath.Split('/');
                UnityEngine.Transform current = prefabAsset.transform;
                foreach (string segment in segments)
                {
                    if (string.IsNullOrEmpty(segment)) continue;
                    UnityEngine.Transform child = FindChildByName(current, segment);
                    if (child == null)
                        return ToolUtils.CreateErrorResponse($"Parent object '{parentPath}' not found in prefab hierarchy");
                    current = child;
                }
                parentObject = current.gameObject;
            }

            // Create child GameObject
            UnityEngine.GameObject childObject;
            if (!string.IsNullOrEmpty(primitiveType))
            {
                PrimitiveType type = primitiveType switch
                {
                    "Cube" => PrimitiveType.Cube,
                    "Sphere" => PrimitiveType.Sphere,
                    "Capsule" => PrimitiveType.Capsule,
                    "Cylinder" => PrimitiveType.Cylinder,
                    "Plane" => PrimitiveType.Plane,
                    "Quad" => PrimitiveType.Quad,
                    _ => PrimitiveType.Cube
                };
                childObject = UnityEngine.GameObject.CreatePrimitive(type);
                childObject.name = childName;
            }
            else
            {
                childObject = new UnityEngine.GameObject(childName);
            }

            // Set parent
            childObject.transform.SetParent(parentObject.transform, false);

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["parentPath"] = parentPath ?? prefabAsset.name,
                ["childName"] = childName,
                ["childPath"] = parentPath != null ? $"{parentPath}/{childName}" : childName
            };

            return ToolUtils.CreateSuccessResponse($"Added child '{childName}' to '{parentObject.name}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras);
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
