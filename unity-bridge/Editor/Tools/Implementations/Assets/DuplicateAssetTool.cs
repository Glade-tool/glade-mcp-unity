using System;
using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class DuplicateAssetTool : ITool
    {
        public string Name => "duplicate_asset";

        public string Execute(Dictionary<string, object> args)
        {
            string sourcePath = args.ContainsKey("sourcePath") ? args["sourcePath"].ToString() : "";
            string destinationPath = args.ContainsKey("destinationPath") ? args["destinationPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(sourcePath))
            {
                return ToolUtils.CreateErrorResponse("sourcePath is required");
            }
            
            // Ensure source path starts with Assets/
            if (!sourcePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                sourcePath = "Assets/" + sourcePath;
            
            // Check source exists
            if (!System.IO.File.Exists(sourcePath))
            {
                return ToolUtils.CreateErrorResponse($"Source asset not found at '{sourcePath}'");
            }
            
            // Generate destination path if not provided
            if (string.IsNullOrEmpty(destinationPath))
            {
                string dir = System.IO.Path.GetDirectoryName(sourcePath);
                string name = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
                string ext = System.IO.Path.GetExtension(sourcePath);
                destinationPath = System.IO.Path.Combine(dir, name + "_copy" + ext);
            }
            else if (!destinationPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                destinationPath = "Assets/" + destinationPath;
            }
            
            // Copy file
            bool success = AssetDatabase.CopyAsset(sourcePath, destinationPath);
            
            if (success)
            {
                AssetDatabase.Refresh();
                var extras = new Dictionary<string, object>
                {
                    { "newPath", destinationPath }
                };
                return ToolUtils.CreateSuccessResponse($"Duplicated asset to '{destinationPath}'", extras);
            }
            else
            {
                return ToolUtils.CreateErrorResponse("Failed to duplicate asset");
            }
        }
    }
}
