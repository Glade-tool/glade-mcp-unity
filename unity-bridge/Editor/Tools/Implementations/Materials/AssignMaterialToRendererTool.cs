using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Materials
{
    public class AssignMaterialToRendererTool : ITool
    {
        public string Name => "assign_material_to_renderer";

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
            
            // Find GameObject
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            UnityEngine.GameObject obj = string.IsNullOrEmpty(gameObjectPath) ? null : ToolUtils.FindGameObjectByPath(gameObjectPath);
            
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            // Get renderer
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' has no Renderer component");
            }
            
            // Assign material
            int slot = 0;
            if (args.ContainsKey("materialSlot"))
            {
                int.TryParse(args["materialSlot"].ToString(), out slot);
            }
            
            // Capture previous material state BEFORE modification
            Material[] prevMaterials = renderer.sharedMaterials;
            string prevMaterialPath = null;
            if (prevMaterials != null && slot < prevMaterials.Length && prevMaterials[slot] != null)
            {
                prevMaterialPath = AssetDatabase.GetAssetPath(prevMaterials[slot]);
            }
            
            Undo.RecordObject(renderer, $"Assign Material: {gameObjectPath}");
            
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length <= slot)
            {
                Material[] newMaterials = new Material[slot + 1];
                System.Array.Copy(materials, newMaterials, materials.Length);
                materials = newMaterials;
            }
            materials[slot] = mat;
            renderer.sharedMaterials = materials;
            
            // Return previous state in response for revert capability
            var extras = new Dictionary<string, object>
            {
                ["previousState"] = new Dictionary<string, object>
                {
                    ["materialPath"] = prevMaterialPath ?? "",
                    ["materialSlot"] = slot
                }
            };
            
            return ToolUtils.CreateSuccessResponse($"Assigned material '{materialPath}' to '{gameObjectPath}'", extras);
        }
    }
}
