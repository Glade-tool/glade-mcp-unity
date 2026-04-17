"""
Physics category tools — colliders, rigidbodies, character controllers, physics materials.
"""

from typing import List, Dict

CATEGORY = {
    "name": "physics",
    "display_name": "Physics",
    "keywords": [
        "physics",
        "collider",
        "rigidbody",
        "rigid body",
        "gravity",
        "collision",
        "trigger",
        "character controller",
        "bounce",
        "friction",
        "wheel",
        "raycast",
        "linecast",
        "overlap",
        "shapecast",
        "sphere cast",
        "box cast",
        "collision matrix",
        "layer collision",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "create_collider",
            "description": "Add a collider to a GameObject. Types: Box (walls/floors), Sphere (balls), Capsule (characters — auto-detects axis), Mesh (static exact shape), Convex (mesh + Rigidbody), Wheel (vehicles), Terrain. Auto-aligns to mesh bounds unless size/center/radius/height is provided. Checks for CharacterController/duplicate conflicts; warnings in response.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject",
                    },
                    "colliderType": {
                        "type": "string",
                        "enum": [
                            "Box",
                            "Sphere",
                            "Capsule",
                            "Mesh",
                            "Convex",
                            "Wheel",
                            "Terrain",
                        ],
                        "description": "Type of collider to create. Box/Sphere/Capsule are primitives. Mesh/Convex use actual mesh geometry. Wheel is for vehicle wheels. Terrain wraps Unity Terrain.",
                    },
                    "isTrigger": {
                        "type": "boolean",
                        "description": "Whether the collider is a trigger (not applicable to WheelCollider)",
                    },
                    "center": {
                        "type": "string",
                        "description": "Center offset as 'x,y,z' for Box/Sphere/CapsuleCollider (disables auto-alignment if provided)",
                    },
                    "size": {
                        "type": "string",
                        "description": "Size as 'x,y,z' for BoxCollider (disables auto-alignment if provided)",
                    },
                    "radius": {
                        "type": "number",
                        "description": "Radius for SphereCollider, CapsuleCollider, or WheelCollider (disables auto-alignment if provided)",
                    },
                    "height": {
                        "type": "number",
                        "description": "Height for CapsuleCollider (disables auto-alignment if provided)",
                    },
                    "direction": {
                        "type": "integer",
                        "enum": [0, 1, 2],
                        "description": "CapsuleCollider axis direction: 0=X-axis, 1=Y-axis (default/vertical), 2=Z-axis. Auto-detected if not provided.",
                    },
                    "meshPath": {
                        "type": "string",
                        "description": "Asset path to mesh for MeshCollider/ConvexCollider. Auto-found from MeshFilter if omitted.",
                    },
                    "convex": {
                        "type": "boolean",
                        "description": "Whether MeshCollider is convex (required when used with Rigidbody)",
                    },
                    "suspensionDistance": {
                        "type": "number",
                        "description": "WheelCollider suspension travel distance (meters)",
                    },
                    "wheelMass": {
                        "type": "number",
                        "description": "WheelCollider wheel mass (kg)",
                    },
                    "forwardFriction": {
                        "type": "number",
                        "description": "WheelCollider forward friction stiffness",
                    },
                    "sidewaysFriction": {
                        "type": "number",
                        "description": "WheelCollider sideways friction stiffness",
                    },
                    "terrainDataPath": {
                        "type": "string",
                        "description": "Asset path to TerrainData for TerrainCollider. Auto-linked from sibling Terrain component if omitted.",
                    },
                    "autoAlign": {
                        "type": "boolean",
                        "description": "Auto-align collider with mesh bounds aggregated across all child meshes (default: true). Set to false to use default Unity collider size.",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_collider_properties",
            "description": "Get detailed information about a Collider component including type-specific properties (size, radius, center, direction, convex, terrainDataPath, wheel properties, etc.). Use to inspect collider settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Collider component",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_collider_properties",
            "description": "Update collider properties on a GameObject. Works for all types (Box, Sphere, Capsule, Mesh, Wheel, Terrain). Response includes conflict warnings for CharacterController or multiple colliders.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject",
                    },
                    "isTrigger": {
                        "type": "boolean",
                        "description": "Whether the collider is a trigger (not applicable to WheelCollider)",
                    },
                    "center": {
                        "type": "string",
                        "description": "Center offset as 'x,y,z' for Box/Sphere/CapsuleCollider",
                    },
                    "size": {
                        "type": "string",
                        "description": "Size as 'x,y,z' for BoxCollider",
                    },
                    "radius": {
                        "type": "number",
                        "description": "Radius for SphereCollider, CapsuleCollider, or WheelCollider",
                    },
                    "height": {
                        "type": "number",
                        "description": "Height for CapsuleCollider",
                    },
                    "direction": {
                        "type": "integer",
                        "enum": [0, 1, 2],
                        "description": "CapsuleCollider axis direction: 0=X-axis, 1=Y-axis (default), 2=Z-axis",
                    },
                    "meshPath": {
                        "type": "string",
                        "description": "Asset path to mesh for MeshCollider",
                    },
                    "convex": {
                        "type": "boolean",
                        "description": "Whether MeshCollider is convex",
                    },
                    "suspensionDistance": {
                        "type": "number",
                        "description": "WheelCollider suspension travel distance",
                    },
                    "wheelMass": {
                        "type": "number",
                        "description": "WheelCollider wheel mass",
                    },
                    "forwardFriction": {
                        "type": "number",
                        "description": "WheelCollider forward friction stiffness",
                    },
                    "sidewaysFriction": {
                        "type": "number",
                        "description": "WheelCollider sideways friction stiffness",
                    },
                    "terrainDataPath": {
                        "type": "string",
                        "description": "Asset path to TerrainData for TerrainCollider",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_character_controller",
            "description": "Add a CharacterController to a GameObject. Auto-removes existing colliders (CharacterController has its own built-in capsule collider). Auto-aligns to mesh bounds unless radius/height/center provided. Set keepExistingColliders=true to prevent auto-removal. Use for player movement; use Rigidbody for physics-based movement.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject",
                    },
                    "radius": {
                        "type": "number",
                        "description": "Radius of the capsule (disables auto-alignment if provided)",
                    },
                    "height": {
                        "type": "number",
                        "description": "Height of the capsule (disables auto-alignment if provided)",
                    },
                    "center": {
                        "type": "string",
                        "description": "Center offset as 'x,y,z' (disables auto-alignment if provided)",
                    },
                    "slopeLimit": {
                        "type": "number",
                        "description": "Maximum slope angle in degrees (default: 45)",
                    },
                    "stepOffset": {
                        "type": "number",
                        "description": "Maximum step height",
                    },
                    "skinWidth": {
                        "type": "number",
                        "description": "Skin width for collision detection (default: 0.08)",
                    },
                    "minMoveDistance": {
                        "type": "number",
                        "description": "Minimum move distance threshold (default: 0.001)",
                    },
                    "autoAlign": {
                        "type": "boolean",
                        "description": "Auto-align with mesh bounds (default: true). Set to false to use default Unity CharacterController size.",
                    },
                    "keepExistingColliders": {
                        "type": "boolean",
                        "description": "If true, keeps existing colliders (not recommended - may cause conflicts). Default: false (auto-removes colliders).",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_character_controller_properties",
            "description": "Get detailed information about a CharacterController component including radius, height, center, slope limit, and other properties. Use to inspect character controller settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with CharacterController component",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_character_controller_properties",
            "description": "Update CharacterController properties on a GameObject. Response includes warnings if other colliders are present (may cause conflicts).",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject",
                    },
                    "radius": {
                        "type": "number",
                        "description": "Radius of the capsule",
                    },
                    "height": {
                        "type": "number",
                        "description": "Height of the capsule",
                    },
                    "center": {
                        "type": "string",
                        "description": "Center offset as 'x,y,z'",
                    },
                    "slopeLimit": {
                        "type": "number",
                        "description": "Maximum slope angle in degrees",
                    },
                    "stepOffset": {
                        "type": "number",
                        "description": "Maximum step height",
                    },
                    "skinWidth": {
                        "type": "number",
                        "description": "Skin width for collision detection",
                    },
                    "minMoveDistance": {
                        "type": "number",
                        "description": "Minimum move distance threshold",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "add_rigidbody",
            "description": "Add a Rigidbody to a GameObject. Checks for existing Rigidbody, CharacterController conflicts, and missing colliders. Use Rigidbody for physics-based movement; do NOT combine with CharacterController on the same object.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject",
                    },
                    "mass": {
                        "type": "number",
                        "description": "Mass of the rigidbody (default: 1)",
                    },
                    "drag": {
                        "type": "number",
                        "description": "Linear drag (air resistance)",
                    },
                    "angularDrag": {
                        "type": "number",
                        "description": "Angular drag (rotational resistance)",
                    },
                    "useGravity": {
                        "type": "boolean",
                        "description": "Whether the rigidbody is affected by gravity",
                    },
                    "isKinematic": {
                        "type": "boolean",
                        "description": "If true, the rigidbody won't be affected by physics forces",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_rigidbody_properties",
            "description": "Get detailed information about a Rigidbody component including mass, drag, gravity, kinematic state, velocity, and other properties. Use to inspect rigidbody settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Rigidbody component",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_rigidbody_properties",
            "description": "Update properties on a Rigidbody. Response includes warnings if CharacterController is also present.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject",
                    },
                    "mass": {"type": "number", "description": "Mass of the rigidbody"},
                    "drag": {
                        "type": "number",
                        "description": "Linear drag (air resistance)",
                    },
                    "angularDrag": {
                        "type": "number",
                        "description": "Angular drag (rotational resistance)",
                    },
                    "useGravity": {
                        "type": "boolean",
                        "description": "Whether the rigidbody is affected by gravity",
                    },
                    "isKinematic": {
                        "type": "boolean",
                        "description": "If true, the rigidbody won't be affected by physics forces",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_physics_material",
            "description": "Create a PhysicMaterial asset. Defines friction and bounciness for colliders. Assign to colliders via assign_physics_material. Returns error if asset already exists at path.",
            "parameters": {
                "type": "object",
                "properties": {
                    "materialPath": {
                        "type": "string",
                        "description": "Asset path where the PhysicMaterial will be created (e.g., 'PhysicsMaterials/Ice.mat')",
                    },
                    "dynamicFriction": {
                        "type": "number",
                        "description": "Friction when object is moving (0-1)",
                    },
                    "staticFriction": {
                        "type": "number",
                        "description": "Friction when object is at rest (0-1)",
                    },
                    "bounciness": {
                        "type": "number",
                        "description": "How bouncy the material is (0-1, where 1 is perfectly bouncy)",
                    },
                    "frictionCombine": {
                        "type": "string",
                        "description": "How to combine friction values: Average, Minimum, Maximum, Multiply",
                        "enum": ["Average", "Minimum", "Maximum", "Multiply"],
                    },
                    "bounceCombine": {
                        "type": "string",
                        "description": "How to combine bounciness values: Average, Minimum, Maximum, Multiply",
                        "enum": ["Average", "Minimum", "Maximum", "Multiply"],
                    },
                },
                "required": ["materialPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "assign_physics_material",
            "description": "Assign a PhysicMaterial to Collider(s) on a GameObject. If multiple colliders exist, assigns to all of them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with collider(s)",
                    },
                    "materialPath": {
                        "type": "string",
                        "description": "Asset path to the PhysicMaterial (e.g., 'PhysicsMaterials/Ice.mat')",
                    },
                },
                "required": ["gameObjectPath", "materialPath"],
            },
        },
    },
    # ── Physics Query Tools ──────────────────────────────────────────────────
    {
        "type": "function",
        "function": {
            "name": "raycast",
            "description": "Cast a ray from origin in direction. Returns first hit (or all hits with all=true): GameObject path, point, normal, distance. Use for line-of-sight, ground detection, click-to-world.",
            "parameters": {
                "type": "object",
                "properties": {
                    "origin": {
                        "type": "string",
                        "description": "Ray origin as 'x,y,z'",
                    },
                    "direction": {
                        "type": "string",
                        "description": "Ray direction as 'x,y,z' (will be normalized)",
                    },
                    "maxDistance": {
                        "type": "number",
                        "description": "Maximum ray distance (default: infinity)",
                    },
                    "layerMask": {
                        "type": "integer",
                        "description": "Layer mask bitmask to filter which layers to hit",
                    },
                    "all": {
                        "type": "boolean",
                        "description": "If true, return all hits sorted by distance (RaycastAll). Default: false (first hit only).",
                    },
                },
                "required": ["origin", "direction"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "linecast",
            "description": "Test if any collider intersects the line between start and end points. Returns hit info if obstructed. Use for visibility checks between two points.",
            "parameters": {
                "type": "object",
                "properties": {
                    "start": {
                        "type": "string",
                        "description": "Line start point as 'x,y,z'",
                    },
                    "end": {
                        "type": "string",
                        "description": "Line end point as 'x,y,z'",
                    },
                    "layerMask": {
                        "type": "integer",
                        "description": "Layer mask bitmask to filter which layers to test",
                    },
                },
                "required": ["start", "end"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "overlap_sphere",
            "description": "Find all colliders within a sphere. Returns list of GameObjects and collider types. Use for proximity detection, area-of-effect queries.",
            "parameters": {
                "type": "object",
                "properties": {
                    "center": {
                        "type": "string",
                        "description": "Sphere center as 'x,y,z'",
                    },
                    "radius": {
                        "type": "number",
                        "description": "Sphere radius",
                    },
                    "layerMask": {
                        "type": "integer",
                        "description": "Layer mask bitmask to filter results",
                    },
                },
                "required": ["center", "radius"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "overlap_box",
            "description": "Find all colliders within an axis-aligned or oriented box. Returns list of GameObjects and collider types. Use for area queries, trigger zone testing.",
            "parameters": {
                "type": "object",
                "properties": {
                    "center": {
                        "type": "string",
                        "description": "Box center as 'x,y,z'",
                    },
                    "halfExtents": {
                        "type": "string",
                        "description": "Half-size of the box as 'x,y,z'",
                    },
                    "orientation": {
                        "type": "string",
                        "description": "Euler angles as 'x,y,z' for box orientation (default: identity)",
                    },
                    "layerMask": {
                        "type": "integer",
                        "description": "Layer mask bitmask to filter results",
                    },
                },
                "required": ["center", "halfExtents"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "sphere_cast",
            "description": "Cast a sphere along a direction (swept sphere query). Like a thick raycast. Returns first hit or all hits. Use for character movement checks, projectile queries.",
            "parameters": {
                "type": "object",
                "properties": {
                    "origin": {
                        "type": "string",
                        "description": "Sphere start center as 'x,y,z'",
                    },
                    "radius": {
                        "type": "number",
                        "description": "Sphere radius",
                    },
                    "direction": {
                        "type": "string",
                        "description": "Cast direction as 'x,y,z' (will be normalized)",
                    },
                    "maxDistance": {
                        "type": "number",
                        "description": "Maximum cast distance (default: infinity)",
                    },
                    "layerMask": {
                        "type": "integer",
                        "description": "Layer mask bitmask",
                    },
                    "all": {
                        "type": "boolean",
                        "description": "If true, return all hits (SphereCastAll). Default: false.",
                    },
                },
                "required": ["origin", "radius", "direction"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "box_cast",
            "description": "Cast a box along a direction (swept box query). Returns hit info. Use for wide sweep tests, platform edge detection.",
            "parameters": {
                "type": "object",
                "properties": {
                    "center": {
                        "type": "string",
                        "description": "Box start center as 'x,y,z'",
                    },
                    "halfExtents": {
                        "type": "string",
                        "description": "Half-size of the box as 'x,y,z'",
                    },
                    "direction": {
                        "type": "string",
                        "description": "Cast direction as 'x,y,z' (will be normalized)",
                    },
                    "orientation": {
                        "type": "string",
                        "description": "Euler angles as 'x,y,z' for box orientation (default: identity)",
                    },
                    "maxDistance": {
                        "type": "number",
                        "description": "Maximum cast distance (default: infinity)",
                    },
                    "layerMask": {
                        "type": "integer",
                        "description": "Layer mask bitmask",
                    },
                },
                "required": ["center", "halfExtents", "direction"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_collision_matrix",
            "description": "Read the layer collision matrix. Returns all named layers and which layer pairs have collisions disabled (IgnoreLayerCollision). Use to debug collision filtering.",
            "parameters": {
                "type": "object",
                "properties": {},
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_collision_matrix",
            "description": "Set whether two layers ignore collisions. Use to configure layer-based collision filtering (e.g., projectiles ignore player layer).",
            "parameters": {
                "type": "object",
                "properties": {
                    "layer1": {
                        "type": "string",
                        "description": "First layer name or index (0-31)",
                    },
                    "layer2": {
                        "type": "string",
                        "description": "Second layer name or index (0-31)",
                    },
                    "ignore": {
                        "type": "boolean",
                        "description": "True to disable collisions between layers, false to enable (default: true)",
                    },
                },
                "required": ["layer1", "layer2"],
            },
        },
    },
]
