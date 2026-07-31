"""Read model fields that the MCP SDK renamed between major versions.

SDK 2.0 moved the wire protocol's camelCase field names to snake_case on the
Python models: ``inputSchema`` became ``input_schema``, ``isError`` became
``is_error``, and so on. Construction is unaffected — both generations accept
the camelCase spellings as aliases, which is why the tool schemas this package
builds need no changes — but reading an attribute back requires the field's
real name for the SDK in play.

Tests assert against these models on whichever SDK is installed, so they go
through :func:`model_field` instead of picking a spelling and pinning
themselves to one generation.
"""

from __future__ import annotations

import re
from typing import Any

_SENTINEL = object()


def _snake(name: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).lower()


def model_field(model: Any, name: str, default: Any = _SENTINEL) -> Any:
    """Return ``model``'s field, accepting either the camelCase or snake_case name.

    Raises AttributeError if the field is absent under both spellings and no
    default was supplied — a genuinely missing field should still fail loudly
    rather than silently read as None.
    """
    for candidate in (name, _snake(name)):
        if hasattr(model, candidate):
            return getattr(model, candidate)
    if default is not _SENTINEL:
        return default
    raise AttributeError(f"{type(model).__name__} has no field '{name}' under either naming convention")
