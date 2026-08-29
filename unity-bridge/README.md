# GladeKit MCP Bridge for Unity

Unity Editor extension (UPM package) that hosts a local HTTP server on
`localhost:8765` and exposes 275 editor tools so an MCP server can drive the
open project: scene and GameObject editing, scripts, prefabs, materials,
lighting, physics (3D and 2D), tilemaps, animation, UI, input, terrain and
NavMesh, profiler, one-call gameplay scaffolders, runtime observation,
BuildPipeline, and the CC0 asset pipeline.

This is the bridge half of [GladeKit MCP](https://github.com/Glade-tool/glade-mcp).
The MCP server (`uvx gladekit-mcp`) and client setup are documented in the
[root README](../README.md); the Godot equivalent is [`godot-bridge/`](../godot-bridge/).

## Requirements

- Unity **2021.3** or newer (tested through Unity 6)
- Editor only — the package never ships in builds (`includePlatforms: ["Editor"]`)
- Dependencies declared in `package.json`: `com.unity.inputsystem`,
  `com.unity.ai.navigation`. Cinemachine, URP and HDRP are optional and
  detected via `versionDefines`.

## Install

**Window > Package Manager > + > Add package from git URL...**

```
https://github.com/Glade-tool/glade-mcp.git?path=/unity-bridge
```

Pin to a release tag to make future updates explicit:

```
https://github.com/Glade-tool/glade-mcp.git?path=/unity-bridge#v0.7.23
```

The bridge starts automatically. **Window > GladeKit MCP** shows bridge and
AI-client status, a **Restart** button, a diagnostics ring buffer, and
one-click **Copy MCP Config** / **Copy Unity AI Gateway Config**.

## Layout

```
Editor/
├── Bridge/        UnityBridgeServer (HttpListener on :8765), GladeKitMCPWindow, wire models
├── Services/      context gathering, play-mode observer, runtime log stream, backups,
│                  build manager, C# lexical scanner / outline, asset-pipeline guard
├── SRP/           render-pipeline-specific tools (URP / HDRP, compiled under GLADE_SRP)
└── Tools/
    ├── Implementations/<Category>/   one ITool per file
    └── Registrars/                   explicit Register(new Tool()) per category
Tests/             NUnit edit-mode tests (GladeKit.Bridge.Tests)
```

## Security

- Binds loopback only and enforces localhost access control on every request.
- Raw file endpoints reject path traversal outside the project.
- `import_asset` validates resolved download hosts against a per-provider
  allowlist (HTTPS only) and requires `licenseAcknowledged: true`.
- Play-mode safety: mutating tools refuse to run while the editor is playing.

## Adding a tool

See the Contributing section of the [root README](../README.md#contributing):
implement `ITool`, register it in the matching `Registrars/<Category>Tools.cs`,
and mirror the schema in `mcp-server/src/gladekit_mcp/tools/<category>.py`.
Every file in this package needs a `.meta` sibling or fresh UPM installs fail
to compile.

## License

MIT — see [LICENSE](LICENSE). Release history: [CHANGELOG](../mcp-server/CHANGELOG.md).
