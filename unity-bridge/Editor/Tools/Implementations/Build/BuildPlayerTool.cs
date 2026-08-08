using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using GladeAgenticAI.Core.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Build
{
    /// <summary>
    /// Build the project into a distributable player — the .exe, .app, .apk or
    /// hostable WebGL folder the user actually ships. The terminal step of
    /// every project.
    ///
    /// THE BUILD IS DEFERRED, AND THAT IS THE WHOLE DESIGN.
    /// BuildPipeline.BuildPlayer is synchronous, runs on the main thread, and
    /// has no async variant; Unity also refuses to open a project in a second
    /// instance, so — unlike the Godot side — there is no subprocess to push it
    /// into. The editor is frozen for the entire build and the bridge's request
    /// pump gets no tick. A blocking tool call would therefore outlive the MCP
    /// client's ~30s timeout on any real project: the client gives up, the
    /// build still runs to completion, and nobody can tell whether it worked.
    ///
    /// So this validates everything it can up front, schedules the build on the
    /// next editor tick, and answers immediately. get_build_status reports the
    /// outcome afterwards. The first poll that lands mid-build will itself block
    /// until the editor is free again — that is inherent to Unity, and the tool
    /// says so rather than pretending otherwise.
    /// </summary>
    public class BuildPlayerTool : ITool
    {
        public string Name => "build_player";

        public string Execute(Dictionary<string, object> args)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return ToolUtils.CreateErrorResponse(
                    "Cannot build while the editor is in Play mode. Stop play mode and retry.");
            }

            if (BuildManager.IsRunning())
            {
                return ToolUtils.CreateErrorResponse(
                    "A build is already in progress. Call get_build_status to wait for it.");
            }

            // ── Target ──
            string rawTarget = ToolUtils.GetStringArg(args, "target", "");
            BuildTarget target;
            if (string.IsNullOrWhiteSpace(rawTarget))
            {
                target = EditorUserBuildSettings.activeBuildTarget;
            }
            else if (!BuildManager.TryResolveTarget(rawTarget, out target))
            {
                return ToolUtils.CreateErrorResponse(
                    "Unknown build target '" + rawTarget + "'",
                    new Dictionary<string, object>
                    {
                        { "possible_solutions", new[]
                            {
                                "Use one of: WebGL, StandaloneWindows64, StandaloneOSX, " +
                                "StandaloneLinux64, Android, iOS (aliases like 'web', 'windows', " +
                                "'mac' are accepted)",
                                "Call get_build_info to see which targets are installed",
                            }
                        },
                    });
            }

            // ── Preconditions, checked BEFORE freezing the editor ──
            if (!BuildManager.IsTargetInstalled(target))
            {
                var installed = BuildManager.InstalledTargets();
                return ToolUtils.CreateErrorResponse(
                    "Build support for " + target + " is not installed.",
                    new Dictionary<string, object>
                    {
                        { "installedTargets", installed.ToArray() },
                        { "possible_solutions", new[]
                            {
                                "Install it via Unity Hub > Installs > (gear icon) > Add modules > " +
                                target + ". This cannot be done from here.",
                                "Installed targets you can build right now: " +
                                (installed.Count > 0 ? string.Join(", ", installed) : "(none)"),
                            }
                        },
                    });
            }

            var explicitScenes = ToolUtils.HasArg(args, "scenes")
                ? ToolUtils.GetPathsFromArgsOrSelection(args, "scenes")
                : null;
            string sceneSource;
            var scenes = BuildManager.ResolveScenes(explicitScenes, out sceneSource);
            if (scenes.Count == 0)
            {
                return ToolUtils.CreateErrorResponse(
                    "No scenes to build: Build Settings has none enabled and no saved scene is open.",
                    new Dictionary<string, object>
                    {
                        { "possible_solutions", new[]
                            {
                                "Call set_build_scenes with the scenes to include (index 0 boots first)",
                                "Or save the currently open scene and retry",
                            }
                        },
                    });
            }

            // Fail here, not 11 seconds into a build, and say what is actually
            // wrong — BuildPlayer blames the path format for a missing file.
            var missing = BuildManager.MissingScenes(scenes);
            if (missing.Count > 0)
            {
                return ToolUtils.CreateErrorResponse(
                    "These scenes are listed for the build but do not exist: " +
                    string.Join(", ", missing) +
                    ". (Build Settings keeps pointing at scenes after they are deleted.)",
                    new Dictionary<string, object>
                    {
                        { "missingScenes", missing.ToArray() },
                        { "sceneSource", sceneSource },
                        { "possible_solutions", new[]
                            {
                                "Call set_build_scenes with scenes that exist — get_build_info lists them",
                                "Or pass `scenes` explicitly to build_player for a one-off build",
                            }
                        },
                    });
            }

            // ── Output path ──
            string outputPath = ToolUtils.GetStringArg(args, "outputPath", "");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = BuildManager.DefaultOutputPath(target);
            }
            else if (!Path.IsPathRooted(outputPath))
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                outputPath = Path.GetFullPath(Path.Combine(projectRoot, outputPath));
            }

            // A player written under Assets/ gets imported as project assets on
            // the next refresh — thousands of files, a very long reimport, and a
            // polluted project. Refuse rather than "help".
            string assetsRoot = Path.GetFullPath(Application.dataPath);
            if (Path.GetFullPath(outputPath).StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                return ToolUtils.CreateErrorResponse(
                    "outputPath must not be inside Assets/ — Unity would import the whole player " +
                    "as project assets. Use a sibling folder such as Builds/.");
            }

            // Create the PARENT only. For macOS the path itself is the .app
            // bundle and for WebGL/iOS it is the build folder Unity creates —
            // pre-creating those can confuse the builder.
            try
            {
                string parent = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                    Directory.CreateDirectory(parent);
            }
            catch (Exception e)
            {
                return ToolUtils.CreateErrorResponse(
                    "Could not create the output directory for '" + outputPath + "': " + e.Message);
            }

            bool development = ToolUtils.HasArg(args, "development") &&
                               ToolUtils.ParseBool(args["development"]);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = outputPath,
                target = target,
                targetGroup = BuildManager.GroupFor(target),
                options = development
                    ? (BuildOptions.Development | BuildOptions.AllowDebugging)
                    : BuildOptions.None,
            };

            BuildManager.SetRunning(target.ToString(), outputPath);

            // Hand the build to a later tick so this response is written and
            // flushed before the editor locks up.
            //
            // NOT EditorApplication.delayCall. That was the first attempt and it
            // silently never fired: the tool reported "Build started", the state
            // said "running", and Unity's log showed no build activity at all —
            // a build that never happened, reported as one in progress.
            // EditorApplication.update is the tick the bridge's own request pump
            // already runs on, so it is proven to fire in this exact context.
            ScheduleBuild(options, outputPath);

            return ToolUtils.CreateSuccessResponse(
                "Build started: " + target + " -> " + outputPath + " (" + scenes.Count +
                " scene(s) from " + sceneSource + "). The Unity editor is frozen until it " +
                "finishes; call get_build_status for the result.",
                new Dictionary<string, object>
                {
                    { "status", "started" },
                    { "target", target.ToString() },
                    { "outputPath", outputPath },
                    { "scenes", scenes.ToArray() },
                    { "sceneSource", sceneSource },
                    { "development", development },
                    { "next_step",
                        "Call get_build_status. Unity blocks its own main thread while building, " +
                        "so a poll sent mid-build will not answer until the build ends — that is " +
                        "expected, not a hang. Retry if the call times out." },
                });
        }

        // One-shot deferral onto the editor's update tick. Static because the
        // tool instance is registry-owned and must not capture per-call state
        // across a tick boundary.
        private static BuildPlayerOptions _pendingOptions;
        private static string _pendingOutput;
        private static bool _scheduled;

        private static void ScheduleBuild(BuildPlayerOptions options, string outputPath)
        {
            _pendingOptions = options;
            _pendingOutput = outputPath;
            if (_scheduled) return;
            _scheduled = true;
            EditorApplication.update += PumpOnce;
        }

        private static void PumpOnce()
        {
            // Unsubscribe FIRST: BuildPlayer blocks this thread for minutes, and
            // a still-subscribed handler would re-enter and start a second build
            // the moment the first returns.
            EditorApplication.update -= PumpOnce;
            _scheduled = false;
            var options = _pendingOptions;
            string outputPath = _pendingOutput;
            _pendingOutput = null;
            RunBuild(options, outputPath);
        }

        private static void RunBuild(BuildPlayerOptions options, string outputPath)
        {
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var payload = BuildManager.DescribeReport(report, outputPath);
                BuildManager.SetResult(payload);

                if (payload.ContainsKey("status") && (string)payload["status"] == "succeeded")
                {
                    Debug.Log("[GladeKit] Build succeeded: " + outputPath +
                              " (" + payload["sizeHuman"] + ")");
                }
                else
                {
                    Debug.LogError("[GladeKit] Build failed: " + outputPath +
                                   " — result=" + payload["result"]);
                }
            }
            catch (Exception e)
            {
                // BuildPlayer throws on some misconfigurations rather than
                // returning a Failed report; without this the persisted state
                // would stay "running" forever and get_build_status would lie.
                BuildManager.SetResult(new Dictionary<string, object>
                {
                    { "status", "failed" },
                    { "target", options.target.ToString() },
                    { "outputPath", outputPath },
                    { "result", "Exception" },
                    { "errors", new[] { e.Message } },
                    { "sizeBytes", 0 },
                    { "sizeHuman", "0 B" },
                    { "buildSeconds", 0 },
                });
                Debug.LogError("[GladeKit] Build threw: " + e);
            }
        }
    }
}
