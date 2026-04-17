"""
Lighting category tools — lights, render settings, quality, reflection probes.
"""

from typing import Dict, List

CATEGORY = {
    "name": "lighting",
    "display_name": "Lighting & Rendering",
    "keywords": [
        "light",
        "lighting",
        "shadow",
        "ambient",
        "skybox",
        "reflection",
        "quality",
        "render settings",
        "fog",
        "hdr",
        "msaa",
        "render pipeline",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "create_light",
            "description": "Create a new Light GameObject in the scene.",
            "parameters": {
                "type": "object",
                "properties": {
                    "lightType": {
                        "type": "string",
                        "enum": ["Directional", "Point", "Spot", "Area"],
                        "description": "Type of light to create",
                    },
                    "name": {
                        "type": "string",
                        "description": "Optional: Name for the light GameObject. Default: based on type (e.g., 'Directional Light')",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Position as 'x,y,z'. Default: origin for Point/Spot/Area, (0,3,0) for Directional",
                    },
                    "rotation": {
                        "type": "string",
                        "description": "Optional: Rotation as 'x,y,z' Euler angles. Default: (50,-30,0) for Directional",
                    },
                    "color": {
                        "type": "string",
                        "description": "Optional: Light color as 'r,g,b' (0-1). Default: white (1,1,1)",
                    },
                    "intensity": {
                        "type": "number",
                        "description": "Optional: Light intensity. Default: 1",
                    },
                    "range": {
                        "type": "number",
                        "description": "Optional: Range for Point/Spot lights. Default: 10",
                    },
                    "spotAngle": {
                        "type": "number",
                        "description": "Optional: Spot angle for Spot lights. Default: 30",
                    },
                },
                "required": ["lightType"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_light_properties",
            "description": "Set properties on a Light component.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to the GameObject with Light component",
                    },
                    "color": {
                        "type": "string",
                        "description": "Optional: Light color as 'r,g,b' (0-1)",
                    },
                    "intensity": {
                        "type": "number",
                        "description": "Optional: Light intensity",
                    },
                    "range": {
                        "type": "number",
                        "description": "Optional: Range for Point/Spot lights",
                    },
                    "spotAngle": {
                        "type": "number",
                        "description": "Optional: Spot angle for Spot lights",
                    },
                    "shadows": {
                        "type": "string",
                        "enum": ["None", "Hard", "Soft"],
                        "description": "Optional: Shadow type",
                    },
                    "shadowStrength": {
                        "type": "number",
                        "description": "Optional: Shadow strength (0-1)",
                    },
                    "colorTemperature": {
                        "type": "number",
                        "description": "Optional: Color temperature in Kelvin (1000-20000)",
                    },
                    "useColorTemperature": {
                        "type": "boolean",
                        "description": "Optional: Whether to use color temperature mode",
                    },
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_render_settings",
            "description": "Configure global render settings for the scene (fog, ambient lighting, skybox).",
            "parameters": {
                "type": "object",
                "properties": {
                    "fogEnabled": {
                        "type": "boolean",
                        "description": "Optional: Enable/disable fog",
                    },
                    "fogColor": {
                        "type": "string",
                        "description": "Optional: Fog color as 'r,g,b' (0-1)",
                    },
                    "fogMode": {
                        "type": "string",
                        "enum": ["Linear", "Exponential", "ExponentialSquared"],
                        "description": "Optional: Fog mode",
                    },
                    "fogDensity": {
                        "type": "number",
                        "description": "Optional: Fog density for Exponential modes (0-1)",
                    },
                    "fogStartDistance": {
                        "type": "number",
                        "description": "Optional: Fog start distance for Linear mode",
                    },
                    "fogEndDistance": {
                        "type": "number",
                        "description": "Optional: Fog end distance for Linear mode",
                    },
                    "ambientMode": {
                        "type": "string",
                        "enum": ["Skybox", "Trilight", "Flat"],
                        "description": "Optional: Ambient lighting mode",
                    },
                    "ambientColor": {
                        "type": "string",
                        "description": "Optional: Ambient color as 'r,g,b' for Flat mode",
                    },
                    "ambientIntensity": {
                        "type": "number",
                        "description": "Optional: Ambient intensity multiplier",
                    },
                    "skyboxMaterial": {
                        "type": "string",
                        "description": "Optional: Path to skybox material asset",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_light_info",
            "description": "Get detailed information about a Light component including type, color, intensity, range, shadows, and other properties. Use to inspect light settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Path to GameObject with Light component (e.g., 'Directional Light', 'Point Light')",
                    }
                },
                "required": ["gameObjectPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_render_settings",
            "description": "Get current RenderSettings including fog settings, ambient lighting, skybox, and other global rendering properties. Use to inspect current settings before modifying them with set_render_settings.",
            "parameters": {"type": "object", "properties": {}, "required": []},
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_lighting_settings",
            "description": "Get scene LightingSettings asset information including lightmap settings, GI (Global Illumination) settings, and baking configuration. Use to inspect lighting setup before modifying or baking lightmaps.",
            "parameters": {
                "type": "object",
                "properties": {
                    "scenePath": {
                        "type": "string",
                        "description": "Optional: Scene path relative to Assets folder. Defaults to active scene.",
                    }
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_quality_settings",
            "description": "Get QualitySettings information including current quality level, all available quality levels, and settings for shadow quality, pixel light count, anti-aliasing, etc. Use to inspect quality settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "qualityLevel": {
                        "type": "string",
                        "description": "Optional: Quality level name or index to read (defaults to current quality level)",
                    }
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_quality_settings",
            "description": "Set QualitySettings properties including quality level, shadow quality, pixel light count, shadow distance, shadow cascades, and anti-aliasing. Modifies ProjectSettings/QualitySettings.asset.",
            "parameters": {
                "type": "object",
                "properties": {
                    "qualityLevel": {
                        "type": "string",
                        "description": "Optional: Quality level name or index to modify (defaults to current quality level)",
                    },
                    "pixelLightCount": {
                        "type": "integer",
                        "description": "Optional: Maximum number of pixel lights",
                    },
                    "shadows": {
                        "type": "string",
                        "enum": ["Disable", "HardOnly", "All"],
                        "description": "Optional: Shadow quality",
                    },
                    "shadowResolution": {
                        "type": "string",
                        "enum": ["Low", "Medium", "High", "VeryHigh"],
                        "description": "Optional: Shadow resolution quality",
                    },
                    "shadowDistance": {
                        "type": "number",
                        "description": "Optional: Maximum shadow distance",
                    },
                    "shadowCascades": {
                        "type": "string",
                        "enum": ["NoCascades", "TwoCascades", "FourCascades"],
                        "description": "Optional: Number of shadow cascades",
                    },
                    "antiAliasing": {
                        "type": "integer",
                        "description": "Optional: Anti-aliasing mode (0, 2, 4, or 8)",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_render_pipeline_asset_settings",
            "description": "Get Render Pipeline Asset settings (URP/HDRP) including shadow settings, render scale, HDR, and MSAA configuration. Use to inspect render pipeline settings before modifying them.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {
                        "type": "string",
                        "description": "Optional: Path to render pipeline asset (defaults to active pipeline)",
                    }
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_render_pipeline_asset_settings",
            "description": "Set Render Pipeline Asset settings (URP/HDRP) including shadow settings, render scale, HDR, and MSAA configuration. Modifies the render pipeline asset file.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {
                        "type": "string",
                        "description": "Optional: Path to render pipeline asset (defaults to active pipeline)",
                    },
                    "mainLightShadowmapResolution": {
                        "type": "integer",
                        "description": "Optional: Main light shadowmap resolution (256, 512, 1024, 2048, 4096) - URP only",
                    },
                    "mainLightShadowsSupported": {
                        "type": "boolean",
                        "description": "Optional: Whether main light shadows are supported - URP only",
                    },
                    "additionalLightsShadowResolutionTierLow": {
                        "type": "integer",
                        "description": "Optional: Additional lights shadow resolution for low tier - URP only",
                    },
                    "additionalLightsShadowResolutionTierMedium": {
                        "type": "integer",
                        "description": "Optional: Additional lights shadow resolution for medium tier - URP only",
                    },
                    "additionalLightsShadowResolutionTierHigh": {
                        "type": "integer",
                        "description": "Optional: Additional lights shadow resolution for high tier - URP only",
                    },
                    "additionalLightShadowsSupported": {
                        "type": "boolean",
                        "description": "Optional: Whether additional light shadows are supported - URP only",
                    },
                    "shadowDistance": {
                        "type": "number",
                        "description": "Optional: Maximum shadow distance",
                    },
                    "shadowCascadeCount": {
                        "type": "integer",
                        "description": "Optional: Number of shadow cascades (1, 2, or 4)",
                    },
                    "renderScale": {
                        "type": "number",
                        "description": "Optional: Render scale (0.1 to 2.0)",
                    },
                    "supportsHDR": {
                        "type": "boolean",
                        "description": "Optional: Whether HDR is supported",
                    },
                    "msaaSampleCount": {
                        "type": "integer",
                        "description": "Optional: MSAA sample count (1, 2, 4, or 8)",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "create_reflection_probe",
            "description": "Create a Reflection Probe in the scene for realistic reflections.",
            "parameters": {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string",
                        "description": "Optional: Name for the probe. Default: 'Reflection Probe'",
                    },
                    "position": {
                        "type": "string",
                        "description": "Optional: Position as 'x,y,z'. Default: origin",
                    },
                    "size": {
                        "type": "string",
                        "description": "Optional: Box size as 'x,y,z'. Default: '10,10,10'",
                    },
                    "mode": {
                        "type": "string",
                        "enum": ["Baked", "Realtime", "Custom"],
                        "description": "Optional: Probe mode. Default: 'Baked'",
                    },
                    "resolution": {
                        "type": "integer",
                        "description": "Optional: Cubemap resolution (16, 32, 64, 128, 256, 512, 1024, 2048). Default: 128",
                    },
                    "intensity": {
                        "type": "number",
                        "description": "Optional: Reflection intensity. Default: 1",
                    },
                    "boxProjection": {
                        "type": "boolean",
                        "description": "Optional: Enable box projection for parallax correction. Default: false",
                    },
                },
                "required": [],
            },
        },
    },
]
