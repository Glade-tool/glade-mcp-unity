using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.GameObject
{
    public class GetGameObjectInfoTool : ITool
    {
        public string Name => "get_gameobject_info";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"]?.ToString() : "";
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");

            var transform = obj.transform;
            var pos = transform.position;
            var rot = transform.rotation.eulerAngles;
            var scale = transform.localScale; // Scale is always local
            var localPos = transform.localPosition;
            var localRot = transform.localRotation.eulerAngles;
            
            var info = new Dictionary<string, object>
            {
                ["name"] = obj.name,
                ["active"] = obj.activeSelf,
                // World transform
                ["position"] = $"{pos.x},{pos.y},{pos.z}",
                ["rotation"] = $"{rot.x},{rot.y},{rot.z}",
                ["scale"] = $"{scale.x},{scale.y},{scale.z}",
                // Local transform
                ["localPosition"] = $"{localPos.x},{localPos.y},{localPos.z}",
                ["localRotation"] = $"{localRot.x},{localRot.y},{localRot.z}",
                ["localScale"] = $"{scale.x},{scale.y},{scale.z}",
                ["components"] = new List<string>()
            };
            foreach (var comp in obj.GetComponents<Component>())
            {
                ((List<string>)info["components"]).Add(comp.GetType().Name);
            }
            
            // Get material information from renderer
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var materials = new List<Dictionary<string, object>>();
                Material[] sharedMats = renderer.sharedMaterials;
                if (sharedMats != null && sharedMats.Length > 0)
                {
                    for (int i = 0; i < sharedMats.Length; i++)
                    {
                        var matInfo = new Dictionary<string, object>();
                        if (sharedMats[i] != null)
                        {
                            string matPath = AssetDatabase.GetAssetPath(sharedMats[i]);
                            if (!string.IsNullOrEmpty(matPath))
                            {
                                // Remove "Assets/" prefix for consistency with other tools
                                if (matPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                                    matPath = matPath.Substring(7);
                                matInfo["path"] = matPath;
                            }
                            else
                            {
                                // Material might be a runtime material (not an asset)
                                matInfo["name"] = sharedMats[i].name;
                            }
                            matInfo["shaderName"] = sharedMats[i].shader.name;
                        }
                        materials.Add(matInfo);
                    }
                }
                info["materials"] = materials;
            }
            else
            {
                info["materials"] = new List<Dictionary<string, object>>();
            }
            
            // Get terrain component data if GameObject has Terrain component
            UnityEngine.Terrain terrain = obj.GetComponent<UnityEngine.Terrain>();
            if (terrain != null && terrain.terrainData != null)
            {
                var terrainInfo = new Dictionary<string, object>();
                string terrainDataPath = AssetDatabase.GetAssetPath(terrain.terrainData);
                if (!string.IsNullOrEmpty(terrainDataPath))
                {
                    // Remove "Assets/" prefix for consistency with other tools
                    if (terrainDataPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                        terrainDataPath = terrainDataPath.Substring(7);
                    terrainInfo["terrainDataPath"] = terrainDataPath;
                }
                
                // Get prototype data from terrain
                var treePrototypes = new List<Dictionary<string, object>>();
                if (terrain.terrainData.treePrototypes != null)
                {
                    for (int i = 0; i < terrain.terrainData.treePrototypes.Length; i++)
                    {
                        var proto = terrain.terrainData.treePrototypes[i];
                        var protoInfo = new Dictionary<string, object>
                        {
                            ["index"] = i,
                            ["bendFactor"] = proto.bendFactor
                        };
                        
                        // Get prefab path if available
                        if (proto.prefab != null)
                        {
                            string prefabPath = AssetDatabase.GetAssetPath(proto.prefab);
                            if (!string.IsNullOrEmpty(prefabPath))
                            {
                                if (prefabPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                                    prefabPath = prefabPath.Substring(7);
                                protoInfo["prefabPath"] = prefabPath;
                            }
                            
                            // Get material from prefab renderer if available
                            Renderer prefabRenderer = proto.prefab.GetComponent<Renderer>();
                            if (prefabRenderer != null && prefabRenderer.sharedMaterial != null)
                            {
                                string matPath = AssetDatabase.GetAssetPath(prefabRenderer.sharedMaterial);
                                if (!string.IsNullOrEmpty(matPath))
                                {
                                    if (matPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                                        matPath = matPath.Substring(7);
                                    protoInfo["materialPath"] = matPath;
                                }
                                protoInfo["shaderName"] = prefabRenderer.sharedMaterial.shader.name;
                            }
                        }
                        treePrototypes.Add(protoInfo);
                    }
                }
                terrainInfo["treePrototypes"] = treePrototypes;
                info["terrain"] = terrainInfo;
            }
            
            return ToolUtils.SerializeDictToJson(info);
        }
    }
}
