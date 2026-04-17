using System.Collections.Generic;

namespace GladeAgenticAI.Services
{
    /// <summary>
    /// Enforces the "Reference demo assets" setting: when disabled, no asset under the plugin's DemoAssets folder may be used or listed.
    /// Simplified version for Electron-based projects - always allows demo assets (no UI setting available).
    /// </summary>
    public static class DemoAssetsGuard
    {
        private const string DemoAssetsRoot = "Assets/Editor/GladeAgenticAI/DemoAssets";
        private static readonly string DemoRootForward = DemoAssetsRoot + "/";

        /// <summary>
        /// Returns true if the given asset path is under the plugin's DemoAssets folder (case-insensitive, normalizes path).
        /// </summary>
        public static bool IsPathUnderDemoAssets(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string normalized = path.Replace('\\', '/').Trim();
            if (!normalized.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                normalized = "Assets/" + normalized;
            return normalized.StartsWith(DemoRootForward, System.StringComparison.OrdinalIgnoreCase)
                   || normalized.Equals(DemoAssetsRoot, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if the path is allowed to be used (load, save, open, delete, etc.).
        /// Reads setting from EditorPrefs (set by Electron app via Unity Bridge).
        /// </summary>
        public static bool AllowUseOfDemoAssetPath(string path)
        {
            if (!IsPathUnderDemoAssets(path)) return true;
            // Read setting from EditorPrefs (defaults to true if not set)
            return UnityEditor.EditorPrefs.GetBool("GladeAI.ReferenceDemoAssets", true);
        }

        /// <summary>
        /// When "Reference demo assets" is false, removes any path under DemoAssets from the list.
        /// Otherwise returns the list unchanged.
        /// Reads setting from EditorPrefs (set by Electron app via Unity Bridge).
        /// </summary>
        public static List<string> FilterPathsExcludingDemoAssets(List<string> paths)
        {
            if (paths == null || paths.Count == 0) return paths;
            // Read setting from EditorPrefs (defaults to true if not set)
            if (UnityEditor.EditorPrefs.GetBool("GladeAI.ReferenceDemoAssets", true)) return paths;
            var filtered = new List<string>(paths.Count);
            foreach (string p in paths)
            {
                if (!IsPathUnderDemoAssets(p))
                    filtered.Add(p);
            }
            return filtered;
        }
    }
}
