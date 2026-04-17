"""Tests for bridge error handling — no Unity instance required (HTTP is mocked)."""

from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from gladekit_mcp.bridge import UnityBridgeError, check_health, execute_batch, execute_tool


def _mock_client(get=None, post=None):
    """Build a mock httpx.AsyncClient with the given async methods.

    Pass an Exception instance to raise, or a return-value object to return.
    """
    mock = MagicMock()
    if isinstance(get, BaseException):
        mock.get = AsyncMock(side_effect=get)
    else:
        mock.get = AsyncMock(return_value=get)
    if isinstance(post, BaseException):
        mock.post = AsyncMock(side_effect=post)
    else:
        mock.post = AsyncMock(return_value=post)
    return mock


@pytest.mark.asyncio
async def test_check_health_connection_refused():
    """Bridge not running → UnityBridgeError with a useful message."""
    mock = _mock_client(get=httpx.ConnectError("Connection refused"))
    with patch("gladekit_mcp.bridge._get_client", return_value=mock):
        with pytest.raises(UnityBridgeError) as exc_info:
            await check_health()

    assert "not reachable" in str(exc_info.value)


@pytest.mark.asyncio
async def test_check_health_timeout():
    """Bridge timeout → UnityBridgeError (not a raw timeout exception)."""
    mock = _mock_client(get=httpx.TimeoutException("timed out"))
    with patch("gladekit_mcp.bridge._get_client", return_value=mock):
        with pytest.raises(UnityBridgeError):
            await check_health()


@pytest.mark.asyncio
async def test_execute_tool_bridge_error_returns_json():
    """execute_tool returns a JSON error string (not an exception) on connection failure.
    This lets the MCP client display the error message rather than crashing."""
    import json as _json

    mock = _mock_client(post=httpx.ConnectError("Connection refused"))
    with patch("gladekit_mcp.bridge._get_client", return_value=mock):
        result = await execute_tool("create_game_object", {"name": "Test"})

    data = _json.loads(result)
    assert data["success"] is False
    assert "Unity bridge error" in data["message"]


@pytest.mark.asyncio
async def test_check_health_returns_dict_on_success():
    """Successful health check returns the response dict."""
    mock_response = MagicMock()
    mock_response.raise_for_status = MagicMock(return_value=None)
    mock_response.json = MagicMock(return_value={"status": "ok", "projectPath": "/tmp/project"})

    mock = _mock_client(get=mock_response)
    with patch("gladekit_mcp.bridge._get_client", return_value=mock):
        result = await check_health()

    assert result["status"] == "ok"
    assert "projectPath" in result


@pytest.mark.asyncio
async def test_execute_batch_connection_error_returns_error_list():
    """execute_batch returns a single error entry on connection failure."""
    mock = _mock_client(post=httpx.ConnectError("Connection refused"))
    with patch("gladekit_mcp.bridge._get_client", return_value=mock):
        results = await execute_batch(
            [
                {"toolName": "create_game_object", "arguments": {"name": "Test"}},
            ]
        )

    assert len(results) == 1
    assert results[0]["success"] is False
    assert "Unity bridge error" in results[0]["error"]


@pytest.mark.asyncio
async def test_execute_batch_success():
    """Successful batch returns per-call results."""
    mock_response = MagicMock()
    mock_response.json = MagicMock(
        return_value={
            "success": True,
            "results": [
                {
                    "toolName": "create_game_object",
                    "success": True,
                    "result": '{"success":true}',
                    "requiresCompilation": False,
                },
                {
                    "toolName": "set_transform",
                    "success": True,
                    "result": '{"success":true}',
                    "requiresCompilation": False,
                },
            ],
        }
    )

    mock = _mock_client(post=mock_response)
    with patch("gladekit_mcp.bridge._get_client", return_value=mock):
        results = await execute_batch(
            [
                {"toolName": "create_game_object", "arguments": {"name": "Cube"}},
                {"toolName": "set_transform", "arguments": {"gameObjectPath": "Cube"}},
            ]
        )

    assert len(results) == 2
    assert results[0]["success"] is True
    assert results[1]["success"] is True
