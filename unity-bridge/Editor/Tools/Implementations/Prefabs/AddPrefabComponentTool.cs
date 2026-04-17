using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to add components to prefab assets (root or children).
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class AddPrefabComponentTool : ITool
    {
        public string Name => "add_prefab_component";

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
                // Check if there's a script file on disk that might contain this class
                string[] possiblePaths = new string[]
                {
                    $"Assets/Scripts/{componentType}.cs",
                    $"Assets/{componentType}.cs",
                    $"Assets/Scripts/Player/{componentType}.cs",
                    $"Assets/Scripts/Game/{componentType}.cs"
                };
                
                string foundScriptPath = null;
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        foundScriptPath = path;
                        break;
                    }
                }
                
                if (foundScriptPath == null)
                {
                    string[] allScripts = AssetDatabase.FindAssets("t:MonoScript");
                    foreach (var guid in allScripts)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (path.EndsWith($"{componentType}.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            foundScriptPath = path;
                            break;
                        }
                    }
                }
                
                if (foundScriptPath != null)
                {
                    var extras = new Dictionary<string, object>
                    {
                        { "scriptExists", true },
                        { "scriptPath", foundScriptPath },
                        { "requiresCompilation", true }
                    };
                    return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found. Script file exists at '{foundScriptPath}' but is not yet compiled. Wait for Unity to finish compiling scripts, then try again.", extras);
                }
                
                return ToolUtils.CreateErrorResponse($"Component type '{componentType}' not found. No matching script file found in the project.");
            }
            
            if (targetObject.GetComponent(type) != null)
            {
                return ToolUtils.CreateSuccessResponse($"Component '{componentType}' already exists on '{targetObject.name}' in prefab '{prefabPath}'");
            }
            
            // Add component using SerializedObject to ensure it's saved to prefab
            Component newComponent = Undo.AddComponent(targetObject, type);
            
            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras2 = new Dictionary<string, object>
            {
                ["prefabPath"] = prefabPath,
                ["objectPath"] = objectPath ?? prefabAsset.name,
                ["componentType"] = componentType
            };

            return ToolUtils.CreateSuccessResponse($"Added component '{componentType}' to '{targetObject.name}' in prefab '{prefabPath}'. Changes apply to all future instantiations.", extras2);
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
