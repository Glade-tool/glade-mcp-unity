using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GladeAgenticAI.Core.Services
{
    /// <summary>
    /// Shared helpers behind the four build tools — get_build_info,
    /// set_build_scenes, build_player and get_build_status.
    ///
    /// Mirrors the Godot bridge's export_manager.gd, but Unity forces one
    /// structural difference that shapes every tool here:
    ///
    ///   Godot exports by spawning a SECOND headless engine, so the editor
    ///   stays responsive and the build is cancellable. Unity cannot do that —
    ///   a project directory is locked to one editor instance, so a second
    ///   `Unity -batchmode` on the same project refuses to open. The build has
    ///   to run IN THIS PROCESS, and BuildPipeline.BuildPlayer is a fully
    ///   synchronous main-thread call with no async variant. The editor is
    ///   frozen for its whole duration and nothing — not IAsyncTool's poll
    ///   loop, not the bridge's request pump — gets a tick until it returns.
    ///
    /// So build_player DEFERS the build to a later editor tick and answers
    /// immediately, and the result is persisted here for get_build_status to
    /// read afterwards. Without that split, the single blocking call would
    /// outlive the MCP client's ~30s timeout on any real project: the client
    /// gives up, the build still succeeds, and nobody can tell.
    /// </summary>
    public static class BuildManager
    {
        // Survives domain reloads within the editor session, which matters
        // because building can trigger a script recompile for the target
        // platform. A plain static would be wiped and the agent would be told
        // "no build has run" seconds after one finished.
        private const string StateKey = "GladeKit.BuildManager.LastBuild";

        // ── Target naming ──────────────────────────────────────────────────
        // Unity matches BuildTarget by enum, but a user (or a model relaying
        // one) says "mac" or "browser". An unmapped guess would either throw or
        // silently build the wrong platform, so resolve explicitly and report a
        // miss rather than defaulting to the active target.
        private static readonly Dictionary<string, BuildTarget> TargetAliases =
            new Dictionary<string, BuildTarget>(StringComparer.OrdinalIgnoreCase)
        {
            { "webgl", BuildTarget.WebGL },
            { "web", BuildTarget.WebGL },
            { "html", BuildTarget.WebGL },
            { "html5", BuildTarget.WebGL },
            { "browser", BuildTarget.WebGL },
            { "itch", BuildTarget.WebGL },
            { "mac", BuildTarget.StandaloneOSX },
            { "macos", BuildTarget.StandaloneOSX },
            { "mac os", BuildTarget.StandaloneOSX },
            { "osx", BuildTarget.StandaloneOSX },
            { "standaloneosx", BuildTarget.StandaloneOSX },
            { "apple", BuildTarget.StandaloneOSX },
            { "windows", BuildTarget.StandaloneWindows64 },
            { "windows desktop", BuildTarget.StandaloneWindows64 },
            { "win", BuildTarget.StandaloneWindows64 },
            { "win64", BuildTarget.StandaloneWindows64 },
            { "pc", BuildTarget.StandaloneWindows64 },
            { "exe", BuildTarget.StandaloneWindows64 },
            { "standalonewindows64", BuildTarget.StandaloneWindows64 },
            { "linux", BuildTarget.StandaloneLinux64 },
            { "linux64", BuildTarget.StandaloneLinux64 },
            { "standalonelinux64", BuildTarget.StandaloneLinux64 },
            { "android", BuildTarget.Android },
            { "apk", BuildTarget.Android },
            { "ios", BuildTarget.iOS },
            { "iphone", BuildTarget.iOS },
            { "ipad", BuildTarget.iOS },
        };

        /// <summary>Targets we advertise and can resolve. Ordered for reporting.</summary>
        public static readonly BuildTarget[] KnownTargets =
        {
            BuildTarget.WebGL,
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneLinux64,
            BuildTarget.Android,
            BuildTarget.iOS,
        };

        /// <summary>
        /// Resolve whatever the caller typed to a BuildTarget. Returns false
        /// when there is no confident match so the caller can list the valid
        /// options instead of building the wrong platform.
        /// </summary>
        public static bool TryResolveTarget(string raw, out BuildTarget target)
        {
            target = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            string key = raw.Trim();

            BuildTarget aliased;
            if (TargetAliases.TryGetValue(key, out aliased))
            {
                target = aliased;
                return true;
            }

            // Exact enum spelling, but matched ONLY against the targets we
            // actually support.
            //
            // The first version fell back to Enum.TryParse + Enum.IsDefined.
            // UnityEditor.BuildTarget still carries a long tail of legacy and
            // obsolete platforms, so that fallback would accept names we cannot
            // build and hand BuildPlayer a dead target. Whitelisting is the
            // conservative reading and costs nothing: six supported targets,
            // matched exactly, everything else rejected with the list.
            foreach (var known in KnownTargets)
            {
                if (string.Equals(known.ToString(), key, StringComparison.OrdinalIgnoreCase))
                {
                    target = known;
                    return true;
                }
            }
            return false;
        }

        public static BuildTargetGroup GroupFor(BuildTarget target)
        {
            return BuildPipeline.GetBuildTargetGroup(target);
        }

        /// <summary>
        /// Is the build-support module for this target actually installed?
        /// This is the Unity analog of Godot's missing export templates and the
        /// single most common reason a build fails — and, like them, the fix is
        /// a Unity Hub action no tool can perform for the user.
        /// </summary>
        public static bool IsTargetInstalled(BuildTarget target)
        {
            try
            {
                return BuildPipeline.IsBuildTargetSupported(GroupFor(target), target);
            }
            catch
            {
                return false;
            }
        }

        public static List<string> InstalledTargets()
        {
            var names = new List<string>();
            foreach (var t in KnownTargets)
            {
                if (IsTargetInstalled(t)) names.Add(t.ToString());
            }
            return names;
        }

        // ── Output paths ───────────────────────────────────────────────────
        // The extension is load-bearing: Unity infers the player layout from
        // it. WebGL and iOS build into a DIRECTORY (no extension); the desktop
        // targets want a file.
        public static string ExtensionFor(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64: return ".exe";
                case BuildTarget.StandaloneOSX: return ".app";
                case BuildTarget.StandaloneLinux64: return ".x86_64";
                case BuildTarget.Android: return ".apk";
                case BuildTarget.WebGL: return "";   // directory
                case BuildTarget.iOS: return "";     // Xcode project directory
                default: return "";
            }
        }

        public static string SubdirFor(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64: return "Windows";
                case BuildTarget.StandaloneOSX: return "macOS";
                case BuildTarget.StandaloneLinux64: return "Linux";
                case BuildTarget.Android: return "Android";
                case BuildTarget.WebGL: return "WebGL";
                case BuildTarget.iOS: return "iOS";
                default: return target.ToString();
            }
        }

        /// <summary>
        /// Default output path, absolute, outside Assets/. Builds must NOT land
        /// under Assets/ — Unity would import the entire player as project
        /// assets on the next refresh.
        /// </summary>
        public static string DefaultOutputPath(BuildTarget target)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string stem = SanitizeStem(Application.productName);
            string dir = Path.Combine(projectRoot, "Builds", SubdirFor(target));
            string ext = ExtensionFor(target);
            // WebGL/iOS take a folder, not a file inside one.
            if (string.IsNullOrEmpty(ext)) return dir;
            return Path.Combine(dir, stem + ext);
        }

        private const string StemSafe =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Product names routinely carry spaces and punctuation ("My Game (Demo)");
        /// an unsanitized stem produces a shell-hostile and sometimes illegal path.
        /// </summary>
        public static string SanitizeStem(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName)) return "Game";
            var sb = new System.Text.StringBuilder();
            foreach (char c in productName.Trim())
            {
                if (StemSafe.IndexOf(c) >= 0) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_' || c == '.') sb.Append('_');
            }
            string outName = sb.ToString();
            while (outName.Contains("__")) outName = outName.Replace("__", "_");
            outName = outName.Trim('_');
            return string.IsNullOrEmpty(outName) ? "Game" : outName;
        }

        // ── Scenes ─────────────────────────────────────────────────────────

        /// <summary>
        /// Scenes in the list that do not exist on disk.
        ///
        /// Build Settings happily keeps pointing at a scene after it is deleted,
        /// and BuildPlayer's own diagnosis is actively misleading: it reports
        /// "'Assets/Scenes/X.unity' is an incorrect path for a scene file.
        /// BuildPlayer expects paths relative to the project folder" — blaming
        /// the path FORMAT when the path is perfectly well-formed and the file
        /// simply is not there. Measured on the dogfood project, which had a
        /// stale SampleScene entry: an agent reading that error would rewrite a
        /// correct path over and over. Catching it here costs microseconds and
        /// replaces an 11-second failed build with an accurate sentence.
        /// </summary>
        public static List<string> MissingScenes(IEnumerable<string> scenePaths)
        {
            var missing = new List<string>();
            if (scenePaths == null) return missing;
            foreach (var p in scenePaths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(p) == null && !File.Exists(p))
                    missing.Add(p);
            }
            return missing;
        }

        /// <summary>Enabled scenes from File > Build Settings, in order.</summary>
        public static List<string> EnabledBuildScenes()
        {
            return EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToList();
        }

        /// <summary>
        /// Which scenes a build should contain, and where that list came from.
        /// A brand-new project has NOTHING in Build Settings, so a literal
        /// reading ("build what's configured") makes "build my game" dead-end on
        /// an empty player. Falling back to the open scene is what the user
        /// means, and `source` reports the choice so it is never silent.
        /// </summary>
        public static List<string> ResolveScenes(List<string> explicitScenes, out string source)
        {
            if (explicitScenes != null && explicitScenes.Count > 0)
            {
                source = "explicit";
                return explicitScenes;
            }

            var configured = EnabledBuildScenes();
            if (configured.Count > 0)
            {
                source = "buildSettings";
                return configured;
            }

            var open = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (open.IsValid() && !string.IsNullOrEmpty(open.path))
            {
                source = "openScene";
                return new List<string> { open.path };
            }

            source = "none";
            return new List<string>();
        }

        // ── Persisted build state ──────────────────────────────────────────

        // Discrete keys rather than one serialized blob. SessionState only
        // stores scalars, so a blob would mean hand-rolling both a JSON writer
        // AND a reader here — and the reader's output would then be re-encoded
        // by ToolUtils on the way out, giving the reported result two chances to
        // drift from what the build actually produced. Scalars round-trip
        // exactly and need neither.
        private const string KStatus = StateKey + ".status";
        private const string KTarget = StateKey + ".target";
        private const string KOutput = StateKey + ".outputPath";
        private const string KResult = StateKey + ".result";
        private const string KErrors = StateKey + ".errors";
        private const string KSize = StateKey + ".sizeBytes";
        private const string KSeconds = StateKey + ".buildSeconds";
        private const string KErrCount = StateKey + ".totalErrors";
        private const string KWarnCount = StateKey + ".totalWarnings";
        private const string KExists = StateKey + ".outputExists";

        // Errors are a list; SessionState is scalar-only. Newline is safe as a
        // separator because Unity build messages are single-line.
        private const string ErrorSeparator = "\n";

        public static void SetRunning(string target, string outputPath)
        {
            SessionState.SetString(KStatus, "running");
            SessionState.SetString(KTarget, target);
            SessionState.SetString(KOutput, outputPath);
            SessionState.SetString(KResult, "");
            SessionState.SetString(KErrors, "");
            SessionState.SetString(KSize, "0");
            SessionState.SetFloat(KSeconds, 0f);
            SessionState.SetInt(KErrCount, 0);
            SessionState.SetInt(KWarnCount, 0);
            SessionState.SetBool(KExists, false);
        }

        public static void SetResult(Dictionary<string, object> payload)
        {
            SessionState.SetString(KStatus, Str(payload, "status", "failed"));
            SessionState.SetString(KTarget, Str(payload, "target", ""));
            SessionState.SetString(KOutput, Str(payload, "outputPath", ""));
            SessionState.SetString(KResult, Str(payload, "result", ""));
            SessionState.SetString(KSize, Str(payload, "sizeBytes", "0"));
            SessionState.SetBool(KExists, payload.ContainsKey("outputExists") &&
                                          Convert.ToBoolean(payload["outputExists"]));
            SessionState.SetInt(KErrCount, IntOf(payload, "totalErrors"));
            SessionState.SetInt(KWarnCount, IntOf(payload, "totalWarnings"));
            float secs = 0f;
            if (payload.ContainsKey("buildSeconds"))
                float.TryParse(Convert.ToString(payload["buildSeconds"],
                    System.Globalization.CultureInfo.InvariantCulture), out secs);
            SessionState.SetFloat(KSeconds, secs);

            var errs = payload.ContainsKey("errors") ? payload["errors"] as string[] : null;
            SessionState.SetString(KErrors, errs == null ? "" : string.Join(ErrorSeparator, errs));
        }

        /// <summary>Last build's state, or null if none has run this session.</summary>
        public static Dictionary<string, object> GetResult()
        {
            string status = SessionState.GetString(KStatus, "");
            if (string.IsNullOrEmpty(status)) return null;

            long size = 0;
            long.TryParse(SessionState.GetString(KSize, "0"), out size);
            string errBlob = SessionState.GetString(KErrors, "");

            return new Dictionary<string, object>
            {
                { "status", status },
                { "target", SessionState.GetString(KTarget, "") },
                { "outputPath", SessionState.GetString(KOutput, "") },
                { "result", SessionState.GetString(KResult, "") },
                { "outputExists", SessionState.GetBool(KExists, false) },
                { "sizeBytes", size },
                { "sizeHuman", HumanSize(size) },
                { "buildSeconds", Math.Round(SessionState.GetFloat(KSeconds, 0f), 2) },
                { "totalErrors", SessionState.GetInt(KErrCount, 0) },
                { "totalWarnings", SessionState.GetInt(KWarnCount, 0) },
                { "errors", string.IsNullOrEmpty(errBlob)
                    ? new string[0]
                    : errBlob.Split(new[] { ErrorSeparator }, StringSplitOptions.RemoveEmptyEntries) },
            };
        }

        public static bool IsRunning()
        {
            return SessionState.GetString(KStatus, "") == "running";
        }

        /// <summary>
        /// A build is scheduled onto an EditorApplication.update callback, which
        /// does NOT survive a domain reload — but the "running" flag lives in
        /// SessionState, which does. A reload between scheduling and finishing
        /// (a script edit, an asset import) would therefore strand the state at
        /// "running" forever: get_build_status would report a build in progress
        /// that no longer exists, and every future build_player would refuse to
        /// start because one is "already in progress".
        ///
        /// Reconcile at load: if we come up with a build marked running, the
        /// callback that would have finished it is gone, so report it as
        /// interrupted rather than leaving a permanent lie.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void ReconcileInterruptedBuild()
        {
            if (!IsRunning()) return;
            SessionState.SetString(KStatus, "failed");
            SessionState.SetString(KResult, "Interrupted");
            SessionState.SetString(KErrors,
                "The editor reloaded its script domain before the build finished, so the build " +
                "was abandoned. This usually means a script changed or an asset was imported " +
                "mid-build. Re-run build_player.");
        }

        public static void Clear()
        {
            foreach (var k in new[] { KStatus, KTarget, KOutput, KResult, KErrors, KSize })
                SessionState.EraseString(k);
            SessionState.EraseInt(KErrCount);
            SessionState.EraseInt(KWarnCount);
            SessionState.EraseFloat(KSeconds);
            SessionState.EraseBool(KExists);
        }

        private static string Str(Dictionary<string, object> d, string key, string fallback)
        {
            if (!d.ContainsKey(key) || d[key] == null) return fallback;
            return Convert.ToString(d[key], System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int IntOf(Dictionary<string, object> d, string key)
        {
            if (!d.ContainsKey(key) || d[key] == null) return 0;
            int v;
            return int.TryParse(Convert.ToString(d[key],
                System.Globalization.CultureInfo.InvariantCulture), out v) ? v : 0;
        }

        // ── Report interpretation ──────────────────────────────────────────

        /// <summary>
        /// Turn a BuildReport into the tool payload. Success is taken from
        /// summary.result AND a real artifact on disk — Unity can report a
        /// non-Failed result while leaving nothing usable behind, and an agent
        /// told "built" with no file is worse than a clean failure.
        /// </summary>
        public static Dictionary<string, object> DescribeReport(BuildReport report, string outputPath)
        {
            var payload = new Dictionary<string, object>();
            var summary = report.summary;

            bool artifactExists = File.Exists(outputPath) || Directory.Exists(outputPath);
            long size = MeasureOutput(outputPath);

            payload["target"] = summary.platform.ToString();
            payload["outputPath"] = outputPath;
            payload["outputExists"] = artifactExists;
            payload["result"] = summary.result.ToString();
            payload["totalErrors"] = summary.totalErrors;
            payload["totalWarnings"] = summary.totalWarnings;
            payload["buildSeconds"] = Math.Round(summary.totalTime.TotalSeconds, 2);
            payload["sizeBytes"] = size;
            payload["sizeHuman"] = HumanSize(size);
            payload["errors"] = CollectMessages(report, LogType.Error)
                .Concat(CollectMessages(report, LogType.Exception))
                .Take(20).ToArray();

            bool ok = summary.result == BuildResult.Succeeded && artifactExists && size > 0;
            payload["status"] = ok ? "succeeded" : "failed";
            return payload;
        }

        public static List<string> CollectMessages(BuildReport report, LogType type)
        {
            var msgs = new List<string>();
            if (report == null || report.steps == null) return msgs;
            foreach (var step in report.steps)
            {
                if (step.messages == null) continue;
                foreach (var m in step.messages)
                {
                    if (m.type == type && !string.IsNullOrEmpty(m.content) && !msgs.Contains(m.content))
                        msgs.Add(m.content);
                }
            }
            return msgs;
        }

        /// <summary>
        /// Size of the produced player. WebGL, iOS and macOS .app builds are
        /// DIRECTORIES, so a File-only measurement would report 0 bytes on
        /// exactly the platforms people most want to share.
        /// </summary>
        public static long MeasureOutput(string path)
        {
            try
            {
                if (File.Exists(path)) return new FileInfo(path).Length;
                if (!Directory.Exists(path)) return 0;
                long total = 0;
                foreach (var f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(f).Length; } catch { /* unreadable file, skip */ }
                }
                return total;
            }
            catch
            {
                return 0;
            }
        }

        public static string HumanSize(long numBytes)
        {
            if (numBytes < 1024) return numBytes + " B";
            double kb = numBytes / 1024.0;
            if (kb < 1024.0) return kb.ToString("0.0") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024.0) return mb.ToString("0.0") + " MB";
            return (mb / 1024.0).ToString("0.00") + " GB";
        }

    }
}
