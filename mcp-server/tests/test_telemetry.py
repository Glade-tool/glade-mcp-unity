"""Tests for batch discipline telemetry."""

from __future__ import annotations

import json
from unittest.mock import AsyncMock, patch

import pytest

from gladekit_mcp import telemetry
from gladekit_mcp.server import call_tool, read_resource

# ── Clock fixture ─────────────────────────────────────────────────────────────


class _FakeClock:
    """Monotonically advancing clock with explicit ``advance``."""

    def __init__(self, start: float = 1000.0) -> None:
        self._t = start

    def __call__(self) -> float:
        return self._t

    def advance(self, seconds: float) -> None:
        self._t += seconds


@pytest.fixture
def fake_clock():
    clock = _FakeClock()
    telemetry.set_clock(clock)
    yield clock
    telemetry.reset_clock()


# ── record_single_call ────────────────────────────────────────────────────────


class TestRecordSingleCall:
    def test_first_readonly_call_is_single_not_should_have_batched(self, fake_clock):
        kind = telemetry.record_single_call("s1", "get_scene_hierarchy")
        assert kind == "single_readonly"
        s = telemetry.get_summary("s1")
        assert s["single_readonly_calls"] == 1
        assert s["should_have_batched"] == 0
        assert s["missed_batch_bursts"] == 0

    def test_two_readonly_within_window_flags_should_have_batched(self, fake_clock):
        telemetry.record_single_call("s1", "get_scene_hierarchy")
        fake_clock.advance(1.5)
        kind = telemetry.record_single_call("s1", "find_game_objects")
        assert kind == "should_have_batched"
        s = telemetry.get_summary("s1")
        assert s["single_readonly_calls"] == 2
        assert s["should_have_batched"] == 1
        assert s["missed_batch_bursts"] == 1

    def test_three_readonly_in_a_row_counts_one_burst_two_misses(self, fake_clock):
        telemetry.record_single_call("s1", "get_scene_hierarchy")
        fake_clock.advance(1.0)
        telemetry.record_single_call("s1", "find_game_objects")
        fake_clock.advance(1.0)
        telemetry.record_single_call("s1", "get_gameobject_info")
        s = telemetry.get_summary("s1")
        assert s["single_readonly_calls"] == 3
        assert s["should_have_batched"] == 2
        # All three calls belong to ONE burst — only one missed opportunity.
        assert s["missed_batch_bursts"] == 1

    def test_long_gap_starts_fresh_burst(self, fake_clock):
        telemetry.record_single_call("s1", "get_scene_hierarchy")
        fake_clock.advance(10.0)  # gap_window_s default = 4s
        kind = telemetry.record_single_call("s1", "find_game_objects")
        assert kind == "single_readonly"
        s = telemetry.get_summary("s1")
        assert s["should_have_batched"] == 0
        assert s["missed_batch_bursts"] == 0

    def test_mutating_call_breaks_burst(self, fake_clock):
        telemetry.record_single_call("s1", "get_scene_hierarchy")
        fake_clock.advance(0.1)
        telemetry.record_single_call("s1", "create_game_object")  # mutating
        fake_clock.advance(0.1)
        kind = telemetry.record_single_call("s1", "find_game_objects")
        # Mutation reset the burst — this read-only is fresh, not a missed batch.
        assert kind == "single_readonly"
        s = telemetry.get_summary("s1")
        assert s["should_have_batched"] == 0
        assert s["single_mutating_calls"] == 1

    def test_unknown_tool_treated_as_mutating(self, fake_clock):
        # Conservative classification: anything not in READ_ONLY_TOOLS is
        # treated as mutating so we don't accidentally penalize a write
        # call as "should have batched."
        kind = telemetry.record_single_call("s1", "made_up_tool")
        assert kind == "single_mutating"
        s = telemetry.get_summary("s1")
        assert s["single_mutating_calls"] == 1

    def test_custom_gap_window(self, fake_clock):
        telemetry.record_single_call("s1", "get_scene_hierarchy", gap_window_s=0.5)
        fake_clock.advance(0.7)
        kind = telemetry.record_single_call("s1", "find_game_objects", gap_window_s=0.5)
        # 0.7s > 0.5s window → fresh burst.
        assert kind == "single_readonly"


# ── record_batch_execute ──────────────────────────────────────────────────────


class TestRecordBatchExecute:
    def test_pure_readonly_batch_increments_batched_counters(self, fake_clock):
        telemetry.record_batch_execute(
            "s1",
            [
                {"toolName": "get_scene_hierarchy"},
                {"toolName": "find_game_objects"},
                {"toolName": "list_assets"},
            ],
        )
        s = telemetry.get_summary("s1")
        assert s["batch_invocations"] == 1
        assert s["batched_readonly_calls"] == 3
        assert s["batched_mutating_calls"] == 0
        assert s["should_have_batched"] == 0

    def test_mixed_batch_classifies_each_call(self, fake_clock):
        telemetry.record_batch_execute(
            "s1",
            [
                {"toolName": "get_scene_hierarchy"},
                {"toolName": "create_game_object"},
            ],
        )
        s = telemetry.get_summary("s1")
        assert s["batched_readonly_calls"] == 1
        assert s["batched_mutating_calls"] == 1

    def test_batch_execute_resets_burst_state(self, fake_clock):
        telemetry.record_single_call("s1", "get_scene_hierarchy")
        fake_clock.advance(0.1)
        telemetry.record_batch_execute("s1", [{"toolName": "find_game_objects"}])
        fake_clock.advance(0.1)
        # Following single read-only should NOT count as missed batch — the
        # batch_execute interrupted any prior burst.
        kind = telemetry.record_single_call("s1", "list_assets")
        assert kind == "single_readonly"
        s = telemetry.get_summary("s1")
        assert s["should_have_batched"] == 0


# ── get_summary ──────────────────────────────────────────────────────────────


class TestSummary:
    def test_empty_summary_returns_none_ratios(self, fake_clock):
        s = telemetry.get_summary("s_empty")
        assert s["total_calls"] == 0
        assert s["batched_readonly_ratio"] is None
        assert s["missed_batch_ratio"] is None

    def test_ratios_when_only_batched(self, fake_clock):
        telemetry.record_batch_execute(
            "s1",
            [{"toolName": "get_scene_hierarchy"}, {"toolName": "find_game_objects"}],
        )
        s = telemetry.get_summary("s1")
        assert s["batched_readonly_ratio"] == 1.0
        assert s["missed_batch_ratio"] is None  # no single read-only baseline

    def test_ratios_when_mixed(self, fake_clock):
        # 2 batched, 2 single (1 of which is missed)
        telemetry.record_batch_execute(
            "s1",
            [{"toolName": "get_scene_hierarchy"}, {"toolName": "find_game_objects"}],
        )
        fake_clock.advance(10.0)
        telemetry.record_single_call("s1", "list_assets")
        fake_clock.advance(0.5)
        telemetry.record_single_call("s1", "get_gameobject_info")
        s = telemetry.get_summary("s1")
        # 2 batched / (2 batched + 2 single) = 0.5
        assert s["batched_readonly_ratio"] == 0.5
        # 1 missed / 2 single = 0.5
        assert s["missed_batch_ratio"] == 0.5

    def test_per_session_isolation(self, fake_clock):
        telemetry.record_single_call("alice", "get_scene_hierarchy")
        fake_clock.advance(0.1)
        telemetry.record_single_call("alice", "find_game_objects")
        # Bob's session is untouched.
        telemetry.record_single_call("bob", "get_scene_hierarchy")
        a = telemetry.get_summary("alice")
        b = telemetry.get_summary("bob")
        assert a["should_have_batched"] == 1
        assert b["should_have_batched"] == 0

    def test_render_summary_text_empty(self, fake_clock):
        text = telemetry.render_summary_text("s_empty")
        assert "No tool calls recorded" in text

    def test_render_summary_text_includes_hint_when_discipline_low(self, fake_clock):
        # 4 missed batches in a row → missed_batch_ratio = 0.75 > 0.3
        telemetry.record_single_call("s1", "get_scene_hierarchy")
        for _ in range(3):
            fake_clock.advance(0.5)
            telemetry.record_single_call("s1", "find_game_objects")
        text = telemetry.render_summary_text("s1")
        assert "leaving latency on the table" in text


# ── reset ────────────────────────────────────────────────────────────────────


class TestReset:
    def test_reset_specific_session_keeps_others(self, fake_clock):
        telemetry.record_single_call("alice", "get_scene_hierarchy")
        telemetry.record_single_call("bob", "get_scene_hierarchy")
        telemetry.reset("alice")
        assert telemetry.get_summary("alice")["total_calls"] == 0
        assert telemetry.get_summary("bob")["total_calls"] == 1

    def test_reset_no_arg_clears_all(self, fake_clock):
        telemetry.record_single_call("alice", "get_scene_hierarchy")
        telemetry.record_single_call("bob", "get_scene_hierarchy")
        telemetry.reset()
        assert telemetry.get_summary("alice")["total_calls"] == 0
        assert telemetry.get_summary("bob")["total_calls"] == 0


# ── Integration with server ──────────────────────────────────────────────────


class TestServerIntegration:
    """Verify that server.py wires into telemetry on real dispatch paths."""

    @pytest.mark.asyncio
    async def test_single_dispatch_records_in_telemetry(self, fake_clock):
        async def _execute(_name, _args, **_kwargs):
            return json.dumps({"success": True})

        with patch("gladekit_mcp.bridge.execute_tool", new=_execute):
            await call_tool("get_scene_hierarchy", {})
            fake_clock.advance(0.5)
            await call_tool("find_game_objects", {"nameContains": "Player"})

        # We don't know the exact session_id under test (it's keyed off
        # request_context.session id, falling back to "_stdio" outside a
        # request). Aggregate across all sessions to verify discipline counts.
        totals = {
            "single_readonly_calls": 0,
            "should_have_batched": 0,
        }
        for sid in list(telemetry._session_counters.keys()):
            s = telemetry.get_summary(sid)
            totals["single_readonly_calls"] += s["single_readonly_calls"]
            totals["should_have_batched"] += s["should_have_batched"]
        assert totals["single_readonly_calls"] == 2
        assert totals["should_have_batched"] == 1

    @pytest.mark.asyncio
    async def test_batch_execute_records_as_batch(self, fake_clock):
        mock_results = [
            {
                "toolName": "get_scene_hierarchy",
                "success": True,
                "result": "{}",
                "error": None,
            },
            {
                "toolName": "find_game_objects",
                "success": True,
                "result": "{}",
                "error": None,
            },
        ]
        with patch(
            "gladekit_mcp.bridge.execute_batch",
            new=AsyncMock(return_value=mock_results),
        ):
            await call_tool(
                "batch_execute",
                {
                    "calls": [
                        {"toolName": "get_scene_hierarchy", "arguments": {}},
                        {"toolName": "find_game_objects", "arguments": {}},
                    ]
                },
            )

        totals = {"batch_invocations": 0, "batched_readonly_calls": 0}
        for sid in list(telemetry._session_counters.keys()):
            s = telemetry.get_summary(sid)
            totals["batch_invocations"] += s["batch_invocations"]
            totals["batched_readonly_calls"] += s["batched_readonly_calls"]
        assert totals["batch_invocations"] == 1
        assert totals["batched_readonly_calls"] == 2

    @pytest.mark.asyncio
    async def test_meta_tools_do_not_pollute_telemetry(self, fake_clock):
        # get_relevant_tools should not be recorded as a Unity dispatch.
        # It's prompt sugar and counting it would inflate single_mutating.
        await call_tool("get_relevant_tools", {"message": "make the cube red"})
        await call_tool("remember_for_session", {"fact": "x"})
        await call_tool("recall_session_memories", {})

        for sid in list(telemetry._session_counters.keys()):
            s = telemetry.get_summary(sid)
            assert s["total_calls"] == 0, f"meta-tool leaked into telemetry: {s}"

    @pytest.mark.asyncio
    async def test_resource_returns_summary_json(self, fake_clock):
        async def _execute(_name, _args, **_kwargs):
            return json.dumps({"success": True})

        with patch("gladekit_mcp.bridge.execute_tool", new=_execute):
            await call_tool("get_scene_hierarchy", {})

        body = await read_resource("unity://telemetry/batch-discipline")
        data = json.loads(body)
        # Resource fetches the *current* request's session — outside a request
        # context that maps to "_stdio". The single dispatch above ran under
        # whatever session the call_tool path created; both should agree this
        # is a valid summary shape with expected keys.
        for key in (
            "batch_invocations",
            "batched_readonly_calls",
            "single_readonly_calls",
            "should_have_batched",
            "batched_readonly_ratio",
            "missed_batch_ratio",
            "total_calls",
        ):
            assert key in data
