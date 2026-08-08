"""
Godot build/export tools (3): ship the game.

Exporting is the terminal step of every project and was the one thing the
whole catalog could not do — a user could build a game with GladeKit but not
produce anything they could send to another person.

The three tools form a recon -> configure -> build chain:

    get_export_info       what can this project export, and what is blocking it
    create_export_preset  author/update a preset in export_presets.cfg
    export_project        run the build, verify the artifact

Descriptions here are written with request-shaped triggers ("export", "build",
"ship", "publish", "playable link", ".exe", "itch.io") because the JIT tool
selector ranks on embeddings against the USER's phrasing — the lesson from the
batch-tool selection fix, where a tool with a thin description lost the ranking
to a worse-suited sibling and was measured at 1/4 before the rewrite.
"""

from typing import Dict, List

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "get_export_info",
            "description": (
                "Read-only: can this project be exported, and to what? Answers "
                '"can I ship/build/publish this game?" in one call. Returns which '
                "export templates are installed, every preset in "
                "export_presets.cfg, and — importantly — a per-preset `blockers` "
                "list naming anything that would make a build fail.\n\n"
                "Call this FIRST when the user asks to export, build, ship, "
                "publish, or share their game. Exporting has preconditions that "
                "all fail with cryptic engine errors, and one of them (missing "
                "export templates) can only be fixed by the user in the Godot GUI "
                "— Editor > Manage Export Templates. When `templates_installed` "
                "is false, TELL THE USER to install them rather than retrying the "
                "export; no tool can do it for them.\n\n"
                "Safe during play mode."
            ),
            "parameters": {"type": "object", "properties": {}, "required": []},
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_export_preset",
            "description": (
                "Create (or update) an export preset so the project can be built. "
                "Godot normally requires the Project > Export dialog for this; "
                "this writes the same export_presets.cfg entry directly.\n\n"
                "Idempotent by `name` — calling it twice with the same name "
                "UPDATES that preset in place, so retrying never leaves "
                '"Web", "Web 2", "Web 3" behind.\n\n'
                'For "let people play my game" / "put it online" / "share a link", '
                'use platform="Web": it produces a build that runs on any static '
                "host (GitHub Pages, S3, itch.io) with no special server headers. "
                'Use "Windows Desktop" for a .exe, "Linux", "macOS", "Android" '
                'for an .apk, or "iOS". Common aliases are normalized '
                '("html5"/"browser" -> Web, "windows"/"win64" -> Windows Desktop).\n\n'
                "By default the GladeKit bridge addon is EXCLUDED from the build — "
                "it is editor-only tooling and shipping it bloats the game "
                "(measured 9.5x smaller .pck with it excluded). Leave that default "
                "alone unless the user specifically wants the addon shipped.\n\n"
                "Then call export_project to actually build."
            ),
            "parameters": {
                "type": "object",
                "properties": {
                    "platform": {
                        "type": "string",
                        "description": (
                            'Target platform: "Web" (runs in a browser, easiest to '
                            'share), "Windows Desktop", "Linux", "macOS", "Android", '
                            'or "iOS". Aliases like "html5" or "win64" are accepted.'
                        ),
                    },
                    "name": {
                        "type": "string",
                        "description": (
                            "Preset name. export_project matches this EXACTLY. Default: the platform name."
                        ),
                    },
                    "export_path": {
                        "type": "string",
                        "description": (
                            "Output path. Relative paths resolve against the project "
                            "root. Default: build/<platform>/<project>.<ext> — e.g. "
                            "build/web/index.html."
                        ),
                    },
                    "runnable": {
                        "type": "boolean",
                        "description": "Mark the preset runnable in the editor. Default true.",
                    },
                    "exclude_gladekit_bridge": {
                        "type": "boolean",
                        "description": (
                            "Keep the GladeKit editor addon out of the shipped game. "
                            "Default true. Only set false if the user explicitly wants it."
                        ),
                    },
                    "exclude_filter": {
                        "type": "string",
                        "description": (
                            "Extra comma-separated globs to exclude from the build, "
                            'e.g. "tests/*, *.md". Appended to the addon exclusion.'
                        ),
                    },
                },
                "required": ["platform"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "export_project",
            "description": (
                "Build the game into a distributable artifact — the .exe, .apk, or "
                "playable web build the user actually ships. Use this for "
                '"export my game", "build it", "make a playable version", '
                '"ship it", "give me something I can send to a friend".\n\n'
                "Runs a headless Godot build in a separate process; the editor "
                "stays usable throughout. The edited scene is saved first by "
                "default, so a change made earlier in the same turn is included "
                "(Godot exports what is on disk, not what is in the editor's "
                "memory).\n\n"
                "Requires a preset — call get_export_info to list them, or "
                "create_export_preset to add one. The preset name is matched "
                "EXACTLY.\n\n"
                "On success returns the artifact path, its total size, and every "
                "file produced. For a Web build it also returns `share_hint` "
                "explaining how to serve it — relay that, because opening "
                "index.html directly from disk (file://) does NOT work; the "
                "browser blocks the wasm fetch and the user sees a blank page. "
                "On failure, `errors` carries the engine's own message and "
                "`possible_solutions` the fix."
            ),
            "parameters": {
                "type": "object",
                "properties": {
                    "preset": {
                        "type": "string",
                        "description": (
                            "Name of the export preset to build, matched exactly as it appears in export_presets.cfg."
                        ),
                    },
                    "output_path": {
                        "type": "string",
                        "description": (
                            "Override the preset's export path. Relative resolves "
                            "against the project root. The directory is created if "
                            "missing."
                        ),
                    },
                    "debug": {
                        "type": "boolean",
                        "description": (
                            "Build a debug export (includes the debugger, for "
                            "testing). Default false — release, which is what you "
                            "want for anything the user will share."
                        ),
                    },
                    "auto_save": {
                        "type": "boolean",
                        "description": (
                            "Save the edited scene before building so the build includes unsaved changes. Default true."
                        ),
                    },
                    "timeout_seconds": {
                        "type": "integer",
                        "description": (
                            "Give up after this long. Default 600. A small project "
                            "exports in a few seconds; large projects with texture "
                            "compression take minutes."
                        ),
                    },
                },
                "required": ["preset"],
            },
        },
    },
]
