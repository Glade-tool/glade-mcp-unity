"""
Materials category tools — materials, shaders, textures, sprites, model/audio import settings.
"""

from typing import Dict, List

CATEGORY = {
    "name": "materials",
    "display_name": "Materials & Shaders",
    "keywords": [
        "material",
        "shader",
        "color",
        "texture",
        "sprite",
        "render pipeline",
        "urp",
        "hdrp",
        "import",
        "model",
        "spritesheet",
        "slice",
        "audio import",
    ],
}

TOOLS: List[Dict] = [
    {
        "type": "function",
        "function": {
            "name": "create_material",
            "description": "Create a new Material asset. Check system prompt for 'ACTIVE RENDER PIPELINE' to pick the right shader: URP='Universal Render Pipeline/Lit', HDRP='HDRP/Lit', Built-in='Standard'. After creating, call assign_material_to_renderer to apply it.",
            "parameters": {
                "type": "object",
                "properties": {
                    "materialPath": {
                        "type": "string",
                        "description": "Path relative to Assets folder (e.g., 'Materials/Red.mat', 'Materials/Blue.mat')",
                    },
                    "shaderName": {
                        "type": "string",
                        "description": "Required. Check system prompt for active render pipeline: URP='Universal Render Pipeline/Lit', HDRP='HDRP/Lit', Built-in='Standard'.",
                    },
                    "color": {
                        "type": "string",
                        "description": "Base color as a comma-separated string 'r,g,b,a' (e.g., '1,0,0,1' for red, '0,0,1,1' for blue). All values 0-1. Must be a string, not an array.",
                    },
                    "metallic": {
                        "type": "string",
                        "description": "Metallic value as string (e.g., '0.5'). Range 0-1. Omit to use default.",
                    },
                    "smoothness": {
                        "type": "string",
                        "description": "Smoothness value as string (e.g., '0.5'). Range 0-1. Omit to use default.",
                    },
                },
                "required": ["materialPath", "shaderName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_material_property",
            "description": "Set a property on a Material asset. For base color: '_BaseColor' (URP/HDRP) or '_Color' (Built-in). Check the system prompt for active render pipeline. Use list_materials to find the path if needed. If material is shared, consider creating a new one instead.",
            "parameters": {
                "type": "object",
                "properties": {
                    "materialPath": {
                        "type": "string",
                        "description": "Path relative to Assets folder (e.g., 'Materials/MyMaterial.mat'). If you only know the material name, use list_materials first to find the path.",
                    },
                    "propertyName": {
                        "type": "string",
                        "description": "Name of the shader property. For base color: use '_BaseColor' for URP/HDRP, '_Color' for Standard. Other examples: '_MainTex', '_Metallic', '_Glossiness', '_BumpMap', '_EmissionColor'",
                    },
                    "value": {
                        "type": "string",
                        "description": "Value as string. Colors: 'r,g,b,a' (e.g., '1,0,0,1'). Textures: asset path (e.g., 'Textures/MyTexture.png'). Floats: numeric string (e.g., '0.5'). Must be a string.",
                    },
                },
                "required": ["materialPath", "propertyName", "value"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "assign_material_to_renderer",
            "description": "Assign a Material to a GameObject's Renderer. Call after create_material. Verify existing materials exist with check_asset_exists first.",
            "parameters": {
                "type": "object",
                "properties": {
                    "materialPath": {
                        "type": "string",
                        "description": "Path relative to Assets folder (e.g., 'Materials/Red.mat'). Must match the path used in create_material.",
                    },
                    "gameObjectPath": {
                        "type": "string",
                        "description": "Name or path of the target GameObject (e.g., 'RedCube', 'BlueCube'). Required when you just created multiple objects.",
                    },
                    "materialSlot": {
                        "type": "string",
                        "description": "Material slot index as string (e.g., '0' for first slot). Omit to use slot 0.",
                    },
                },
                "required": ["materialPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "list_materials",
            "description": "List all Material assets in the project. Returns a list of material paths. Use this to find existing materials before creating new ones, or when you need to find a material by name (e.g., if user says 'change the blue material', use searchPattern='blue' to find it).",
            "parameters": {
                "type": "object",
                "properties": {
                    "searchPattern": {
                        "type": "string",
                        "description": "Optional: Search pattern to filter materials by name (e.g., 'Blue' to find materials containing 'Blue' in their name or path)",
                    },
                    "maxResults": {
                        "type": "integer",
                        "description": "Optional: Max results to return (1-500). Default: 200",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_material_usage",
            "description": "Check which GameObjects in the scene are using a specific material. Returns the list of GameObjects using the material and whether it's shared (used by multiple objects). Use this to decide whether to modify an existing material (if unique) or create a new one (if shared and you only want to change one object).",
            "parameters": {
                "type": "object",
                "properties": {
                    "materialPath": {
                        "type": "string",
                        "description": "Path relative to Assets folder (e.g., 'Materials/MyMaterial.mat')",
                    }
                },
                "required": ["materialPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "find_materials_by_shader",
            "description": "Find all materials in the project that use a specific shader. Returns a list of material paths and names.",
            "parameters": {
                "type": "object",
                "properties": {
                    "shaderName": {
                        "type": "string",
                        "description": "Exact shader name to search for (e.g., 'Nature/Soft Occlusion', 'Standard', 'Universal Render Pipeline/Lit')",
                    }
                },
                "required": ["shaderName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "get_shader_info",
            "description": "Get detailed information about a shader including its properties, render queue, and supported features. Use to understand what properties a shader supports before creating or modifying materials.",
            "parameters": {
                "type": "object",
                "properties": {
                    "shaderName": {
                        "type": "string",
                        "description": "Exact shader name to query (e.g., 'Universal Render Pipeline/Lit', 'Standard')",
                    }
                },
                "required": ["shaderName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "list_available_shaders",
            "description": "List all available shaders in the project. Returns shader names that can be used when creating materials or specifying shader names.",
            "parameters": {
                "type": "object",
                "properties": {
                    "searchPattern": {
                        "type": "string",
                        "description": "Optional: Filter shaders by name pattern (e.g., 'URP' to find all URP shaders)",
                    },
                    "maxResults": {
                        "type": "integer",
                        "description": "Maximum results to return (default: 200, max: 500)",
                    },
                },
                "required": [],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "change_material_shader",
            "description": "Change a material's shader to a different shader. Automatically preserves common shader properties (color, textures, metallic, smoothness) when converting between similar shaders. CRITICAL: Check system prompt for active render pipeline and use appropriate shader names.",
            "parameters": {
                "type": "object",
                "properties": {
                    "materialPath": {
                        "type": "string",
                        "description": "Path relative to Assets folder (e.g., 'Materials/TreeMaterial.mat')",
                    },
                    "newShaderName": {
                        "type": "string",
                        "description": "Exact shader name to change to. Check system prompt for render pipeline and use appropriate shader (e.g., 'Universal Render Pipeline/Lit' for URP, 'HDRP/Lit' for HDRP)",
                    },
                },
                "required": ["materialPath", "newShaderName"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "convert_materials_to_render_pipeline",
            "description": "Convert all materials using a specific shader to a target shader for the target render pipeline. Finds all materials using the source shader across the entire project, converts them to the target shader, and automatically handles shader property mapping (color, textures, metallic, smoothness, etc.). Requires shaderName (source shader), targetPipeline (URP or HDRP), and targetShader (destination shader name).",
            "parameters": {
                "type": "object",
                "properties": {
                    "shaderName": {
                        "type": "string",
                        "description": "Shader name to find materials using (e.g., 'Nature/Soft Occlusion', 'Standard'). This should be the OLD/BIRP shader name that materials are currently using. All materials using this shader will be converted to the target shader.",
                    },
                    "targetPipeline": {
                        "type": "string",
                        "description": "Target render pipeline: 'URP' or 'HDRP'. Check system prompt to confirm which pipeline the project uses.",
                        "enum": ["URP", "HDRP"],
                    },
                    "targetShader": {
                        "type": "string",
                        "description": "Target shader name to convert materials to (e.g., 'Universal Render Pipeline/Nature/SpeedTree7', 'Universal Render Pipeline/Lit'). Use RAG context to determine the correct target shader based on the source shader and target pipeline.",
                    },
                },
                "required": ["shaderName", "targetPipeline", "targetShader"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_texture_import_settings",
            "description": "Update Texture import settings. Can convert Texture2D to Sprite by setting textureType to 'Sprite'. Valid textureType values: 'Default', 'NormalMap', 'EditorGUI', 'Sprite', 'Cursor', 'Cookie', 'Lightmap', 'SingleChannel', 'GUID'. For converting to Sprite, prefer set_sprite_import_settings which automatically sets sprite-specific defaults.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {"type": "string"},
                    "textureType": {
                        "type": "string",
                        "description": "Texture import type. Use 'Sprite' to convert Texture2D to Sprite (2D/UI). Valid values: 'Default', 'NormalMap', 'EditorGUI', 'Sprite', 'Cursor', 'Cookie', 'Lightmap', 'SingleChannel', 'GUID'.",
                    },
                    "sRGBTexture": {"type": "boolean"},
                    "alphaIsTransparency": {"type": "boolean"},
                    "mipmapEnabled": {"type": "boolean"},
                    "maxTextureSize": {"type": "integer"},
                },
                "required": ["assetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_sprite_import_settings",
            "description": "Convert a texture import from Texture2D to Sprite (2D/UI) and configure sprite-specific import settings. This is REQUIRED when you need to assign a texture to a SpriteRenderer component - textures imported as Texture2D cannot be assigned to SpriteRenderer. Use this tool to change the import type to Sprite, then you can use set_component_property to assign it. Configure sprite-specific texture import settings for 2D workflows.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {"type": "string"},
                    "spriteMode": {
                        "type": "string",
                        "enum": ["Single", "Multiple", "Polygon"],
                    },
                    "pixelsPerUnit": {"type": "number"},
                    "meshType": {"type": "string", "enum": ["FullRect", "Tight"]},
                    "filterMode": {
                        "type": "string",
                        "enum": ["Point", "Bilinear", "Trilinear"],
                    },
                    "wrapMode": {
                        "type": "string",
                        "enum": ["Repeat", "Clamp", "Mirror", "MirrorOnce"],
                    },
                    "compression": {
                        "type": "string",
                        "enum": [
                            "Uncompressed",
                            "Compressed",
                            "CompressedHQ",
                            "CompressedLQ",
                        ],
                    },
                    "alphaIsTransparency": {"type": "boolean"},
                    "sRGBTexture": {"type": "boolean"},
                    "mipmapEnabled": {"type": "boolean"},
                    "maxTextureSize": {"type": "integer"},
                },
                "required": ["assetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "slice_spritesheet_grid",
            "description": "Slice a spritesheet into multiple sprites using a uniform grid. The texture must be imported as Sprite (use set_sprite_import_settings first if needed). Parameters: cellWidth/cellHeight define each sprite's size, offsetX/offsetY define the starting position (top-left of first cell), paddingX/paddingY define spacing between cells. Unity uses bottom-left origin for sprite coordinates. For dynamic sizing, set autoDetectCellSize=true and provide columns and/or rows; cell size is computed from texture dimensions, offsets, and padding. If no cells are generated, check that cell size + padding fits within texture dimensions and offsets don't exceed available space.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {"type": "string"},
                    "cellWidth": {"type": "integer"},
                    "cellHeight": {"type": "integer"},
                    "autoDetectCellSize": {"type": "boolean"},
                    "columns": {"type": "integer"},
                    "rows": {"type": "integer"},
                    "offsetX": {"type": "integer"},
                    "offsetY": {"type": "integer"},
                    "paddingX": {"type": "integer"},
                    "paddingY": {"type": "integer"},
                    "pivotX": {"type": "number"},
                    "pivotY": {"type": "number"},
                    "namePrefix": {"type": "string"},
                },
                "required": ["assetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_model_import_settings",
            "description": "Update import settings for assets. Automatically detects asset type: for 3D model files (FBX, OBJ, DAE, etc.) applies model import settings; for texture files (PNG, JPG, etc.) automatically converts to Sprite import type. Works with both models and textures - the tool automatically handles the correct import settings based on file type.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {"type": "string"},
                    "scaleFactor": {"type": "number"},
                    "importAnimation": {"type": "boolean"},
                    "importMaterials": {"type": "boolean"},
                    "meshCompression": {"type": "string"},
                },
                "required": ["assetPath"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "set_audio_import_settings",
            "description": "Update Audio import settings.",
            "parameters": {
                "type": "object",
                "properties": {
                    "assetPath": {"type": "string"},
                    "loadType": {"type": "string"},
                    "compressionFormat": {"type": "string"},
                    "quality": {"type": "number"},
                    "sampleRateSetting": {"type": "string"},
                    "forceToMono": {"type": "boolean"},
                    "preloadAudioData": {"type": "boolean"},
                    "loadInBackground": {"type": "boolean"},
                },
                "required": ["assetPath"],
            },
        },
    },
]
