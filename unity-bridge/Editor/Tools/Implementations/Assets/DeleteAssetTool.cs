using System;
using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class DeleteAssetTool : ITool
    {
        public string Name => "delete_asset";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }
            
            // Ensure path starts with Assets/
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;
            
            // Check exists
            if (!System.IO.File.Exists(assetPath) && !AssetDatabase.IsValidFolder(assetPath))
            {
                return ToolUtils.CreateErrorResponse($"Asset not found at '{assetPath}'");
            }
            
            bool success = AssetDatabase.DeleteAsset(assetPath);
            
            if (success)
            {
                AssetDatabase.Refresh();
                bool isScript = assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                
                var extras = new Dictionary<string, object>();
                if (isScript)
                {
                    extras["scriptPath"] = assetPath;
                    extras["requiresCompilation"] = true;
                }
                
                return ToolUtils.CreateSuccessResponse($"Deleted asset at '{assetPath}'", extras);
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"Failed to delete asset at '{assetPath}'");
            }
        }
    }
}
