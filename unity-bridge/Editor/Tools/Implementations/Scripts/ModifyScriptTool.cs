using System;
using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    public class ModifyScriptTool : ITool
    {
        public string Name => "modify_script";

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
            
            // Determine file type for error messages
            string fileType = extension.Equals(".shader", StringComparison.OrdinalIgnoreCase) ? "shader" : "script";
            
            // Check if file exists
            if (!System.IO.File.Exists(scriptPath))
            {
                return ToolUtils.CreateErrorResponse($"{fileType.Substring(0, 1).ToUpper() + fileType.Substring(1)} does not exist at '{scriptPath}'. Use create_script to create a new {fileType}.");
            }
            
            // NOTE: Backup is handled by the revert system via /api/file/backup endpoint
            // The frontend calls backupFile() before executing modify_script
            // No need to create .backup files in Assets folder anymore
            
            // Write modified file
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
            
            string message = requiresCompilation 
                ? $"Modified {fileType} at '{scriptPath}'. Unity will auto-compile the script."
                : $"Modified {fileType} at '{scriptPath}'. Unity will import the {fileType}.";
            
            return ToolUtils.CreateSuccessResponse(message, extras);
        }
    }
}
