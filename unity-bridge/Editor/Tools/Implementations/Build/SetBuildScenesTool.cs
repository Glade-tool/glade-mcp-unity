using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using GladeAgenticAI.Core.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Build
{
    /// <summary>
    /// Configure File > Build Settings "Scenes In Build" — which scenes ship in
    /// the player, and in what order. Index 0 is the scene the game boots into,
    /// which is the part people actually care about ("it builds but launches
    /// into the wrong level").
    ///
    /// This is the Unity analog of Godot's create_export_preset: the configure
    /// step between recon and build. It is separate from build_player because
    /// the build list is persistent project state a user inspects in the
    /// Inspector, not a per-build argument.
    /// </summary>
    public class SetBuildScenesTool : ITool
    {
        public string Name => "set_build_scenes";

        public string Execute(Dictionary<string, object> args)
        {
            var paths = ToolUtils.GetPathsFromArgsOrSelection(args, "scenePaths");
            if (paths == null || paths.Count == 0)
            {
                return ToolUtils.CreateErrorResponse(
                    "scenePaths is required — pass the scenes to include, in boot order " +
                    "(index 0 is the scene the game starts in).");
            }

            // Validate every path up front. A half-applied build list is worse
            // than a rejected call: the player would ship silently missing a
            // level and the failure only shows at runtime.
            var missing = new List<string>();
            var normalized = new List<string>();
            foreach (var raw in paths)
            {
                string p = raw.Replace('\\', '/').Trim();
                if (!p.StartsWith("Assets/")) p = "Assets/" + p.TrimStart('/');
                if (!p.EndsWith(".unity")) p += ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(p) == null) missing.Add(p);
                else if (!normalized.Contains(p)) normalized.Add(p);
            }

            if (missing.Count > 0)
            {
                return ToolUtils.CreateErrorResponse(
                    "These scenes do not exist: " + string.Join(", ", missing),
                    new Dictionary<string, object>
                    {
                        { "missingScenes", missing.ToArray() },
                        { "possible_solutions", new[]
                            {
                                "Call get_build_info to list the scenes already in Build Settings",
                                "Check the path is relative to the project root, e.g. Assets/Scenes/Main.unity",
                            }
                        },
                    });
            }

            bool append = ToolUtils.HasArg(args, "append") && ToolUtils.ParseBool(args["append"]);

            var final = new List<EditorBuildSettingsScene>();
            if (append)
            {
                // Keep existing entries, minus any the caller is re-specifying,
                // so appending never produces a duplicate entry (Unity tolerates
                // duplicates in the list but they bloat the build).
                foreach (var existing in EditorBuildSettings.scenes)
                {
                    if (!normalized.Contains(existing.path)) final.Add(existing);
                }
            }
            foreach (var p in normalized) final.Add(new EditorBuildSettingsScene(p, true));

            EditorBuildSettings.scenes = final.ToArray();

            var resulting = final.Select(s => new Dictionary<string, object>
            {
                { "path", s.path },
                { "enabled", s.enabled },
            }).ToList();

            return ToolUtils.CreateSuccessResponse(
                (append ? "Appended " : "Set ") + normalized.Count + " scene(s) in Build Settings. " +
                "Boot scene: " + final[0].path,
                new Dictionary<string, object>
                {
                    { "scenes", resulting },
                    { "bootScene", final[0].path },
                    { "sceneCount", final.Count },
                    { "next_step", "Call build_player to produce the player." },
                });
        }
    }
}
