using System;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Services
{
    /// <summary>
    /// Editor-only observer of Unity Play Mode lifecycle. Surfaces state
    /// via the <c>get_play_mode_state</c> tool so any agent can detect
    /// Play start, Play exit, and re-entry transitions — useful for
    /// "watch then act" flows like applying queued fixes only after the
    /// user stops the simulation.
    ///
    /// Survives domain reload: <c>[InitializeOnLoad]</c> re-subscribes the
    /// listener on every load. In-memory transition timestamps reset on
    /// reload — that is intentional. The polling agent re-arms observation
    /// after the reload, so brief domain reloads do not leave the watch
    /// state stuck.
    ///
    /// Threading: <c>playModeStateChanged</c> is a Unity Editor-only event
    /// that fires on the main thread. All public read accessors are simple
    /// getters; no locking needed.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeObserver
    {
        public enum LastTransitionKind
        {
            None = 0,
            EnteredEditMode = 1,
            ExitingEditMode = 2,
            EnteredPlayMode = 3,
            ExitingPlayMode = 4,
        }

        public static bool IsPlaying => EditorApplication.isPlaying;
        public static bool WillChangePlayMode => EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying;

        public static double LastPlayEnterTimestamp { get; private set; }
        public static double LastPlayExitTimestamp { get; private set; }
        public static LastTransitionKind LastTransition { get; private set; }
        public static double LastTransitionTimestamp { get; private set; }
        public static int PlayEnterCountThisDomain { get; private set; }
        public static int PlayExitCountThisDomain { get; private set; }

        // Observation arming: callers set this true via
        // start_runtime_observation. Resets on domain reload (intentional —
        // see class docstring). Multiple consumers can arm without conflict
        // because there is no per-consumer state on the bridge; each agent
        // tracks its own session-level observation lifecycle.
        public static bool ObservationActive { get; private set; }

        // The cursor at which observation began. The polling agent uses
        // this to ignore prior errors when fetching events on the first
        // poll — arming should not retroactively surface an error that
        // fired ten minutes ago.
        public static long ObservationStartCursor { get; private set; }
        public static double ObservationStartTimestamp { get; private set; }

        static PlayModeObserver()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            double ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            LastTransitionTimestamp = ts;
            switch (change)
            {
                case PlayModeStateChange.EnteredEditMode:
                    LastTransition = LastTransitionKind.EnteredEditMode;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    LastTransition = LastTransitionKind.ExitingEditMode;
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    LastTransition = LastTransitionKind.EnteredPlayMode;
                    LastPlayEnterTimestamp = ts;
                    PlayEnterCountThisDomain++;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    LastTransition = LastTransitionKind.ExitingPlayMode;
                    LastPlayExitTimestamp = ts;
                    PlayExitCountThisDomain++;
                    break;
            }
        }

        /// <summary>
        /// Arms observation. Snapshots the current RuntimeLogStream cursor
        /// so the first poll returns only events from this point forward.
        /// Idempotent — re-arming with observation already active just
        /// updates the snapshot cursor (useful after a reconnect when the
        /// caller wants a fresh baseline).
        /// </summary>
        public static void StartObservation()
        {
            ObservationActive = true;
            ObservationStartCursor = RuntimeLogStream.LatestCursor();
            ObservationStartTimestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>Disarms observation. The runtime log stream keeps
        /// recording events; stopping observation just signals the bridge
        /// that the runner is no longer interested.</summary>
        public static void StopObservation()
        {
            ObservationActive = false;
        }

        /// <summary>Test / diagnostic helper.</summary>
        public static void Reset()
        {
            LastPlayEnterTimestamp = 0;
            LastPlayExitTimestamp = 0;
            LastTransition = LastTransitionKind.None;
            LastTransitionTimestamp = 0;
            PlayEnterCountThisDomain = 0;
            PlayExitCountThisDomain = 0;
            ObservationActive = false;
            ObservationStartCursor = 0;
            ObservationStartTimestamp = 0;
        }
    }
}
