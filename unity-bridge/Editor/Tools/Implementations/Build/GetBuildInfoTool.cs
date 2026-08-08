using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Build
{
    /// <summary>
    /// Read-only answer to "can this project be built, and to what?"
    ///
    /// Call this BEFORE build_player. Building has preconditions that all fail
    /// with unhelpful errors, and the most common one — the build-support
    /// module for the target is not installed — can only be fixed by the user
    /// in the Unity Hub (Installs > gear > Add modules). No tool can do it for
    /// them, so the agent needs to say so rather than retry the build.
    /// </summary>
    public class GetBuildInfoTool : ITool
    {
        public string Name => "get_build_info";

        public string Execute(Dictionary<string, object> args)
        {
            var installed = BuildManager.InstalledTargets();
            var active = EditorUserBuildSettings.activeBuildTarget;

            // Scenes In Build, with the enabled flag — a scene present but
            // unticked is a common "why is my level missing" cause.
            var configured = EditorBuildSettings.scenes.Select(s => new Dictionary<string, object>
            {
                { "path", s.path },
                { "enabled", s.enabled },
            }).ToList();

            string sceneSource;
            var wouldBuild = BuildManager.ResolveScenes(null, out sceneSource);

            var targets = new List<Dictionary<string, object>>();
            foreach (var t in BuildManager.KnownTargets)
            {
                targets.Add(new Dictionary<string, object>
                {
                    { "target", t.ToString() },
                    { "installed", BuildManager.IsTargetInstalled(t) },
                    { "defaultOutputPath", BuildManager.DefaultOutputPath(t) },
                });
            }

            var blockers = new List<string>();
            if (installed.Count == 0)
            {
                blockers.Add(
                    "No build-support modules are installed. Add one via Unity Hub > Installs > " +
                    "(gear) > Add modules.");
            }
            if (wouldBuild.Count == 0)
            {
                blockers.Add(
                    "No scenes would be included: File > Build Settings has none enabled and no " +
                    "saved scene is open. Save a scene, or pass scenes explicitly to build_player.");
            }

            // A deleted scene left in Build Settings is a build failure sitting
            // in the project waiting to happen, and BuildPlayer's own error
            // blames the path format rather than the missing file. Surface it
            // during recon so it never costs a build.
            var missing = BuildManager.MissingScenes(wouldBuild);
            if (missing.Count > 0)
            {
                blockers.Add(
                    "Build Settings references scenes that no longer exist: " +
                    string.Join(", ", missing) + ". Fix with set_build_scenes.");
            }

            var extras = new Dictionary<string, object>
            {
                { "activeBuildTarget", active.ToString() },
                { "installedTargets", installed.ToArray() },
                { "targets", targets },
                { "scenesInBuildSettings", configured },
                { "scenesThatWouldBuild", wouldBuild.ToArray() },
                { "missingScenes", missing.ToArray() },
                { "sceneSource", sceneSource },
                { "productName", Application.productName },
                { "companyName", Application.companyName },
                { "version", Application.version },
                { "unityVersion", Application.unityVersion },
                { "blockers", blockers.ToArray() },
                { "lastBuild", (object)BuildManager.GetResult() ?? "none" },
                { "webNote",
                    "WebGL is the shareable target: it produces a folder you can host on any " +
                    "static server. It must be served over HTTP — opening index.html from disk " +
                    "(file://) will not work." },
            };

            string message;
            if (installed.Count == 0)
            {
                message =
                    "No build-support modules are installed for Unity " + Application.unityVersion +
                    ". Building is blocked until one is added in Unity Hub > Installs > (gear) > " +
                    "Add modules. This cannot be done from here.";
            }
            else if (blockers.Count > 0)
            {
                message = "Buildable targets: " + string.Join(", ", installed) +
                          ". Not ready yet: " + string.Join(" ", blockers);
            }
            else
            {
                message = "Buildable targets: " + string.Join(", ", installed) +
                          ". " + wouldBuild.Count + " scene(s) would be included (from " +
                          sceneSource + "). Active target: " + active + ".";
            }

            return ToolUtils.CreateSuccessResponse(message, extras);
        }
    }
}
