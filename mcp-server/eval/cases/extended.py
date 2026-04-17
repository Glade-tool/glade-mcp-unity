"""
Extended MCP eval cases — multi-step workflows and specialized tools.

Tests tool chains that MCP clients (Claude Code, Cursor) commonly trigger:
creating objects with components, setting up complete systems, etc.
"""

from __future__ import annotations

from eval.cases import MCPEvalCase, ToolParamAssertion

EXTENDED = [
    # ── Multi-step: Create + position ────────────────────────────────────────
    MCPEvalCase(
        id="mcp-multi-01",
        prompt="Create a Sphere at position (0, 5, 0)",
        description="create_primitive + set_transform chain",
        category="multi-step",
        required_tools=["create_primitive", "set_transform"],
        param_assertions=[
            ToolParamAssertion("create_primitive", {"primitiveType": "Sphere"}),
        ],
        tags=["intermediate", "multi-tool"],
    ),
    # ── Multi-step: Create + physics ─────────────────────────────────────────
    MCPEvalCase(
        id="mcp-multi-02",
        prompt="Create a Cube called Ball and add a Rigidbody with gravity enabled",
        description="create + rigidbody chain",
        category="multi-step",
        required_tools=["create_primitive", "add_rigidbody"],
        tags=["intermediate", "multi-tool"],
    ),
    # ── Multi-step: Material pipeline ────────────────────────────────────────
    MCPEvalCase(
        id="mcp-multi-03",
        prompt="Create a red material and apply it to the Player",
        description="create_material + assign chain",
        category="multi-step",
        required_tools=["create_material", "assign_material_to_renderer"],
        tags=["intermediate", "multi-tool"],
    ),
    # ── Multi-step: Script + compile ─────────────────────────────────────────
    MCPEvalCase(
        id="mcp-multi-04",
        prompt="Create a health system script that tracks HP and add it to the Player",
        description="create_script + add_component or set_script_component_property",
        category="multi-step",
        required_tools=["create_script"],
        tags=["intermediate", "multi-tool", "scripting"],
    ),
    # ── Multi-step: Animator setup ───────────────────────────────────────────
    MCPEvalCase(
        id="mcp-multi-05",
        prompt="Create an animator controller with Idle and Run states, a Speed float parameter, and assign it to the Player",
        description="Full animator pipeline",
        category="multi-step",
        required_tools=[
            "create_animator_controller",
            "add_animator_state",
            "add_animator_parameters",
            "assign_animator_controller",
        ],
        tags=["advanced", "multi-tool", "animation"],
    ),
    # ── Multi-step: UI setup ────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-multi-06",
        prompt="Set up a basic UI with a Canvas and a Button",
        description="Canvas + UI element chain",
        category="multi-step",
        required_tools=["create_canvas", "create_ui_element"],
        tags=["intermediate", "multi-tool", "ui"],
    ),
    # ── Component operations ─────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-comp-01",
        prompt="Add a BoxCollider and Rigidbody to the Player",
        description="Multiple component additions",
        category="components",
        required_tools=["create_collider", "add_rigidbody"],
        tags=["intermediate", "multi-tool"],
    ),
    # ── Character controller ─────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-cc-01",
        prompt="Add a CharacterController to the Player",
        description="create_character_controller dispatch",
        category="physics",
        required_tools=["create_character_controller"],
        param_assertions=[
            ToolParamAssertion("create_character_controller", {"gameObjectName": "Player"}),
        ],
        tags=["intermediate"],
    ),
    # ── Duplicate + rename ───────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-dup-01",
        prompt="Duplicate the Player and rename the copy to Player2",
        description="duplicate + rename chain",
        category="gameobjects",
        required_tools=["duplicate_game_object", "rename_game_object"],
        tags=["intermediate", "multi-tool"],
    ),
    # ── Shader change ────────────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-shader-01",
        prompt="Change the material shader to Unlit/Color",
        description="change_material_shader dispatch",
        category="materials",
        required_tools=["change_material_shader"],
        tags=["intermediate"],
    ),
    # ── Lighting setup ───────────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-light-01",
        prompt="Create a Spot Light and set its intensity to 2",
        description="create_light + set_light_properties chain",
        category="lighting",
        required_tools=["create_light", "set_light_properties"],
        tags=["intermediate", "multi-tool"],
    ),
    # ── Scene operations ─────────────────────────────────────────────────────
    MCPEvalCase(
        id="mcp-scene-02",
        prompt="Save the current scene",
        description="save_scene dispatch (write operation)",
        category="scene",
        required_tools=["save_scene"],
        tags=["beginner", "regression"],
        suite_type="regression",
    ),
]
