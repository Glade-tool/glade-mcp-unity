using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class CreateFolderTool : ITool
    {
        public string Name => "create_folder";

        public string Execute(Dictionary<string, object> args)
        {
            string folderPath = args.ContainsKey("folderPath") ? args["folderPath"]?.ToString() : "";
            if (string.IsNullOrEmpty(folderPath))
                return ToolUtils.CreateErrorResponse("folderPath is required");

            if (!folderPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                folderPath = "Assets/" + folderPath;

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                var extras = new Dictionary<string, object> { { "folderPath", folderPath } };
                return ToolUtils.CreateSuccessResponse($"Folder already exists at '{folderPath}'", extras);
            }

            ToolUtils.EnsureAssetFolder(folderPath);
            AssetDatabase.Refresh();

            var extrasCreated = new Dictionary<string, object> { { "folderPath", folderPath } };
            return ToolUtils.CreateSuccessResponse($"Created folder at '{folderPath}'", extrasCreated);
        }
    }
}
