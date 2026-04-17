"""
Mock Unity bridge HTTP server for eval harness.

Simulates the Unity Editor's HTTP API on localhost so the MCP server can
be tested end-to-end without a real Unity instance. Returns configurable
mock responses for each tool call.

Start with:
    server = start_mock_bridge(port=0)  # random port
    url = f"http://127.0.0.1:{server.server_address[1]}"
    ...
    server.shutdown()
"""
from __future__ import annotations

import json
import threading
from http.server import HTTPServer, BaseHTTPRequestHandler
from typing import Any, Optional


# ── Default mock data ────────────────────────────────────────────────────────

DEFAULT_SCENE = {
    "objects": [
        {
            "name": "Main Camera",
            "path": "Main Camera",
            "components": ["Transform", "Camera", "AudioListener"],
            "active": True,
        },
        {
            "name": "Directional Light",
            "path": "Directional Light",
            "components": ["Transform", "Light"],
            "active": True,
        },
        {
            "name": "Player",
            "path": "Player",
            "components": ["Transform", "MeshRenderer", "MeshFilter"],
            "active": True,
            "position": {"x": 0, "y": 1, "z": 0},
        },
        {
            "name": "Ground",
            "path": "Ground",
            "components": ["Transform", "MeshRenderer", "MeshFilter", "BoxCollider"],
            "active": True,
            "position": {"x": 0, "y": 0, "z": 0},
        },
    ]
}

SCENE_WITH_ENEMY = {
    "objects": [
        *DEFAULT_SCENE["objects"],
        {
            "name": "Enemy",
            "path": "Enemy",
            "components": ["Transform", "MeshRenderer", "MeshFilter"],
            "active": True,
            "position": {"x": 5, "y": 1, "z": 0},
        },
    ]
}


def _ok(message: str, **kwargs: Any) -> str:
    return json.dumps({"success": True, "message": message, **kwargs})


TOOL_DEFAULTS: dict[str, str] = {
    "create_game_object": _ok("Created GameObject", gameObjectPath="Player"),
    "create_primitive": _ok("Created Cube", gameObjectPath="Cube"),
    "destroy_game_object": _ok("Destroyed GameObject"),
    "set_transform": _ok("Transform updated"),
    "set_local_transform": _ok("Local transform updated"),
    "add_component": _ok("Component added"),
    "remove_component": _ok("Component removed"),
    "add_rigidbody": _ok("Rigidbody added"),
    "create_collider": _ok("Collider created"),
    "create_script": _ok("Script created", scriptPath="Assets/Scripts/NewScript.cs"),
    "modify_script": _ok("Script modified"),
    "get_script_content": _ok("Script content", content="using UnityEngine;\npublic class Test : MonoBehaviour { }"),
    "find_scripts": _ok("Scripts found", scripts=[]),
    "get_scene_hierarchy": _ok("Scene hierarchy retrieved", hierarchy=[]),
    "get_gameobject_info": _ok("GameObject info", name="Player"),
    "find_game_objects": _ok("Found", gameObjects=[{"name": "Player", "path": "Player"}]),
    "create_material": _ok("Material created", materialPath="Assets/Materials/New.mat"),
    "assign_material_to_renderer": _ok("Material assigned"),
    "create_light": _ok("Light created"),
    "set_light_properties": _ok("Light updated"),
    "create_prefab": _ok("Prefab created"),
    "instantiate_prefab": _ok("Prefab instantiated"),
    "create_animator_controller": _ok("Animator created"),
    "add_animator_state": _ok("State added"),
    "add_animator_transition": _ok("Transition added"),
    "add_animator_parameters": _ok("Parameter added"),
    "create_canvas": _ok("Canvas created"),
    "create_ui_element": _ok("UI element created"),
    "create_camera": _ok("Camera created"),
    "set_camera_properties": _ok("Camera properties set"),
    "create_audio_source": _ok("Audio source created"),
    "set_audio_source_properties": _ok("Audio properties set"),
    "set_game_object_active": _ok("Active state set"),
    "set_game_object_parent": _ok("Parent set"),
    "duplicate_game_object": _ok("Duplicated"),
    "rename_game_object": _ok("Renamed"),
    "set_component_property": _ok("Property set"),
    "save_scene": _ok("Scene saved"),
    "think": _ok("Reasoning complete"),
    "compile_scripts": _ok("Compilation triggered"),
    "set_render_settings": _ok("Render settings updated"),
    "create_character_controller": _ok("Character controller created"),
    "set_rigidbody_properties": _ok("Rigidbody updated"),
    "set_collider_properties": _ok("Collider updated"),
    "change_material_shader": _ok("Shader changed"),
    "set_material_property": _ok("Material property set"),
    "list_materials": _ok("Materials listed", materials=[]),
    "check_asset_exists": _ok("Asset found", exists=True),
    "list_assets": _ok("Assets listed", assets=[]),
    "create_folder": _ok("Folder created"),
    "get_unity_console_logs": _ok("Console logs", logs=[]),
    "get_gameobject_components": _ok("Components", components=["Transform"]),
    "list_children": _ok("Children", children=[]),
    "get_selection": _ok("Selection", selection=[]),
    "set_selection": _ok("Selection updated"),
    "refresh_asset_database": _ok("Refreshed"),
}


def get_result_for(
    tool_name: str,
    tool_args: dict,
    scene: Optional[dict] = None,
    overrides: Optional[dict[str, Any]] = None,
) -> str:
    """Return mock result for a tool call."""
    if overrides and tool_name in overrides:
        v = overrides[tool_name]
        return json.dumps(v) if isinstance(v, dict) else str(v)

    if tool_name == "get_scene_hierarchy" and scene:
        return _ok(
            "Scene hierarchy retrieved",
            hierarchy=[
                {"name": o["name"], "path": o["path"], "active": o.get("active", True)}
                for o in scene.get("objects", [])
            ],
        )

    if tool_name in TOOL_DEFAULTS:
        return TOOL_DEFAULTS[tool_name]

    return _ok(f"Tool '{tool_name}' executed successfully")


# ── HTTP server ──────────────────────────────────────────────────────────────


class MockBridgeHandler(BaseHTTPRequestHandler):
    """HTTP request handler simulating the Unity bridge API."""

    # Set by the server factory function
    scene: dict = DEFAULT_SCENE
    tool_overrides: Optional[dict[str, Any]] = None
    tool_log: list[dict] = []

    def log_message(self, format, *args):
        pass

    def do_GET(self):
        if self.path == "/api/health":
            self._respond(200, {
                "status": "ok",
                "unityVersion": "6000.0.0f1",
                "projectName": "EvalProject",
                "projectPath": "/tmp/EvalProject",
            })
        elif self.path == "/api/compilation/status":
            self._respond(200, {"isCompiling": False, "compilationCount": 99})
        else:
            self._respond(404, {"error": "Not found"})

    def do_POST(self):
        content_length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(content_length)) if content_length else {}

        if self.path == "/api/tools/execute":
            tool_name = body.get("toolName", "")
            args_raw = body.get("arguments", "{}")
            args = json.loads(args_raw) if isinstance(args_raw, str) else args_raw

            # Log the call for assertions
            self.__class__.tool_log.append({"name": tool_name, "args": args})

            result = get_result_for(
                tool_name, args, self.__class__.scene, self.__class__.tool_overrides
            )
            self._respond(200, {
                "success": True,
                "result": result,
                "requiresCompilation": tool_name in ("create_script", "modify_script"),
                "compilationCount": 99,
            })

        elif self.path == "/api/context/gather":
            ctx = {
                "sceneHierarchy": [
                    {"name": o["name"], "path": o["path"]}
                    for o in self.__class__.scene.get("objects", [])
                ],
                "scripts": [],
                "selection": {},
            }
            self._respond(200, {"context": json.dumps(ctx)})
        else:
            self._respond(404, {"error": "Not found"})

    def _respond(self, status: int, data: dict):
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(json.dumps(data).encode())


def start_mock_bridge(
    scene: Optional[dict] = None,
    tool_overrides: Optional[dict[str, Any]] = None,
    port: int = 0,
) -> HTTPServer:
    """Start a mock Unity bridge and return the HTTPServer instance.

    The server runs in a daemon thread. Call server.shutdown() to stop.
    Access the URL via f"http://127.0.0.1:{server.server_address[1]}".
    """
    MockBridgeHandler.scene = scene or DEFAULT_SCENE
    MockBridgeHandler.tool_overrides = tool_overrides
    MockBridgeHandler.tool_log = []

    server = HTTPServer(("127.0.0.1", port), MockBridgeHandler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    return server
