using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    public class InstantiatePrefabTool : ITool
    {
        public string Name => "instantiate_prefab";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            string name = args.ContainsKey("name") ? args["name"].ToString() : "";
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                return ToolUtils.CreateErrorResponse("prefabPath is required");
            }
            
            // Ensure path starts with Assets/
            if (!prefabPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                prefabPath = "Assets/" + prefabPath;
            }
            
            // Load prefab (may resolve by name)
            UnityEngine.GameObject prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
            List<string> matches = null;
            if (prefab == null)
            {
                string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                string resolvedPath = ToolUtils.FindPrefabPathByName(prefabName, out matches);
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    prefabPath = resolvedPath;
                    prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
                }
            }
            
            if (prefab == null)
            {
                if (matches != null && matches.Count > 1)
                {
                    string prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                    string options = string.Join(", ", matches.Take(5));
                    return ToolUtils.CreateErrorResponse($"Prefab not found at '{prefabPath}'. Found multiple matches for '{prefabName}': {options}");
                }
                return ToolUtils.CreateErrorResponse($"Prefab not found at '{prefabPath}'");
            }
            
            // Instantiate prefab
            UnityEngine.GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as UnityEngine.GameObject;
            if (instance == null)
            {
                return ToolUtils.CreateErrorResponse($"Failed to instantiate prefab from '{prefabPath}'");
            }
            
            // Set name if provided
            if (!string.IsNullOrEmpty(name))
            {
                instance.name = name;
            }
            
            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate Prefab: {prefabPath}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", instance.name }
            };
            
            return ToolUtils.CreateSuccessResponse($"Instantiated prefab '{prefabPath}' as '{instance.name}'", extras);
        }
    }
}
