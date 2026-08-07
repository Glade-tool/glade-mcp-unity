extends GutTest

# Headless tests for set_node_transform_batch's own contract: entry
# normalization, batch-wide default inheritance, and how the fan-out is
# summarized. The transform math itself is set_node_transform's (the batch
# delegates to it) and is covered there.
#
# These are deliberately unit tests. The fan-out that touches real nodes lives
# in tests/integration/test_scene_node_tools.gd, which needs a live
# EditorInterface and therefore cannot run under GUT's play_custom_scene runner
# (see tests/README.md) — so every part of this tool that CAN be checked without
# the editor is checked here, where CI actually runs it.

const BatchTool = preload("res://addons/com.gladekit.mcp-bridge/tools/implementations/scene/set_node_transform_batch.gd")


# ── prepare_entry ─────────────────────────────────────────────────────────

func test_prepare_entry_passes_snake_case_through() -> void:
	var e := BatchTool.prepare_entry({"node_path": "Coin1", "position": "1,2,3"}, {})
	assert_eq(e["node_path"], "Coin1")
	assert_eq(e["position"], "1,2,3")


func test_prepare_entry_normalizes_camel_case_keys() -> void:
	# ws_server only normalizes the TOP level of the payload; nested entry keys
	# would otherwise reach set_node_transform unread and no-op silently.
	var e := BatchTool.prepare_entry({"nodePath": "Coin1", "position": "1,2,3"}, {})
	assert_true(e.has("node_path"), "nodePath must fold to node_path")
	assert_eq(e["node_path"], "Coin1")


func test_prepare_entry_inherits_batch_wide_defaults() -> void:
	var e := BatchTool.prepare_entry(
		{"node_path": "Coin1", "position": "1,0,0"},
		{"space": "global", "operation": "add"}
	)
	assert_eq(e["space"], "global")
	assert_eq(e["operation"], "add")


func test_prepare_entry_own_values_win_over_batch_defaults() -> void:
	var e := BatchTool.prepare_entry(
		{"node_path": "Coin1", "position": "1,0,0", "operation": "set"},
		{"operation": "add"}
	)
	assert_eq(e["operation"], "set", "an entry's own operation must win")


func test_prepare_entry_does_not_inherit_unrelated_batch_args() -> void:
	var e := BatchTool.prepare_entry({"node_path": "Coin1"}, {"transforms": [], "position": "9,9,9"})
	assert_false(e.has("transforms"))
	assert_false(e.has("position"), "only space/operation are batch-wide")


# ── build_result ──────────────────────────────────────────────────────────

func test_build_result_reports_count_and_entries() -> void:
	var r := BatchTool.build_result([
		{"node_path": "Coin1", "previous_state": {}},
		{"node_path": "Coin2", "previous_state": {}},
	], [])
	assert_true(r.success)
	assert_eq(r.count, 2)
	assert_eq(r.updated.size(), 2)
	assert_eq(r.failed.size(), 0)
	assert_string_contains(r.message, "2 nodes")


func test_build_result_surfaces_partial_failure_in_the_message() -> void:
	# A half-landed batch that reported plain success would leave the agent
	# acting on positions it never set.
	var r := BatchTool.build_result(
		[{"node_path": "Coin1", "previous_state": {}}],
		[{"node_path": "Ghost", "error": "Node 'Ghost' not found"}]
	)
	assert_true(r.success)
	assert_eq(r.count, 1)
	assert_eq(r.failed.size(), 1)
	assert_string_contains(r.message, "1 failed")


func test_build_result_with_nothing_updated_is_an_error() -> void:
	var r := BatchTool.build_result([], [
		{"node_path": "Ghost1", "error": "Node 'Ghost1' not found"},
		{"node_path": "Ghost2", "error": "Node 'Ghost2' not found"},
	])
	assert_false(r.success, "a batch that moved nothing must not report success")
	assert_string_contains(r.error, "2 entries failed")
	assert_string_contains(r.error, "Ghost1", "the first error should be quoted")
	assert_true(r.has("possible_solutions"))


func test_build_result_singular_wording_for_one_failure() -> void:
	var r := BatchTool.build_result([], [{"node_path": "Ghost", "error": "nope"}])
	assert_string_contains(r.error, "1 entry failed")


# ── empty_batch_error ─────────────────────────────────────────────────────

func test_empty_batch_error_routes_to_the_right_alternatives() -> void:
	# The refusal is also the routing hint: one node → set_node_transform, a
	# regular layout → arrange_nodes.
	var r := BatchTool.empty_batch_error()
	assert_false(r.success)
	var hints: String = "\n".join(r.possible_solutions)
	assert_string_contains(hints, "set_node_transform")
	assert_string_contains(hints, "arrange_nodes")


# ── registration ──────────────────────────────────────────────────────────

func test_tool_name_and_edit_mode_gate() -> void:
	var t = BatchTool.new()
	assert_eq(t.tool_name, "set_node_transform_batch")
	assert_true(t.requires_edit_mode, "a mutator must not dispatch during play mode")
