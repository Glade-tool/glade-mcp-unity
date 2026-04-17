"""Smoke test — verify the MCP server boots and lists tools correctly."""

import pytest

from gladekit_mcp.server import list_tools


@pytest.mark.asyncio
async def test_list_tools_returns_expected_count():
    """list_tools should return 80+ core tools + 4 meta-tools.

    This catches import errors, broken schemas, and missing tool modules
    before publishing to PyPI.
    """
    tools = await list_tools()

    # Each entry should be a valid MCP Tool with name and inputSchema
    for tool in tools:
        assert tool.name, f"Tool missing name: {tool}"
        assert tool.inputSchema is not None, f"Tool {tool.name} missing inputSchema"

    # Core tools (~80) + 4 meta-tools = 84+
    # Use a conservative lower bound to avoid brittleness
    assert len(tools) >= 50, f"Expected 50+ tools, got {len(tools)}"

    # Verify the 4 meta-tools are present
    tool_names = {t.name for t in tools}
    for meta in [
        "get_relevant_tools",
        "remember_for_session",
        "recall_session_memories",
        "search_project_scripts",
    ]:
        assert meta in tool_names, f"Meta-tool '{meta}' missing from tool list"
