using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateIKControllerScriptTool : ITool
    {
        public string Name => "create_ik_controller_script";

        public string Execute(Dictionary<string, object> args)
        {
            string scriptPath = args.ContainsKey("scriptPath") ? args["scriptPath"].ToString() : "";
            string className = args.ContainsKey("className") ? args["className"].ToString() : "IKController";
            
            if (string.IsNullOrEmpty(scriptPath))
                return ToolUtils.CreateErrorResponse("scriptPath is required");
            
            // Ensure path starts with Assets/ and ends with .cs
            if (!scriptPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                scriptPath = "Assets/" + scriptPath;
            
            if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                scriptPath += ".cs";
            
            // Ensure directory exists
            string dir = Path.GetDirectoryName(scriptPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            // Load template
            string templatePath = "Assets/Editor/GladeAgenticAI/Tools/Templates/IKController.cs";
            if (!File.Exists(templatePath))
            {
                return ToolUtils.CreateErrorResponse($"Template file not found at '{templatePath}'");
            }
            
            string templateContent = File.ReadAllText(templatePath);
            
            // Replace class name if custom
            if (!className.Equals("IKController", StringComparison.OrdinalIgnoreCase))
            {
                templateContent = templateContent.Replace("public class IKController", $"public class {className}");
            }
            
            // Write script file
            File.WriteAllText(scriptPath, templateContent);
            
            // Refresh AssetDatabase
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            
            var extras = new Dictionary<string, object>
            {
                { "scriptPath", scriptPath },
                { "className", className },
                { "requiresCompilation", true }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created IK controller script at '{scriptPath}'. Unity will auto-compile the script.", extras);
        }
    }
}
