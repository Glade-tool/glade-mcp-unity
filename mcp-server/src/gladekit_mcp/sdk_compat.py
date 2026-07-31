"""Binding layer for the two incompatible generations of the MCP Python SDK.

SDK 1.x registers request handlers as decorators on the low-level ``Server``
(``@server.list_tools()``) and lets them return bare lists. SDK 2.0 removed
those decorators outright: handlers are passed to the ``Server`` constructor as
``on_list_tools=``-style callbacks, receive a request context plus a typed
params model, and must return the matching ``*Result`` object.

Which generation is present is not something this package fully controls. It is
frequently installed into an environment the host application owns, and that
host may have already moved to 2.x. So rather than fork the server module,
handlers stay written in the 1.x shape — plain async functions over plain
arguments — and :func:`create_server` adapts them to whichever SDK is actually
importable. There is one copy of the server logic, not one per generation.

The 2.x adapter deliberately reproduces 1.x's wrapping semantics rather than
taking the SDK's newer defaults:

* resource contents keep the ``text/plain`` type that 1.x assigns when a
  handler returns a plain string;
* tool arguments are validated against the advertised ``inputSchema``, and only
  for tools the client was actually shown — tools that are callable but absent
  from the listing stay unvalidated, as on 1.x;
* an exception raised inside a tool becomes an ``isError`` tool result the
  model can read and recover from, not a transport-level error.

A client cannot tell the two generations apart from the wire.

Everything else this package touches is common to both: ``types.*`` models
still accept their camelCase aliases, and ``stdio_server``,
``StreamableHTTPSessionManager``, ``TransportSecuritySettings`` and
``Server.run()`` are unchanged.
"""

from __future__ import annotations

import contextlib
import contextvars
import functools
import inspect
import itertools
from collections.abc import Awaitable, Callable
from typing import Any

import jsonschema
from mcp import types
from mcp.server import Server

# Feature detection, not version parsing. The decorator factories are methods on
# Server in 1.x and absent in 2.x, and asking the class directly stays correct
# regardless of how the SDK is versioned or which release restores what.
HAS_DECORATOR_API = hasattr(Server, "list_tools")

SDK_GENERATION = 1 if HAS_DECORATOR_API else 2


def optional_kwargs(func: Callable[..., Any], **candidates: Any) -> dict[str, Any]:
    """Keep only the keyword arguments ``func`` actually accepts.

    For parameters that arrived in some later SDK release and are a refinement
    rather than a requirement. Passing one to an older SDK is a TypeError, so
    without this the newest such parameter would silently set the floor of the
    supported version range — which is how an optional transport's tuning knob
    came to gate installation for every stdio user.

    Dropping a parameter is only correct when its absence degrades to the
    behaviour that SDK already had. Document that at each call site.
    """
    accepted = inspect.signature(func).parameters
    return {name: value for name, value in candidates.items() if name in accepted}


# 2.x hands each handler its request context as an argument; 1.x keeps one on
# the server object. Normalizing both into a ContextVar lets code deep inside a
# handler reach the context the same way on either SDK, without threading it
# through every call.
_request_context: contextvars.ContextVar[Any] = contextvars.ContextVar("gladekit_mcp_request_context", default=None)

ListToolsHandler = Callable[[], Awaitable[list[types.Tool]]]
CallToolHandler = Callable[[str, dict], Awaitable[list[types.ContentBlock]]]
ListResourcesHandler = Callable[[], Awaitable[list[types.Resource]]]
ReadResourceHandler = Callable[[str], Awaitable[str]]
ListPromptsHandler = Callable[[], Awaitable[list[types.Prompt]]]
GetPromptHandler = Callable[[str, dict | None], Awaitable[types.GetPromptResult]]


# Namespaced so stashing it in the SDK's lifespan mapping cannot collide.
_SESSION_KEY_FIELD = "_gladekit_session_key"
_session_counter = itertools.count(1)

NO_SESSION_KEY = "_stdio"


def current_session_key() -> str:
    """Return a stable identifier for the client connection serving this request.

    Per-client state — session memory, telemetry counters, skill calibration —
    hangs off this, so it has to be constant for the life of a connection and
    distinct between concurrent ones.

    The obvious candidate, ``id(ctx.session)``, only satisfies that on 1.x. 2.x
    builds a fresh ``ServerSession`` per request, so keying on it silently gives
    every call its own bucket and nothing is ever recalled. The lifespan mapping
    is the one thing verified to be per-connection on both: the SDK enters it
    once per ``Server.run()``, which is once per stdio process and once per
    streamable-HTTP session.

    The key is stored *in* that mapping rather than derived from its ``id()``,
    because CPython reuses ids once an object is collected — a long-lived HTTP
    server could otherwise hand a new client the key of a disconnected one and
    leak the previous session's memory into it.

    Falls back to ``NO_SESSION_KEY`` outside a request, which is what handlers
    called directly (as the tests do) see.
    """
    ctx = _request_context.get()
    if ctx is None:
        return NO_SESSION_KEY

    store = getattr(ctx, "lifespan_context", None)
    if isinstance(store, dict):
        key = store.get(_SESSION_KEY_FIELD)
        if key is None:
            key = f"mcp-{next(_session_counter)}"
            store[_SESSION_KEY_FIELD] = key
        return key

    # A custom lifespan returning something other than a mapping has nowhere to
    # stash the key. Identity is still per-connection; only id reuse is a risk.
    return f"mcp-{id(store)}"


@contextlib.contextmanager
def _active_request(ctx: Any):
    token = _request_context.set(ctx)
    try:
        yield
    finally:
        _request_context.reset(token)


def _error_result(message: str) -> types.CallToolResult:
    """Build the same error payload 1.x's ``_make_error_result`` produces."""
    return types.CallToolResult(content=[types.TextContent(type="text", text=message)], isError=True)


def create_server(
    name: str,
    *,
    instructions: str,
    list_tools: ListToolsHandler,
    call_tool: CallToolHandler,
    list_resources: ListResourcesHandler,
    read_resource: ReadResourceHandler,
    list_prompts: ListPromptsHandler,
    get_prompt: GetPromptHandler,
) -> Server:
    """Build a low-level ``Server`` with these handlers bound to the installed SDK.

    Handlers are supplied in the 1.x shape and this call does whatever the
    importable SDK requires to register them.
    """
    if HAS_DECORATOR_API:
        return _create_server_v1(
            name,
            instructions=instructions,
            list_tools=list_tools,
            call_tool=call_tool,
            list_resources=list_resources,
            read_resource=read_resource,
            list_prompts=list_prompts,
            get_prompt=get_prompt,
        )
    return _create_server_v2(
        name,
        instructions=instructions,
        list_tools=list_tools,
        call_tool=call_tool,
        list_resources=list_resources,
        read_resource=read_resource,
        list_prompts=list_prompts,
        get_prompt=get_prompt,
    )


def _create_server_v1(
    name: str,
    *,
    instructions: str,
    list_tools: ListToolsHandler,
    call_tool: CallToolHandler,
    list_resources: ListResourcesHandler,
    read_resource: ReadResourceHandler,
    list_prompts: ListPromptsHandler,
    get_prompt: GetPromptHandler,
) -> Server:
    """Register handlers through the 1.x decorator API."""
    server: Server = Server(name, instructions=instructions)

    def _tracked(func):
        """Republish 1.x's server-held request context through the shared ContextVar."""

        @functools.wraps(func)
        async def wrapper(*args, **kwargs):
            try:
                ctx = server.request_context
            except LookupError:
                ctx = None
            with _active_request(ctx):
                return await func(*args, **kwargs)

        return wrapper

    # The decorators return the function unchanged, so nothing here rebinds the
    # module-level handler names the callers and tests import.
    server.list_tools()(_tracked(list_tools))
    server.call_tool()(_tracked(call_tool))
    server.list_resources()(_tracked(list_resources))
    server.read_resource()(_tracked(lambda uri: read_resource(str(uri))))
    server.list_prompts()(_tracked(list_prompts))
    server.get_prompt()(_tracked(get_prompt))
    return server


def _create_server_v2(
    name: str,
    *,
    instructions: str,
    list_tools: ListToolsHandler,
    call_tool: CallToolHandler,
    list_resources: ListResourcesHandler,
    read_resource: ReadResourceHandler,
    list_prompts: ListPromptsHandler,
    get_prompt: GetPromptHandler,
) -> Server:
    """Register handlers through the 2.x constructor-callback API."""
    # Mirrors the tool cache 1.x keeps: input validation runs only against tools
    # the client has actually been listed. Tools that are reachable but unlisted
    # are dispatched unvalidated, which is what 1.x does when a name misses the
    # cache.
    tool_cache: dict[str, types.Tool] = {}

    async def on_list_tools(ctx, params):
        with _active_request(ctx):
            tools = await list_tools()
        tool_cache.clear()
        tool_cache.update({tool.name: tool for tool in tools})
        return types.ListToolsResult(tools=tools)

    async def on_call_tool(ctx, params):
        with _active_request(ctx):
            arguments = params.arguments or {}
            tool = tool_cache.get(params.name)
            if tool is not None:
                try:
                    # `input_schema`, not `inputSchema`: 2.x renamed the model
                    # fields to snake_case. The camelCase spellings still work as
                    # aliases when *constructing* a model — which is why the
                    # registry's types.Tool(inputSchema=...) calls are unaffected —
                    # but reading one back needs the field's real name. This branch
                    # only ever runs under 2.x, so it can use it unconditionally.
                    jsonschema.validate(instance=arguments, schema=tool.input_schema)
                except jsonschema.ValidationError as exc:
                    return _error_result(f"Input validation error: {exc.message}")
            try:
                content = await call_tool(params.name, arguments)
            except Exception as exc:
                # 1.x turns a handler exception into an error *result*. Keeping
                # that means a failing tool still reaches the model as readable
                # text instead of collapsing the request.
                return _error_result(str(exc))
            return types.CallToolResult(content=list(content), isError=False)

    async def on_list_resources(ctx, params):
        with _active_request(ctx):
            return types.ListResourcesResult(resources=await list_resources())

    async def on_read_resource(ctx, params):
        with _active_request(ctx):
            text = await read_resource(str(params.uri))
        # 1.x labels a plain-string return text/plain regardless of the mime type
        # advertised in the resource listing. Keep that so the content type does
        # not shift underneath clients when the SDK generation changes.
        return types.ReadResourceResult(
            contents=[types.TextResourceContents(uri=params.uri, text=text, mimeType="text/plain")]
        )

    async def on_list_prompts(ctx, params):
        with _active_request(ctx):
            return types.ListPromptsResult(prompts=await list_prompts())

    async def on_get_prompt(ctx, params):
        with _active_request(ctx):
            return await get_prompt(params.name, params.arguments)

    return Server(
        name,
        instructions=instructions,
        on_list_tools=on_list_tools,
        on_call_tool=on_call_tool,
        on_list_resources=on_list_resources,
        on_read_resource=on_read_resource,
        on_list_prompts=on_list_prompts,
        on_get_prompt=on_get_prompt,
    )
