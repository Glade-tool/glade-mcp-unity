"""Tests for batch_execute meta-tool — HTTP is mocked, no Unity required."""

from __future__ import annotations

import json
from unittest.mock import AsyncMock, patch

import pytest

from gladekit_mcp.server import call_tool

# ── Helpers ──────────────────────────────────────────────────────────────────


def _mock_batch_response(results: list[dict]):
    """Build a mock bridge batch response."""
    return results


def _success_result(tool_name: str, result_data: dict | None = None):
    return {
        "toolName": tool_name,
        "success": True,
        "result": json.dumps(result_data or {"success": True, "message": f"{tool_name} done"}),
        "error": None,
        "requiresCompilation": False,
    }


def _failure_result(tool_name: str, error: str):
    return {
        "toolName": tool_name,
        "success": False,
        "result": None,
        "error": error,
        "requiresCompilation": False,
    }


# ── Tests ────────────────────────────────────────────────────────────────────


class TestBatchExecute:
    @pytest.mark.asyncio
    async def test_basic_batch_two_tools(self):
        """Two successful tool calls return per-result status."""
        mock_results = [
            _success_result("create_game_object"),
            _success_result("set_transform"),
        ]
        with patch("gladekit_mcp.bridge.execute_batch", new=AsyncMock(return_value=mock_results)):
            result = await call_tool(
                "batch_execute",
                {
                    "calls": [
                        {"toolName": "create_game_object", "arguments": {"name": "Cube"}},
                        {"toolName": "set_transform", "arguments": {"gameObjectPath": "Cube", "positionX": "1"}},
                    ]
                },
            )

        text = result[0].text
        assert "2 tool(s)" in text
        assert "[1] create_game_object: OK" in text
        assert "[2] set_transform: OK" in text

    @pytest.mark.asyncio
    async def test_partial_failure(self):
        """One failure in a batch doesn't prevent others from succeeding."""
        mock_results = [
            _success_result("create_game_object"),
            _failure_result("set_transform", "GameObject not found"),
            _success_result("create_material"),
        ]
        with patch("gladekit_mcp.bridge.execute_batch", new=AsyncMock(return_value=mock_results)):
            result = await call_tool(
                "batch_execute",
                {
                    "calls": [
                        {"toolName": "create_game_object", "arguments": {"name": "Cube"}},
                        {"toolName": "set_transform", "arguments": {"gameObjectPath": "Missing"}},
                        {"toolName": "create_material", "arguments": {"name": "Red"}},
                    ]
                },
            )

        text = result[0].text
        assert "[1] create_game_object: OK" in text
        assert "[2] set_transform: FAILED" in text
        assert "GameObject not found" in text
        assert "[3] create_material: OK" in text

    @pytest.mark.asyncio
    async def test_empty_calls_returns_error(self):
        """Empty calls array returns an error message."""
        result = await call_tool("batch_execute", {"calls": []})
        assert "No tool calls" in result[0].text

    @pytest.mark.asyncio
    async def test_too_many_calls_returns_error(self):
        """More than 50 calls returns an error."""
        calls = [{"toolName": f"tool_{i}"} for i in range(51)]
        result = await call_tool("batch_execute", {"calls": calls})
        assert "Maximum 50" in result[0].text

    @pytest.mark.asyncio
    async def test_bridge_connection_error(self):
        """Bridge unreachable returns error, not exception."""
        with patch(
            "gladekit_mcp.bridge.execute_batch",
            new=AsyncMock(side_effect=Exception("Connection refused")),
        ):
            result = await call_tool(
                "batch_execute", {"calls": [{"toolName": "create_game_object", "arguments": {"name": "Test"}}]}
            )

        text = result[0].text
        assert "error" in text.lower()

    @pytest.mark.asyncio
    async def test_argument_sanitization(self):
        """Numeric arguments are coerced to strings before dispatch."""
        captured_calls = []

        async def _mock_batch(calls, **kwargs):
            captured_calls.extend(calls)
            return [_success_result(c["toolName"]) for c in calls]

        with patch("gladekit_mcp.bridge.execute_batch", new=_mock_batch):
            await call_tool(
                "batch_execute",
                {
                    "calls": [
                        {"toolName": "set_transform", "arguments": {"positionX": 1.5, "positionY": 0}},
                    ]
                },
            )

        assert len(captured_calls) == 1
        args = captured_calls[0]["arguments"]
        assert args["positionX"] == "1.5"
        assert args["positionY"] == "0"

    @pytest.mark.asyncio
    async def test_null_arguments_stripped(self):
        """None/null argument values are stripped before dispatch."""
        captured_calls = []

        async def _mock_batch(calls, **kwargs):
            captured_calls.extend(calls)
            return [_success_result(c["toolName"]) for c in calls]

        with patch("gladekit_mcp.bridge.execute_batch", new=_mock_batch):
            await call_tool(
                "batch_execute",
                {
                    "calls": [
                        {"toolName": "create_game_object", "arguments": {"name": "Cube", "parent": None}},
                    ]
                },
            )

        args = captured_calls[0]["arguments"]
        assert "parent" not in args
        assert args["name"] == "Cube"

    @pytest.mark.asyncio
    async def test_missing_arguments_defaults_to_empty(self):
        """Omitted arguments field defaults to empty dict."""
        captured_calls = []

        async def _mock_batch(calls, **kwargs):
            captured_calls.extend(calls)
            return [_success_result(c["toolName"]) for c in calls]

        with patch("gladekit_mcp.bridge.execute_batch", new=_mock_batch):
            await call_tool("batch_execute", {"calls": [{"toolName": "get_scene_hierarchy"}]})

        assert captured_calls[0]["arguments"] == {}
