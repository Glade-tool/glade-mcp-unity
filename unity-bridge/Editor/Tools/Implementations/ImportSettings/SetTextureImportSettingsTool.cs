using System;
using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ImportSettings
{
    public class SetTextureImportSettingsTool : ITool
    {
        public string Name => "set_texture_import_settings";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return ToolUtils.CreateErrorResponse($"TextureImporter not found for '{assetPath}'");
            }

            if (args.ContainsKey("textureType") && System.Enum.TryParse(args["textureType"].ToString(), true, out TextureImporterType type))
                importer.textureType = type;
            if (args.ContainsKey("sRGBTexture") && bool.TryParse(args["sRGBTexture"].ToString(), out bool srgb))
                importer.sRGBTexture = srgb;
            if (args.ContainsKey("alphaIsTransparency") && bool.TryParse(args["alphaIsTransparency"].ToString(), out bool alpha))
                importer.alphaIsTransparency = alpha;
            if (args.ContainsKey("mipmapEnabled") && bool.TryParse(args["mipmapEnabled"].ToString(), out bool mip))
                importer.mipmapEnabled = mip;
            if (args.ContainsKey("maxTextureSize") && int.TryParse(args["maxTextureSize"].ToString(), out int maxSize))
                importer.maxTextureSize = maxSize;

            importer.SaveAndReimport();
            return ToolUtils.CreateSuccessResponse($"Updated texture import settings for '{assetPath}'");
        }
    }
}
