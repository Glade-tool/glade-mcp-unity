using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    public class CreatePrefabTool : ITool
    {
        public string Name => "create_prefab";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                return ToolUtils.CreateErrorResponse("prefabPath is required");
            }
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            // Ensure path starts with Assets/ and ends with .prefab
            if (!prefabPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                prefabPath = "Assets/" + prefabPath;
            }
            if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                prefabPath += ".prefab";
            }
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            // Create prefab
            UnityEngine.GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            
            if (prefab == null)
            {
                return ToolUtils.CreateErrorResponse($"Failed to create prefab at '{prefabPath}'");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            var extras = new Dictionary<string, object>
            {
                { "prefabPath", prefabPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created prefab at '{prefabPath}' from '{gameObjectPath}'", extras);
        }
    }
}
