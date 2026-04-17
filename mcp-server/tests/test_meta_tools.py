"""Tests for MCP meta-tools — session memory, relevant tools, resources."""

from __future__ import annotations

import json
from unittest.mock import patch

import pytest

from gladekit_mcp.server import call_tool, list_resources, read_resource


# ── Session memory ────────────────────────────────────────────────────────────


class TestSessionMemory:
    @pytest.mark.asyncio
    async def test_remember_and_recall(self):
        """Store a fact, then recall it."""
        # Store
        result = await call_tool("remember_for_session", {"fact": "Player uses CharacterController"})
        assert len(result) == 1
        assert "1 item" in result[0].text

        # Recall
        result = await call_tool("recall_session_memories", {})
        assert "Player uses CharacterController" in result[0].text

    @pytest.mark.asyncio
    async def test_recall_empty(self):
        """Recalling with no stored memories returns a helpful message."""
        result = await call_tool("recall_session_memories", {})
        assert "No session memories" in result[0].text

    @pytest.mark.asyncio
    async def test_remember_multiple(self):
        """Multiple facts accumulate in order."""
        await call_tool("remember_for_session", {"fact": "Fact A"})
        await call_tool("remember_for_session", {"fact": "Fact B"})
        result = await call_tool("recall_session_memories", {})
        text = result[0].text
        assert "1. Fact A" in text
        assert "2. Fact B" in text

    @pytest.mark.asyncio
    async def test_remember_empty_fact(self):
        """Empty fact string returns an error, doesn't store."""
        result = await call_tool("remember_for_session", {"fact": "   "})
        assert "No fact" in result[0].text

        result = await call_tool("recall_session_memories", {})
        assert "No session memories" in result[0].text


# ── get_relevant_tools ───────────────────────────────────────────────────────


class TestGetRelevantTools:
    @pytest.mark.asyncio
    async def test_returns_relevant_summary(self):
        result = await call_tool("get_relevant_tools", {"message": "add a rigidbody"})
        text = result[0].text
        assert "physics" in text.lower() or "rigidbody" in text.lower()

    @pytest.mark.asyncio
    async def test_unrecognized_returns_all(self):
        result = await call_tool("get_relevant_tools", {"message": "xyzzy"})
        text = result[0].text
        assert "all" in text.lower() or "All" in text


# ── Resources ────────────────────────────────────────────────────────────────


class TestResources:
    @pytest.mark.asyncio
    async def test_list_resources(self):
        resources = await list_resources()
        uris = {str(r.uri) for r in resources}
        assert "unity://health" in uris
        assert "unity://context" in uris
        assert "unity://scene/hierarchy" in uris
        assert "unity://session-memory" in uris

    @pytest.mark.asyncio
    async def test_session_memory_resource_empty(self):
        text = await read_resource("unity://session-memory")
        assert "No session memories" in text

    @pytest.mark.asyncio
    async def test_session_memory_resource_with_data(self):
        await call_tool("remember_for_session", {"fact": "Test fact"})
        text = await read_resource("unity://session-memory")
        assert "Test fact" in text

    @pytest.mark.asyncio
    async def test_health_resource_bridge_down(self):
        """Health resource returns error JSON when bridge is unreachable."""
        from gladekit_mcp.bridge import UnityBridgeError
        with patch(
            "gladekit_mcp.bridge.check_health",
            side_effect=UnityBridgeError("Connection refused"),
        ):
            text = await read_resource("unity://health")
            data = json.loads(text)
            assert "error" in data

    @pytest.mark.asyncio
    async def test_health_resource_bridge_up(self, mock_bridge_health):
        text = await read_resource("unity://health")
        data = json.loads(text)
        assert data["status"] == "ok"
        assert data["projectName"] == "TestProject"

    @pytest.mark.asyncio
    async def test_context_resource(self, mock_bridge_context):
        text = await read_resource("unity://context")
        data = json.loads(text)
        assert "sceneHierarchy" in data

    @pytest.mark.asyncio
    async def test_unknown_resource(self):
        text = await read_resource("unity://nonexistent")
        data = json.loads(text)
        assert "error" in data
