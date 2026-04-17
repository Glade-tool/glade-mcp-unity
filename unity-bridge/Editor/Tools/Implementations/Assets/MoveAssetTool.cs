using System;
using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class MoveAssetTool : ITool
    {
        public string Name => "move_asset";

        public string Execute(Dictionary<string, object> args)
        {
            string sourcePath = args.ContainsKey("sourcePath") ? args["sourcePath"].ToString() : "";
            string destinationPath = args.ContainsKey("destinationPath") ? args["destinationPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(sourcePath))
            {
                return ToolUtils.CreateErrorResponse("sourcePath is required");
            }
            
            if (string.IsNullOrEmpty(destinationPath))
            {
                return ToolUtils.CreateErrorResponse("destinationPath is required");
            }
            
            // Ensure paths start with Assets/
            if (!sourcePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                sourcePath = "Assets/" + sourcePath;
            if (!destinationPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                destinationPath = "Assets/" + destinationPath;
            
            // Check source exists
            if (!System.IO.File.Exists(sourcePath) && !AssetDatabase.IsValidFolder(sourcePath))
            {
                return ToolUtils.CreateErrorResponse($"Source asset not found at '{sourcePath}'");
            }
            
            // Ensure destination directory exists
            string destDir = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !AssetDatabase.IsValidFolder(destDir))
                ToolUtils.EnsureAssetFolder(destDir);
            
            string result = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            
            if (string.IsNullOrEmpty(result))
            {
                AssetDatabase.Refresh();
                bool isScript = sourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                                || destinationPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                
                var extras = new Dictionary<string, object>
                {
                    { "newPath", destinationPath }
                };
                
                if (isScript)
                {
                    extras["scriptPath"] = destinationPath;
                    extras["requiresCompilation"] = true;
                }
                
                return ToolUtils.CreateSuccessResponse($"Moved asset from '{sourcePath}' to '{destinationPath}'", extras);
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Failed to move asset: {result}");
            }
        }
    }
}
