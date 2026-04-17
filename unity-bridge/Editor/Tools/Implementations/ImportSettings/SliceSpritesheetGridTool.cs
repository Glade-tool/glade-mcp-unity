using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.ImportSettings
{
    public class SliceSpritesheetGridTool : ITool
    {
        private const int FinalFallbackCellSize = 180;
        public string Name => "slice_spritesheet_grid";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            if (string.IsNullOrEmpty(assetPath))
                return ToolUtils.CreateErrorResponse("assetPath is required");

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            int cellWidth = ParseInt(args, "cellWidth", 0);
            int cellHeight = ParseInt(args, "cellHeight", 0);
            bool autoDetectCellSize = ParseBool(args, "autoDetectCellSize", false);
            int columns = ParseInt(args, "columns", 0);
            int rows = ParseInt(args, "rows", 0);

            int offsetX = ParseInt(args, "offsetX", 0);
            int offsetY = ParseInt(args, "offsetY", 0);
            int paddingX = ParseInt(args, "paddingX", 0);
            int paddingY = ParseInt(args, "paddingY", 0);
            string namePrefix = args.ContainsKey("namePrefix") ? args["namePrefix"].ToString() : "frame";

            float pivotX = ParseFloat(args, "pivotX", 0.5f);
            float pivotY = ParseFloat(args, "pivotY", 0.5f);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return ToolUtils.CreateErrorResponse($"TextureImporter not found for '{assetPath}'");

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
                return ToolUtils.CreateErrorResponse($"Texture not found at '{assetPath}'");

            bool usedFinalFallback180 = false;
            if (autoDetectCellSize)
            {
                string autoDetectError = TryResolveCellSizeFromGrid(
                    texture.width,
                    texture.height,
                    offsetX,
                    offsetY,
                    paddingX,
                    paddingY,
                    columns,
                    rows,
                    ref cellWidth,
                    ref cellHeight
                );

                if (!string.IsNullOrEmpty(autoDetectError))
                {
                    if (!TryApplyFinalFallback180(texture.width, texture.height, offsetX, offsetY, ref cellWidth, ref cellHeight))
                        return ToolUtils.CreateErrorResponse(autoDetectError);

                    usedFinalFallback180 = true;
                }
            }
            else if (cellWidth <= 0 || cellHeight <= 0)
            {
                if (!TryApplyFinalFallback180(texture.width, texture.height, offsetX, offsetY, ref cellWidth, ref cellHeight))
                    return ToolUtils.CreateErrorResponse("cellWidth and cellHeight must be greater than 0 unless autoDetectCellSize=true with valid columns/rows.");

                usedFinalFallback180 = true;
            }

            // Validate texture dimensions
            if (texture.width < cellWidth || texture.height < cellHeight)
            {
                return ToolUtils.CreateErrorResponse($"Texture dimensions ({texture.width}x{texture.height}) are smaller than cell size ({cellWidth}x{cellHeight}). Cannot slice spritesheet.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;

            var spriteMetaDataList = new List<SpriteMetaData>();
            int frameIndex = 0;

            // Unity sprite rect coordinates use bottom-left origin.
            // Calculate the maximum Y position where a cell can start
            int maxY = texture.height - cellHeight;
            
            // Validate that we can fit at least one cell
            if (maxY < offsetY)
            {
                return ToolUtils.CreateErrorResponse($"OffsetY ({offsetY}) is too large. Maximum Y position is {maxY} (texture height {texture.height} - cell height {cellHeight}).");
            }
            
            if (texture.width < offsetX + cellWidth)
            {
                return ToolUtils.CreateErrorResponse($"OffsetX ({offsetX}) + cellWidth ({cellWidth}) = {offsetX + cellWidth} exceeds texture width ({texture.width}).");
            }

            // Generate sprite cells from bottom to top (Unity uses bottom-left origin)
            int startY = maxY - offsetY;
            for (int y = startY; y >= 0; y -= (cellHeight + paddingY))
            {
                // Check if this row can fit a cell
                if (y + cellHeight > texture.height)
                    continue;
                    
                for (int x = offsetX; x + cellWidth <= texture.width; x += (cellWidth + paddingX))
                {
                    // Validate cell bounds
                    if (x < 0 || y < 0 || x + cellWidth > texture.width || y + cellHeight > texture.height)
                        continue;
                        
                    var rect = new Rect(x, y, cellWidth, cellHeight);
                    spriteMetaDataList.Add(new SpriteMetaData
                    {
                        name = $"{namePrefix}_{frameIndex:D3}",
                        rect = rect,
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(pivotX, pivotY)
                    });
                    frameIndex++;
                }
            }

            if (spriteMetaDataList.Count == 0)
            {
                // Provide detailed diagnostic information
                int availableWidth = texture.width - offsetX;
                int availableHeight = texture.height - offsetY;
                int cellsPerRow = (availableWidth + paddingX) / (cellWidth + paddingX);
                int cellsPerColumn = (availableHeight + paddingY) / (cellHeight + paddingY);
                
                return ToolUtils.CreateErrorResponse(
                    $"No sprite cells were generated. " +
                    $"Texture: {texture.width}x{texture.height}, " +
                    $"Cell: {cellWidth}x{cellHeight}, " +
                    $"Offset: {offsetX},{offsetY}, " +
                    $"Padding: {paddingX},{paddingY}. " +
                    $"Expected cells: ~{cellsPerRow} per row, ~{cellsPerColumn} per column. " +
                    $"Check that cell size + padding fits within texture dimensions."
                );
            }

#pragma warning disable CS0618
            importer.spritesheet = spriteMetaDataList.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();

            var extras = new Dictionary<string, object>
            {
                { "assetPath", assetPath },
                { "sliceCount", spriteMetaDataList.Count },
                { "cellWidth", cellWidth },
                { "cellHeight", cellHeight },
                { "autoDetectCellSize", autoDetectCellSize },
                { "usedFinalFallback180", usedFinalFallback180 }
            };

            if (autoDetectCellSize)
            {
                extras["columns"] = columns;
                extras["rows"] = rows;
            }

            return ToolUtils.CreateSuccessResponse($"Sliced spritesheet into {spriteMetaDataList.Count} sprites", extras);
        }

        private static int ParseInt(Dictionary<string, object> args, string key, int defaultValue)
        {
            if (!args.ContainsKey(key)) return defaultValue;
            return int.TryParse(args[key].ToString(), out int value) ? value : defaultValue;
        }

        private static float ParseFloat(Dictionary<string, object> args, string key, float defaultValue)
        {
            if (!args.ContainsKey(key)) return defaultValue;
            return float.TryParse(args[key].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value)
                ? value
                : defaultValue;
        }

        private static bool ParseBool(Dictionary<string, object> args, string key, bool defaultValue)
        {
            if (!args.ContainsKey(key)) return defaultValue;
            return bool.TryParse(args[key].ToString(), out bool value) ? value : defaultValue;
        }

        private static string TryResolveCellSizeFromGrid(
            int textureWidth,
            int textureHeight,
            int offsetX,
            int offsetY,
            int paddingX,
            int paddingY,
            int columns,
            int rows,
            ref int cellWidth,
            ref int cellHeight)
        {
            if (textureWidth <= 0 || textureHeight <= 0)
                return "Texture dimensions are invalid.";

            if (cellWidth <= 0 && columns <= 0)
                return "autoDetectCellSize=true requires columns>0 or explicit cellWidth.";

            if (cellHeight <= 0 && rows <= 0)
                return "autoDetectCellSize=true requires rows>0 or explicit cellHeight.";

            if (columns > 0)
            {
                int availableWidth = textureWidth - offsetX - (Math.Max(0, columns - 1) * paddingX);
                if (availableWidth <= 0)
                    return $"Cannot infer cellWidth: available width is {availableWidth} after offset/padding.";

                if (availableWidth % columns != 0)
                    return $"Cannot infer cellWidth evenly: available width {availableWidth} is not divisible by columns {columns}.";

                cellWidth = availableWidth / columns;
            }

            if (rows > 0)
            {
                int availableHeight = textureHeight - offsetY - (Math.Max(0, rows - 1) * paddingY);
                if (availableHeight <= 0)
                    return $"Cannot infer cellHeight: available height is {availableHeight} after offset/padding.";

                if (availableHeight % rows != 0)
                    return $"Cannot infer cellHeight evenly: available height {availableHeight} is not divisible by rows {rows}.";

                cellHeight = availableHeight / rows;
            }

            if (cellWidth <= 0 || cellHeight <= 0)
                return "Resolved cellWidth/cellHeight must both be greater than 0.";

            return null;
        }

        private static bool TryApplyFinalFallback180(
            int textureWidth,
            int textureHeight,
            int offsetX,
            int offsetY,
            ref int cellWidth,
            ref int cellHeight)
        {
            if (textureWidth < FinalFallbackCellSize || textureHeight < FinalFallbackCellSize)
                return false;

            if (offsetX + FinalFallbackCellSize > textureWidth || offsetY + FinalFallbackCellSize > textureHeight)
                return false;

            cellWidth = FinalFallbackCellSize;
            cellHeight = FinalFallbackCellSize;
            return true;
        }
    }
}
