"""
Animation category tools — Animator Controllers, blend trees, states, transitions, animation clips.
"""

from typing import List, Dict

CATEGORY = {
    "name": "animation",
    "display_name": "Animation",
    "keywords": [
        "animation",
        "animate",
        "animator",
        "blend tree",
        "state machine",
        "transition",
        "keyframe",
        "clip",
        "controller",
        "locomotion",
        "walk",
        "run",
        "idle",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "create_animator_controller",
            "description": "Create a new Animator Controller asset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path for the controller asset (e.g., 'Animation/PlayerController.controller')",
                    }
                },
                "required": ["controllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_animator_parameters",
            "description": "Add parameters to an Animator Controller.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "parameterList": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "name": {
                                    "type": "string",
                                    "description": "Parameter name",
                                },
                                "paramType": {
                                    "type": "string",
                                    "enum": ["Float", "Int", "Bool", "Trigger"],
                                    "description": "Parameter type",
                                },
                                "defaultValue": {
                                    "type": "string",
                                    "description": "Optional: Default value (number for Float/Int, 'true'/'false' for Bool)",
                                },
                            },
                        },
                        "description": "Array of parameters to add",
                    },
                },
                "required": ["controllerPath", "parameterList"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_animator_state",
            "description": "Add a state to an Animator Controller layer with an animation clip. After creating states, call add_animator_transition and add_animator_transition_conditions to wire up logic.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name for the new state",
                    },
                    "clipPath": {
                        "type": "string",
                        "description": "Optional: Path to the AnimationClip to assign",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index (0 = Base Layer). Default: 0",
                    },
                    "isDefault": {
                        "type": "boolean",
                        "description": "Optional: Set as default state. Default: false",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Position in state machine as 'x,y'. Default: auto-positioned",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_blend_tree_1d",
            "description": "Create a 1D Blend Tree state for locomotion (Idle/Walk/Run). Creates a blend tree driven by a float parameter.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name for the blend tree state (e.g., 'Locomotion')",
                    },
                    "parameterName": {
                        "type": "string",
                        "description": "Float parameter to drive the blend (e.g., 'Speed')",
                    },
                    "motions": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "clipPath": {
                                    "type": "string",
                                    "description": "Path to the AnimationClip",
                                },
                                "threshold": {
                                    "type": "number",
                                    "description": "Threshold value for this motion (e.g., 0 for Idle, 0.5 for Walk, 1 for Run)",
                                },
                            },
                        },
                        "description": "Array of motions with thresholds",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                    "isDefault": {
                        "type": "boolean",
                        "description": "Optional: Set as default state. Default: true",
                    },
                },
                "required": ["controllerPath", "stateName", "parameterName", "motions"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_blend_tree_2d",
            "description": "Create a 2D Blend Tree state for directional locomotion (forward/backward + strafe). Use for 8-directional movement or aiming. Supports SimpleDirectional2D, FreeformDirectional2D, and FreeformCartesian2D blend types.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name for the blend tree state (e.g., 'Locomotion2D')",
                    },
                    "parameterX": {
                        "type": "string",
                        "description": "Float parameter for X axis (e.g., 'VelocityX' or 'Horizontal')",
                    },
                    "parameterY": {
                        "type": "string",
                        "description": "Float parameter for Y axis (e.g., 'VelocityZ' or 'Vertical')",
                    },
                    "blendType": {
                        "type": "string",
                        "enum": [
                            "SimpleDirectional2D",
                            "FreeformDirectional2D",
                            "FreeformCartesian2D",
                        ],
                        "description": "2D blend type. SimpleDirectional2D for basic 4/8 directions, FreeformDirectional2D for smooth directional, FreeformCartesian2D for independent X/Y control. Default: SimpleDirectional2D",
                    },
                    "motions": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "clipPath": {
                                    "type": "string",
                                    "description": "Path to the AnimationClip",
                                },
                                "positionX": {
                                    "type": "number",
                                    "description": "X position in blend space (e.g., -1 for left, 0 for center, 1 for right)",
                                },
                                "positionY": {
                                    "type": "number",
                                    "description": "Y position in blend space (e.g., -1 for backward, 0 for idle, 1 for forward)",
                                },
                            },
                        },
                        "description": "Array of motions with 2D positions. Example: [{clipPath:'Idle.anim', positionX:0, positionY:0}, {clipPath:'WalkForward.anim', positionX:0, positionY:1}]",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                    "isDefault": {
                        "type": "boolean",
                        "description": "Optional: Set as default state. Default: true",
                    },
                },
                "required": [
                    "controllerPath",
                    "stateName",
                    "parameterX",
                    "parameterY",
                    "motions",
                ],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_animator_layer",
            "description": "Add a new layer to an Animator Controller. Use for upper body overrides, additive animations, etc.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "layerName": {
                        "type": "string",
                        "description": "Name for the new layer (e.g., 'Upper Body', 'Combat')",
                    },
                    "defaultWeight": {
                        "type": "number",
                        "description": "Optional: Default weight (0-1). Default: 1",
                    },
                    "blendingMode": {
                        "type": "string",
                        "enum": ["Override", "Additive"],
                        "description": "Optional: Blending mode. Override replaces base layer, Additive adds on top. Default: Override",
                    },
                    "avatarMaskPath": {
                        "type": "string",
                        "description": "Optional: Path to AvatarMask asset for masking body parts (e.g., 'Animation/UpperBodyMask.mask')",
                    },
                },
                "required": ["controllerPath", "layerName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_animator_layer_properties",
            "description": "Modify properties of an existing animator layer (weight, blending mode, avatar mask).",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index to modify (0 = Base Layer)",
                    },
                    "layerName": {
                        "type": "string",
                        "description": "Alternative: Find layer by name instead of index",
                    },
                    "weight": {
                        "type": "number",
                        "description": "Optional: Layer weight (0-1)",
                    },
                    "blendingMode": {
                        "type": "string",
                        "enum": ["Override", "Additive"],
                        "description": "Optional: Blending mode",
                    },
                    "avatarMaskPath": {
                        "type": "string",
                        "description": "Optional: Path to AvatarMask asset (empty string to clear)",
                    },
                    "syncedLayerIndex": {
                        "type": "integer",
                        "description": "Optional: Index of layer to sync with (-1 to disable)",
                    },
                },
                "required": ["controllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_sub_state_machine",
            "description": "Create a sub-state machine for organizing complex animator logic (e.g., Locomotion, Combat, Climbing sub-machines).",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "subStateMachineName": {
                        "type": "string",
                        "description": "Name for the sub-state machine (e.g., 'Locomotion', 'Combat')",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                    "parentPath": {
                        "type": "string",
                        "description": "Optional: Path to parent sub-state machine if nesting (e.g., 'Combat/Melee')",
                    },
                },
                "required": ["controllerPath", "subStateMachineName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_blend_tree_child",
            "description": "Add a new motion to an existing BlendTree. Use to expand locomotion blend trees.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name of the state containing the BlendTree",
                    },
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip to add",
                    },
                    "threshold": {
                        "type": "number",
                        "description": "For 1D blend trees: threshold value for this motion",
                    },
                    "positionX": {
                        "type": "number",
                        "description": "For 2D blend trees: X position in blend space",
                    },
                    "positionY": {
                        "type": "number",
                        "description": "For 2D blend trees: Y position in blend space",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName", "clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_blend_tree_info",
            "description": "Get complete information about a blend tree including structure, children, thresholds/positions, blend type, and parameters.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name of state containing the blend tree",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "modify_blend_tree_properties",
            "description": "Modify blend tree properties like blend type, parameters, threshold ranges, useAutomaticThresholds, normalizedBlendValues.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name of state containing the blend tree",
                    },
                    "blendType": {
                        "type": "string",
                        "enum": [
                            "Simple1D",
                            "SimpleDirectional2D",
                            "FreeformDirectional2D",
                            "FreeformCartesian2D",
                        ],
                        "description": "Blend tree type",
                    },
                    "blendParameter": {
                        "type": "string",
                        "description": "Parameter name for 1D or X axis",
                    },
                    "blendParameterY": {
                        "type": "string",
                        "description": "Parameter name for Y axis (2D only)",
                    },
                    "minThreshold": {
                        "type": "number",
                        "description": "Minimum threshold (1D only)",
                    },
                    "maxThreshold": {
                        "type": "number",
                        "description": "Maximum threshold (1D only)",
                    },
                    "useAutomaticThresholds": {
                        "type": "boolean",
                        "description": "Whether to use automatic thresholds",
                    },
                    "normalizedBlendValues": {
                        "type": "boolean",
                        "description": "Whether blend values are normalized",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_blend_tree_child",
            "description": "Remove a child motion from a blend tree by index or by clip path.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name of state containing the blend tree",
                    },
                    "childIndex": {
                        "type": "integer",
                        "description": "Index of child to remove (0-based)",
                    },
                    "clipPath": {
                        "type": "string",
                        "description": "Path to clip to remove (first match)",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "modify_blend_tree_child",
            "description": "Modify properties of a specific blend tree child (threshold, position, mirror, cycleOffset).",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name of state containing the blend tree",
                    },
                    "childIndex": {
                        "type": "integer",
                        "description": "Index of child to modify (0-based)",
                    },
                    "threshold": {
                        "type": "number",
                        "description": "New threshold value (1D only)",
                    },
                    "positionX": {
                        "type": "number",
                        "description": "New X position (2D only)",
                    },
                    "positionY": {
                        "type": "number",
                        "description": "New Y position (2D only)",
                    },
                    "mirror": {"type": "boolean", "description": "Mirror flag"},
                    "cycleOffset": {"type": "number", "description": "Cycle offset"},
                    "directBlendParameter": {
                        "type": "string",
                        "description": "Direct blend parameter",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName", "childIndex"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_animator_transition",
            "description": "Add a transition between animator states. After creating, call add_animator_transition_conditions to add conditions (transitions without conditions fire immediately). hasExitTime=false for locomotion; hasExitTime=true + exitTime=0.9 for attacks/jumps returning to idle.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "fromState": {
                        "type": "string",
                        "description": "Source state name (use 'Any State' for global transitions like Jump from anywhere)",
                    },
                    "toState": {
                        "type": "string",
                        "description": "Destination state name",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                    "hasExitTime": {
                        "type": "boolean",
                        "description": "IMPORTANT: false=immediate transition when condition met (locomotion), true=wait for animation to reach exitTime (attacks returning to idle). Default: false",
                    },
                    "exitTime": {
                        "type": "number",
                        "description": "When hasExitTime=true, exit at this point (0-1). Use 0.9 for 'near end of animation'. Default: 1",
                    },
                    "duration": {
                        "type": "number",
                        "description": "Blend duration in seconds. Default: 0.25",
                    },
                },
                "required": ["controllerPath", "fromState", "toState"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_animator_transition_conditions",
            "description": "Add conditions to an existing animator transition. Parameters must already exist (call add_animator_parameters first). Modes: If/IfNot for Bool/Trigger, Greater/Less for Float, Equals/NotEqual for Int.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "fromState": {
                        "type": "string",
                        "description": "Source state name (use 'Any State' for global transitions like Jump from anywhere)",
                    },
                    "toState": {
                        "type": "string",
                        "description": "Destination state name",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                    "transitionIndex": {
                        "type": "integer",
                        "description": "Optional: Index if multiple transitions exist between the same states. Default: 0",
                    },
                    "conditions": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "parameter": {
                                    "type": "string",
                                    "description": "Parameter name (MUST exist - call add_animator_parameters first)",
                                },
                                "mode": {
                                    "type": "string",
                                    "enum": [
                                        "If",
                                        "IfNot",
                                        "Greater",
                                        "Less",
                                        "Equals",
                                        "NotEqual",
                                    ],
                                    "description": "If/IfNot for Bool/Trigger, Greater/Less for Float, Equals/NotEqual for Int",
                                },
                                "threshold": {
                                    "type": "number",
                                    "description": "Value to compare against (required for Greater/Less/Equals/NotEqual, use 0 for If/IfNot)",
                                },
                            },
                            "required": ["parameter", "mode"],
                        },
                        "description": 'Conditions for this transition. Example: [{"parameter": "Speed", "mode": "Greater", "threshold": 0.1}]',
                    },
                },
                "required": ["controllerPath", "fromState", "toState", "conditions"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_animator_transition",
            "description": "Remove transition(s) between states in an Animator Controller. Use removeAll to delete every matching transition; optionally match by conditions.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "fromState": {
                        "type": "string",
                        "description": "Source state name (use 'Any State' for global transitions)",
                    },
                    "toState": {
                        "type": "string",
                        "description": "Destination state name",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                    "removeAll": {
                        "type": "boolean",
                        "description": "Optional: Remove all matching transitions. Default: false",
                    },
                    "conditions": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "parameter": {
                                    "type": "string",
                                    "description": "Parameter name (must exist in controller)",
                                },
                                "mode": {
                                    "type": "string",
                                    "enum": [
                                        "If",
                                        "IfNot",
                                        "Greater",
                                        "Less",
                                        "Equals",
                                        "NotEqual",
                                    ],
                                    "description": "Condition mode",
                                },
                                "threshold": {
                                    "type": "number",
                                    "description": "Threshold value (required for Greater/Less/Equals/NotEqual)",
                                },
                            },
                        },
                        "description": "Optional: Only remove transitions matching these conditions.",
                    },
                },
                "required": ["controllerPath", "fromState", "toState"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_animator_state",
            "description": "Remove a state node from an Animator Controller state machine.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "stateName": {
                        "type": "string",
                        "description": "Name of the state to remove",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                    "stateMachinePath": {
                        "type": "string",
                        "description": "Optional: Path to a sub-state machine, e.g., 'Locomotion/Sub' (empty for root)",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_animator_state_machine",
            "description": "Remove a sub-state machine node from an Animator Controller.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "stateMachinePath": {
                        "type": "string",
                        "description": "Path to the sub-state machine to remove, e.g., 'Locomotion/Sub'",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Optional: Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateMachinePath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_animator_parameter",
            "description": "Remove a parameter from an Animator Controller.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                    "parameterName": {
                        "type": "string",
                        "description": "Name of the parameter to remove",
                    },
                },
                "required": ["controllerPath", "parameterName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "assign_animator_controller",
            "description": "Assign an Animator Controller to a GameObject's Animator component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with Animator component",
                    },
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the Animator Controller asset",
                    },
                },
                "required": ["gameObjectPath", "controllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_animator_parameter",
            "description": "Set an animator parameter at runtime (SetBool, SetFloat, SetInteger, SetTrigger, ResetTrigger).",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with Animator component",
                    },
                    "parameterName": {
                        "type": "string",
                        "description": "Name of the parameter",
                    },
                    "parameterType": {
                        "type": "string",
                        "enum": ["Bool", "Float", "Int", "Trigger"],
                        "description": "Type of parameter",
                    },
                    "value": {
                        "type": ["number", "boolean"],
                        "description": "Value for Bool/Float/Int parameters",
                    },
                    "resetTrigger": {
                        "type": "boolean",
                        "description": "For Trigger type: true = ResetTrigger, false = SetTrigger. Default: false",
                    },
                },
                "required": ["gameObjectPath", "parameterName", "parameterType"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animation_clip_info",
            "description": "Get information about an AnimationClip asset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip asset",
                    }
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_animation_clip",
            "description": "Create a new AnimationClip asset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {"type": "string"},
                    "frameRate": {"type": "number"},
                    "wrapMode": {"type": "string"},
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_animation_clip_curves",
            "description": "Set animation curves on an AnimationClip.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {"type": "string"},
                    "curves": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "path": {"type": "string"},
                                "componentType": {"type": "string"},
                                "propertyName": {"type": "string"},
                                "keys": {
                                    "type": "array",
                                    "items": {
                                        "type": "object",
                                        "properties": {
                                            "time": {"type": "number"},
                                            "value": {"type": "number"},
                                            "inTangent": {"type": "number"},
                                            "outTangent": {"type": "number"},
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                "required": ["clipPath", "curves"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_sprite_animation_clip",
            "description": "Create an AnimationClip from a spritesheet. Loads all sprites from the spritesheet and creates keyframes for sprite animation.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path where the AnimationClip will be created",
                    },
                    "spritesheetPath": {
                        "type": "string",
                        "description": "Path to sliced spritesheet PNG",
                    },
                    "frameRate": {
                        "type": "number",
                        "description": "Frame rate for the animation. Default: 12",
                    },
                    "loopTime": {
                        "type": "boolean",
                        "description": "Whether the animation loops. Default: true",
                    },
                    "spriteOrder": {
                        "type": "array",
                        "items": {"type": "string"},
                        "description": "Custom sprite order by name. If not provided, sprites are sorted by name.",
                    },
                },
                "required": ["clipPath", "spritesheetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_sprite_animation_curves",
            "description": "Set sprite animation curves on an AnimationClip using ObjectReferenceKeyframes. Use this for 2D sprite frame animation.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "spriteKeyframes": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "time": {
                                    "type": "number",
                                    "description": "Time in seconds for this keyframe",
                                },
                                "spritePath": {
                                    "type": "string",
                                    "description": "Path to the sprite asset",
                                },
                            },
                            "required": ["time", "spritePath"],
                        },
                        "description": "Array of sprite keyframes with time and sprite path",
                    },
                    "path": {
                        "type": "string",
                        "description": "GameObject path in hierarchy. Default: empty string (root)",
                    },
                    "clearExisting": {
                        "type": "boolean",
                        "description": "Clear existing sprite curves before adding new ones. Default: true",
                    },
                },
                "required": ["clipPath", "spriteKeyframes"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_sprite_animation_info",
            "description": "Get sprite keyframe information from an AnimationClip.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    }
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "delete_animation_clip",
            "description": "Delete an AnimationClip asset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip to delete",
                    }
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "modify_animation_clip",
            "description": "Modify properties of an AnimationClip (frameRate, wrapMode, loopTime, loopPose, cycleOffset, additiveReferencePoseClipPath, hasAdditiveReferencePose).",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "frameRate": {"type": "number", "description": "New frame rate"},
                    "wrapMode": {
                        "type": "string",
                        "enum": ["Once", "Loop", "PingPong", "Default", "ClampForever"],
                        "description": "Wrap mode for the animation",
                    },
                    "loopTime": {
                        "type": "boolean",
                        "description": "Whether the animation loops",
                    },
                    "loopPose": {
                        "type": "boolean",
                        "description": "Whether to loop the pose",
                    },
                    "cycleOffset": {
                        "type": "number",
                        "description": "Cycle offset for the animation",
                    },
                    "additiveReferencePoseClipPath": {
                        "type": "string",
                        "description": "Path to additive reference pose clip",
                    },
                    "hasAdditiveReferencePose": {
                        "type": "boolean",
                        "description": "Whether additive reference pose is enabled",
                    },
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "duplicate_animation_clip",
            "description": "Duplicate (clone) an AnimationClip to a new path.",
            "parameters": {
                "type": "object",
                "properties": {
                    "sourceClipPath": {
                        "type": "string",
                        "description": "Path to the source AnimationClip",
                    },
                    "destinationClipPath": {
                        "type": "string",
                        "description": "Path where the duplicate will be created",
                    },
                },
                "required": ["sourceClipPath", "destinationClipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_animation_clip_curves",
            "description": "Remove specific curves from an AnimationClip.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "curves": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "path": {
                                    "type": "string",
                                    "description": "GameObject path",
                                },
                                "propertyName": {
                                    "type": "string",
                                    "description": "Property name",
                                },
                                "type": {
                                    "type": "string",
                                    "description": "Component type full name (e.g., 'UnityEngine.Transform')",
                                },
                            },
                            "required": ["path", "propertyName", "type"],
                        },
                        "description": "Array of curves to remove",
                    },
                },
                "required": ["clipPath", "curves"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animation_clip_curves",
            "description": "Get all curves (both editor curves and object reference curves) from an AnimationClip.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    }
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_animation_curve_tangents",
            "description": "Control keyframe tangents and interpolation modes for animation curves. Modify tangent modes, custom tangents, broken tangents, and weighted modes.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "bindingPath": {
                        "type": "string",
                        "description": "GameObject path (empty string for root)",
                    },
                    "propertyName": {
                        "type": "string",
                        "description": "Property name (e.g., 'm_LocalPosition.x')",
                    },
                    "keyframeIndex": {
                        "type": "integer",
                        "description": "Index of the keyframe to modify",
                    },
                    "tangentMode": {
                        "type": "string",
                        "enum": ["Auto", "Linear", "Constant", "ClampedAuto"],
                        "description": "Tangent mode",
                    },
                    "inTangent": {
                        "type": "number",
                        "description": "Incoming tangent value",
                    },
                    "outTangent": {
                        "type": "number",
                        "description": "Outgoing tangent value",
                    },
                    "broken": {
                        "type": "boolean",
                        "description": "Whether tangents are broken",
                    },
                    "weightedMode": {
                        "type": "string",
                        "enum": ["None", "In", "Out", "Both"],
                        "description": "Weighted mode",
                    },
                },
                "required": [
                    "clipPath",
                    "bindingPath",
                    "propertyName",
                    "keyframeIndex",
                ],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animator_controller_info",
            "description": "Get information about an AnimatorController including layers, parameters, state count, and transition count.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    }
                },
                "required": ["controllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animator_state_info",
            "description": "Get information about a specific state in an AnimatorController.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "stateName": {"type": "string", "description": "Name of the state"},
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animator_transition_info",
            "description": "Get information about a transition between two states in an AnimatorController.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "fromState": {
                        "type": "string",
                        "description": "Name of the source state",
                    },
                    "toState": {
                        "type": "string",
                        "description": "Name of the destination state",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                    "transitionIndex": {
                        "type": "integer",
                        "description": "Index if multiple transitions exist between states. Default: 0",
                    },
                },
                "required": ["controllerPath", "fromState", "toState"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animator_layer_info",
            "description": "Get information about a layer in an AnimatorController.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                    "layerName": {
                        "type": "string",
                        "description": "Alternative to layerIndex: find layer by name",
                    },
                },
                "required": ["controllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animator_parameter_info",
            "description": "Get information about a parameter in an AnimatorController (name, type, default values).",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "parameterName": {
                        "type": "string",
                        "description": "Name of the parameter",
                    },
                },
                "required": ["controllerPath", "parameterName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_animator_state_properties",
            "description": "Modify properties of a state in an AnimatorController (speed, mirror, cycleOffset, writeDefaultValues, iKOnFeet, tag, motionPath, timeParameter, timeParameterActive).",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "stateName": {"type": "string", "description": "Name of the state"},
                    "speed": {
                        "type": "number",
                        "description": "State speed multiplier",
                    },
                    "mirror": {
                        "type": "boolean",
                        "description": "Whether the state is mirrored",
                    },
                    "cycleOffset": {
                        "type": "number",
                        "description": "Cycle offset for the state",
                    },
                    "writeDefaultValues": {
                        "type": "boolean",
                        "description": "Whether to write default values",
                    },
                    "iKOnFeet": {
                        "type": "boolean",
                        "description": "Whether IK on feet is enabled",
                    },
                    "tag": {"type": "string", "description": "State tag"},
                    "motionPath": {
                        "type": "string",
                        "description": "Path to AnimationClip or BlendTree to assign",
                    },
                    "timeParameter": {
                        "type": "string",
                        "description": "Time parameter name",
                    },
                    "timeParameterActive": {
                        "type": "boolean",
                        "description": "Whether time parameter is active",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                },
                "required": ["controllerPath", "stateName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_animator_transition_properties",
            "description": "Modify properties of a transition in an AnimatorController (hasExitTime, exitTime, duration, offset, interruptionSource, orderedInterruption, canTransitionToSelf, solo, mute).",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController",
                    },
                    "fromState": {
                        "type": "string",
                        "description": "Name of the source state",
                    },
                    "toState": {
                        "type": "string",
                        "description": "Name of the destination state",
                    },
                    "hasExitTime": {
                        "type": "boolean",
                        "description": "Whether the transition requires exit time",
                    },
                    "exitTime": {
                        "type": "number",
                        "description": "Exit time for the transition",
                    },
                    "duration": {
                        "type": "number",
                        "description": "Transition duration",
                    },
                    "offset": {"type": "number", "description": "Transition offset"},
                    "interruptionSource": {
                        "type": "string",
                        "description": "Interruption source (None, Source, Destination, SourceThenDestination, DestinationThenSource)",
                    },
                    "orderedInterruption": {
                        "type": "boolean",
                        "description": "Whether interruption is ordered",
                    },
                    "canTransitionToSelf": {
                        "type": "boolean",
                        "description": "Whether transition can transition to self",
                    },
                    "solo": {
                        "type": "boolean",
                        "description": "Whether transition is solo",
                    },
                    "mute": {
                        "type": "boolean",
                        "description": "Whether transition is muted",
                    },
                    "layerIndex": {
                        "type": "integer",
                        "description": "Layer index. Default: 0",
                    },
                    "transitionIndex": {
                        "type": "integer",
                        "description": "Index if multiple transitions exist. Default: 0",
                    },
                },
                "required": ["controllerPath", "fromState", "toState"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "delete_animator_controller",
            "description": "Delete an AnimatorController asset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "controllerPath": {
                        "type": "string",
                        "description": "Path to the AnimatorController to delete",
                    }
                },
                "required": ["controllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "duplicate_animator_controller",
            "description": "Duplicate (clone) an AnimatorController to a new path.",
            "parameters": {
                "type": "object",
                "properties": {
                    "sourceControllerPath": {
                        "type": "string",
                        "description": "Path to the source AnimatorController",
                    },
                    "destinationControllerPath": {
                        "type": "string",
                        "description": "Path where the duplicate will be created",
                    },
                },
                "required": ["sourceControllerPath", "destinationControllerPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_animation_event",
            "description": "Add an AnimationEvent to an AnimationClip.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "time": {
                        "type": "number",
                        "description": "Time in seconds when the event fires",
                    },
                    "functionName": {
                        "type": "string",
                        "description": "Name of the function to call",
                    },
                    "floatParameter": {
                        "type": "number",
                        "description": "Float parameter for the event",
                    },
                    "intParameter": {
                        "type": "integer",
                        "description": "Integer parameter for the event",
                    },
                    "stringParameter": {
                        "type": "string",
                        "description": "String parameter for the event",
                    },
                    "objectReferenceParameter": {
                        "type": "string",
                        "description": "Path to object reference parameter",
                    },
                },
                "required": ["clipPath", "time", "functionName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "remove_animation_event",
            "description": "Remove an AnimationEvent from an AnimationClip. Can remove by index, time, or function name.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "eventIndex": {
                        "type": "integer",
                        "description": "Index of the event to remove",
                    },
                    "time": {
                        "type": "number",
                        "description": "Time of the event to remove",
                    },
                    "functionName": {
                        "type": "string",
                        "description": "Function name of events to remove",
                    },
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_animation_events",
            "description": "Get all AnimationEvents from an AnimationClip.",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    }
                },
                "required": ["clipPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "modify_animation_event",
            "description": "Modify an existing animation event (time, functionName, parameter).",
            "parameters": {
                "type": "object",
                "properties": {
                    "clipPath": {
                        "type": "string",
                        "description": "Path to the AnimationClip",
                    },
                    "eventIndex": {
                        "type": "integer",
                        "description": "Index of the event to modify",
                    },
                    "time": {"type": "number", "description": "New time for the event"},
                    "functionName": {
                        "type": "string",
                        "description": "New function name",
                    },
                    "parameter": {
                        "type": "string",
                        "description": "Parameter value (can be string, int, or float)",
                    },
                    "floatParameter": {
                        "type": "number",
                        "description": "Float parameter",
                    },
                    "intParameter": {
                        "type": "integer",
                        "description": "Integer parameter",
                    },
                    "stringParameter": {
                        "type": "string",
                        "description": "String parameter",
                    },
                },
                "required": ["clipPath", "eventIndex"],
            },
        },
    },
]
