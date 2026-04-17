using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ImportSettings
{
    public class SetSpriteImportSettingsTool : ITool
    {
        public string Name => "set_sprite_import_settings";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
                return ToolUtils.CreateErrorResponse("assetPath is required");

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return ToolUtils.CreateErrorResponse($"TextureImporter not found for '{assetPath}'");

            importer.textureType = TextureImporterType.Sprite;

            if (args.ContainsKey("spriteMode") &&
                Enum.TryParse(args["spriteMode"].ToString(), true, out SpriteImportMode spriteMode))
            {
                importer.spriteImportMode = spriteMode;
            }

            if (args.ContainsKey("pixelsPerUnit") &&
                float.TryParse(args["pixelsPerUnit"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ppu))
            {
                importer.spritePixelsPerUnit = ppu;
            }

            TextureImporterSettings textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            if (args.ContainsKey("meshType") &&
                Enum.TryParse(args["meshType"].ToString(), true, out SpriteMeshType meshType))
            {
                textureSettings.spriteMeshType = meshType;
            }
            importer.SetTextureSettings(textureSettings);

            if (args.ContainsKey("filterMode") &&
                Enum.TryParse(args["filterMode"].ToString(), true, out FilterMode filterMode))
            {
                importer.filterMode = filterMode;
            }

            if (args.ContainsKey("wrapMode") &&
                Enum.TryParse(args["wrapMode"].ToString(), true, out TextureWrapMode wrapMode))
            {
                importer.wrapMode = wrapMode;
            }

            if (args.ContainsKey("compression") &&
                Enum.TryParse(args["compression"].ToString(), true, out TextureImporterCompression compression))
            {
                importer.textureCompression = compression;
            }

            if (args.ContainsKey("alphaIsTransparency") &&
                bool.TryParse(args["alphaIsTransparency"].ToString(), out bool alphaIsTransparency))
            {
                importer.alphaIsTransparency = alphaIsTransparency;
            }

            if (args.ContainsKey("sRGBTexture") &&
                bool.TryParse(args["sRGBTexture"].ToString(), out bool srgb))
            {
                importer.sRGBTexture = srgb;
            }

            if (args.ContainsKey("mipmapEnabled") &&
                bool.TryParse(args["mipmapEnabled"].ToString(), out bool mipmapEnabled))
            {
                importer.mipmapEnabled = mipmapEnabled;
            }

            if (args.ContainsKey("maxTextureSize") &&
                int.TryParse(args["maxTextureSize"].ToString(), out int maxTextureSize))
            {
                importer.maxTextureSize = maxTextureSize;
            }

            importer.SaveAndReimport();

            var extras = new Dictionary<string, object>
            {
                { "assetPath", assetPath },
                { "textureType", importer.textureType.ToString() },
                { "spriteMode", importer.spriteImportMode.ToString() },
                { "pixelsPerUnit", importer.spritePixelsPerUnit }
            };

            return ToolUtils.CreateSuccessResponse($"Updated sprite import settings for '{assetPath}'", extras);
        }
    }
}
