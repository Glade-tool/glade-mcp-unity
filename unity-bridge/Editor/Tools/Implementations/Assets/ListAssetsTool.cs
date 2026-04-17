using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class ListAssetsTool : ITool
    {
        public string Name => "list_assets";

        public string Execute(Dictionary<string, object> args)
        {
            string assetType = args.ContainsKey("assetType") ? args["assetType"].ToString() : "All";
            string nameContains = args.ContainsKey("nameContains") ? args["nameContains"].ToString() : "";
            string folderPath = args.ContainsKey("folderPath") ? args["folderPath"].ToString() : "";
            int maxResults = 20; // Lower default to avoid context bloat
            if (args.ContainsKey("maxResults"))
            {
                if (args["maxResults"] is int i) maxResults = i;
                else if (args["maxResults"] is float f) maxResults = (int)f;
                else int.TryParse(args["maxResults"].ToString(), out maxResults);
            }
            maxResults = Math.Min(maxResults, 50); // Hard cap to prevent context bloat
            
            // Build search filter
            string typeFilter = assetType.ToLower() switch
            {
                "material" => "t:Material",
                "prefab" => "t:Prefab",
                "texture" => "t:Texture",
                "audioclip" => "t:AudioClip",
                "animationclip" => "t:AnimationClip",
                "animatorcontroller" => "t:AnimatorController",
                "scene" => "t:Scene",
                "script" => "t:Script",
                _ => ""
            };
            
            string searchQuery = typeFilter;
            if (!string.IsNullOrEmpty(nameContains))
            {
                searchQuery += " " + nameContains;
            }
            
            string[] searchFolders = null;
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (!folderPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    folderPath = "Assets/" + folderPath;
                searchFolders = new string[] { folderPath };
            }
            
            string[] guids;
            if (searchFolders != null)
            {
                guids = AssetDatabase.FindAssets(searchQuery, searchFolders);
            }
            else
            {
                guids = AssetDatabase.FindAssets(searchQuery);
            }
            
            var allPaths = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                allPaths.Add(path);
            }
            var assetsFiltered = DemoAssetsGuard.FilterPathsExcludingDemoAssets(allPaths);
            var assets = new List<string>();
            for (int i = 0; i < assetsFiltered.Count && assets.Count < maxResults; i++)
                assets.Add(assetsFiltered[i]);
            int totalNonDemo = assetsFiltered.Count;
            
            // Build message with paths included (for summarization)
            // Strip "Assets/" prefix since tools auto-add it - saves tokens
            var msgBuilder = new StringBuilder();
            msgBuilder.Append($"Found {totalNonDemo} {assetType} asset(s)");
            if (guids.Length > assets.Count)
                msgBuilder.Append($" (showing {assets.Count})");
            msgBuilder.Append(": ");
            for (int i = 0; i < assets.Count; i++)
            {
                if (i > 0) msgBuilder.Append(", ");
                string path = assets[i];
                // Strip Assets/ prefix - tools add it back automatically
                if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    path = path.Substring(7);
                msgBuilder.Append(path);
            }
            if (totalNonDemo > assets.Count)
                msgBuilder.Append($" (+{totalNonDemo - assets.Count} more)");
            
            var extras = new Dictionary<string, object>
            {
                { "count", assets.Count },
                { "totalFound", totalNonDemo },
                { "assets", assets }
            };
            
            return ToolUtils.CreateSuccessResponse(msgBuilder.ToString(), extras);
        }
    }
}
