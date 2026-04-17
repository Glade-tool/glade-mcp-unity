using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    public class CreateRenderTextureTool : ITool
    {
        public string Name => "create_render_texture";

        public string Execute(Dictionary<string, object> args)
        {
            string assetPath = args.ContainsKey("assetPath") ? args["assetPath"].ToString() : "";
            int width = args.ContainsKey("width") ? (int)float.Parse(args["width"].ToString(), System.Globalization.CultureInfo.InvariantCulture) : 256;
            int height = args.ContainsKey("height") ? (int)float.Parse(args["height"].ToString(), System.Globalization.CultureInfo.InvariantCulture) : 256;
            int depth = args.ContainsKey("depth") ? (int)float.Parse(args["depth"].ToString(), System.Globalization.CultureInfo.InvariantCulture) : 24;

            if (string.IsNullOrEmpty(assetPath))
            {
                return ToolUtils.CreateErrorResponse("assetPath is required");
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                assetPath = "Assets/" + assetPath;

            string dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }

            RenderTextureFormat format = RenderTextureFormat.Default;
            if (args.ContainsKey("format"))
            {
                System.Enum.TryParse(args["format"].ToString(), true, out format);
            }

            var rt = new RenderTexture(width, height, depth, format);
            rt.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            rt.Create();

            AssetDatabase.CreateAsset(rt, assetPath);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "assetPath", assetPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created RenderTexture at '{assetPath}'", extras);
        }
    }
}
