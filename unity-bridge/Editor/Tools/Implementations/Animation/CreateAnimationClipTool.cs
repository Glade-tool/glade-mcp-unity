using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateAnimationClipTool : ITool
    {
        public string Name => "create_animation_clip";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            // Ensure path starts with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            // Ensure has .anim extension
            if (!clipPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                clipPath += ".anim";
            
            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(clipPath);
            // Normalize path separators (Windows uses backslashes, Unity needs forward slashes)
            if (!string.IsNullOrEmpty(dir))
            {
                dir = dir.Replace('\\', '/');
            }
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }
            
            // Create clip
            AnimationClip clip = new AnimationClip();
            clip.name = System.IO.Path.GetFileNameWithoutExtension(clipPath);
            
            // Set frame rate if provided
            if (args.ContainsKey("frameRate"))
            {
                float frameRate = 60f;
                if (args["frameRate"] is float f) frameRate = f;
                else float.TryParse(args["frameRate"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out frameRate);
                clip.frameRate = frameRate;
            }
            
            // Set wrap mode if provided
            if (args.ContainsKey("wrapMode"))
            {
                string wrapModeStr = args["wrapMode"].ToString();
                if (System.Enum.TryParse<WrapMode>(wrapModeStr, true, out WrapMode wrapMode))
                {
                    clip.wrapMode = wrapMode;
                }
            }
            
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "clipPath", clipPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created AnimationClip at '{clipPath}'", extras);
        }
    }
}
