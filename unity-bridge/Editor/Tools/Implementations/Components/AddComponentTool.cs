using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Components
{
    public class AddComponentTool : ITool
    {
        public string Name => "add_component";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string componentType = args.ContainsKey("componentType") ? args["componentType"].ToString() : "";
            
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }
            
            if (string.IsNullOrEmpty(componentType))
            {
                return ToolUtils.CreateErrorResponse("componentType is required");
            }
            
            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }
            
            // Try to find component type.
            // NOTE: Unity scripts are compiled into assemblies; until compilation + domain reload completes,
            // the type will not exist and cannot be added as a component.
            System.Type type = ToolUtils.FindComponentType(componentType);
            
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                // Check if there's a script file on disk that might contain this class
                // This helps the AI understand that the script needs to compile first
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
                
                // Also search all .cs files for the class name
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
                    // Script file exists but type not compiled yet
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
            
            if (obj.GetComponent(type) != null)
            {
                return ToolUtils.CreateSuccessResponse($"Component '{componentType}' already exists on '{gameObjectPath}'");
            }
            
            Undo.AddComponent(obj, type);
            
            return ToolUtils.CreateSuccessResponse($"Added component '{componentType}' to '{gameObjectPath}'");
        }
    }
}
