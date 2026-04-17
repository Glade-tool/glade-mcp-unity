using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    public class GetMaterialUsageTool : ITool
    {
        public string Name => "get_material_usage";

        public string Execute(Dictionary<string, object> args)
        {
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("materialPath is required");
            }
            
            // Ensure path starts with Assets/
            if (!materialPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                materialPath = "Assets/" + materialPath;
            }
            
            // Load material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                return ToolUtils.CreateErrorResponse($"Material not found at '{materialPath}'");
            }
            
            // Find all GameObjects in the scene that use this material
            var gameObjectsUsingMaterial = new List<string>();
            var allObjects = UnityEngine.Object.FindObjectsByType<UnityEngine.GameObject>(FindObjectsSortMode.None);
            
            foreach (var obj in allObjects)
            {
                // Only check objects in the active scene
                if (obj.scene != SceneManager.GetActiveScene())
                    continue;
                
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (materials != null)
                    {
                        foreach (var sharedMat in materials)
                        {
                            if (sharedMat == mat)
                            {
                                // Get full path of GameObject
                                string objPath = obj.name;
                                UnityEngine.Transform parent = obj.transform.parent;
                                while (parent != null)
                                {
                                    objPath = parent.name + "/" + objPath;
                                    parent = parent.parent;
                                }
                                gameObjectsUsingMaterial.Add(objPath);
                                break; // Found it on this object, move to next
                            }
                        }
                    }
                }
            }
            
            var result = new Dictionary<string, object>
            {
                ["materialPath"] = materialPath.StartsWith("Assets/") ? materialPath.Substring(7) : materialPath,
                ["gameObjects"] = gameObjectsUsingMaterial,
                ["usageCount"] = gameObjectsUsingMaterial.Count,
                ["isShared"] = gameObjectsUsingMaterial.Count > 1
            };
            
            string message = gameObjectsUsingMaterial.Count == 0
                ? $"Material '{materialPath}' is not currently used by any GameObjects in the scene"
                : gameObjectsUsingMaterial.Count == 1
                    ? $"Material '{materialPath}' is used by 1 GameObject (not shared)"
                    : $"Material '{materialPath}' is shared and used by {gameObjectsUsingMaterial.Count} GameObjects";
            
            return ToolUtils.CreateSuccessResponse(message, result);
        }
    }
}
