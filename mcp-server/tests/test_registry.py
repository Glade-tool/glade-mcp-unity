"""Tests for tool registry — schema conversion, core filtering, dispatch, arg sanitization."""

from __future__ import annotations

import json

import pytest

from gladekit_mcp.tools import get_unity_tool_schemas
from gladekit_mcp.tools.registry import (
    CORE_TOOLS,
    _convert_openai_to_mcp,
    dispatch_tool_call,
    get_mcp_tools,
)

# ── Schema validation ────────────────────────────────────────────────────────


class TestSchemaValidation:
    """Verify all tool schemas are well-formed."""

    def test_all_schemas_have_required_fields(self):
        """Every tool schema must have type=function and function.name."""
        schemas = get_unity_tool_schemas()
        for schema in schemas:
            assert schema.get("type") == "function", f"Bad schema type: {schema}"
            func = schema.get("function", {})
            assert func.get("name"), f"Schema missing function.name: {schema}"
            assert func.get("description"), f"Schema missing description: {func.get('name')}"

    def test_all_schemas_have_valid_parameters(self):
        """Every tool schema's parameters must be a valid JSON Schema object."""
        schemas = get_unity_tool_schemas()
        for schema in schemas:
            func = schema["function"]
            params = func.get("parameters", {})
            assert params.get("type") == "object", (
                f"Tool {func['name']} parameters.type must be 'object', got {params.get('type')}"
            )
            assert "properties" in params, f"Tool {func['name']} parameters missing 'properties'"

    def test_no_duplicate_tool_names(self):
        """Tool names must be unique across all schemas.

        MCP spec requires unique tool names. Windsurf hard-fails on duplicates
        ("Duplicate tool name: mcp_*_X") and the chat becomes unusable until the
        server is disabled. Claude Code is tolerant, which is why a regression
        here is invisible to our daily usage — keep this test strict.
        """
        schemas = get_unity_tool_schemas()
        names = [s["function"]["name"] for s in schemas]
        duplicates = sorted(n for n in set(names) if names.count(n) > 1)
        assert not duplicates, f"Duplicate tool names: {duplicates}"

    def test_tool_names_are_snake_case(self):
        """All tool names must be snake_case (lowercase with underscores)."""
        schemas = get_unity_tool_schemas()
        import re

        snake_re = re.compile(r"^[a-z][a-z0-9_]*$")
        for schema in schemas:
            name = schema["function"]["name"]
            assert snake_re.match(name), f"Tool name not snake_case: '{name}'"

    def test_minimum_tool_count(self):
        """We should have 200+ tools total."""
        schemas = get_unity_tool_schemas()
        assert len(schemas) >= 200, f"Expected 200+ tools, got {len(schemas)}"


# ── Core tool set ────────────────────────────────────────────────────────────


class TestCoreToolSet:
    """Verify core tool filtering and MCP conversion."""

    def test_core_tools_are_subset_of_all(self):
        """Every CORE_TOOLS entry must exist in the full schema set."""
        all_names = {s["function"]["name"] for s in get_unity_tool_schemas()}
        missing = CORE_TOOLS - all_names
        assert not missing, f"CORE_TOOLS references non-existent tools: {missing}"

    def test_get_mcp_tools_returns_only_core(self):
        """get_mcp_tools must return only tools in CORE_TOOLS."""
        tools = get_mcp_tools()
        for tool in tools:
            assert tool.name in CORE_TOOLS, f"Non-core tool '{tool.name}' in MCP tool list"

    def test_get_mcp_tools_count_within_budget(self):
        """Core tools must stay within Claude Code's ~128 tool budget."""
        tools = get_mcp_tools()
        assert len(tools) <= 128, f"Too many core tools ({len(tools)}); Claude Code limit is ~128"

    def test_essential_tools_in_core(self):
        """Critical everyday tools must be in CORE_TOOLS."""
        essentials = {
            "get_scene_hierarchy",
            "create_game_object",
            "create_primitive",
            "set_transform",
            "create_script",
            "modify_script",
            "add_rigidbody",
            "create_material",
            "create_light",
            "add_component",
            "destroy_game_object",
            "save_scene",
        }
        missing = essentials - CORE_TOOLS
        assert not missing, f"Essential tools missing from CORE_TOOLS: {missing}"


# ── OpenAI → MCP conversion ─────────────────────────────────────────────────


class TestSchemaConversion:
    def test_converts_basic_schema(self):
        openai_schema = {
            "type": "function",
            "function": {
                "name": "test_tool",
                "description": "A test tool",
                "parameters": {
                    "type": "object",
                    "properties": {
                        "name": {"type": "string", "description": "Name param"},
                    },
                    "required": ["name"],
                },
            },
        }
        mcp_tool = _convert_openai_to_mcp(openai_schema)
        assert mcp_tool.name == "test_tool"
        assert mcp_tool.description == "A test tool"
        assert mcp_tool.inputSchema["type"] == "object"
        assert "name" in mcp_tool.inputSchema["properties"]

    def test_handles_missing_parameters(self):
        """Tools with no parameters get a default empty schema."""
        openai_schema = {
            "type": "function",
            "function": {
                "name": "no_params",
                "description": "No params",
            },
        }
        mcp_tool = _convert_openai_to_mcp(openai_schema)
        assert mcp_tool.inputSchema == {"type": "object", "properties": {}}


# ── Argument sanitization ────────────────────────────────────────────────────


class TestArgSanitization:
    """Verify dispatch_tool_call sanitizes LLM quirks."""

    @pytest.mark.asyncio
    async def test_null_values_stripped(self, mock_bridge_success):
        """Null argument values should be stripped before dispatch."""
        result = await dispatch_tool_call("create_game_object", {"name": "Cube", "parent": None})
        data = json.loads(result)
        assert data["success"] is True

    @pytest.mark.asyncio
    async def test_numeric_values_coerced_to_string(self, mock_bridge_success):
        """Numeric values should be coerced to strings for Unity bridge."""
        result = await dispatch_tool_call("set_transform", {"gameObjectName": "Cube", "x": 1.5, "y": 0})
        data = json.loads(result)
        assert data["success"] is True

    @pytest.mark.asyncio
    async def test_bool_values_preserved(self, mock_bridge_success):
        """Boolean values should NOT be coerced to strings."""
        # Booleans are isinstance(bool, int) → True, so the coercion must skip them
        result = await dispatch_tool_call("set_game_object_active", {"gameObjectName": "Cube", "active": True})
        data = json.loads(result)
        assert data["success"] is True

    @pytest.mark.asyncio
    async def test_unknown_tool_returns_error(self):
        """Dispatching a non-existent tool returns a JSON error (not exception)."""
        result = await dispatch_tool_call("completely_fake_tool", {})
        data = json.loads(result)
        assert data["success"] is False
        assert "Unknown tool" in data["message"]
