"""
Integration tests — full MCP tool call → bridge → result round-trip.

Uses a mock HTTP server to simulate the Unity bridge, so no Unity instance
is needed. Tests the entire dispatch path: MCP call_tool → registry →
bridge HTTP → response parsing → MCP TextContent result.
"""

from __future__ import annotations

import asyncio
import json
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer
from unittest.mock import patch

import pytest

from gladekit_mcp.bridge import UnityBridgeError, execute_tool, gather_scene_context
from gladekit_mcp.server import call_tool

# ── Mock Unity bridge HTTP server ────────────────────────────────────────────


class MockBridgeHandler(BaseHTTPRequestHandler):
    """Simulates the Unity bridge HTTP API."""

    def log_message(self, format, *args):
        pass  # Suppress request logs during tests

    def do_GET(self):
        if self.path == "/api/health":
            self._respond(
                200,
                {
                    "status": "ok",
                    "unityVersion": "6000.0.0f1",
                    "projectName": "TestProject",
                    "projectPath": "/tmp/TestProject",
                },
            )
        elif self.path == "/api/compilation/status":
            self._respond(
                200,
                {
                    "isCompiling": False,
                    "compilationCount": 5,
                },
            )
        else:
            self._respond(404, {"error": "Not found"})

    def do_POST(self):
        content_length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(content_length)) if content_length else {}

        if self.path == "/api/tools/execute":
            tool_name = body.get("toolName", "")
            args = json.loads(body.get("arguments", "{}"))
            result = json.dumps(
                {
                    "success": True,
                    "message": f"{tool_name} executed",
                    **{k: v for k, v in args.items()},
                }
            )
            self._respond(
                200,
                {
                    "success": True,
                    "result": result,
                    "requiresCompilation": tool_name in ("create_script", "modify_script"),
                },
            )
        elif self.path == "/api/context/gather":
            self._respond(
                200,
                {
                    "context": json.dumps(
                        {
                            "sceneHierarchy": [
                                {"name": "Main Camera", "path": "Main Camera"},
                                {"name": "Player", "path": "Player"},
                            ],
                            "scripts": [
                                {"name": "PlayerController", "path": "Assets/Scripts/PlayerController.cs"},
                            ],
                        }
                    )
                },
            )
        else:
            self._respond(404, {"error": "Not found"})

    def _respond(self, status: int, data: dict):
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.end_headers()
        self.wfile.write(json.dumps(data).encode())


@pytest.fixture(scope="module")
def mock_bridge_server():
    """Start a mock Unity bridge HTTP server on a random port."""
    server = HTTPServer(("127.0.0.1", 0), MockBridgeHandler)
    port = server.server_address[1]
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    yield f"http://127.0.0.1:{port}"
    server.shutdown()


# ── Bridge HTTP round-trip tests ─────────────────────────────────────────────


class TestBridgeRoundTrip:
    """Test the bridge HTTP client against the mock server."""

    @pytest.mark.asyncio
    async def test_execute_tool_success(self, mock_bridge_server):
        result = await execute_tool(
            "create_game_object",
            {"name": "TestCube"},
            bridge_url=mock_bridge_server,
            wait_for_compilation=False,
        )
        data = json.loads(result)
        assert data["success"] is True
        assert "create_game_object" in data["message"]

    @pytest.mark.asyncio
    async def test_execute_tool_args_forwarded(self, mock_bridge_server):
        result = await execute_tool(
            "set_transform",
            {"gameObjectName": "Player", "x": "1", "y": "2", "z": "3"},
            bridge_url=mock_bridge_server,
            wait_for_compilation=False,
        )
        data = json.loads(result)
        assert data["success"] is True
        assert data.get("gameObjectName") == "Player"

    @pytest.mark.asyncio
    async def test_gather_scene_context(self, mock_bridge_server):
        context = await gather_scene_context(bridge_url=mock_bridge_server)
        assert "sceneHierarchy" in context
        assert len(context["sceneHierarchy"]) == 2
        assert context["sceneHierarchy"][0]["name"] == "Main Camera"

    @pytest.mark.asyncio
    async def test_execute_tool_unreachable(self):
        result = await execute_tool(
            "create_game_object",
            {"name": "Test"},
            bridge_url="http://127.0.0.1:1",  # unreachable port
            wait_for_compilation=False,
        )
        data = json.loads(result)
        assert data["success"] is False

    @pytest.mark.asyncio
    async def test_gather_context_unreachable(self):
        with pytest.raises(UnityBridgeError):
            await gather_scene_context(bridge_url="http://127.0.0.1:1")


# ── MCP → bridge end-to-end ──────────────────────────────────────────────────


class TestMCPToolDispatch:
    """Test full MCP call_tool → dispatch → bridge → result path."""

    @pytest.mark.asyncio
    async def test_call_tool_dispatches_to_bridge(self, mock_bridge_server):
        """call_tool for a Unity tool should dispatch through the bridge."""
        # Patch execute_tool to use our mock bridge URL
        from gladekit_mcp import bridge as bridge_mod

        original = bridge_mod.execute_tool

        async def _patched(name, args, bridge_url=None, **kwargs):
            return await original(name, args, bridge_url=mock_bridge_server, wait_for_compilation=False)

        with patch.object(bridge_mod, "execute_tool", _patched):
            result = await call_tool("create_game_object", {"name": "MCPTest"})

        assert len(result) == 1
        data = json.loads(result[0].text)
        assert data["success"] is True

    @pytest.mark.asyncio
    async def test_call_tool_unknown_returns_error(self):
        """Unknown tool name → error JSON, not an exception."""
        result = await call_tool("totally_fake_tool_xyz", {})
        assert len(result) == 1
        data = json.loads(result[0].text)
        assert data["success"] is False

    @pytest.mark.asyncio
    async def test_meta_tool_bypasses_bridge(self):
        """Meta-tools (session memory) should NOT hit the bridge."""
        result = await call_tool("recall_session_memories", {})
        assert "No session memories" in result[0].text


# ── Compilation wait ─────────────────────────────────────────────────────────


class TestCompilationWait:
    @pytest.mark.asyncio
    async def test_compilation_wait_returns_on_count_increment(self, mock_bridge_server):
        """_wait_for_compilation returns when compilationCount exceeds baseline."""
        from gladekit_mcp.bridge import _wait_for_compilation

        # Mock server returns compilationCount=5, baseline=4 → immediate return
        await asyncio.wait_for(
            _wait_for_compilation(mock_bridge_server, baseline_count=4),
            timeout=5.0,
        )
