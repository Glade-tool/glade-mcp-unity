"""
IK (Inverse Kinematics) category tools — IK targets, weights, positions, controller scripts.
"""

from typing import Dict, List

CATEGORY = {
    "name": "ik",
    "display_name": "Inverse Kinematics (IK)",
    "keywords": [
        "ik",
        "inverse kinematics",
        "rig",
        "bone",
        "hand",
        "foot",
        "reach",
        "aim",
        "procedural animation",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "create_ik_target",
            "description": "Create an IK target GameObject for runtime animator IK (reaching, foot placement, weapon alignment). IK is for RUNTIME procedural positioning — use standard animation tools for locomotion, keyframe clips, and state machines.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Animator component",
                    },
                    "targetName": {
                        "type": "string",
                        "description": "Name for the IK target GameObject",
                    },
                    "ikGoal": {
                        "type": "string",
                        "enum": [
                            "LeftHand",
                            "RightHand",
                            "LeftFoot",
                            "RightFoot",
                            "LeftElbow",
                            "RightElbow",
                            "LeftKnee",
                            "RightKnee",
                        ],
                        "description": "Which body part this IK target controls",
                    },
                    "parentPath": {
                        "type": "string",
                        "description": "Optional: Parent GameObject path. Useful for organizing targets or attaching to moving objects.",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Initial position as 'x,y,z' in world space",
                    },
                    "rotation": {
                        "type": "string",
                        "description": "Optional: Initial rotation as 'x,y,z' (Euler angles)",
                    },
                },
                "required": ["gameObjectPath", "targetName", "ikGoal"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "assign_ik_target",
            "description": "Assign an existing GameObject as an IK target for runtime body part control (connecting weapons, items, or interaction points). For locomotion and animation authoring, use standard animation tools.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Animator",
                    },
                    "targetPath": {
                        "type": "string",
                        "description": "Path to IK target GameObject (can be any GameObject in the scene)",
                    },
                    "ikGoal": {
                        "type": "string",
                        "enum": [
                            "LeftHand",
                            "RightHand",
                            "LeftFoot",
                            "RightFoot",
                            "LeftElbow",
                            "RightElbow",
                            "LeftKnee",
                            "RightKnee",
                        ],
                        "description": "Which body part to control",
                    },
                },
                "required": ["gameObjectPath", "targetPath", "ikGoal"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_ik_target_info",
            "description": "Get information about configured IK targets on a GameObject. Returns all assigned IK targets with their goals and positions.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Animator",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_ik_weight",
            "description": "Set IK position and rotation weights (0-1) for runtime body part blending. Enable the IK pass on the animator layer first via set_animator_layer_properties (iKPass=true).",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Animator",
                    },
                    "ikGoal": {
                        "type": "string",
                        "enum": [
                            "LeftHand",
                            "RightHand",
                            "LeftFoot",
                            "RightFoot",
                            "LeftElbow",
                            "RightElbow",
                            "LeftKnee",
                            "RightKnee",
                        ],
                        "description": "Body part to control",
                    },
                    "positionWeight": {
                        "type": "number",
                        "description": "Position weight (0-1). Controls how much IK affects position. 0 = disabled, 1 = full control.",
                    },
                    "rotationWeight": {
                        "type": "number",
                        "description": "Rotation weight (0-1). Controls how much IK affects rotation. 0 = disabled, 1 = full control.",
                    },
                    "hintWeight": {
                        "type": "number",
                        "description": "Optional: Hint weight for elbow/knee (0-1). Controls joint bending direction.",
                    },
                },
                "required": [
                    "gameObjectPath",
                    "ikGoal",
                    "positionWeight",
                    "rotationWeight",
                ],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_ik_position",
            "description": "Set IK target position and/or rotation for runtime body part control. For locomotion and animation authoring, use standard animation tools.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Animator",
                    },
                    "ikGoal": {
                        "type": "string",
                        "enum": [
                            "LeftHand",
                            "RightHand",
                            "LeftFoot",
                            "RightFoot",
                            "LeftElbow",
                            "RightElbow",
                            "LeftKnee",
                            "RightKnee",
                        ],
                        "description": "Body part to control",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Target position as 'x,y,z'",
                    },
                    "rotation": {
                        "type": "string",
                        "description": "Optional: Target rotation as 'x,y,z' (Euler angles)",
                    },
                    "useWorldSpace": {
                        "type": "boolean",
                        "description": "Use world space coordinates. Default: true",
                    },
                },
                "required": ["gameObjectPath", "ikGoal"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_ik_weight",
            "description": "Get current IK weights and target positions for a body part or all body parts. Returns weight values and target transform information.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Animator",
                    },
                    "ikGoal": {
                        "type": "string",
                        "enum": [
                            "LeftHand",
                            "RightHand",
                            "LeftFoot",
                            "RightFoot",
                            "LeftElbow",
                            "RightElbow",
                            "LeftKnee",
                            "RightKnee",
                        ],
                        "description": "Optional: Specific body part, or omit to get all",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_ik_controller_script",
            "description": "Create a minimal IK controller script implementing OnAnimatorIK with standard targets, weights, and public methods for runtime control. Extensible base for custom IK logic.",
            "parameters": {
                "type": "object",
                "properties": {
                    "scriptPath": {
                        "type": "string",
                        "description": "Path for the script (e.g., 'Assets/Scripts/MyIKController.cs')",
                    },
                    "className": {
                        "type": "string",
                        "description": "Optional: Custom class name (defaults to 'IKController')",
                    },
                },
                "required": ["scriptPath"],
            },
        },
    },
]
