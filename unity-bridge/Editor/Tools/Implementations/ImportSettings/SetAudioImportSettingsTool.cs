using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ImportSettings
{
    public class SetAudioImportSettingsTool : ITool
    {
        public string Name => "set_audio_import_settings";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                return ToolUtils.CreateErrorResponse($"AudioImporter not found for '{assetPath}'");
            }

            var settings = importer.defaultSampleSettings;
            if (args.ContainsKey("loadType") && System.Enum.TryParse(args["loadType"].ToString(), true, out AudioClipLoadType loadType))
                settings.loadType = loadType;
            if (args.ContainsKey("compressionFormat") && System.Enum.TryParse(args["compressionFormat"].ToString(), true, out AudioCompressionFormat format))
                settings.compressionFormat = format;
            if (args.ContainsKey("quality") && float.TryParse(args["quality"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float quality))
                settings.quality = quality;
            if (args.ContainsKey("sampleRateSetting") && System.Enum.TryParse(args["sampleRateSetting"].ToString(), true, out AudioSampleRateSetting rateSetting))
                settings.sampleRateSetting = rateSetting;
            importer.defaultSampleSettings = settings;

            if (args.ContainsKey("forceToMono") && bool.TryParse(args["forceToMono"].ToString(), out bool mono))
                importer.forceToMono = mono;
            if (args.ContainsKey("preloadAudioData") && bool.TryParse(args["preloadAudioData"].ToString(), out bool preload))
                settings.preloadAudioData = preload;
            if (args.ContainsKey("loadInBackground") && bool.TryParse(args["loadInBackground"].ToString(), out bool background))
                importer.loadInBackground = background;

            importer.SaveAndReimport();
            return ToolUtils.CreateSuccessResponse($"Updated audio import settings for '{assetPath}'");
        }
    }
}
