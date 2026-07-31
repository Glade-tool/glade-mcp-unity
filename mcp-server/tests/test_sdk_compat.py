"""End-to-end checks that the server speaks MCP on whichever SDK is installed.

Every other test in this suite calls the handler functions directly, which is
fast but proves nothing about registration. That is exactly the gap the SDK 2.0
break fell through: the handlers were fine, and `@server.list_tools()` simply
did not exist any more, so the server came up exposing nothing and failed at
import with an AttributeError no test could see.

These tests drive the real `server` object through an in-memory transport with
a real `ClientSession` on the other end, so registration, dispatch, result
wrapping and the per-session context are all exercised the way a client
exercises them. CI runs the file against each supported SDK major.

Client-side attribute access is the one place the two generations genuinely
differ for a caller: 2.x renamed the model fields to snake_case (`is_error`,
`mime_type`) while 1.x uses camelCase. `model_field` papers over that so each
assertion can be written once.
"""

from __future__ import annotations

from contextlib import asynccontextmanager
from importlib.metadata import version

import anyio
import pytest
from mcp import ClientSession
from mcp.shared.memory import create_client_server_memory_streams
from packaging.version import Version

from gladekit_mcp import sdk_compat
from gladekit_mcp.server import server
from tests.sdk_fields import model_field


@asynccontextmanager
async def connected_client():
    """Run the real server over in-memory streams and yield a client session.

    Built by hand rather than with the SDK's own connected-session helper:
    1.x ships one, 2.x dropped it, and the point of this module is to run
    unmodified against both.
    """
    async with create_client_server_memory_streams() as (client_streams, server_streams):
        client_read, client_write = client_streams
        server_read, server_write = server_streams

        async with anyio.create_task_group() as tg:
            tg.start_soon(
                lambda: server.run(
                    server_read,
                    server_write,
                    server.create_initialization_options(),
                    raise_exceptions=True,
                )
            )
            async with ClientSession(client_read, client_write) as session:
                await session.initialize()
                yield session
            tg.cancel_scope.cancel()


@pytest.fixture(autouse=True)
def offline_cloud(monkeypatch):
    """Keep the tools in this file from reaching for the network.

    remember_for_session opportunistically saves to the cloud tier when an API
    key is present in the environment, which would make these tests depend on
    whoever is running them.
    """
    monkeypatch.setattr("gladekit_mcp.cloud.is_available", lambda: False)


def test_detected_generation_matches_the_installed_sdk():
    """Feature detection must agree with the SDK that is actually importable.

    If this fails, `create_server` is about to register handlers through an API
    the installed SDK does not have.
    """
    installed = Version(version("mcp"))
    expected = 1 if installed.major < 2 else 2
    assert sdk_compat.SDK_GENERATION == expected, (
        f"mcp {installed} is installed but sdk_compat detected generation {sdk_compat.SDK_GENERATION}"
    )


def test_optional_kwargs_drops_arguments_the_installed_sdk_lacks():
    """The mechanism that keeps the declared floor from creeping upward.

    A parameter added in a later SDK release should be passed when it exists and
    skipped when it does not, so adopting one does not silently drop every older
    SDK out of the supported range.
    """

    def older(app, security_settings=None):
        pass

    def newer(app, security_settings=None, session_idle_timeout=None):
        pass

    assert sdk_compat.optional_kwargs(newer, session_idle_timeout=1800) == {"session_idle_timeout": 1800}
    assert sdk_compat.optional_kwargs(older, session_idle_timeout=1800) == {}
    # Arguments the function does accept are passed through untouched.
    assert sdk_compat.optional_kwargs(older, security_settings="x") == {"security_settings": "x"}


def test_session_key_falls_back_outside_a_request():
    """Handlers are callable directly, and must not require a live session.

    Most of this suite drives them that way, so the key has to resolve to the
    no-session fallback rather than raising for want of a request context.
    """
    assert sdk_compat.current_session_key() == sdk_compat.NO_SESSION_KEY


@pytest.mark.asyncio
async def test_tools_are_registered_and_listable():
    """The regression: on 2.x nothing was registered, so this returned nothing."""
    async with connected_client() as session:
        result = await session.list_tools()

    names = {tool.name for tool in result.tools}
    assert len(result.tools) >= 50, f"expected the full tool surface, got {len(result.tools)}"
    for meta in ("get_relevant_tools", "remember_for_session", "recall_session_memories", "batch_execute"):
        assert meta in names, f"meta-tool '{meta}' was not advertised"


@pytest.mark.asyncio
async def test_tool_call_round_trip_carries_session_state():
    """Two calls on one session must land in the same session bucket.

    This is the load-bearing check on the request-context plumbing: the session
    key is derived from the in-flight request context, which 1.x keeps on the
    server and 2.x passes as a handler argument.
    """
    async with connected_client() as session:
        await session.list_tools()
        await session.call_tool("remember_for_session", {"fact": "Player uses CharacterController"})
        recalled = await session.call_tool("recall_session_memories", {})

    text = recalled.content[0].text
    assert "CharacterController" in text, f"session memory did not survive the round trip: {text!r}"


@pytest.mark.asyncio
async def test_separate_connections_do_not_share_session_memory():
    """The other half of the session-key contract: no bleed between clients.

    A key stable enough to survive a round trip is not automatically distinct
    per connection — keying on something process-wide would pass the test above
    and hand one user's project notes to the next.
    """
    async with connected_client() as first:
        await first.call_tool("remember_for_session", {"fact": "Player uses CharacterController"})

    async with connected_client() as second:
        recalled = await second.call_tool("recall_session_memories", {})

    assert "CharacterController" not in recalled.content[0].text, (
        "session memory leaked across two separate client connections"
    )


@pytest.mark.asyncio
async def test_invalid_tool_arguments_come_back_as_an_error_result():
    """Schema violations must be a readable tool error, not a transport failure.

    1.x validates arguments against the advertised inputSchema; 2.x does not,
    so sdk_compat does it there to keep the behaviour identical.
    """
    async with connected_client() as session:
        await session.list_tools()  # populates the schema cache
        result = await session.call_tool("remember_for_session", {"fact": 123})

    assert model_field(result, "isError") is True
    assert "Input validation error" in result.content[0].text


@pytest.mark.asyncio
async def test_resource_read_round_trip():
    async with connected_client() as session:
        listing = await session.list_resources()
        result = await session.read_resource("unity://session-memory")

    assert "unity://health" in {str(resource.uri) for resource in listing.resources}
    assert "No session memories stored yet" in result.contents[0].text
    # 1.x labels a plain-string resource return text/plain regardless of the
    # mime type advertised in the listing; sdk_compat keeps that on 2.x.
    assert model_field(result.contents[0], "mimeType") == "text/plain"


@pytest.mark.asyncio
async def test_prompt_listing_round_trip():
    async with connected_client() as session:
        result = await session.list_prompts()

    assert {prompt.name for prompt in result.prompts} == {"unity-assistant"}
