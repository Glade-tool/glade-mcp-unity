#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace GladeAgenticAI.Core
{
    /// <summary>
    /// Syncs scripting define symbols (GLADE_UGUI, GLADE_INPUT_SYSTEM, GLADE_AI_NAVIGATION, GLADE_CINEMACHINE)
    /// with installed Unity packages so the plugin can be exported and used in projects
    /// that do not have those packages. Runs once on load and updates defines only when needed.
    /// Note: GLADE_SRP is handled via asmdef versionDefines, not PlayerSettings, to avoid stale-define deadlocks.
    /// </summary>
    [InitializeOnLoad]
    public static class GladePackageDefines
    {
        private const string DefineUGUI = "GLADE_UGUI";
        private const string DefineInputSystem = "GLADE_INPUT_SYSTEM";
        private const string DefineAINavigation = "GLADE_AI_NAVIGATION";
        private const string DefineCinemachine = "GLADE_CINEMACHINE";

        static GladePackageDefines()
        {
            EditorApplication.delayCall += () =>
            {
                var request = UnityEditor.PackageManager.Client.List(true);
                void OnUpdate()
                {
                    if (!request.IsCompleted) return;
                    EditorApplication.update -= OnUpdate;
                    if (request.Status != UnityEditor.PackageManager.StatusCode.Success)
                        return;
                    var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pkg in request.Result)
                    {
                        if (!string.IsNullOrEmpty(pkg.name))
                            installed.Add(pkg.name);
                    }
                    bool wantUgui = installed.Contains("com.unity.ugui");
                    bool wantInput = installed.Contains("com.unity.inputsystem");
                    bool wantNav = installed.Contains("com.unity.ai.navigation");
                    bool wantCinemachine = installed.Contains("com.unity.cinemachine");
                    ApplyDefines(wantUgui, wantInput, wantNav, wantCinemachine);
                }
                EditorApplication.update += OnUpdate;
            };
        }

        private static void ApplyDefines(bool wantUgui, bool wantInput, bool wantNav, bool wantCinemachine)
        {
            var targets = new[] { NamedBuildTarget.Standalone };

            foreach (var target in targets)
            {
                string current = PlayerSettings.GetScriptingDefineSymbols(target);
                var defines = new HashSet<string>(current.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));

                bool changed = false;
                if (wantUgui && !defines.Contains(DefineUGUI)) { defines.Add(DefineUGUI); changed = true; }
                else if (!wantUgui && defines.Contains(DefineUGUI)) { defines.Remove(DefineUGUI); changed = true; }
                if (wantInput && !defines.Contains(DefineInputSystem)) { defines.Add(DefineInputSystem); changed = true; }
                else if (!wantInput && defines.Contains(DefineInputSystem)) { defines.Remove(DefineInputSystem); changed = true; }
                if (wantNav && !defines.Contains(DefineAINavigation)) { defines.Add(DefineAINavigation); changed = true; }
                else if (!wantNav && defines.Contains(DefineAINavigation)) { defines.Remove(DefineAINavigation); changed = true; }
                if (wantCinemachine && !defines.Contains(DefineCinemachine)) { defines.Add(DefineCinemachine); changed = true; }
                else if (!wantCinemachine && defines.Contains(DefineCinemachine)) { defines.Remove(DefineCinemachine); changed = true; }
                // GLADE_SRP is now handled by asmdef versionDefines — remove any stale global entry
                if (defines.Remove("GLADE_SRP")) changed = true;

                if (changed)
                {
                    string newDefines = string.Join(";", defines.OrderBy(s => s));
                    PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                }
            }
        }
    }
}
#endif
