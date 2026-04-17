"""
Unity tool schemas organized by category.

Categories allow task-aware tool filtering: only expose tools relevant
to the current request rather than all 222 tools simultaneously.

    Task flow:
    User message
        │
        ▼
    ┌──────────────────────────┐
    │  keyword → category map  │
    │  "animate", "blend tree" │──► animation group
    │  "red", "material"       │──► materials group
    └──────────────────────────┘
        │
        ▼
    get_tools_for_request(message) → filtered list
    (always includes ALWAYS_INCLUDED + matched groups)
"""

from typing import List, Dict, Set

from .core import TOOLS as CORE_TOOLS, CATEGORY as CORE_CATEGORY
from .scene import TOOLS as SCENE_TOOLS, CATEGORY as SCENE_CATEGORY
from .scripting import TOOLS as SCRIPTING_TOOLS, CATEGORY as SCRIPTING_CATEGORY
from .prefabs import TOOLS as PREFAB_TOOLS, CATEGORY as PREFAB_CATEGORY
from .materials import TOOLS as MATERIAL_TOOLS, CATEGORY as MATERIAL_CATEGORY
from .lighting import TOOLS as LIGHTING_TOOLS, CATEGORY as LIGHTING_CATEGORY
from .vfx_audio import TOOLS as VFX_AUDIO_TOOLS, CATEGORY as VFX_AUDIO_CATEGORY
from .animation import TOOLS as ANIMATION_TOOLS, CATEGORY as ANIMATION_CATEGORY
from .ik import TOOLS as IK_TOOLS, CATEGORY as IK_CATEGORY
from .physics import TOOLS as PHYSICS_TOOLS, CATEGORY as PHYSICS_CATEGORY
from .profiler import TOOLS as PROFILER_TOOLS, CATEGORY as PROFILER_CATEGORY
from .camera import TOOLS as CAMERA_TOOLS, CATEGORY as CAMERA_CATEGORY
from .ui import TOOLS as UI_TOOLS, CATEGORY as UI_CATEGORY
from .input_system import TOOLS as INPUT_TOOLS, CATEGORY as INPUT_CATEGORY
from .terrain_nav import TOOLS as TERRAIN_NAV_TOOLS, CATEGORY as TERRAIN_NAV_CATEGORY

# All categories in priority order (core always first)
ALL_CATEGORIES = [
    (CORE_CATEGORY, CORE_TOOLS),
    (SCENE_CATEGORY, SCENE_TOOLS),
    (SCRIPTING_CATEGORY, SCRIPTING_TOOLS),
    (PREFAB_CATEGORY, PREFAB_TOOLS),
    (MATERIAL_CATEGORY, MATERIAL_TOOLS),
    (LIGHTING_CATEGORY, LIGHTING_TOOLS),
    (VFX_AUDIO_CATEGORY, VFX_AUDIO_TOOLS),
    (ANIMATION_CATEGORY, ANIMATION_TOOLS),
    (IK_CATEGORY, IK_TOOLS),
    (PHYSICS_CATEGORY, PHYSICS_TOOLS),
    (PROFILER_CATEGORY, PROFILER_TOOLS),
    (CAMERA_CATEGORY, CAMERA_TOOLS),
    (UI_CATEGORY, UI_TOOLS),
    (INPUT_CATEGORY, INPUT_TOOLS),
    (TERRAIN_NAV_CATEGORY, TERRAIN_NAV_TOOLS),
]

# Categories always included regardless of request content
ALWAYS_INCLUDED = {"core", "scene", "scripting"}


def get_unity_tool_schemas() -> List[Dict]:
    """Return all tool schemas (maintains original API for backwards compatibility)."""
    all_tools = []
    for _, tools in ALL_CATEGORIES:
        all_tools.extend(tools)
    return all_tools


def get_tool_categories() -> List[Dict]:
    """Return category metadata for all categories."""
    return [cat for cat, _ in ALL_CATEGORIES]


def get_tools_for_categories(category_names: Set[str]) -> List[Dict]:
    """Return tools for the given category names + always-included categories."""
    active = category_names | ALWAYS_INCLUDED
    result = []
    for cat, tools in ALL_CATEGORIES:
        if cat["name"] in active:
            result.extend(tools)
    return result


def get_category_for_tool(tool_name: str) -> str:
    """Return the category name for a given tool name."""
    for cat, tools in ALL_CATEGORIES:
        for tool in tools:
            if tool.get("function", {}).get("name") == tool_name:
                return cat["name"]
    return "core"
