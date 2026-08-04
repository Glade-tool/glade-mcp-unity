using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    // This namespace has sibling sub-namespaces `...Implementations.GameObject`
    // and `...Implementations.Camera`. When resolving a bare `GameObject` /
    // `Camera`, C# searches enclosing namespaces (here, `...Implementations`)
    // for a matching member BEFORE it ever consults a file-level using-alias, so
    // an alias placed outside this block is shadowed by those namespaces and the
    // name binds to the namespace (CS0118). A using-alias declared INSIDE the
    // namespace body is consulted first, so it correctly wins — alias both names
    // to their Unity types here rather than fully qualifying every usage.
    using GameObject = UnityEngine.GameObject;
    using Camera = UnityEngine.Camera;

    /// <summary>
    /// Produces a complete, playable third-person player in ONE call. It copies
    /// two vetted template scripts VERBATIM — ThirdPersonController.cs
    /// (CharacterController movement + grounded jump) and FollowCamera.cs
    /// (modern mouse / right-stick orbit camera) — then assembles the scene around them:
    /// ensures a Player capsule and a Main Camera exist, adds the built-in
    /// CharacterController immediately, and QUEUES the two custom components to
    /// attach automatically once the scripts compile.
    ///
    /// Why a template tool instead of generating the controller from scratch:
    /// an AI client asked to write a third-person controller tends to re-derive
    /// subtly-broken gameplay code — most commonly a self-referential camera
    /// offset that makes the player circle, and a fragile grounded-check that
    /// blocks the jump. Both compile cleanly and look correct, so they slip past
    /// a quick review and only show up in Play mode. Copying known-good code
    /// verbatim removes that failure mode.
    ///
    /// Why this tool is ATOMIC (does the wiring itself, see
    /// <see cref="PendingControllerWiring"/>): even with verbatim scripts, the
    /// caller used to have to wait for compilation and then issue separate
    /// add_component calls — a multi-step contract an AI client frequently
    /// dropped under load, leaving a bare capsule with no controller. A
    /// MonoBehaviour type cannot be AddComponent'd until its script has compiled
    /// AND a domain reload has loaded the new assembly, so this tool can't attach
    /// the custom components synchronously. Instead it queues them; Unity's normal
    /// compile → reload cycle fires the attach. The caller has nothing left to do
    /// but let compilation finish.
    ///
    /// The .cs.txt template files in Editor/Tools/Templates/ are the single
    /// source of truth for the script bodies.
    /// </summary>
    public class CreateThirdPersonControllerScriptTool : ITool
    {
        public string Name => "create_third_person_controller";

        // (template file name on disk, written file name in the project)
        private static readonly (string template, string scriptName)[] Scripts =
        {
            ("ThirdPersonController.cs.txt", "ThirdPersonController.cs"),
            ("FollowCamera.cs.txt", "FollowCamera.cs"),
        };

        public string Execute(Dictionary<string, object> args)
        {
            // Where to write the scripts. Default mirrors the project convention.
            string directory = ToolUtils.GetStringArg(args, "directory", "Assets/Scripts");
            bool confirmExistingFileModification =
                ToolUtils.GetBoolArg(args, "confirmExistingFileModification", false);
            string playerName = ToolUtils.GetStringArg(args, "playerName", "Player");
            if (string.IsNullOrEmpty(playerName)) playerName = "Player";
            bool createGround = ToolUtils.GetBoolArg(args, "createGround", true);

            if (string.IsNullOrEmpty(directory)) directory = "Assets/Scripts";
            directory = directory.Replace('\\', '/').TrimEnd('/');
            if (!directory.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                && !directory.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                directory = "Assets/" + directory;
            }

            // Resolve every template up front so a missing-template failure happens
            // before we write anything (no half-written controller).
            var resolved = new List<(string templatePath, string scriptName)>();
            foreach (var (template, scriptName) in Scripts)
            {
                string templatePath = ToolUtils.ResolveTemplatePath(template);
                if (string.IsNullOrEmpty(templatePath))
                {
                    return ToolUtils.CreateErrorResponse(
                        $"Template '{template}' could not be found in any known bridge location. " +
                        "The bridge install may be incomplete — reinstall com.gladekit.mcp-bridge.");
                }
                resolved.Add((templatePath, scriptName));
            }

            // ── Reuse-don't-refuse, PROJECT-WIDE (see ResolveTarget) ──────────
            // Resolve every target before writing anything, so a scaffold never
            // half-lands.
            var targets = new List<ScriptTarget>();
            foreach (var (templatePath, scriptName) in resolved)
            {
                targets.Add(ResolveTarget(templatePath, scriptName, directory,
                    confirmExistingFileModification));
            }

            var createdScripts = new List<string>();   // paths this call WROTE
            var scriptPaths = new List<string>();      // paths in play (written or reused)
            var reuseNotes = new List<string>();
            var collisionWarnings = new List<string>();

            foreach (var t in targets)
            {
                scriptPaths.Add(t.Path);

                if (t.DuplicatePaths.Count > 1)
                {
                    // Pre-existing breakage we did not cause, but the caller can't
                    // diagnose CS0101 from the console alone — name the files.
                    collisionWarnings.Add(
                        $"'{t.ScriptName}' exists {t.DuplicatePaths.Count} times in this project " +
                        $"({string.Join(", ", t.DuplicatePaths)}). Unity compiles every script under " +
                        "Assets/ into one assembly, so duplicate class names fail to compile (CS0101). " +
                        "Delete all but one copy.");
                }

                if (!t.ShouldWrite)
                {
                    if (t.IsIdenticalToTemplate)
                    {
                        // Byte-identical to the template, so it IS our content — there
                        // is no user authorship to protect, and a follow-up
                        // modify_script (e.g. tuning moveSpeed) shouldn't be refused.
                        SessionTracker.MarkScriptCreated(t.Path);
                        reuseNotes.Add($"reused existing {t.Path} (already identical to the vetted template)");
                    }
                    else
                    {
                        // Deliberately NOT marked session-created: this is user-authored
                        // code we chose not to clobber, so the overwrite guard must keep
                        // protecting it from a later modify_script.
                        reuseNotes.Add(
                            $"reused existing {t.Path} — it DIFFERS from the vetted template, so it was " +
                            "left untouched. If the player doesn't move or the camera doesn't follow, " +
                            "that script is the reason; pass confirmExistingFileModification=true to " +
                            "replace it with the vetted version.");
                    }
                    continue;
                }

                string dir = Path.GetDirectoryName(t.Path).Replace('\\', '/');
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(t.Path, File.ReadAllText(t.TemplatePath));
                // Mark so a follow-up create_script / modify_script on this path is
                // recognized as session-created and not refused by the guard.
                SessionTracker.MarkScriptCreated(t.Path);
                createdScripts.Add(t.Path);
                if (t.Existed) reuseNotes.Add($"replaced {t.Path} with the vetted template (confirmed)");
            }

            // ── Assemble the scene around the scripts (the ATOMIC part) ───────
            // Everything below uses only built-in types (Capsule/CharacterController/
            // Camera/Plane), so it can run synchronously right now. The two custom
            // MonoBehaviours can't exist until the scripts above compile, so they
            // are QUEUED for PendingControllerWiring to attach after the reload.
            var sceneNotes = new List<string>();
            GameObject player = EnsurePlayer(playerName, sceneNotes);
            GameObject camera = EnsureMainCamera(sceneNotes);
            if (createGround) EnsureGround(sceneNotes);

            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest(player.name, "Player", "ThirdPersonController"),
                new PendingControllerWiring.WiringRequest(camera.name, "MainCamera", "FollowCamera"),
            });

            // The created GameObjects survive the impending domain reload (edit-mode
            // scene objects persist across assembly reloads), but mark the scene
            // dirty so the changes are recognised as part of the open scene.
            try { EditorSceneManager.MarkSceneDirty(camera.scene); } catch { /* no open scene */ }

            // Triggers compile + domain reload; PendingControllerWiring fires on the
            // afterAssemblyReload that follows, attaching the two components.
            AssetDatabase.Refresh(ImportAssetOptions.Default);

            var extras = new Dictionary<string, object>
            {
                { "createdScripts", createdScripts },
                { "scriptPaths", scriptPaths },
                { "reusedScripts", reuseNotes },
                { "requiresCompilation", true },
                { "playerObject", player.name },
                { "cameraObject", camera.name },
                { "sceneSetup", sceneNotes },
                {
                    "queuedComponents",
                    new List<string>
                    {
                        $"ThirdPersonController → {player.name}",
                        $"FollowCamera → {camera.name}",
                    }
                },
                {
                    "wiring",
                    "Automatic — this tool already created the Player + Main Camera, added " +
                    "CharacterController to the Player, and queued ThirdPersonController + FollowCamera " +
                    "to attach the moment the scripts finish compiling. The scripts self-resolve their " +
                    "references (ThirdPersonController → Camera.main, FollowCamera → the 'Player' tag), " +
                    "so no object-reference wiring is needed."
                },
            };
            if (collisionWarnings.Count > 0)
            {
                extras["duplicateScriptWarnings"] = collisionWarnings;
            }

            // Lead with what the caller must ACT on. A reuse is normal and needs no
            // action; a pre-existing duplicate is the one thing that will fail the
            // compile no matter what this tool did, so it goes first when present.
            string headline = collisionWarnings.Count > 0
                ? "Set up the third-person player, but this project has DUPLICATE script files that " +
                  "will fail to compile until you delete the extra copies: " +
                  string.Join(" ", collisionWarnings) + " "
                : $"Set up a complete, playable third-person controller (scripts: {string.Join(", ", scriptPaths)}). ";

            return ToolUtils.CreateSuccessResponse(
                headline +
                (reuseNotes.Count > 0 ? string.Join("; ", reuseNotes) + ". " : "") +
                "This tool is ATOMIC — the two vetted scripts are in place, a Player capsule and a " +
                "Main Camera exist, CharacterController is on the Player, and ThirdPersonController + " +
                "FollowCamera are QUEUED to attach as soon as the scripts compile. " +
                "DO NOT call add_component for these components — that happens for you on the next compile, " +
                "and the scripts self-resolve their references so no object-reference wiring is needed. " +
                "DO NOT write these scripts to another folder — Unity compiles all of Assets/ into one " +
                "assembly, so a second copy of either class is a guaranteed compile error. " +
                "Your ONLY remaining step is to call compile_scripts and wait until status='idle'; after that " +
                "the player moves with WASD and jumps with Space, and the camera follows.",
                extras);
        }

        // ── Target resolution (reuse-don't-refuse) ───────────────────────────

        /// <summary>Where one template script will land, and why.</summary>
        private sealed class ScriptTarget
        {
            public string ScriptName;
            public string TemplatePath;
            public string Path;                 // the ONE path in play for this script
            public bool Existed;                // a file was already at Path
            public bool ShouldWrite;            // false => reuse in place, write nothing
            public bool IsIdenticalToTemplate;  // only meaningful when Existed
            public List<string> DuplicatePaths = new List<string>();
        }

        /// <summary>
        /// Decide the single path this script occupies, reusing whatever is already
        /// in the project instead of writing a second copy.
        ///
        /// Why project-wide and not just <paramref name="directory"/>: the vetted
        /// templates declare NO namespace, so every script under Assets/ lands in
        /// Assembly-CSharp's global namespace. Two files named
        /// ThirdPersonController.cs are therefore two definitions of the same type
        /// (CS0101) no matter which folders they sit in — writing "somewhere else"
        /// to avoid clobbering is not a safe fallback, it is a guaranteed compile
        /// break. (This tool used to refuse an existing file and tell the caller to
        /// "pass a different directory"; callers dutifully did, and the build broke
        /// every time.)
        ///
        /// Policy: reuse an existing same-named script wherever it lives. Only
        /// overwrite it when the caller explicitly confirms, and even then overwrite
        /// THAT file in place rather than adding a copy. Write fresh into
        /// <paramref name="directory"/> only when the project has no such script.
        /// </summary>
        private static ScriptTarget ResolveTarget(string templatePath, string scriptName,
            string directory, bool confirmOverwrite)
        {
            var target = new ScriptTarget
            {
                ScriptName = scriptName,
                TemplatePath = templatePath,
                Path = $"{directory}/{scriptName}",
            };

            target.DuplicatePaths = Gameplay.GameplayScaffold.FindExistingScripts(scriptName);

            // Prefer a copy already sitting at the requested path; otherwise adopt
            // whichever one the project has, so we never add a second definition.
            string existing = null;
            foreach (string p in target.DuplicatePaths)
            {
                if (string.Equals(p, target.Path, StringComparison.OrdinalIgnoreCase))
                {
                    existing = p;
                    break;
                }
            }
            if (existing == null && target.DuplicatePaths.Count > 0)
            {
                existing = target.DuplicatePaths[0];
            }

            if (existing == null)
            {
                target.ShouldWrite = true; // nothing in the project — write it fresh
                return target;
            }

            target.Path = existing;
            target.Existed = true;
            target.IsIdenticalToTemplate = FilesMatch(existing, templatePath);

            // An existing file is overwritten ONLY on explicit confirmation. No
            // session-created shortcut: this tool marks a reused byte-identical
            // script as session-created (so a follow-up modify_script can tune it),
            // and keying the overwrite on that flag meant "reuse it once, then the
            // user edits it" silently became "safe to clobber" — the tool replaced
            // hand-edited code and reported it as "(confirmed)" when nothing had
            // been confirmed. Caught in live testing, 2026-08-04.
            //
            // Skipping the write when content already matches the template is not a
            // policy call, just avoided churn: rewriting identical bytes dirties the
            // asset and costs an extra domain reload for no change.
            target.ShouldWrite = confirmOverwrite;

            return target;
        }

        private static bool FilesMatch(string pathA, string pathB)
        {
            try
            {
                return File.ReadAllText(pathA) == File.ReadAllText(pathB);
            }
            catch
            {
                return false; // unreadable — treat as "differs" so we never claim a false match
            }
        }

        // ── Scene-assembly helpers (built-in types only — safe to run now) ────

        /// <summary>Returns the existing Player (by 'Player' tag, then by name) or
        /// creates a Capsule named <paramref name="playerName"/> at y=1. Either way
        /// the returned object has a CharacterController (the movement script's
        /// RequireComponent, added now since it is a built-in type).</summary>
        private static GameObject EnsurePlayer(string playerName, List<string> notes)
        {
            GameObject player = FindByTag("Player") ?? GameObject.Find(playerName);
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = playerName;
                player.transform.position = new Vector3(0f, 1f, 0f);
                Undo.RegisterCreatedObjectUndo(player, "Create Player");
                notes.Add($"created Player capsule '{playerName}' at (0,1,0)");
            }
            else
            {
                notes.Add($"reused existing player '{player.name}'");
            }

            TrySetTag(player, "Player");

            if (player.GetComponent<CharacterController>() == null)
            {
                player.AddComponent<CharacterController>();
                notes.Add("added CharacterController to player");
            }

            return player;
        }

        /// <summary>Returns the scene's Main Camera, retagging the first camera it
        /// finds as MainCamera if none is tagged, or creating one if the scene has
        /// no camera at all.</summary>
        private static GameObject EnsureMainCamera(List<string> notes)
        {
            if (Camera.main != null)
            {
                notes.Add($"reused Main Camera '{Camera.main.gameObject.name}'");
                return Camera.main.gameObject;
            }

            var anyCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (anyCamera != null)
            {
                TrySetTag(anyCamera.gameObject, "MainCamera");
                notes.Add($"tagged existing camera '{anyCamera.gameObject.name}' as MainCamera");
                return anyCamera.gameObject;
            }

            var camGo = new GameObject("Main Camera");
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            TrySetTag(camGo, "MainCamera");
            camGo.transform.position = new Vector3(0f, 4f, -7f);
            camGo.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            notes.Add("created a Main Camera (scene had none)");
            return camGo;
        }

        /// <summary>Creates a scaled-up ground Plane if the scene has nothing that
        /// looks like a floor — so a standalone call yields a player that can stand
        /// somewhere. Skipped when the scene already has a plausible ground (the
        /// common case when the caller built the level first).</summary>
        private static void EnsureGround(List<string> notes)
        {
            if (SceneHasGround()) return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
            notes.Add("created a 50x50 Ground plane (scene had none)");
        }

        private static bool SceneHasGround()
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                string n = go.name.ToLowerInvariant();
                if (n.Contains("ground") || n.Contains("floor") || n.Contains("terrain"))
                    return true;

                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null &&
                    mf.sharedMesh.name.IndexOf("Plane", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static GameObject FindByTag(string tag)
        {
            try { return GameObject.FindWithTag(tag); }
            catch { return null; } // tag not defined in this project
        }

        private static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch { /* tag not defined — the scripts also fall back to name lookup */ }
        }
    }
}
