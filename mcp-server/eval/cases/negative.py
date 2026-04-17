"""
Negative MCP eval cases — tests what the MCP server should NOT do.

These verify error handling, edge cases, and safety boundaries
at the MCP protocol layer.
"""
from __future__ import annotations

from eval.cases import MCPEvalCase

NEGATIVE = [
    # ── Unknown tool ─────────────────────────────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-01",
        prompt="Call a non-existent tool called super_transform",
        description="Unknown tool should return error JSON, not crash",
        category="error-handling",
        forbidden_tools=["super_transform"],
        tags=["negative", "error-handling"],
    ),

    # ── Empty arguments ──────────────────────────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-02",
        prompt="Create a game object with no name",
        description="Missing required params handled gracefully",
        category="error-handling",
        required_tools=["create_game_object"],
        tags=["negative", "error-handling"],
    ),

    # ── Read-only queries should not mutate ──────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-03",
        prompt="List all GameObjects in the scene",
        description="Query should use get_scene_hierarchy, not create/destroy",
        category="read-only",
        required_tools=["get_scene_hierarchy"],
        forbidden_tools=[
            "create_game_object",
            "create_primitive",
            "destroy_game_object",
            "create_script",
            "modify_script",
        ],
        tags=["negative", "read-only"],
    ),

    # ── Session memory edge cases ────────────────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-04",
        prompt="Remember an empty fact",
        description="Empty fact should be rejected gracefully",
        category="meta",
        required_tools=["remember_for_session"],
        tags=["negative", "meta"],
    ),

    MCPEvalCase(
        id="mcp-neg-05",
        prompt="Recall memories when none exist",
        description="Empty recall returns helpful message, not error",
        category="meta",
        required_tools=["recall_session_memories"],
        tags=["negative", "meta"],
    ),

    # ── Bridge unavailable ───────────────────────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-06",
        prompt="Create a cube when Unity bridge is down",
        description="Bridge offline returns JSON error, not exception",
        category="error-handling",
        required_tools=["create_game_object"],
        tags=["negative", "error-handling", "bridge-down"],
    ),

    # ── Tool dispatch with null args ─────────────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-07",
        prompt="Set transform with null values",
        description="Null args stripped before dispatch (LLM quirk handling)",
        category="sanitization",
        required_tools=["set_transform"],
        tags=["negative", "sanitization"],
    ),

    # ── Numeric coercion ─────────────────────────────────────────────────────

    MCPEvalCase(
        id="mcp-neg-08",
        prompt="Set transform with numeric values instead of strings",
        description="Numeric args coerced to strings for Unity bridge",
        category="sanitization",
        required_tools=["set_transform"],
        tags=["negative", "sanitization"],
    ),
]
