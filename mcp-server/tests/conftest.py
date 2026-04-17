"""Shared fixtures for MCP server tests."""

from __future__ import annotations

import json
from unittest.mock import AsyncMock, MagicMock, patch

import pytest


@pytest.fixture
def mock_bridge_success():
    """Patch bridge.execute_tool to return a generic success response."""

    async def _execute(tool_name, args, **kwargs):
        return json.dumps({"success": True, "message": f"{tool_name} executed"})

    with patch("gladekit_mcp.bridge.execute_tool", new=_execute) as m:
        yield m


@pytest.fixture
def mock_bridge_health():
    """Patch bridge.check_health to return a healthy response."""

    async def _health(bridge_url=None):
        return {
            "status": "ok",
            "unityVersion": "6000.0.0f1",
            "projectName": "TestProject",
            "projectPath": "/tmp/TestProject",
        }

    with patch("gladekit_mcp.bridge.check_health", new=_health):
        yield


@pytest.fixture
def mock_bridge_context():
    """Patch bridge.gather_scene_context to return a test scene."""

    async def _gather(bridge_url=None):
        return {
            "sceneHierarchy": [
                {"name": "Main Camera", "path": "Main Camera"},
                {"name": "Player", "path": "Player"},
            ],
            "scripts": [
                {"name": "PlayerController", "path": "Assets/Scripts/PlayerController.cs"},
            ],
            "selection": {"selectedObjects": [{"name": "Player"}]},
        }

    with patch("gladekit_mcp.bridge.gather_scene_context", new=_gather):
        yield


@pytest.fixture(autouse=True)
def reset_session_memory():
    """Clear session memory between tests."""
    from gladekit_mcp import server
    server._session_memory.clear()
    yield
    server._session_memory.clear()
