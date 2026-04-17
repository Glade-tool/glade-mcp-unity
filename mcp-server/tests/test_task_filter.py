"""Tests for task-aware tool filtering — keyword → category mapping."""

from __future__ import annotations

import pytest

from gladekit_mcp.tools.task_filter import (
    ALWAYS_INCLUDED,
    categorize_message,
    get_relevant_tool_summary,
    get_tools_for_request,
)


class TestCategorizeMessage:
    """Verify keyword → category mapping."""

    @pytest.mark.parametrize(
        "message, expected_category",
        [
            ("change the material color to red", "materials"),
            ("change the color of the player", "materials"),
            ("apply a shader to the wall", "materials"),
            ("import a texture", "materials"),
            ("add a rigidbody to the player", "physics"),
            ("create a box collider", "physics"),
            ("enable gravity on the enemy", "physics"),
            ("add a character controller", "physics"),
            ("set up a blend tree", "animation"),
            ("create an animator controller", "animation"),
            ("add a transition from idle to run", "animation"),
            ("create a point light", "lighting"),
            ("adjust the ambient lighting", "lighting"),
            ("bake shadows", "lighting"),
            ("add an audio source", "vfx_audio"),
            ("play a sound effect", "vfx_audio"),
            ("create a particle system", "vfx_audio"),
            ("create a UI button", "ui"),
            ("add a canvas", "ui"),
            ("set up a health bar HUD", "ui"),
            ("add a camera", "camera"),
            ("adjust the field of view", "camera"),
            ("set up cinemachine", "camera"),
            ("bake the navmesh", "terrain_nav"),
            ("add a nav agent to the enemy", "terrain_nav"),
            ("create a terrain", "terrain_nav"),
            ("create a prefab from the player", "prefabs"),
            ("instantiate the enemy prefab", "prefabs"),
            ("set up input bindings", "input_system"),
            ("configure the gamepad", "input_system"),
        ],
    )
    def test_keyword_matches_category(self, message, expected_category):
        result = categorize_message(message)
        assert expected_category in result, (
            f"'{message}' should match '{expected_category}', got {result}"
        )

    def test_empty_message_returns_empty(self):
        assert categorize_message("") == set()
        assert categorize_message("   ") == set()

    def test_unrecognized_message_returns_empty(self):
        """Unrecognized messages return empty set (fail-open at caller level)."""
        result = categorize_message("do something amazing and unique")
        assert result == set()

    def test_multiple_categories_matched(self):
        """A message mentioning multiple domains should match all of them."""
        result = categorize_message("add a rigidbody and play a sound when it collides")
        assert "physics" in result
        assert "vfx_audio" in result


class TestGetToolsForRequest:
    """Verify filtered tool lists include always-included categories."""

    def test_matched_request_includes_always(self):
        """When categories match, the result should include core+scene+scripting tools."""
        tools = get_tools_for_request("add a rigidbody")
        tool_names = {t["function"]["name"] for t in tools}
        # Should include physics tools
        assert "add_rigidbody" in tool_names
        # Should include always-included core tools
        assert "create_game_object" in tool_names

    def test_unmatched_returns_all(self):
        """Unrecognized message → fail-open, all tools returned."""
        from gladekit_mcp.tools import get_unity_tool_schemas
        all_tools = get_tools_for_request("something totally unique and unrecognizable")
        full_set = get_unity_tool_schemas()
        assert len(all_tools) == len(full_set)


class TestGetRelevantToolSummary:
    """Verify the meta-tool output format."""

    def test_matched_summary_has_categories(self):
        summary = get_relevant_tool_summary("add a rigidbody to the player")
        assert "physics" in summary.lower()
        assert "Categories:" in summary

    def test_unmatched_summary_says_all(self):
        summary = get_relevant_tool_summary("xyzzy foobar")
        assert "all" in summary.lower() or "All" in summary
