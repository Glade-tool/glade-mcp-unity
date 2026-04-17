"""
VFX & Audio category tools — particle systems, audio sources, audio clips.
"""

from typing import Dict, List

CATEGORY = {
    "name": "vfx_audio",
    "display_name": "VFX & Audio",
    "keywords": [
        "particle",
        "vfx",
        "effect",
        "audio",
        "sound",
        "music",
        "clip",
        "emit",
        "explosion",
        "fire",
        "smoke",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "create_particle_system",
            "description": "Create a new Particle System GameObject.",
            "parameters": {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string",
                        "description": "Optional: Name for the particle system. Default: 'Particle System'",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Position as 'x,y,z'. Default: origin",
                    },
                    "parentPath": {
                        "type": "string",
                        "description": "Optional: Path to parent GameObject",
                    },
                    "duration": {
                        "type": "number",
                        "description": "Optional: Duration in seconds. Default: 5",
                    },
                    "looping": {
                        "type": "boolean",
                        "description": "Optional: Whether to loop. Default: true",
                    },
                    "startLifetime": {
                        "type": "number",
                        "description": "Optional: Particle lifetime in seconds. Default: 5",
                    },
                    "startSpeed": {
                        "type": "number",
                        "description": "Optional: Initial particle speed. Default: 5",
                    },
                    "startSize": {
                        "type": "number",
                        "description": "Optional: Initial particle size. Default: 1",
                    },
                    "startColor": {
                        "type": "string",
                        "description": "Optional: Start color as 'r,g,b,a'. Default: white",
                    },
                    "maxParticles": {
                        "type": "integer",
                        "description": "Optional: Maximum particles. Default: 1000",
                    },
                    "playOnAwake": {
                        "type": "boolean",
                        "description": "Optional: Play on awake. Default: true",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_particle_system_properties",
            "description": "Get detailed information about a ParticleSystem component including all main properties and module settings (Shape, Velocity Over Lifetime, Color Over Lifetime, Size Over Lifetime, Rotation Over Lifetime, Texture Sheet Animation). Use to inspect particle system settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with ParticleSystem component",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_particle_system_properties",
            "description": "Set properties on a Particle System component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with ParticleSystem component",
                    },
                    "duration": {
                        "type": "number",
                        "description": "Optional: Duration in seconds",
                    },
                    "looping": {
                        "type": "boolean",
                        "description": "Optional: Whether to loop",
                    },
                    "startLifetime": {
                        "type": "number",
                        "description": "Optional: Particle lifetime in seconds",
                    },
                    "startSpeed": {
                        "type": "number",
                        "description": "Optional: Initial particle speed",
                    },
                    "startSize": {
                        "type": "number",
                        "description": "Optional: Initial particle size",
                    },
                    "startColor": {
                        "type": "string",
                        "description": "Optional: Start color as 'r,g,b,a'",
                    },
                    "maxParticles": {
                        "type": "integer",
                        "description": "Optional: Maximum particles",
                    },
                    "emissionRateOverTime": {
                        "type": "number",
                        "description": "Optional: Emission rate (particles per second)",
                    },
                    "gravityModifier": {
                        "type": "number",
                        "description": "Optional: Gravity modifier (-1 to 1+)",
                    },
                    "simulationSpace": {
                        "type": "string",
                        "enum": ["Local", "World"],
                        "description": "Optional: Simulation space",
                    },
                    "playOnAwake": {
                        "type": "boolean",
                        "description": "Optional: Play on awake",
                    },
                    "enableShape": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable Shape module",
                    },
                    "shapeType": {
                        "type": "string",
                        "description": "Optional: Shape type: 'Sphere', 'Hemisphere', 'Cone', 'Box', 'Mesh', 'Circle', 'Edge', 'Rectangle', etc.",
                    },
                    "shapeRadius": {
                        "type": "number",
                        "description": "Optional: Shape radius (for Sphere, Hemisphere, Circle)",
                    },
                    "shapeAngle": {
                        "type": "number",
                        "description": "Optional: Shape angle in degrees (for Cone)",
                    },
                    "enableVelocityOverLifetime": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable Velocity Over Lifetime module",
                    },
                    "velocityOverLifetimeX": {
                        "type": "number",
                        "description": "Optional: X velocity component",
                    },
                    "velocityOverLifetimeY": {
                        "type": "number",
                        "description": "Optional: Y velocity component",
                    },
                    "velocityOverLifetimeZ": {
                        "type": "number",
                        "description": "Optional: Z velocity component",
                    },
                    "enableColorOverLifetime": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable Color Over Lifetime module",
                    },
                    "colorOverLifetime": {
                        "type": "string",
                        "description": "Optional: Color value (e.g., '1,0,0,1' for red or 'red')",
                    },
                    "enableSizeOverLifetime": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable Size Over Lifetime module",
                    },
                    "sizeOverLifetimeMultiplier": {
                        "type": "number",
                        "description": "Optional: Size multiplier over lifetime",
                    },
                    "enableRotationOverLifetime": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable Rotation Over Lifetime module",
                    },
                    "rotationOverLifetimeZ": {
                        "type": "number",
                        "description": "Optional: Z-axis rotation speed (degrees per second)",
                    },
                    "enableTextureSheetAnimation": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable Texture Sheet Animation module",
                    },
                    "textureSheetAnimationTilesX": {
                        "type": "integer",
                        "description": "Optional: Number of tiles in X direction",
                    },
                    "textureSheetAnimationTilesY": {
                        "type": "integer",
                        "description": "Optional: Number of tiles in Y direction",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_audio_source",
            "description": "Create a new GameObject with an AudioSource component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string",
                        "description": "Optional: Name for the audio source. Default: 'Audio Source'",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Position as 'x,y,z'. Default: origin",
                    },
                    "parentPath": {
                        "type": "string",
                        "description": "Optional: Path to parent GameObject",
                    },
                    "clipPath": {
                        "type": "string",
                        "description": "Optional: Path to AudioClip asset to assign",
                    },
                    "playOnAwake": {
                        "type": "boolean",
                        "description": "Optional: Play on awake. Default: false",
                    },
                    "loop": {
                        "type": "boolean",
                        "description": "Optional: Loop audio. Default: false",
                    },
                    "volume": {
                        "type": "number",
                        "description": "Optional: Volume (0-1). Default: 1",
                    },
                    "pitch": {
                        "type": "number",
                        "description": "Optional: Pitch. Default: 1",
                    },
                    "spatialBlend": {
                        "type": "number",
                        "description": "Optional: 2D (0) to 3D (1) blend. Default: 0",
                    },
                    "minDistance": {
                        "type": "number",
                        "description": "Optional: Min distance for 3D falloff. Default: 1",
                    },
                    "maxDistance": {
                        "type": "number",
                        "description": "Optional: Max distance for 3D falloff. Default: 500",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_audio_source_properties",
            "description": "Get detailed information about an AudioSource component including clip, volume, pitch, spatial settings, and other properties. Use to inspect audio source settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with AudioSource component",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_audio_source_properties",
            "description": "Set properties on an AudioSource component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with AudioSource component",
                    },
                    "clipPath": {
                        "type": "string",
                        "description": "Optional: Path to AudioClip asset",
                    },
                    "playOnAwake": {
                        "type": "boolean",
                        "description": "Optional: Play on awake",
                    },
                    "loop": {"type": "boolean", "description": "Optional: Loop audio"},
                    "volume": {
                        "type": "number",
                        "description": "Optional: Volume (0-1)",
                    },
                    "pitch": {"type": "number", "description": "Optional: Pitch"},
                    "spatialBlend": {
                        "type": "number",
                        "description": "Optional: 2D (0) to 3D (1) blend",
                    },
                    "minDistance": {
                        "type": "number",
                        "description": "Optional: Min distance for 3D falloff",
                    },
                    "maxDistance": {
                        "type": "number",
                        "description": "Optional: Max distance for 3D falloff",
                    },
                    "mute": {
                        "type": "boolean",
                        "description": "Optional: Mute the audio source",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "assign_audio_clip",
            "description": "Assign an AudioClip asset to an AudioSource component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with AudioSource component",
                    },
                    "clipPath": {
                        "type": "string",
                        "description": "Path to AudioClip asset (e.g., 'Audio/Sounds/Jump.wav')",
                    },
                },
                "required": ["gameObjectPath", "clipPath"],
            },
        },
    },
]
