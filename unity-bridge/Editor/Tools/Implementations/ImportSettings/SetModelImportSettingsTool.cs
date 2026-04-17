using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ImportSettings
{
    public class SetModelImportSettingsTool : ITool
    {
        public string Name => "set_model_import_settings";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                // Auto-detect asset type and handle automatically
                string fileExtension = Path.GetExtension(assetPath).ToLower();
                
                // If it's a texture/image file, automatically apply texture import settings instead
                if (fileExtension == ".png" || fileExtension == ".jpg" || fileExtension == ".jpeg" || 
                    fileExtension == ".tga" || fileExtension == ".psd" || fileExtension == ".exr" || 
                    fileExtension == ".tiff" || fileExtension == ".bmp")
                {
                    var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (textureImporter != null)
                    {
                        // Automatically convert to Sprite if it's not already (common use case)
                        if (textureImporter.textureType != TextureImporterType.Sprite)
                        {
                            textureImporter.textureType = TextureImporterType.Sprite;
                            if (textureImporter.spriteImportMode == SpriteImportMode.None)
                            {
                                textureImporter.spriteImportMode = SpriteImportMode.Single;
                            }
                        }
                        textureImporter.SaveAndReimport();
                        return ToolUtils.CreateSuccessResponse($"Asset '{assetPath}' is a texture file. Automatically converted to Sprite import type and applied texture import settings.");
                    }
                }
                
                // If it's a model file extension but can't be imported, provide error
                if (fileExtension == ".fbx" || fileExtension == ".obj" || fileExtension == ".dae" || 
                    fileExtension == ".3ds" || fileExtension == ".blend")
                {
                    return ToolUtils.CreateErrorResponse($"Model file '{assetPath}' found but Unity cannot import it. Check that the file is valid and in the Assets folder.");
                }
                
                // For unknown types, try to get the asset and see what it is
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                {
                    return ToolUtils.CreateErrorResponse($"Asset not found at path '{assetPath}'. Make sure the file exists in the Assets folder.");
                }
                
                // If we get here, it's some other asset type - just return success with info
                return ToolUtils.CreateSuccessResponse($"Asset '{assetPath}' is of type '{asset.GetType().Name}', not a 3D model. Model import settings don't apply, but no error occurred.");
            }

            if (args.ContainsKey("scaleFactor") && float.TryParse(args["scaleFactor"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                importer.globalScale = scale;
            if (args.ContainsKey("importAnimation") && bool.TryParse(args["importAnimation"].ToString(), out bool anim))
                importer.importAnimation = anim;
            if (args.ContainsKey("importMaterials") && bool.TryParse(args["importMaterials"].ToString(), out bool mats))
                importer.materialImportMode = mats ? ModelImporterMaterialImportMode.ImportStandard : ModelImporterMaterialImportMode.None;
            if (args.ContainsKey("meshCompression") && System.Enum.TryParse(args["meshCompression"].ToString(), true, out ModelImporterMeshCompression compression))
                importer.meshCompression = compression;

            importer.SaveAndReimport();
            return ToolUtils.CreateSuccessResponse($"Updated model import settings for '{assetPath}'");
        }
    }
}
