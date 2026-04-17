using System;
using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    public class CreateScriptTool : ITool
    {
        public string Name => "create_script";

        public string Execute(Dictionary<string, object> args)
        {
            string scriptPath = args.ContainsKey("scriptPath") ? args["scriptPath"].ToString() : "";
            // Tool schema uses "scriptContent", but also check "scriptText" for backward compatibility
            string scriptContent = args.ContainsKey("scriptContent") ? args["scriptContent"].ToString() 
                : (args.ContainsKey("scriptText") ? args["scriptText"].ToString() : "");
            
            if (string.IsNullOrEmpty(scriptPath))
            {
                return ToolUtils.CreateErrorResponse("scriptPath is required");
            }
            
            if (string.IsNullOrEmpty(scriptContent))
            {
                return ToolUtils.CreateErrorResponse("scriptContent is required");
            }
            
            // Ensure path starts with Assets/
            if (!scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                scriptPath = "Assets/" + scriptPath;
            }
            
            // Detect file extension from path, default to .cs if no extension
            string extension = System.IO.Path.GetExtension(scriptPath);
            if (string.IsNullOrEmpty(extension))
            {
                scriptPath += ".cs";
                extension = ".cs";
            }
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(scriptPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            
            // Write file
            System.IO.File.WriteAllText(scriptPath, scriptContent);

            // Refresh AssetDatabase
            AssetDatabase.Refresh(ImportAssetOptions.Default);

            // Determine if compilation is needed (only for .cs files)
            bool requiresCompilation = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
            
            var extras = new Dictionary<string, object>
            {
                { "scriptPath", scriptPath }
            };
            if (requiresCompilation)
            {
                extras.Add("requiresCompilation", true);
            }
            
            string fileType = extension.Equals(".shader", StringComparison.OrdinalIgnoreCase) ? "shader" : "script";
            string message = requiresCompilation
                ? $"Created {fileType} at '{scriptPath}'. IMPORTANT: Unity is compiling this script. You MUST call compile_scripts and wait until it reports 'Compilation complete' BEFORE calling add_component with this script type."
                : $"Created {fileType} at '{scriptPath}'. Unity will import the {fileType}.";
            
            return ToolUtils.CreateSuccessResponse(message, extras);
        }
    }
}
