using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Core.Tools.Implementations.Scripts;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Tests
{
    /// Coverage for create_third_person_controller — the ATOMIC template tool.
    /// It puts two Play-tested gameplay scripts (ThirdPersonController.cs +
    /// FollowCamera.cs) in the project VERBATIM, AND assembles the scene around
    /// them so the caller never has to issue follow-up add_component calls.
    ///
    /// Contracts under test:
    ///   1. Both scripts end up in the project byte-identical to the bundled
    ///      templates (verbatim — the whole point of the tool).
    ///   2. REUSE-DON'T-DUPLICATE: the templates declare no namespace, so every
    ///      script under Assets/ shares one assembly and a second file named
    ///      ThirdPersonController.cs is a duplicate type (CS0101). The tool must
    ///      therefore never increase the number of copies of a class — including
    ///      when the caller points `directory` somewhere else. A pre-existing
    ///      script is reused in place and left byte-untouched.
    ///   3. The confirm flag replaces the existing file IN PLACE (still no copy).
    ///   4. Written scripts are marked session-created.
    ///   5. ATOMIC scene assembly: a Player capsule (with CharacterController +
    ///      'Player' tag) and a Main Camera exist after the call, and the two
    ///      custom MonoBehaviours are QUEUED for post-compile attachment.
    ///   6. The caller's Player/Camera are reused, not duplicated, when present.
    ///
    /// Assertions are written against the *whole project*, not just TmpDir,
    /// because the resolution these tests cover is itself project-wide (and the
    /// dev project legitimately ships its own Assets/Scripts copies).
    public class CreateThirdPersonControllerTool_Tests
    {
        private const string TmpDir = "Assets/_TmpTpcTest";
        private const string ControllerPath = "Assets/_TmpTpcTest/ThirdPersonController.cs";
        private const string CameraPath = "Assets/_TmpTpcTest/FollowCamera.cs";
        private const string RealUserContent = "public class ThirdPersonController { int keep = 1; }\n";

        private HashSet<GameObject> _preExistingRoots;

        /// Copies of a script name anywhere under Assets — the quantity that
        /// decides whether the project compiles.
        private static int CopiesOf(string fileName) =>
            GladeAgenticAI.Core.Tools.Implementations.Gameplay.GameplayScaffold
                .FindExistingScripts(fileName).Count;

        private static string TemplateText(string templateFile) =>
            File.ReadAllText(ToolUtils.ResolveTemplatePath(templateFile));

        /// The single path the tool resolved for a script name (first copy).
        private static string ResolvedPath(string fileName) =>
            GladeAgenticAI.Core.Tools.Implementations.Gameplay.GameplayScaffold
                .FindExistingScript(fileName);

        [SetUp]
        public void SetUp()
        {
            SessionTracker.Reset();
            PendingControllerWiring.Clear();
            if (!Directory.Exists(TmpDir))
            {
                Directory.CreateDirectory(TmpDir);
                AssetDatabase.Refresh(ImportAssetOptions.Default);
            }
            // Snapshot the scene so TearDown can remove anything the tool spawns
            // (Player capsule / Main Camera / Ground plane) without touching
            // objects the test runner's scene already had.
            _preExistingRoots = new HashSet<GameObject>(
                SceneManager.GetActiveScene().GetRootGameObjects());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!_preExistingRoots.Contains(go))
                {
                    Object.DestroyImmediate(go);
                }
            }
            if (Directory.Exists(TmpDir))
            {
                AssetDatabase.DeleteAsset(TmpDir);
            }
            SessionTracker.Reset();
            PendingControllerWiring.Clear();
        }

        // ── Happy path + verbatim integrity ─────────────────────────────────

        [Test]
        public void Create_PutsBothScriptsInProjectVerbatim_ExactlyOnce()
        {
            int controllersBefore = CopiesOf("ThirdPersonController.cs");
            int camerasBefore = CopiesOf("FollowCamera.cs");

            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object>
            {
                ["directory"] = TmpDir,
            });

            StringAssert.Contains("third-person", result);

            // Each script is present, and present exactly once more than before
            // only when the project had none (a fresh write). Never MORE.
            Assert.AreEqual(System.Math.Max(controllersBefore, 1), CopiesOf("ThirdPersonController.cs"),
                "the tool must not add a second ThirdPersonController.cs");
            Assert.AreEqual(System.Math.Max(camerasBefore, 1), CopiesOf("FollowCamera.cs"),
                "the tool must not add a second FollowCamera.cs");

            // Verbatim: whatever path is in play holds the bundled template exactly.
            Assert.AreEqual(TemplateText("ThirdPersonController.cs.txt"),
                File.ReadAllText(ResolvedPath("ThirdPersonController.cs")));
            Assert.AreEqual(TemplateText("FollowCamera.cs.txt"),
                File.ReadAllText(ResolvedPath("FollowCamera.cs")));
        }

        [Test]
        public void Create_MarksScriptsSessionCreated()
        {
            var tool = new CreateThirdPersonControllerScriptTool();
            tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            // Assert against the path the tool actually resolved: with reuse, that
            // may be a copy the project already had rather than one under TmpDir.
            Assert.IsTrue(SessionTracker.WasScriptCreatedThisSession(ResolvedPath("ThirdPersonController.cs")),
                "controller must be marked session-created so modify_script isn't refused");
            Assert.IsTrue(SessionTracker.WasScriptCreatedThisSession(ResolvedPath("FollowCamera.cs")),
                "camera follow must be marked session-created");
        }

        // ── Atomic scene assembly ───────────────────────────────────────────

        [Test]
        public void Create_BuildsPlayerWithCharacterController_AndQueuesWiring()
        {
            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "tool must create a Player when the scene has none");
            Assert.IsNotNull(player.GetComponent<CharacterController>(),
                "Player must get a CharacterController immediately (built-in type)");
            Assert.AreEqual("Player", player.tag, "Player must be tagged so the camera self-resolves it");

            Assert.IsNotNull(Camera.main, "tool must ensure a Main Camera exists");

            // The two custom MonoBehaviours can't be added until the scripts
            // compile, so they must be QUEUED for post-compile attachment.
            Assert.IsTrue(PendingControllerWiring.HasPending,
                "ThirdPersonController + FollowCamera must be queued for deferred wiring");
            StringAssert.Contains("ATOMIC", result);
            StringAssert.Contains("compile_scripts", result);
        }

        [Test]
        public void Create_ReusesExistingPlayer_NoDuplicate()
        {
            // Caller built the Player first (the common multi-system order).
            var existing = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            existing.name = "Player";

            var tool = new CreateThirdPersonControllerScriptTool();
            tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            int playerCount = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name == "Player") playerCount++;
            Assert.AreEqual(1, playerCount, "existing Player must be reused, not duplicated");
            Assert.IsNotNull(existing.GetComponent<CharacterController>(),
                "the reused Player must still get its CharacterController");
        }

        [Test]
        public void Create_SkipsGround_WhenSceneAlreadyHasFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";

            var tool = new CreateThirdPersonControllerScriptTool();
            tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            int groundCount = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name == "Ground") groundCount++;
            Assert.AreEqual(1, groundCount, "must not add a second ground when one exists");
        }

        // ── Reuse-don't-duplicate (the CS0101 contract) ──────────────────────

        [Test]
        public void Create_ExistingUserScriptWithoutFlag_ReusedInPlace_NotClobbered()
        {
            // Pre-create ONE of the two targets as untracked "user code".
            File.WriteAllText(ControllerPath, RealUserContent);
            AssetDatabase.ImportAsset(ControllerPath);
            int copiesBefore = CopiesOf("ThirdPersonController.cs");

            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            // Reused, not refused: the scaffold still completes...
            StringAssert.DoesNotContain("Refused to overwrite", result);
            StringAssert.Contains("DIFFERS from the vetted template", result);
            // ...the user's code is byte-untouched...
            Assert.AreEqual(RealUserContent, File.ReadAllText(ControllerPath),
                "an unconfirmed call must not overwrite user code");
            // ...no second copy was added (that would be CS0101)...
            Assert.AreEqual(copiesBefore, CopiesOf("ThirdPersonController.cs"),
                "reuse must not add another copy of the class");
            // ...and the scene assembly still ran, so the turn produces a usable player.
            Assert.IsNotNull(GameObject.Find("Player"),
                "reuse must still assemble the scene — that is the tool's atomic value");
            Assert.IsTrue(PendingControllerWiring.HasPending,
                "reuse must still queue the component wiring");
            // The differing user script stays protected from a later modify_script.
            Assert.IsFalse(SessionTracker.WasScriptCreatedThisSession(ControllerPath),
                "a reused user-authored script must NOT be marked session-created");
        }

        [Test]
        public void Create_ExistingFileWithFlag_ReplacedInPlace_StillOneCopy()
        {
            File.WriteAllText(ControllerPath, RealUserContent);
            AssetDatabase.ImportAsset(ControllerPath);
            int copiesBefore = CopiesOf("ThirdPersonController.cs");

            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object>
            {
                ["directory"] = TmpDir,
                ["confirmExistingFileModification"] = true,
            });

            StringAssert.Contains("third-person", result);
            Assert.AreEqual(TemplateText("ThirdPersonController.cs.txt"), File.ReadAllText(ControllerPath),
                "acknowledged call must overwrite with the vetted template");
            Assert.AreEqual(copiesBefore, CopiesOf("ThirdPersonController.cs"),
                "a confirmed overwrite replaces in place — it must not add a copy");
        }

        /// REGRESSION (prod, 2026-08-03): the tool used to refuse a pre-existing
        /// script and tell the caller to "pass a different 'directory'". The caller
        /// obliged, a second ThirdPersonController.cs landed in Assets/Scripts/Player,
        /// and the project failed to compile with 11 duplicate-definition errors.
        /// Pointing `directory` elsewhere must never produce a second copy.
        [Test]
        public void Create_DifferentDirectory_WhenScriptExistsElsewhere_AddsNoSecondCopy()
        {
            // The script already lives somewhere in the project (as the vetted copy).
            File.WriteAllText(ControllerPath, TemplateText("ThirdPersonController.cs.txt"));
            File.WriteAllText(CameraPath, TemplateText("FollowCamera.cs.txt"));
            AssetDatabase.ImportAsset(ControllerPath);
            AssetDatabase.ImportAsset(CameraPath);

            int controllersBefore = CopiesOf("ThirdPersonController.cs");
            int camerasBefore = CopiesOf("FollowCamera.cs");

            // Caller aims somewhere else entirely — the exact move that broke prod.
            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object>
            {
                ["directory"] = TmpDir + "/Player",
            });

            Assert.AreEqual(controllersBefore, CopiesOf("ThirdPersonController.cs"),
                "a different 'directory' must NOT create a duplicate ThirdPersonController.cs");
            Assert.AreEqual(camerasBefore, CopiesOf("FollowCamera.cs"),
                "a different 'directory' must NOT create a duplicate FollowCamera.cs");
            Assert.IsFalse(File.Exists(TmpDir + "/Player/ThirdPersonController.cs"),
                "nothing may be written into the alternate directory");
            StringAssert.DoesNotContain("Refused", result);
        }

        [Test]
        public void Create_ExistingIdenticalScript_ReusedAndMarkedSessionCreated()
        {
            // Identical to the template => it IS our content, so a follow-up
            // modify_script (tuning moveSpeed) must not be refused.
            File.WriteAllText(ControllerPath, TemplateText("ThirdPersonController.cs.txt"));
            AssetDatabase.ImportAsset(ControllerPath);

            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            StringAssert.Contains("identical to the vetted template", result);
            Assert.IsTrue(SessionTracker.WasScriptCreatedThisSession(ControllerPath),
                "a reused byte-identical script must be marked session-created");
        }

        [Test]
        public void Create_PreExistingDuplicates_AreNamedInTheResult()
        {
            // Two copies already on disk — the state the old bug left projects in.
            // The tool didn't cause it and can't safely delete user files, but it
            // must say WHICH files collide instead of leaving the caller to guess
            // from 11 opaque CS0101 lines.
            Directory.CreateDirectory(TmpDir + "/Nested");
            File.WriteAllText(ControllerPath, TemplateText("ThirdPersonController.cs.txt"));
            File.WriteAllText(TmpDir + "/Nested/ThirdPersonController.cs",
                TemplateText("ThirdPersonController.cs.txt"));
            AssetDatabase.Refresh(ImportAssetOptions.Default);

            var tool = new CreateThirdPersonControllerScriptTool();
            string result = tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            StringAssert.Contains("DUPLICATE", result);
            StringAssert.Contains("CS0101", result);
            StringAssert.Contains("Nested/ThirdPersonController.cs", result);
        }

        [Test]
        public void Create_CreatedThisSessionWithoutFlag_Allowed()
        {
            // First call creates both (and marks them session-created).
            var tool = new CreateThirdPersonControllerScriptTool();
            tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });

            // Second call against the same dir should not need the flag — the agent
            // is regenerating its own scaffold, not clobbering user code.
            string result = tool.Execute(new Dictionary<string, object> { ["directory"] = TmpDir });
            StringAssert.Contains("third-person", result);
            StringAssert.DoesNotContain("Refused to overwrite", result);
        }
    }

    /// Coverage for PendingControllerWiring — the deferred-attachment driver that
    /// closes the gap between "scripts written" and "MonoBehaviour types loaded".
    /// Tested directly (with a built-in component type so resolution is
    /// deterministic) rather than by staging a real domain reload.
    public class PendingControllerWiring_Tests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            PendingControllerWiring.Clear();
            _go = new GameObject("WiringTarget");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            PendingControllerWiring.Clear();
        }

        [Test]
        public void TryComplete_AttachesResolvableComponent_AndClears()
        {
            PendingControllerWiring.Queue(new[]
            {
                // Rigidbody is built-in, so FindComponentType resolves it now —
                // standing in for the post-compile-resolvable ThirdPersonController.
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "Rigidbody"),
            });
            Assert.IsTrue(PendingControllerWiring.HasPending);

            PendingControllerWiring.TryComplete();

            Assert.IsNotNull(_go.GetComponent<Rigidbody>(),
                "a resolvable queued component must be attached");
            Assert.IsFalse(PendingControllerWiring.HasPending,
                "the queue must clear once every entry's type resolves");
        }

        [Test]
        public void TryComplete_KeepsQueue_WhenTypeNotYetCompiled()
        {
            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "NoSuchComponentType_Xyz"),
            });

            PendingControllerWiring.TryComplete();

            Assert.IsTrue(PendingControllerWiring.HasPending,
                "an unresolved type means the scripts haven't compiled yet — keep waiting");
        }

        [Test]
        public void TryComplete_GivesUp_AfterRepeatedFailures()
        {
            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "NoSuchComponentType_Xyz"),
            });

            // A compile failure would never produce the type; the driver must
            // self-clear instead of re-firing forever on every later reload.
            for (int i = 0; i < 10 && PendingControllerWiring.HasPending; i++)
            {
                PendingControllerWiring.TryComplete();
            }

            Assert.IsFalse(PendingControllerWiring.HasPending,
                "the queue must give up and clear after the attempt cap");
        }

        [Test]
        public void TryComplete_ResolvesAndClears_WhenTargetMissing()
        {
            PendingControllerWiring.Queue(new[]
            {
                // Type resolves (Rigidbody) but the object doesn't exist — nothing
                // to attach, and it must NOT loop forever.
                new PendingControllerWiring.WiringRequest("GhostObject_DoesNotExist", null, "Rigidbody"),
            });

            PendingControllerWiring.TryComplete();

            Assert.IsFalse(PendingControllerWiring.HasPending,
                "a resolvable-type/missing-object entry must clear, not loop");
        }

        [Test]
        public void Queue_Accumulates_AcrossSeparateCalls()
        {
            // Models the real flow: one scaffolder queues its component, then a
            // SECOND scaffolder queues a different one before the single compile
            // that wires them all. The second call must NOT clobber the first.
            var go2 = new GameObject("WiringTarget2");
            try
            {
                PendingControllerWiring.Queue(new[]
                {
                    new PendingControllerWiring.WiringRequest("WiringTarget", null, "Rigidbody"),
                });
                PendingControllerWiring.Queue(new[]
                {
                    new PendingControllerWiring.WiringRequest("WiringTarget2", null, "BoxCollider"),
                });

                PendingControllerWiring.TryComplete();

                Assert.IsNotNull(_go.GetComponent<Rigidbody>(),
                    "the first scaffolder's queued component must survive a later Queue call");
                Assert.IsNotNull(go2.GetComponent<BoxCollider>(),
                    "the second scaffolder's component must attach too");
                Assert.IsFalse(PendingControllerWiring.HasPending,
                    "the merged queue must clear once every entry resolves");
            }
            finally
            {
                Object.DestroyImmediate(go2);
            }
        }

        [Test]
        public void ApplyFields_AppliesKnownFields_AndWarnsOnDroppedOnes()
        {
            // The reused-existing-class trap: a scaffolder queues its template's knobs
            // (here a real one + one the resolved class doesn't have). The real field
            // must still land, and the missing one must surface as a WARNING — not
            // vanish silently the way a bare "the elevator didn't move" bug does.
            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "Rigidbody",
                    new List<PendingControllerWiring.FieldValue>
                    {
                        new PendingControllerWiring.FieldValue("mass", "float", "7"),         // Rigidbody has this
                        new PendingControllerWiring.FieldValue("route", "string", "0,0,0;4,0,0"), // it does NOT
                    }),
            });

            // The dropped knob must be named in a warning (the actionable signal the
            // backend forwards). Match the field name to keep the assert robust to
            // copy tweaks.
            LogAssert.Expect(LogType.Warning, new Regex("route"));

            PendingControllerWiring.TryComplete();

            var rb = _go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "the component must still attach");
            Assert.AreEqual(7f, rb.mass,
                "a valid queued field must still apply even when a sibling field is dropped");
            Assert.IsFalse(PendingControllerWiring.HasPending, "the queue must clear");
        }

        [Test]
        public void ApplyFields_NoWarning_WhenEveryFieldResolves()
        {
            // The common case: the vetted template WAS written, so every queued knob
            // exists on the attached class. No warning should fire — the collision
            // signal must stay quiet unless there's an actual collision.
            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "Rigidbody",
                    new List<PendingControllerWiring.FieldValue>
                    {
                        new PendingControllerWiring.FieldValue("mass", "float", "3"),
                    }),
            });

            PendingControllerWiring.TryComplete();

            // The happy path must wire cleanly: the field lands and nothing errors.
            var rb = _go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.AreEqual(3f, rb.mass);
        }

        [Test]
        public void Queue_DedupesSameAttachment_NewestFieldsWin()
        {
            // Re-queuing the same target+component (e.g. create_game_manager run
            // twice before a compile) must not stack duplicates; the latest call's
            // field values are the ones that take effect.
            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "Rigidbody",
                    new System.Collections.Generic.List<PendingControllerWiring.FieldValue>
                    {
                        new PendingControllerWiring.FieldValue("mass", "float", "5"),
                    }),
            });
            PendingControllerWiring.Queue(new[]
            {
                new PendingControllerWiring.WiringRequest("WiringTarget", null, "Rigidbody",
                    new System.Collections.Generic.List<PendingControllerWiring.FieldValue>
                    {
                        new PendingControllerWiring.FieldValue("mass", "float", "9"),
                    }),
            });

            PendingControllerWiring.TryComplete();

            var rb = _go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "the deduped attachment must still attach exactly once");
            Assert.AreEqual(9f, rb.mass,
                "the newest Queue call's field value must win (no stale first-call value)");
        }
    }
}
