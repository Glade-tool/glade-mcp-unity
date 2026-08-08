"""
Unity build/export tools (4): ship the game.

The Unity half of the shipping story; the Godot half lives in
`godot_tools/export.py`. Same recon -> configure -> build shape, plus a
separate poll step Unity forces:

    get_build_info     what can this project build, and what is blocking it
    set_build_scenes   which scenes ship, and which one boots first
    build_player       start the build (returns immediately)
    get_build_status   the outcome

Why the poll is a separate tool rather than a return value: Unity's
BuildPipeline.BuildPlayer is synchronous, runs on the editor's main thread, and
has no async variant — and Unity refuses to open a project in a second
instance, so there is no subprocess to push it into the way the Godot bridge
does. The editor is frozen for the whole build, which on any real project
outlives a client's request timeout. build_player therefore validates, starts,
and answers immediately; get_build_status reports what happened.

Descriptions carry request-shaped triggers ("build", "ship", "export", ".exe",
"playable") because the JIT tool selector ranks on embeddings against the
USER's phrasing — the lesson from the batch-tool selection fix.
"""

from typing import Dict, List

CATEGORY = {
    "name": "build",
    "display_name": "Build & Export",
    "keywords": [
        "build",
        "rebuild",
        "export",
        "ship",
        "shipping",
        "publish",
        "release",
        "distribute",
        "package",
        "player",
        "standalone",
        "webgl",
        "web build",
        "browser",
        "itch",
        "itch.io",
        "exe",
        "apk",
        "app bundle",
        "installer",
        "playable",
        "playable build",
        "send to a friend",
        "share the game",
        "build settings",
        "scenes in build",
        "build target",
        "platform",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "get_build_info",
            "description": (
                "Read-only: can this project be built, and to what? Answers "
                '"can I ship/build/export this game?" in one call. Returns the '
                "installed build targets, the scenes that would ship, and a "
                "`blockers` list naming anything that would make a build fail.\n\n"
                "Call this FIRST when the user asks to build, ship, export, or "
                "share their game. Two blockers matter most:\n"
                "- A target whose build-support module is not installed. Only "
                "the user can fix that, in Unity Hub > Installs > (gear) > Add "
                "modules. TELL THEM rather than retrying the build.\n"
                "- Scenes listed in Build Settings that no longer exist. Unity "
                "keeps pointing at deleted scenes, and its own build error "
                "blames the path FORMAT ('incorrect path for a scene file') "
                "when the file is simply gone — so trust `missingScenes` here "
                "over the engine's wording, and fix it with set_build_scenes.\n\n"
                "Safe during play mode."
            ),
            "parameters": {"type": "object", "properties": {}, "required": []},
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_build_scenes",
            "description": (
                "Set which scenes ship in the build, and in what order — Unity's "
                'File > Build Settings "Scenes In Build" list. Index 0 is the '
                "scene the game BOOTS INTO, which is what people mean by "
                '"it builds but starts on the wrong level".\n\n'
                "Use this when the user wants a level added to the build, the "
                "title screen to come first, or when get_build_info reports "
                "scenes that no longer exist. Every path is validated before "
                "anything is written, so a bad path never leaves a half-applied "
                "build list."
            ),
            "parameters": {
                "type": "object",
                "properties": {
                    "scenePaths": {
                        "type": "array",
                        "items": {"type": "string"},
                        "description": (
                            "Scene paths in boot order, e.g. "
                            '["Assets/Scenes/MainMenu.unity", '
                            '"Assets/Scenes/Level1.unity"]. Index 0 boots first.'
                        ),
                    },
                    "append": {
                        "type": "boolean",
                        "description": (
                            "Add to the existing list instead of replacing it. "
                            "Default false (replace). Re-specifying a scene that "
                            "is already listed moves it rather than duplicating it."
                        ),
                    },
                },
                "required": ["scenePaths"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "build_player",
            "description": (
                "Build the game into a distributable player — the .exe, .app, "
                ".apk or hostable WebGL folder the user actually ships. Use this "
                'for "build my game", "export it", "make a playable version", '
                '"ship it", "give me something I can send to a friend".\n\n'
                "Returns as soon as the build STARTS — it does not wait. Unity "
                "blocks its own main thread for the whole build (no async build "
                "API, and a second Unity instance cannot open the same project), "
                "so a call that waited would outlive the request timeout. Call "
                "get_build_status afterwards for the outcome, and tell the user "
                "the editor will be unresponsive while it runs.\n\n"
                'For "let people play it in a browser" use target="WebGL": it '
                "produces a folder hostable on any static server. Note it must "
                "be served over HTTP — opening index.html from disk (file://) "
                "shows a blank page.\n\n"
                "Everything checkable is checked before the editor freezes: the "
                "target's module is installed, the scenes exist, and the output "
                "is not inside Assets/ (Unity would import the entire player as "
                "project assets)."
            ),
            "parameters": {
                "type": "object",
                "properties": {
                    "target": {
                        "type": "string",
                        "description": (
                            'Build target: "WebGL" (browser, easiest to share), '
                            '"StandaloneWindows64", "StandaloneOSX", '
                            '"StandaloneLinux64", "Android", or "iOS". Aliases '
                            'like "web", "windows", "mac" are accepted. Omit to '
                            "use the project's active build target."
                        ),
                    },
                    "outputPath": {
                        "type": "string",
                        "description": (
                            "Where to write the player. Relative paths resolve "
                            "against the project root. Default: "
                            "Builds/<Platform>/<ProductName> with the right "
                            "extension. Must NOT be inside Assets/."
                        ),
                    },
                    "scenes": {
                        "type": "array",
                        "items": {"type": "string"},
                        "description": (
                            "Scenes for this build only, in boot order. Omit to "
                            "use Build Settings (or the open scene if that list "
                            "is empty). Use set_build_scenes to change the list "
                            "persistently."
                        ),
                    },
                    "development": {
                        "type": "boolean",
                        "description": (
                            "Development build with the debugger attached. "
                            "Default false — release, which is what you want for "
                            "anything the user will share."
                        ),
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_build_status",
            "description": (
                "Report the outcome of the last build_player call: succeeded or "
                "failed, the output path, its size, how long it took, and the "
                "engine's own errors.\n\n"
                'Read-only. If it returns status "running", the build is still '
                "going — Unity blocks its main thread while building, so a call "
                "sent mid-build may not answer until the editor is free again. "
                "That is expected, not a hang; poll again rather than restarting "
                "the build."
            ),
            "parameters": {"type": "object", "properties": {}, "required": []},
        },
    },
]
