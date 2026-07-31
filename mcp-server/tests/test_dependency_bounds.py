"""Guard against unbounded dependency ranges on packages we track an API of.

The MCP SDK is the case that bit us. `pyproject.toml` declared `mcp[cli]>=1.0`
with no ceiling, so when SDK 2.0 shipped a redesigned low-level `Server` — the
`@server.list_tools()` / `@server.call_tool()` decorators removed outright —
every fresh resolve pulled it into a codebase written for 1.x. Users saw

    AttributeError: 'Server' object has no attribute 'list_tools'

at startup, with nothing in the package pointing at the real cause.

A range is a claim about which versions this package works with. These tests
check that the claim is bounded on both sides and that the interpreter actually
running the suite falls inside it, so a resolver picking up a new major fails
here rather than in a user's editor.
"""

from __future__ import annotations

from importlib.metadata import version
from pathlib import Path

import pytest
import tomllib
from packaging.requirements import Requirement
from packaging.version import Version

MCP_SERVER_ROOT = Path(__file__).resolve().parent.parent

# Dependencies whose API surface this package binds to directly, so a major
# bump on their side is a breaking change on ours. Anything listed here must
# carry an upper bound.
API_COUPLED_DEPENDENCIES = {"mcp"}


def _runtime_requirements() -> dict[str, Requirement]:
    data = tomllib.loads((MCP_SERVER_ROOT / "pyproject.toml").read_text(encoding="utf-8"))
    parsed = [Requirement(spec) for spec in data["project"]["dependencies"]]
    return {req.name: req for req in parsed}


@pytest.mark.parametrize("name", sorted(API_COUPLED_DEPENDENCIES))
def test_api_coupled_dependency_has_an_upper_bound(name: str):
    """A dependency we call into by name must not be open-ended at the top.

    Without a ceiling, the next major release of that package is installed
    automatically and breaks users at runtime rather than at resolve time.
    """
    req = _runtime_requirements().get(name)
    assert req is not None, f"{name} is no longer a runtime dependency — update API_COUPLED_DEPENDENCIES"

    operators = {spec.operator for spec in req.specifier}
    assert operators & {"<", "<=", "==", "~="}, (
        f"'{req}' has no upper bound. {name} is an API-coupled dependency: pin it below the "
        f"next major (e.g. '{name}>=X.Y,<Z') so a breaking release fails at install time "
        f"instead of at import time."
    )


@pytest.mark.parametrize("name", sorted(API_COUPLED_DEPENDENCIES))
def test_installed_version_satisfies_declared_range(name: str):
    """The SDK under test must be one we claim to support.

    Catches the inverse mistake: a range narrowed (or widened) in pyproject
    while CI keeps exercising a version outside it, which would let an
    unsupported SDK pass the suite.
    """
    req = _runtime_requirements()[name]
    installed = version(name)
    assert req.specifier.contains(Version(installed), prereleases=True), (
        f"{name} {installed} is installed but pyproject declares '{req}'. Either the range is "
        f"wrong or the test environment is resolving a version this package does not support."
    )
