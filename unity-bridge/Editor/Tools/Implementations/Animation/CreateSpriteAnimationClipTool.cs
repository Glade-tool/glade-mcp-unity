using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Animation
{
    public class CreateSpriteAnimationClipTool : ITool
    {
        public string Name => "create_sprite_animation_clip";

        public string Execute(Dictionary<string, object> args)
        {
            string clipPath = args.ContainsKey("clipPath") ? args["clipPath"].ToString() : "";
            string spritesheetPath = args.ContainsKey("spritesheetPath") ? args["spritesheetPath"].ToString() : "";
            
            if (string.IsNullOrEmpty(clipPath))
                return ToolUtils.CreateErrorResponse("clipPath is required");
            
            if (string.IsNullOrEmpty(spritesheetPath))
                return ToolUtils.CreateErrorResponse("spritesheetPath is required");
            
            // Ensure paths start with Assets/
            if (!clipPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                clipPath = "Assets/" + clipPath;
            
            if (!spritesheetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                spritesheetPath = "Assets/" + spritesheetPath;
            
            // Ensure clip path has .anim extension
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
            
            // Load sprites from spritesheet
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(spritesheetPath);
            List<Sprite> sprites = new List<Sprite>();
            
            foreach (var asset in allAssets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }
            
            if (sprites.Count == 0)
            {
                return ToolUtils.CreateErrorResponse($"No sprites found in spritesheet at '{spritesheetPath}'. Make sure the spritesheet is sliced.");
            }
            
            // Sort sprites by name (handle numeric suffixes)
            if (args.ContainsKey("spriteOrder") && args["spriteOrder"] is List<object> spriteOrderList)
            {
                // Custom order provided
                var orderedSprites = new List<Sprite>();
                foreach (var name in spriteOrderList)
                {
                    var sprite = sprites.FirstOrDefault(s => s.name.Equals(name.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (sprite != null)
                        orderedSprites.Add(sprite);
                }
                // Add any remaining sprites not in the order list
                foreach (var sprite in sprites)
                {
                    if (!orderedSprites.Contains(sprite))
                        orderedSprites.Add(sprite);
                }
                sprites = orderedSprites;
            }
            else
            {
                // Sort by name (natural sort for numeric suffixes)
                sprites = sprites.OrderBy(s => s.name, new NaturalStringComparer()).ToList();
            }
            
            // Get frame rate
            float frameRate = 12f;
            if (args.ContainsKey("frameRate"))
            {
                if (args["frameRate"] is float f) frameRate = f;
                else float.TryParse(args["frameRate"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out frameRate);
            }
            
            // Get loop time
            bool loopTime = true;
            if (args.ContainsKey("loopTime"))
            {
                if (args["loopTime"] is bool b) loopTime = b;
                else bool.TryParse(args["loopTime"].ToString(), out loopTime);
            }
            
            // Create clip
            AnimationClip clip = new AnimationClip();
            clip.name = System.IO.Path.GetFileNameWithoutExtension(clipPath);
            clip.frameRate = frameRate;
            
            // Set loop time
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loopTime;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            
            // Create sprite keyframes
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
            float timePerFrame = 1f / frameRate;
            
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * timePerFrame,
                    value = sprites[i]
                };
            }
            
            // Set sprite curve using ObjectReferenceCurve
            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = "",
                propertyName = "m_Sprite",
                type = typeof(SpriteRenderer)
            };
            
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(clip, $"Create Sprite Animation Clip: {clipPath}");
            
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();
            
            float duration = sprites.Count * timePerFrame;
            
            var extras = new Dictionary<string, object>
            {
                { "clipPath", clipPath },
                { "frameCount", sprites.Count },
                { "duration", duration }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created sprite AnimationClip with {sprites.Count} frames at '{clipPath}'", extras);
        }
        
        // Natural string comparer for sorting sprite names with numeric suffixes
        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                
                int i = 0, j = 0;
                while (i < x.Length && j < y.Length)
                {
                    if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
                    {
                        int numX = 0, numY = 0;
                        int startX = i, startY = j;
                        
                        while (i < x.Length && char.IsDigit(x[i])) i++;
                        while (j < y.Length && char.IsDigit(y[j])) j++;
                        
                        int.TryParse(x.Substring(startX, i - startX), out numX);
                        int.TryParse(y.Substring(startY, j - startY), out numY);
                        
                        if (numX != numY) return numX.CompareTo(numY);
                    }
                    else
                    {
                        int cmp = char.ToLowerInvariant(x[i]).CompareTo(char.ToLowerInvariant(y[j]));
                        if (cmp != 0) return cmp;
                        i++;
                        j++;
                    }
                }
                
                return x.Length.CompareTo(y.Length);
            }
        }
    }
}
