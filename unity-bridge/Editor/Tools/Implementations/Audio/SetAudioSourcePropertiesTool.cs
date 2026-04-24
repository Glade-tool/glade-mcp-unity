using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Audio
{
    public class SetAudioSourcePropertiesTool : ITool
    {
        public string Name => "set_audio_source_properties";

        // Canonical arg names. The case-insensitive wrapper lets the model pass
        // "Volume" or "PITCH" without silently dropping the value.
        private static readonly string[] KnownKeys =
        {
            "gameObjectPath",
            "clipPath",
            "playOnAwake",
            "loop",
            "volume",
            "pitch",
            "spatialBlend",
            "minDistance",
            "maxDistance",
            "mute",
        };

        public string Execute(Dictionary<string, object> args)
        {
            var ci = new Dictionary<string, object>(args, StringComparer.OrdinalIgnoreCase);

            string gameObjectPath = ci.TryGetValue("gameObjectPath", out var gp) ? gp?.ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            AudioSource source = obj.GetComponent<AudioSource>();
            if (source == null)
            {
                var present = obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .Take(15)
                    .ToList();
                var extras = new Dictionary<string, object>
                {
                    ["componentsPresent"] = present,
                    ["hint"] = "Add the AudioSource first via add_component (componentType='AudioSource').",
                };
                return ToolUtils.CreateErrorResponse(
                    $"GameObject '{gameObjectPath}' has no AudioSource component",
                    extras);
            }

            Undo.RecordObject(source, $"Set Audio Source Properties: {gameObjectPath}");

            var applied = new List<string>();

            if (ci.TryGetValue("clipPath", out var clipArg))
            {
                string clipPath = clipArg?.ToString() ?? "";
                if (!string.IsNullOrEmpty(clipPath))
                {
                    AudioClip clip = ToolUtils.LoadAssetAtPathCaseInsensitive<AudioClip>(clipPath);
                    if (clip == null)
                    {
                        return ToolUtils.CreateErrorResponse($"AudioClip not found at '{clipPath}'");
                    }
                    source.clip = clip;
                    applied.Add("clipPath");
                }
            }

            if (TryGetBool(ci, "playOnAwake", out bool playOnAwake))
            {
                source.playOnAwake = playOnAwake;
                applied.Add("playOnAwake");
            }
            if (TryGetBool(ci, "loop", out bool loop))
            {
                source.loop = loop;
                applied.Add("loop");
            }
            if (TryGetBool(ci, "mute", out bool mute))
            {
                source.mute = mute;
                applied.Add("mute");
            }
            if (TryGetFloat(ci, "volume", out float volume))
            {
                source.volume = Mathf.Clamp01(volume);
                applied.Add("volume");
            }
            if (TryGetFloat(ci, "pitch", out float pitch))
            {
                source.pitch = pitch;
                applied.Add("pitch");
            }
            if (TryGetFloat(ci, "spatialBlend", out float blend))
            {
                source.spatialBlend = Mathf.Clamp01(blend);
                applied.Add("spatialBlend");
            }
            if (TryGetFloat(ci, "minDistance", out float minDist))
            {
                source.minDistance = Mathf.Max(0f, minDist);
                applied.Add("minDistance");
            }
            if (TryGetFloat(ci, "maxDistance", out float maxDist))
            {
                source.maxDistance = Mathf.Max(0f, maxDist);
                applied.Add("maxDistance");
            }

            // Flag silently-ignored args so the model can correct typos instead of retrying.
            var unrecognized = ci.Keys
                .Where(k => !KnownKeys.Contains(k, StringComparer.OrdinalIgnoreCase))
                .ToList();

            EditorUtility.SetDirty(source);

            var responseExtras = new Dictionary<string, object>
            {
                ["applied"] = applied,
            };
            if (unrecognized.Count > 0)
            {
                responseExtras["unrecognizedArgs"] = unrecognized;
                responseExtras["knownArgs"] = KnownKeys;
            }
            if (applied.Count == 0)
            {
                responseExtras["hint"] = "No known property keys were supplied. See knownArgs.";
                responseExtras["knownArgs"] = KnownKeys;
            }

            string msg = applied.Count == 0
                ? $"No audio source properties changed on '{gameObjectPath}'"
                : $"Updated {applied.Count} audio source {(applied.Count == 1 ? "property" : "properties")} on '{gameObjectPath}'";
            return ToolUtils.CreateSuccessResponse(msg, responseExtras);
        }

        private static bool TryGetBool(Dictionary<string, object> args, string key, out bool value)
        {
            value = false;
            if (!args.TryGetValue(key, out var raw) || raw == null) return false;
            if (raw is bool b) { value = b; return true; }
            return bool.TryParse(raw.ToString(), out value);
        }

        private static bool TryGetFloat(Dictionary<string, object> args, string key, out float value)
        {
            value = 0f;
            if (!args.TryGetValue(key, out var raw) || raw == null) return false;
            if (raw is float f) { value = f; return true; }
            if (raw is double d) { value = (float)d; return true; }
            if (raw is int i) { value = i; return true; }
            return float.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
