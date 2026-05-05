# Changelog

All notable changes to `gladekit-mcp` are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2026-05-05

### Added

- **Runtime / Live Loop tools (5 new).** A small surface for agents that want to watch a Unity Play session for runtime errors and orchestrate fixes:
  - `start_runtime_observation` / `stop_runtime_observation` — arm/disarm runtime-event capture; the start call snapshots a baseline cursor so you only get events from arming forward.
  - `get_runtime_events` — incremental cursor-based poll for `Error` / `Exception` log entries during Play Mode. Returns events with a stable per-event fingerprint (`condition + first 500 chars of stack`) plus `playModeActive`, `observationActive`, and `nextCursor` for the next call.
  - `get_play_mode_state` — read-only snapshot of Play Mode state (isPlaying, last enter/exit timestamps, observation arming status). Useful for "watch for Play exit then act" flows.
  - `apply_queued_fix` — idempotent multi-step fix dispatcher keyed on `proposalId`. Each `change` is a `{toolName, args, rationale}` triple dispatched through the same path as direct tool calls (so DemoAssetsGuard and SessionTracker hooks fire uniformly). A second call with the same `proposalId` returns `alreadyApplied:true` with the prior result instead of re-executing — safe for retry-on-blip flows.
- **Bridge services to back the Runtime tools (in `unity-bridge/`):** `RuntimeLogStream` (500-entry ring buffer with monotonic cursors and condition-only legacy drain for the existing `/api/console/events` endpoint), `PlayModeObserver` (Play Mode lifecycle tracker, survives domain reload), and `FixApplyTracker` (idempotency registry capped at 200 entries).
- New `runtime` tool category exposed via the standard category-aware filter; the 5 tools are also added to `CORE_TOOLS` so they appear in Claude Code's default tool list.

### Changed

- **`/api/console/events` endpoint behavior change (internal, wire-compatible).** The legacy drain no longer mutates the ring buffer — it tracks a per-server `_legacyDrainCursor` and returns events past it. This lets cursor-based consumers (`get_runtime_events`) and the legacy drain coexist without stealing events from each other. The HTTP response shape is unchanged.
- **`UnityBridgeServer` slimmed by ~50 LOC.** The inline `Application.logMessageReceivedThreaded` subscription + 50-entry queue moved into `RuntimeLogStream`. The bridge static ctor no longer hooks the log event itself.

### Notes

- `propose_fix` is intentionally NOT exposed via the MCP server. It is a cloud-side intercept used by the GladeKit desktop app's Live Loop runner; the bridge has no handler for it. External MCP clients should construct fix payloads themselves and dispatch via `apply_queued_fix` directly.

## [0.4.5] - 2026-05-03

### Changed

- **`gladekit version` now suggests the right upgrade command for your install method.** uvx (the recommended install path) caches resolved versions and won't pick up new releases without `--refresh`, but the previous output unconditionally said `pip install --upgrade gladekit-mcp` — which strands uvx users (pip often isn't on PATH, and the suggested command wouldn't update the cached uvx env even if it ran). Now detects uvx-managed envs via `sys.executable` and prints `uvx --refresh gladekit-mcp` instead.

## [0.4.4] - 2026-05-03

### Fixed

- **Duplicate `compile_scripts` tool name broke Windsurf.** The schema was registered in both `tools/core.py` and `tools/scripting.py`; the aggregator concatenated both, so MCP `tools/list` returned the same name twice. Claude Code tolerated it, but Windsurf strictly enforces the MCP spec's "unique tool names" rule and bricked the chat with `Duplicate tool name: mcp_*_compile_scripts` until the server was disabled. Removed the stub in `core.py` (kept the richer `scripting.py` definition). Reported by an OSS user against `gladekit-mcp` v0.4.2/v0.4.3.

### Changed

- **Defense-in-depth dedupe in `registry.get_mcp_tools()`.** Now dedupes by name (keeping the first occurrence) and emits a `logger.warning` on collision, so a future regression keeps the wire MCP-compliant and surfaces the bug instead of breaking strict clients.
- **`test_no_duplicate_tool_names` is now strict** — previously allowlisted `compile_scripts` as a "known duplicate". Any future duplicate fails CI.

## [0.4.3] - 2026-04-30

### Fixed

- **Six more mutating tools no longer silently drop nested-array args.** Fifth pass of the input-resolution audit. Each tool received a `transforms`/`maps`/`axes`/`motions`/`spriteOrder` array via the schema, but the bridge's flat JSON parser left nested arrays as raw strings — the `is List<object>` type-check then silently failed when the call routed through `batch_execute`. Affected tools: `set_transform_batch`, `set_input_action_bindings`, `ensure_legacy_input_axes`, `create_blend_tree_1d`, `create_blend_tree_2d`, `create_sprite_animation_clip`. All now re-hydrate via `TryParseJsonArrayToList` matching the convention from the fourth pass.
- **`set_input_action_bindings` was the worst silent-success case** — returned `"Updated InputActionAsset bindings"` while applying nothing. Now returns structured `mapsUpdated` / `actionsUpdated` / `bindingsAdded` counts and recursively re-hydrates the nested `actions` and `bindings` arrays inside each map. Live verification surfaced two additional pre-existing bugs in this tool that the deep-parse fix made reachable for the first time: (1) `FindActionMap(name, throwIfNotFound: true)` throws instead of returning null, defeating the `?? AddActionMap` find-or-create fallback (changed to default `false`); (2) `.inputactions` files serialize as JSON, not YAML — `EditorUtility.SetDirty + AssetDatabase.SaveAssets` was a silent no-op for the file. Now persists via `asset.ToJson()` + `File.WriteAllText` + `AssetDatabase.ImportAsset`.
- **`create_blend_tree_1d` / `create_blend_tree_2d` surface skipped motions.** Previously returned `success=true, motionCount=0` when motions were requested but failed to resolve (silent failure mode the audit is designed to catch). Now return structured `skippedMotions` per-entry reasons (`clipPath is required`, `AnimationClip not found at clipPath`) and **error** when `requestedMotions > 0 && motionCount == 0`.

## [0.4.2] - 2026-04-30

### Fixed

- **`set_animation_clip_curves` no longer silently drops curves.** The Python schema advertised `componentType` and `keys` while the C# bridge read `type` and `keyframes` — clients following the schema got every curve dropped with success message "Set 0 curve(s)". The bridge now accepts both naming variants for forward compatibility, and curves that fail to land surface a structured `skippedCurves` array with per-entry reasons.
- **Type strings now resolve from short names.** `Transform`, `SpriteRenderer`, `GameObject`, etc. are accepted alongside fully-qualified names. The previous `System.Type.GetType` lookup silently returned null for short names, dropping the curve.
- **Zero-applied-with-skips now returns an error.** Calls that produce `curvesAdded=0` but had per-entry skip reasons return `success=false` instead of a misleading success.
- **`get_animation_clip_curves` reads back curves correctly.** It was walking SerializedObject fields that don't exist (`propertyName`/`type` vs the actual `attribute`/`classID`), returning empty for clips that had data. Replaced with the canonical `AnimationUtility.GetCurveBindings` API.
- **`set_animation_curve_tangents` accepts non-Transform bindings.** Optional `type` arg lets callers tangent-tune `SpriteRenderer`, `MeshRenderer`, or custom MonoBehaviour curves. On a curve miss, the response enumerates up to 20 actual clip bindings as `path|propertyName|type` strings so the next call can target a real one.
- **`remove_animation_clip_curves` distinguishes failure modes.** Returns structured per-entry skip reasons distinguishing `unresolvedType` from `notFoundOnClip`.
- **`set_sprite_animation_curves` reports skipped frames.** Per-keyframe skip tracking with `similarSprites` hints (filename-based search across `t:Sprite` then `t:Texture2D`) when a sprite asset can't be loaded. Returns `success=false` when zero keyframes parse.

### Internal

- All three setters now re-hydrate JSON-array args via `TryParseJsonArrayToList`. The bridge's flat JSON parser leaves nested arrays as raw strings; the previous `is List<object>` check silently failed for any caller routed through `batch_execute`.

## [0.3.0] - 2026-04-18

### Changed

- **`openai` and `numpy` are now default dependencies.** Script semantic search works out of the box as soon as `OPENAI_API_KEY` is set — no install flags needed. Previously these packages shipped behind the `[search]` extra, which caused silent fallback to unranked results when users set the key without first reinstalling with the extra.

### Removed

- **Dropped `[search]` and `[all]` optional-dependency extras.** They are now redundant. If your install command includes `gladekit-mcp[search]` or `gladekit-mcp[all]`, drop the suffix — plain `gladekit-mcp` now bundles everything the `[search]` extra contained. The `[http]` extra is retained for explicit pinning of `starlette`/`uvicorn`.

### Migration

No config changes required. If you previously launched the server with:

```json
{ "command": "uvx", "args": ["gladekit-mcp[search]"] }
```

simplify to:

```json
{ "command": "uvx", "args": ["gladekit-mcp"] }
```

Semantic search activates automatically when `OPENAI_API_KEY` is present in the `env` block.
