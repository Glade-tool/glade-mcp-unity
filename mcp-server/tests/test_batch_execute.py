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


# ── Godot ────────────────────────────────────────────────────────────────────


class TestBatchExecuteGodot:
    @pytest.mark.asyncio
    async def test_dispatches_each_call_over_the_ws_bridge(self, pin_engine_to_godot):
        """Godot has no batch endpoint: calls go one by one through the WS
        bridge with native JSON args, and a failure does not abort the batch."""
        seen: list[tuple[str, dict]] = []

        async def _godot_execute(tool_name, arguments, **kwargs):
            seen.append((tool_name, arguments))
            if tool_name == "set_node_transform":
                return json.dumps({"success": False, "message": "Node 'Missing' not found"})
            return json.dumps({"success": True, "message": f"{tool_name} ok", "node_path": "Foo"})

        unity_batch = AsyncMock(side_effect=AssertionError("Unity /api/batch must not be used on Godot"))
        with (
            patch("gladekit_mcp.bridge.godot_execute_tool", new=_godot_execute),
            patch("gladekit_mcp.bridge.execute_batch", new=unity_batch),
        ):
            result = await call_tool(
                "batch_execute",
                {
                    "calls": [
                        {"toolName": "create_node", "arguments": {"name": "Foo", "type": "Node3D"}},
                        {
                            "toolName": "set_node_transform",
                            "arguments": {"node_path": "Missing", "position": [1, 2.5, 3]},
                        },
                        {"toolName": "get_scene_tree"},
                    ]
                },
            )

        text = result[0].text
        assert "Batch executed 3 tool(s)" in text
        assert "[1] create_node: OK" in text
        assert "[2] set_node_transform: FAILED — Node 'Missing' not found" in text
        assert "[3] get_scene_tree: OK" in text

        assert [name for name, _ in seen] == ["create_node", "set_node_transform", "get_scene_tree"]
        # Native types survive: no Unity-style number→string coercion.
        assert seen[1][1]["position"] == [1, 2.5, 3]
        assert seen[2][1] == {}
        unity_batch.assert_not_called()

    @pytest.mark.asyncio
    async def test_unknown_tool_is_reported_per_call(self, pin_engine_to_godot):
        with patch("gladekit_mcp.bridge.godot_execute_tool", new=AsyncMock(return_value=json.dumps({"success": True}))):
            result = await call_tool(
                "batch_execute",
                {"calls": [{"toolName": "create_game_object", "arguments": {"name": "Cube"}}]},
            )
        assert "[1] create_game_object: FAILED — Unknown tool: create_game_object" in result[0].text
