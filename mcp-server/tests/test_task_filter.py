"""Tests for task-aware tool filtering — keyword → category mapping."""

from __future__ import annotations

import pytest

from gladekit_mcp.tools.task_filter import (
    GODOT_ALWAYS_INCLUDED,
    categorize_message,
    categorize_message_godot,
    get_godot_tool_schemas,
    get_godot_tools_for_categories,
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
        assert expected_category in result, f"'{message}' should match '{expected_category}', got {result}"

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


# ── Godot ────────────────────────────────────────────────────────────────────


class TestGodotCategorize:
    """Godot keyword → category mapping (categories mirror schemas/godot)."""

    @pytest.mark.parametrize(
        "message, expected_category",
        [
            ("connect the timeout signal to the player", "signal"),
            ("export the game for web", "export"),
            ("build the project for windows", "export"),
            ("add an explosion particle effect", "particles"),
            ("play background music", "audio"),
            ("bake a navigation mesh for the enemy", "navigation"),
            ("raycast from the camera to the ground", "physics"),
            ("set collision layers so coins ignore walls", "physics"),
            ("add a point light and some fog", "camera"),
            ("create a main menu with a start button", "ui"),
            ("add an input action for jump", "project"),
            ("what did you change this session", "project"),
            ("set up an animation tree state machine", "animation"),
            ("import a free kenney asset pack", "asset_pipeline"),
            ("regenerate the uid files", "uid"),
            ("run the project and check for errors", "runtime"),
            ("change the material color to red", "resource"),
        ],
    )
    def test_keyword_maps_to_category(self, message, expected_category):
        assert expected_category in categorize_message_godot(message)

    def test_empty_message_fails_open(self):
        assert categorize_message_godot("") == set()
        assert categorize_message_godot("xyzzy") == set()

    def test_building_a_level_is_not_an_export(self):
        assert "export" not in categorize_message_godot("build a platformer level with three platforms")

    def test_every_keyword_category_exists_in_the_godot_catalog(self):
        from gladekit_mcp.schemas.godot import ALL_CATEGORIES
        from gladekit_mcp.tools.task_filter import _GODOT_CATEGORY_KEYWORDS

        catalog = {name for name, _ in ALL_CATEGORIES}
        assert set(_GODOT_CATEGORY_KEYWORDS) <= catalog
        assert GODOT_ALWAYS_INCLUDED <= catalog


class TestGodotSummary:
    def test_matched_summary_lists_godot_tools_only(self):
        text = get_relevant_tool_summary("connect the timeout signal", engine="godot")
        assert text.startswith("Categories: ")
        assert "signal" in text.split("\n")[0]
        assert "connect_signal" in text
        assert "get_scene_tree" in text  # always-included scene category
        assert "add_component" not in text

    def test_unmatched_summary_counts_the_whole_godot_catalog(self):
        text = get_relevant_tool_summary("xyzzy", engine="godot")
        assert text.startswith(f"All {len(get_godot_tool_schemas())} tools")

    def test_categories_expand_to_tools(self):
        tools = get_godot_tools_for_categories({"export"})
        names = {t["function"]["name"] for t in tools}
        assert {"export_project", "create_export_preset", "get_scene_tree", "create_script"} <= names
        assert "connect_signal" not in names
