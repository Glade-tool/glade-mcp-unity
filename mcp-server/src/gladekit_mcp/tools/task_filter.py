"""
Task-aware tool filtering — select relevant tool categories based on the user's message.

Rather than exposing all 235+ tools on every request, this module analyzes the
user's message using keyword matching and returns only the relevant tool groups
plus always-included core categories.

Design:
  Keyword → category mapping uses simple regex/substring matching (sub-ms latency,
  no API calls). If the message matches nothing specific, ALL tools are returned
  (fail-open — never block the model from tools it might need).

  Always-included categories: core, scene, scripting
  These cover the tools needed for ~80% of requests.

  Examples:
    "make the cube red"             → materials + always (core/scene/scripting)
    "set up a blend tree"           → animation + always
    "add a rigidbody to the player" → physics + always
    "?? (unrecognized message)"     → ALL 235+ tools (fail-open)
"""

import re
from typing import Dict, List, Set

from ..schemas.godot import ALL_CATEGORIES as _GODOT_CATEGORIES
from . import get_tools_for_categories, get_unity_tool_schemas

ALWAYS_INCLUDED: Set[str] = {"core", "scene", "scripting"}

_CATEGORY_KEYWORDS: Dict[str, List[str]] = {
    "prefabs": [
        r"\bprefab\b",
        r"\binstantiate\b",
        r"\bspawn\b",
    ],
    "materials": [
        r"\bmaterial\b",
        r"\bshader\b",
        r"\bcolor\b",
        r"\bcolou?r\b",
        r"\btexture\b",
        r"\bsprite\b",
        r"\brender pipeline\b",
        r"\burp\b",
        r"\bhdrp\b",
        r"\bimport\b.*\.(png|jpg|tga|psd|fbx|obj)\b",
        r"\bslice\b.*sprite\b",
        r"\bsprite sheet\b",
        r"\bmodel import\b",
    ],
    "lighting": [
        r"\blight\b",
        r"\blighting\b",
        r"\bshadows?\b",
        r"\bambient\b",
        r"\bskybox\b",
        r"\breflection probe\b",
        r"\bquality settings\b",
        r"\brender settings\b",
    ],
    "vfx_audio": [
        r"\bparticle\b",
        r"\bvfx\b",
        r"\beffect\b",
        r"\baudio\b",
        r"\bsounds?\b",
        r"\bmusic\b",
        r"\baudio clip\b",
        r"\baudio source\b",
    ],
    "animation": [
        r"\banimat",
        r"\bblend tree\b",
        r"\bstate machine\b",
        r"\btransition\b",
        r"\bkeyframe\b",
        r"\bclip\b",
        r"\bsprite animation\b",
        r"\banimator controller\b",
        r"\bik\b",
        r"\binverse kinematic\b",
        r"\brig\b",
        r"\bbone\b",
    ],
    "ik": [
        r"\bik\b",
        r"\binverse kinematic\b",
        r"\brig\b",
    ],
    "physics": [
        r"\bphysics?\b",
        r"\bcollider\b",
        r"\brigidbody\b",
        r"\brigid body\b",
        r"\bgravity\b",
        r"\bcollision\b",
        r"\btrigger\b",
        r"\bcharacter controller\b",
        r"\bphysics material\b",
        r"\bjump\b",
        r"\bfall\b",
        r"\braycast\b",
        r"\blinecast\b",
        r"\boverlap\b",
        r"\bsphere cast\b",
        r"\bbox cast\b",
        r"\bshapecast\b",
        r"\bcollision matrix\b",
        r"\blayer collision\b",
    ],
    "profiler": [
        r"\bprofil",
        r"\bperformance\b",
        r"\bframe time\b",
        r"\bframe timing\b",
        r"\bfps\b",
        r"\bmemory\b.*\b(usage|stats?|leak)\b",
        r"\bgc\b.*\b(alloc|collect)\b",
        r"\bgarbage collect",
        r"\bdraw calls?\b",
        r"\bbatches\b",
        r"\bframe debugger\b",
        r"\brender pass\b",
        r"\boptimiz",
        r"\bslow\b",
        r"\blag\b",
    ],
    "camera": [
        r"\bcamera\b",
        r"\bcinemachine\b",
        r"\brender texture\b",
        r"\bpost.?process",
        r"\bfrustum\b",
        r"\bfield of view\b",
        r"\bfov\b",
        r"\bvirtual camera\b",
    ],
    "ui": [
        r"\bui\b",
        r"\bcanvas\b",
        r"\bbutton\b",
        r"\btext\b",
        r"\bimage\b",
        r"\bslider\b",
        r"\bscroll\b",
        r"\bpanel\b",
        r"\bhud\b",
        r"\bmenu\b",
        r"\btextmeshpro\b",
        r"\btmp\b",
        r"\blayout\b",
        r"\bevent system\b",
        r"\btooltip\b",
        r"\bpopup\b",
    ],
    "input_system": [
        r"\binput\b",
        r"\bkey\b",
        r"\bkeyboard\b",
        r"\bmouse\b",
        r"\bcontroller\b",
        r"\bgamepad\b",
        r"\baction map\b",
        r"\bbinding\b",
        r"\baxis\b",
        r"\blegacy input\b",
    ],
    "terrain_nav": [
        r"\bterrain\b",
        r"\bnavmesh\b",
        r"\bnavigation\b",
        r"\bpathfind\b",
        r"\bnav agent\b",
        r"\bobstacle\b",
        r"\bwaypoint\b",
        r"\bai path\b",
    ],
    "runtime": [
        r"\bplay mode\b",
        r"\bplaymode\b",
        r"\blive loop\b",
        r"\bruntime error\b",
        r"\bruntime exception\b",
        r"\bnull ?ref\b",
        r"\bnre\b",
        r"\bobserve errors?\b",
        r"\bwatch errors?\b",
        r"\bapply.*fix\b",
    ],
}

_COMPILED_KEYWORDS: Dict[str, List[re.Pattern]] = {
    cat: [re.compile(p, re.IGNORECASE) for p in patterns] for cat, patterns in _CATEGORY_KEYWORDS.items()
}


def _match_categories(message: str, compiled: Dict[str, List[re.Pattern]]) -> Set[str]:
    if not message or not message.strip():
        return set()

    matched: Set[str] = set()
    for cat, patterns in compiled.items():
        for pattern in patterns:
            if pattern.search(message):
                matched.add(cat)
                break
    return matched


def categorize_message(message: str) -> Set[str]:
    """
    Return Unity category names relevant to the user's message.

    Returns empty set if nothing matches (caller should use all tools / fail-open).
    Does NOT include ALWAYS_INCLUDED — caller decides whether to add those.
    """
    return _match_categories(message, _COMPILED_KEYWORDS)


def get_tools_for_request(message: str) -> List[Dict]:
    """
    Return the filtered tool list for a given user message.

    If categories match, returns those + ALWAYS_INCLUDED.
    If nothing matches, returns all tools (fail-open).
    """
    matched = categorize_message(message)
    if not matched:
        return get_unity_tool_schemas()
    return get_tools_for_categories(matched)


def get_relevant_tool_summary(message: str, engine: str = "unity") -> str:
    """
    Return a formatted summary of relevant tools for the given message.

    Used by the get_relevant_tools MCP meta-tool. `engine` selects the
    catalog and keyword map: "unity" (default) or "godot".
    """
    if engine == "godot":
        matched = categorize_message_godot(message)
        all_tools = get_godot_tool_schemas()
        active = matched | GODOT_ALWAYS_INCLUDED
        tools = get_godot_tools_for_categories(matched)
    else:
        matched = categorize_message(message)
        all_tools = get_unity_tool_schemas()
        active = matched | ALWAYS_INCLUDED
        tools = get_tools_for_categories(matched)

    if not matched:
        return (
            f"All {len(all_tools)} tools are potentially relevant for this request. "
            "No specific category detected — all tools are available."
        )

    lines = [f"Categories: {', '.join(sorted(active))} ({len(tools)} tools)\n"]
    for tool in tools:
        func = tool.get("function", {})
        name = func.get("name", "?")
        desc = func.get("description", "")
        # Truncate long descriptions
        if len(desc) > 120:
            desc = desc[:117] + "..."
        lines.append(f"- {name}: {desc}")

    return "\n".join(lines)


# ── Godot ─────────────────────────────────────────────────────────────────────
# The Godot bridge exposes its whole catalog (no CORE_TOOLS filter), so on
# Godot the meta-tool is about grouping the catalog for a task and carrying
# the RAG context, not discovery. Categories follow schemas/godot/ALL_CATEGORIES
# (the bridge's own directory layout). The keyword map reuses the Unity
# patterns where the vocabulary is shared and adds Godot's own — signals,
# autoloads, exports, UIDs — and merges categories Godot groups differently
# (camera + light + WorldEnvironment are one category there).

GODOT_ALWAYS_INCLUDED: Set[str] = {"scene", "script", "scene_io"}

_GODOT_CATEGORY_KEYWORDS: Dict[str, List[str]] = {
    "camera": _CATEGORY_KEYWORDS["camera"]
    + _CATEGORY_KEYWORDS["lighting"]
    + [r"\bworld ?environment\b", r"\bfog\b", r"\bglow\b", r"\bsky\b", r"\btonemap"],
    "resource": _CATEGORY_KEYWORDS["materials"]
    + [r"\bresources?\b", r"\b\.tres\b", r"\bmesh\b", r"\bshape\b", r"\bgradient\b", r"\bcurve\b"],
    "physics": _CATEGORY_KEYWORDS["physics"]
    + [
        r"\bcollision (layer|mask)s?\b",
        r"\bfriction\b",
        r"\bbounce\b",
        r"\bshape ?cast\b",
        r"\b(character|rigid|static|animatable) ?body",
    ],
    "particles": [
        r"\bparticle",
        r"\bvfx\b",
        r"\beffects?\b",
        r"\bexplosion\b",
        r"\bsparkle\b",
        r"\bsmoke\b",
        r"\bfire\b",
        r"\btrail\b",
    ],
    "audio": [r"\baudio\b", r"\bsounds?\b", r"\bmusic\b", r"\bsfx\b", r"\bvolume\b", r"\bstream ?player\b"],
    "animation": _CATEGORY_KEYWORDS["animation"]
    + [r"\banimation ?tree\b", r"\bblend ?space\b", r"\banimation ?player\b", r"\btrack\b"],
    "ui": _CATEGORY_KEYWORDS["ui"]
    + [
        r"\bcontrol\b",
        r"\blabel\b",
        r"\btheme\b",
        r"\banchors?\b",
        r"\bprogress ?bar\b",
        r"\bhealth bar\b",
        r"\bpause menu\b",
        r"\bmain menu\b",
    ],
    "signal": [r"\bsignals?\b", r"\bconnect\b", r"\bdisconnect\b", r"\bemit", r"\bcallbacks?\b"],
    "project": _CATEGORY_KEYWORDS["input_system"]
    + [
        r"\bproject (info|settings)\b",
        r"\baddons?\b",
        r"\bautoload\b",
        r"\bsingleton\b",
        r"\brenderer\b",
        r"\bsession summary\b",
        r"\bwhat did you\b",
        r"\blist (the )?assets\b",
    ],
    "navigation": _CATEGORY_KEYWORDS["terrain_nav"] + [r"\bnav ?mesh\b", r"\bnavigation ?(agent|region)\b", r"\bpursu"],
    "runtime": _CATEGORY_KEYWORDS["runtime"]
    + [
        r"\brun (the )?(game|project|scene)\b",
        r"\bplay ?test",
        r"\bprobe\b",
        r"\bconsole\b",
        r"\berrors?\b",
        r"\bdebug output\b",
        r"\bscreenshot\b",
        r"\blook at\b",
        r"\bgame view\b",
        r"\bselection\b",
        r"\bstop (the )?(game|project)\b",
    ],
    "export": [
        r"\bexport",
        r"\bbuild (the |a |an )?(game|project|executable|binary|for)\b",
        r"\bship\b",
        r"\brelease build\b",
        r"\bweb build\b",
        r"\bitch\.io\b",
        r"\bexecutable\b",
    ],
    "uid": [r"\buids?\b", r"\bresource ?uid\b", r"\b\.uid\b"],
    "asset_pipeline": [
        r"\bkenney\b",
        r"\bfree assets?\b",
        r"\bimport (an? |some )?assets?\b",
        r"\bcc0\b",
        r"\bdownload\b",
        r"\battribution\b",
        r"\blicen[cs]e",
        r"\basset packs?\b",
        r"\bplaceholder (art|sprites?|assets?)\b",
    ],
}

_GODOT_COMPILED_KEYWORDS: Dict[str, List[re.Pattern]] = {
    cat: [re.compile(p, re.IGNORECASE) for p in patterns] for cat, patterns in _GODOT_CATEGORY_KEYWORDS.items()
}


def categorize_message_godot(message: str) -> Set[str]:
    """Godot counterpart of `categorize_message` (fail-open: empty set on no match)."""
    return _match_categories(message, _GODOT_COMPILED_KEYWORDS)


def get_godot_tool_schemas() -> List[Dict]:
    """Every Godot tool schema, in catalog order."""
    return [tool for _, tools in _GODOT_CATEGORIES for tool in tools]


def get_godot_tools_for_categories(category_names: Set[str]) -> List[Dict]:
    """Godot tools for the given categories + GODOT_ALWAYS_INCLUDED."""
    active = category_names | GODOT_ALWAYS_INCLUDED
    return [tool for cat, tools in _GODOT_CATEGORIES if cat in active for tool in tools]
