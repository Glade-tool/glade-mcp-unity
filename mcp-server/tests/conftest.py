"""Shared fixtures for MCP server tests."""

from __future__ import annotations

import json
from unittest.mock import patch

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
    """Clear session memory, skill, and telemetry state between tests."""
    from gladekit_mcp import server, skill, telemetry

    server._session_memory.clear()
    skill._session_messages.clear()
    skill._last_persisted_count.clear()
    telemetry.reset()
    telemetry.reset_clock()
    yield
    server._session_memory.clear()
    skill._session_messages.clear()
    skill._last_persisted_count.clear()
    telemetry.reset()
    telemetry.reset_clock()


@pytest.fixture(autouse=True)
def reset_shared_http_clients():
    """Reset module-level httpx clients between tests.

    pytest-asyncio creates a fresh event loop per test. Shared clients are
    loop-bound, so reusing one across tests trips RuntimeError. Resetting
    forces a fresh client on each test's loop.
    """
    from gladekit_mcp import bridge, cloud, search

    bridge._client = None
    cloud._http_client = None
    search._openai_client = None
    yield
    bridge._client = None
    cloud._http_client = None
    search._openai_client = None
