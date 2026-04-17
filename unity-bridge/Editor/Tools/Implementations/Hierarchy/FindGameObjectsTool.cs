using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class FindGameObjectsTool : ITool
    {
        public string Name => "find_game_objects";

        public string Execute(Dictionary<string, object> args)
        {
            string nameContains = args.ContainsKey("nameContains") ? args["nameContains"].ToString() : "";
            string nameExact = args.ContainsKey("nameExact") ? args["nameExact"].ToString() : "";
            string tag = args.ContainsKey("tag") ? args["tag"].ToString() : "";
            string layer = args.ContainsKey("layer") ? args["layer"].ToString() : "";
            string hasComponent = args.ContainsKey("hasComponent") ? args["hasComponent"].ToString() : "";
            bool includeInactive = false;
            if (args.ContainsKey("includeInactive"))
            {
                if (args["includeInactive"] is bool b) includeInactive = b;
                else bool.TryParse(args["includeInactive"].ToString(), out includeInactive);
            }
            
            var results = new List<string>();
            
            // Get all GameObjects in scene (including inactive if requested)
            UnityEngine.GameObject[] allObjects;
            if (includeInactive)
            {
                allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
                // Filter to only scene objects (not prefabs, etc.)
                allObjects = System.Array.FindAll(allObjects, go => 
                    go.scene.IsValid() && !EditorUtility.IsPersistent(go));
            }
            else
            {
                allObjects = UnityEngine.GameObject.FindObjectsByType<UnityEngine.GameObject>(FindObjectsSortMode.None);
            }
            
            foreach (var obj in allObjects)
            {
                bool matches = true;
                
                // Name contains filter
                if (!string.IsNullOrEmpty(nameContains))
                {
                    if (!obj.name.ToLower().Contains(nameContains.ToLower()))
                        matches = false;
                }
                
                // Name exact filter
                if (!string.IsNullOrEmpty(nameExact))
                {
                    if (obj.name != nameExact)
                        matches = false;
                }
                
                // Tag filter
                if (!string.IsNullOrEmpty(tag))
                {
                    try
                    {
                        if (!obj.CompareTag(tag))
                            matches = false;
                    }
                    catch
                    {
                        matches = false; // Invalid tag
                    }
                }
                
                // Layer filter
                if (!string.IsNullOrEmpty(layer))
                {
                    int layerIndex = LayerMask.NameToLayer(layer);
                    if (layerIndex == -1)
                    {
                        // Try parsing as index
                        int.TryParse(layer, out layerIndex);
                    }
                    if (obj.layer != layerIndex)
                        matches = false;
                }
                
                // Component filter
                if (!string.IsNullOrEmpty(hasComponent))
                {
                    System.Type compType = ToolUtils.FindComponentType(hasComponent);
                    if (compType == null || obj.GetComponent(compType) == null)
                        matches = false;
                }
                
                if (matches)
                {
                    results.Add(ToolUtils.GetGameObjectPath(obj));
                }
            }
            
            return ToolUtils.BuildStringArrayResultWithCount("objects", results, $"Found {results.Count} object(s)");
        }
    }
}
