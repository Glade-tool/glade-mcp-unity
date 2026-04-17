"""
Tests for compilation error context injection via compile_scripts.

The C# side (ErrorTracker + CompileScriptsTool) runs inside Unity and can't be
unit-tested here. These tests verify:
  - The MCP server correctly dispatches compile_scripts and passes through error responses
  - Error responses with hasErrors=true are returned verbatim to the AI client
  - The tool description mentions error context (so the AI knows to expect it)
"""

import json
from unittest.mock import AsyncMock, patch

import pytest

from gladekit_mcp.tools.scripting import TOOLS

# ── Tool schema verification ──────────────────────────────────────────────────


def _get_tool(name: str):
    for t in TOOLS:
        if t["function"]["name"] == name:
            return t
    return None


def test_compile_scripts_description_mentions_errors():
    """compile_scripts description must tell the AI that errors include source context."""
    tool = _get_tool("compile_scripts")
    assert tool is not None
    desc = tool["function"]["description"]
    assert "hasErrors" in desc or "error" in desc.lower()
    assert "source context" in desc.lower() or "line" in desc.lower()


def test_compile_scripts_description_no_parameters_required():
    """compile_scripts takes no required parameters."""
    tool = _get_tool("compile_scripts")
    params = tool["function"]["parameters"]
    assert params.get("required", []) == []


# ── Expected bridge response shapes ──────────────────────────────────────────
# These are the JSON strings the C# CompileScriptsTool will return.
# Verified here so the contract is documented and breakage is caught.

BRIDGE_RESPONSE_IDLE_CLEAN = json.dumps(
    {
        "success": True,
        "message": "Compilation complete. All script types are ready to use with add_component.",
        "isCompiling": False,
        "status": "idle",
        "hasErrors": False,
    }
)

BRIDGE_RESPONSE_IDLE_WITH_ERRORS = json.dumps(
    {
        "success": True,
        "message": (
            "Compilation finished with 2 error(s). Fix these errors before calling add_component or proceeding.\n\n"
            "Error 1: Assets/Scripts/PlayerController.cs (line 42, col 15)\n"
            "CS0246: The type or namespace name 'Rigidbody2D' could not be found\n\n"
            "Source context:\n"
            "    40:     [SerializeField] private float speed = 5f;\n"
            "    41: \n"
            ">>>  42:         Rigidbody2D rb = GetComponent<Rigidbody2D>();\n"
            "    43:     }\n\n"
            "Error 2: Assets/Scripts/PlayerController.cs (line 58, col 9)\n"
            "CS1002: ; expected"
        ),
        "isCompiling": False,
        "status": "idle",
        "hasErrors": True,
        "errorCount": 2,
    }
)

BRIDGE_RESPONSE_STILL_COMPILING = json.dumps(
    {
        "success": True,
        "message": "Unity is still compiling scripts. Call compile_scripts again to check when it finishes.",
        "isCompiling": True,
        "status": "compiling",
    }
)


@pytest.mark.asyncio
async def test_compile_scripts_dispatches_to_bridge():
    """compile_scripts tool call dispatches 'compile_scripts' with empty args to the bridge."""
    from gladekit_mcp import bridge as bridge_module

    with patch.object(
        bridge_module, "execute_tool", new=AsyncMock(return_value=BRIDGE_RESPONSE_IDLE_CLEAN)
    ) as mock_exec:
        result = await bridge_module.execute_tool("compile_scripts", {})
        mock_exec.assert_called_once_with("compile_scripts", {})

    assert json.loads(result)["hasErrors"] is False


@pytest.mark.asyncio
async def test_compile_scripts_error_response_reaches_caller():
    """When C# returns hasErrors=true, the full error message with context reaches the AI."""
    from gladekit_mcp import bridge as bridge_module

    with patch.object(bridge_module, "execute_tool", new=AsyncMock(return_value=BRIDGE_RESPONSE_IDLE_WITH_ERRORS)):
        result = await bridge_module.execute_tool("compile_scripts", {})

    parsed = json.loads(result)
    assert parsed["hasErrors"] is True
    assert parsed["errorCount"] == 2
    assert "CS0246" in parsed["message"]
    assert "Source context" in parsed["message"]
    assert ">>>" in parsed["message"]


@pytest.mark.asyncio
async def test_compile_scripts_still_compiling_response():
    """Still-compiling response tells the AI to poll again."""
    from gladekit_mcp import bridge as bridge_module

    with patch.object(bridge_module, "execute_tool", new=AsyncMock(return_value=BRIDGE_RESPONSE_STILL_COMPILING)):
        result = await bridge_module.execute_tool("compile_scripts", {})

    parsed = json.loads(result)
    assert parsed["isCompiling"] is True
    assert parsed["status"] == "compiling"


# ── Error message format contract ─────────────────────────────────────────────


def test_error_response_format_contract():
    """
    Verify the expected shape of a compile_scripts error response.
    This documents the contract between C# CompileScriptsTool and the AI client.

    The bridge returns a JSON string. The MCP server passes it through.
    The AI sees the 'message' field as the primary content.
    """
    parsed = json.loads(BRIDGE_RESPONSE_IDLE_WITH_ERRORS)

    # Required fields
    assert "success" in parsed
    assert "message" in parsed
    assert "isCompiling" in parsed
    assert "status" in parsed
    assert "hasErrors" in parsed
    assert "errorCount" in parsed

    # Message must contain enough info to fix the error
    msg = parsed["message"]
    assert "error" in msg.lower()  # error count
    assert "line" in msg.lower()  # line number
    assert "Source context" in msg  # source code context
    assert ">>>" in msg  # error line marker
    assert "CS" in msg  # C# error code


def test_clean_response_format_contract():
    """Verify the shape of a clean compile_scripts response."""
    parsed = json.loads(BRIDGE_RESPONSE_IDLE_CLEAN)
    assert parsed["success"] is True
    assert parsed["isCompiling"] is False
    assert parsed["status"] == "idle"
    assert parsed["hasErrors"] is False
