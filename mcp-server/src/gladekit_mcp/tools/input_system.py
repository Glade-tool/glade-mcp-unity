"""
Input System category tools — new Input System, legacy Input Manager.
"""

from typing import List, Dict

CATEGORY = {
    "name": "input_system",
    "display_name": "Input System",
    "keywords": [
        "input",
        "key",
        "button",
        "axis",
        "controller",
        "gamepad",
        "action",
        "binding",
        "player input",
        "legacy input",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "get_input_system_info",
            "description": "Get the project's input system configuration (NEW/OLD/BOTH) and recommended API. Call before creating input actions or legacy axes to confirm which input system to use.",
            "parameters": {"type": "object", "properties": {}, "required": []},
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_input_action_asset",
            "description": "Create a new InputActionAsset.",
            "parameters": {
                "type": "object",
                "properties": {"assetPath": {"type": "string"}},
                "required": ["assetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_input_action_bindings",
            "description": "Create or update action maps, actions, and bindings inside an InputActionAsset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {"type": "string"},
                    "replaceBindings": {"type": "boolean"},
                    "maps": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "name": {"type": "string"},
                                "actions": {
                                    "type": "array",
                                    "items": {
                                        "type": "object",
                                        "properties": {
                                            "name": {"type": "string"},
                                            "type": {
                                                "type": "string",
                                                "enum": [
                                                    "Button",
                                                    "Value",
                                                    "PassThrough",
                                                ],
                                            },
                                            "bindings": {
                                                "type": "array",
                                                "items": {
                                                    "type": "object",
                                                    "properties": {
                                                        "path": {"type": "string"},
                                                        "interactions": {
                                                            "type": "string"
                                                        },
                                                        "processors": {
                                                            "type": "string"
                                                        },
                                                        "groups": {"type": "string"},
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                "required": ["assetPath", "maps"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "assign_input_actions",
            "description": "Assign an InputActionAsset to a PlayerInput component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {"type": "string"},
                    "assetPath": {"type": "string"},
                },
                "required": ["gameObjectPath", "assetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "list_legacy_input_axes",
            "description": "List all axes and buttons in the legacy Input Manager (ProjectSettings/InputManager.asset). Use when the project uses OLD input.",
            "parameters": {"type": "object", "properties": {}, "required": []},
        },
    },
    {
        "type": "function",
        "function": {
            "name": "ensure_legacy_input_axes",
            "description": "Create or update legacy Input Manager axes. Idempotent. Use when the project uses OLD input. Each axis needs at least 'name'; optional: positiveButton, negativeButton, altPositiveButton, altNegativeButton, gravity, dead, sensitivity, snap, invert, type, axis, joyNum.",
            "parameters": {
                "type": "object",
                "properties": {
                    "axes": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "name": {"type": "string"},
                                "positiveButton": {"type": "string"},
                                "negativeButton": {"type": "string"},
                                "altPositiveButton": {"type": "string"},
                                "altNegativeButton": {"type": "string"},
                                "gravity": {"type": "number"},
                                "dead": {"type": "number"},
                                "sensitivity": {"type": "number"},
                                "snap": {"type": "boolean"},
                                "invert": {"type": "boolean"},
                                "type": {
                                    "type": "integer",
                                    "description": "0=KeyOrMouseButton, 1=MouseMovement, 2=JoystickAxis",
                                },
                                "axis": {"type": "integer"},
                                "joyNum": {"type": "integer"},
                            },
                            "required": ["name"],
                        },
                    }
                },
                "required": ["axes"],
            },
        },
    },
]
