using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class CreateScriptableObjectTool : ITool
    {
        public string Name => "create_scriptable_object";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            string scriptTypeName = args.ContainsKey("scriptTypeName") ? args["scriptTypeName"].ToString() : "";
            
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }
            
            if (string.IsNullOrEmpty(scriptTypeName))
            {
                return ToolUtils.CreateErrorResponse("scriptTypeName is required");
            }
            
            // Ensure path starts with Assets/
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;
            
            // Ensure path ends with .asset
            if (!assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                assetPath += ".asset";
            
            // Ensure folder exists
            string dir = System.IO.Path.GetDirectoryName(assetPath);
            // Normalize path separators (Windows uses backslashes, Unity needs forward slashes)
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }
            
            // Check if asset already exists (case-insensitive)
            if (ToolUtils.LoadAssetAtPathCaseInsensitive<ScriptableObject>(assetPath) != null)
            {
                // Get the actual path with correct casing
                string actualPath;
                ToolUtils.NormalizeAssetPath(assetPath, out actualPath);
                return ToolUtils.CreateErrorResponse($"ScriptableObject already exists at '{actualPath}'. Use a different path or delete the existing asset first.");
            }
            
            // Find the ScriptableObject type
            System.Type scriptType = FindScriptableObjectType(scriptTypeName);
            if (scriptType == null)
            {
                return ToolUtils.CreateErrorResponse($"ScriptableObject type '{scriptTypeName}' not found. Make sure the script exists and inherits from ScriptableObject, and that it has been compiled.");
            }
            
            if (!typeof(ScriptableObject).IsAssignableFrom(scriptType))
            {
                return ToolUtils.CreateErrorResponse($"Type '{scriptTypeName}' does not inherit from ScriptableObject.");
            }
            
            try
            {
                // Create instance
                ScriptableObject asset = ScriptableObject.CreateInstance(scriptType);
                
                // Create asset
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                
                var extras = new Dictionary<string, object>
                {
                    { "assetPath", assetPath },
                    { "scriptTypeName", scriptTypeName }
                };
                
                return ToolUtils.CreateSuccessResponse($"Created ScriptableObject '{scriptTypeName}' at '{assetPath}'", extras);
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse($"Failed to create ScriptableObject: {e.Message}");
            }
        }
        
        private System.Type FindScriptableObjectType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            
            // 1) Try direct lookup (works for fully-qualified names)
            System.Type type = System.Type.GetType(typeName);
            if (type != null && typeof(ScriptableObject).IsAssignableFrom(type))
                return type;
            
            // 2) Try with common namespaces
            string[] namespaces = { "", "UnityEngine", "UnityEditor" };
            foreach (string ns in namespaces)
            {
                string fullName = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
                type = System.Type.GetType(fullName);
                if (type != null && typeof(ScriptableObject).IsAssignableFrom(type))
                    return type;
            }
            
            // 3) Try assembly GetType for fully-qualified names
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName);
                    if (type != null && typeof(ScriptableObject).IsAssignableFrom(type))
                        return type;
                    
                    // Try with namespaces
                    foreach (string ns in namespaces)
                    {
                        string fullName = string.IsNullOrEmpty(ns) ? typeName : $"{ns}.{typeName}";
                        type = assembly.GetType(fullName);
                        if (type != null && typeof(ScriptableObject).IsAssignableFrom(type))
                            return type;
                    }
                }
                catch { }
            }
            
            // 4) FINAL fallback: scan all loaded types by short Name
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        var t = types[i];
                        if (t == null) continue;
                        if (!typeof(ScriptableObject).IsAssignableFrom(t)) continue;
                        if (t.Name == typeName)
                            return t;
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException rtle)
                {
                    var types = rtle.Types;
                    if (types != null)
                    {
                        for (int i = 0; i < types.Length; i++)
                        {
                            var t = types[i];
                            if (t == null) continue;
                            if (!typeof(ScriptableObject).IsAssignableFrom(t)) continue;
                            if (t.Name == typeName)
                                return t;
                        }
                    }
                }
                catch { }
            }
            
            return null;
        }
    }
}
