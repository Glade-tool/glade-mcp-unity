# Changelog

All notable changes to `gladekit-mcp` are documented here. Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
