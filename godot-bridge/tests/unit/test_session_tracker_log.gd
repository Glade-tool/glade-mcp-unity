extends GutTest

# Headless tests for SessionTracker's mutation log — the Godot twin of the
# Unity bridge's SessionTracker timeline. The payload shape (camelCase keys)
# is the shared wire contract consumed by the Proxy's _shape_session_summary
# formatter and the Electron SessionSummary type, so key names asserted here
# are contract assertions, not style checks.
#
# Deliberately unit tests: record_dispatch/build_summary are static and
# editor-free, so everything CI actually runs is covered here. The ws_server
# dispatch hooks that call record_dispatch need a live editor and are
# exercised by the integration suite.

const SessionTracker = preload("res://addons/com.gladekit.mcp-bridge/bridge/session_tracker.gd")
const GetSessionSummaryTool = preload("res://addons/com.gladekit.mcp-bridge/tools/implementations/project/get_session_summary.gd")


func before_each() -> void:
	SessionTracker.reset_log()


func after_all() -> void:
	SessionTracker.reset_log()


# ── record_dispatch ───────────────────────────────────────────────────────

func test_mutations_enter_the_timeline() -> void:
	SessionTracker.record_dispatch(
		"create_node",
		{"node_path": "Player"},
		{"success": true, "message": "Created Node3D 'Player'", "node_path": "Player"}
	)
	var s := SessionTracker.build_summary()
	assert_eq(int(s["mutations"]), 1)
	assert_eq(int(s["toolCalls"]), 1)
	assert_eq(int(s["successCount"]), 1)
	assert_eq(int(s["errorCount"]), 0)


func test_read_only_tools_count_as_calls_but_never_enter_the_timeline() -> void:
	# The guard's READ_ONLY_TOOLS is the authoritative read classification;
	# the tracker consults it rather than keeping a second list.
	SessionTracker.record_dispatch("get_scene_tree", {}, {"success": true, "message": "ok"})
	SessionTracker.record_dispatch("get_session_summary", {}, {"success": true, "message": "ok"})
	var s := SessionTracker.build_summary()
	assert_eq(int(s["toolCalls"]), 2)
	assert_eq(int(s["mutations"]), 0)
	assert_eq((s["timeline"] as Array).size(), 0)


func test_failures_are_counted_and_kept_out_of_by_category() -> void:
	SessionTracker.record_dispatch(
		"create_node", {}, {"success": false, "error": "No scene is currently open"}
	)
	var s := SessionTracker.build_summary()
	assert_eq(int(s["errorCount"]), 1)
	assert_eq(int(s["successCount"]), 0)
	# byCategory groups only successful mutations (Unity behavior); the
	# failure is still visible in the timeline with success=false.
	assert_true((s["byCategory"] as Dictionary).is_empty())
	var entry: Dictionary = (s["timeline"] as Array)[0]
	assert_false(bool(entry["success"]))
	assert_true(str(entry["summary"]).begins_with("Error: "))


# ── build_summary shape (the wire contract) ───────────────────────────────

func test_summary_carries_the_shared_camel_case_field_set() -> void:
	SessionTracker.record_dispatch(
		"create_node", {"node_path": "Player"}, {"success": true, "message": "ok", "node_path": "Player"}
	)
	var s := SessionTracker.build_summary()
	for key in [
		"sessionStartedAt", "elapsedSeconds", "toolCalls", "mutations",
		"successCount", "errorCount", "byCategory", "timeline", "timelineTruncated",
	]:
		assert_true(s.has(key), "summary missing contract field '%s'" % key)
	var entry: Dictionary = (s["timeline"] as Array)[0]
	for key in ["tMs", "tIso", "tool", "action", "category", "target", "summary", "success"]:
		assert_true(entry.has(key), "timeline entry missing contract field '%s'" % key)
	# tIso must parse as ISO-8601-ish UTC ("YYYY-MM-DDTHH:MM:SSZ").
	assert_true(str(entry["tIso"]).ends_with("Z"))
	assert_true(str(entry["tIso"]).contains("T"))


func test_by_category_groups_and_dedupes_targets() -> void:
	SessionTracker.record_dispatch(
		"create_node", {}, {"success": true, "message": "ok", "node_path": "Coin1"}
	)
	SessionTracker.record_dispatch(
		"set_node_transform", {"node_path": "Coin1"}, {"success": true, "message": "moved"}
	)
	SessionTracker.record_dispatch(
		"delete_node", {"node_path": "Coin2"}, {"success": true, "message": "gone"}
	)
	var s := SessionTracker.build_summary()
	var nodes: Dictionary = (s["byCategory"] as Dictionary)["nodes"]
	assert_eq(int(nodes["created"]), 1)
	assert_eq(int(nodes["modified"]), 1)
	assert_eq(int(nodes["destroyed"]), 1)
	# Coin1 appears in two records but once in targets.
	assert_eq((nodes["targets"] as Array).count("Coin1"), 1)
	assert_true((nodes["targets"] as Array).has("Coin2"))


func test_timeline_is_recent_first_and_truncation_is_flagged() -> void:
	for i in range(5):
		SessionTracker.record_dispatch(
			"create_node", {}, {"success": true, "message": "ok", "node_path": "N%d" % i}
		)
	var s := SessionTracker.build_summary(3)
	var timeline: Array = s["timeline"]
	assert_eq(timeline.size(), 3)
	assert_eq(str((timeline[0] as Dictionary)["target"]), "N4")
	assert_true(bool(s["timelineTruncated"]))
	var s_all := SessionTracker.build_summary(50)
	assert_false(bool(s_all["timelineTruncated"]))


# ── classification ────────────────────────────────────────────────────────

func test_action_classification_by_prefix() -> void:
	assert_eq(SessionTracker.classify_action("create_node"), "create")
	assert_eq(SessionTracker.classify_action("add_input_action"), "create")
	assert_eq(SessionTracker.classify_action("instantiate_scene"), "create")
	assert_eq(SessionTracker.classify_action("delete_node"), "destroy")
	assert_eq(SessionTracker.classify_action("remove_component"), "destroy")
	assert_eq(SessionTracker.classify_action("set_node_transform"), "modify")


func test_category_classification_uses_godot_vocabulary() -> void:
	# Spot checks across the real catalog, one per major category. The broad
	# node catch-all is checked LAST-WINS-NOTHING style: specific domains
	# must not fall through to "nodes".
	assert_eq(SessionTracker.classify_category("create_script"), "scripts")
	assert_eq(SessionTracker.classify_category("create_material"), "materials")
	assert_eq(SessionTracker.classify_category("add_animation_track"), "animation")
	assert_eq(SessionTracker.classify_category("create_particles_2d"), "particles")
	assert_eq(SessionTracker.classify_category("create_physics_body"), "physics")
	assert_eq(SessionTracker.classify_category("create_audio_player"), "audio")
	assert_eq(SessionTracker.classify_category("set_control_text"), "ui")
	assert_eq(SessionTracker.classify_category("create_camera"), "camera")
	assert_eq(SessionTracker.classify_category("set_world_environment"), "lighting")
	assert_eq(SessionTracker.classify_category("bake_navigation_mesh"), "navigation")
	assert_eq(SessionTracker.classify_category("connect_signal"), "signals")
	assert_eq(SessionTracker.classify_category("create_scene"), "scenes")
	assert_eq(SessionTracker.classify_category("export_project"), "export")
	assert_eq(SessionTracker.classify_category("import_asset"), "assets")
	assert_eq(SessionTracker.classify_category("add_input_action"), "project")
	assert_eq(SessionTracker.classify_category("create_node"), "nodes")
	assert_eq(SessionTracker.classify_category("set_node_transform_batch"), "nodes")
	assert_eq(SessionTracker.classify_category("arrange_nodes"), "nodes")


func test_target_prefers_result_fields_over_args() -> void:
	# The result names what was actually touched; args may carry a
	# placeholder the tool resolved differently.
	SessionTracker.record_dispatch(
		"set_node_transform",
		{"node_path": "."},
		{"success": true, "message": "moved", "node_path": "Level/Player"}
	)
	var s := SessionTracker.build_summary()
	assert_eq(str((s["timeline"] as Array)[0]["target"]), "Level/Player")


# ── the tool wrapper ──────────────────────────────────────────────────────

func test_tool_returns_summary_with_success_and_message() -> void:
	SessionTracker.record_dispatch(
		"create_node", {}, {"success": true, "message": "ok", "node_path": "Player"}
	)
	var tool := GetSessionSummaryTool.new()
	assert_eq(tool.tool_name, "get_session_summary")
	assert_false(tool.requires_edit_mode, "read tool must be callable in play mode")
	var out := tool.execute({})
	assert_true(bool(out["success"]))
	assert_true(out.has("byCategory"))
	assert_true(out.has("timeline"))
	assert_true(str(out["message"]).contains("1 mutation"))


func test_tool_clamps_max_timeline_entries() -> void:
	for i in range(3):
		SessionTracker.record_dispatch(
			"create_node", {}, {"success": true, "message": "ok", "node_path": "N%d" % i}
		)
	var tool := GetSessionSummaryTool.new()
	# ws_server normalizes camelCase at the top level, so the tool reads the
	# snake_case spelling.
	var out := tool.execute({"max_timeline_entries": 1})
	assert_eq((out["timeline"] as Array).size(), 1)
	var out_neg := tool.execute({"max_timeline_entries": -5})
	assert_eq((out_neg["timeline"] as Array).size(), 0)


func test_reset_log_zeroes_everything() -> void:
	SessionTracker.record_dispatch(
		"create_node", {}, {"success": true, "message": "ok"}
	)
	SessionTracker.reset_log()
	var s := SessionTracker.build_summary()
	assert_eq(int(s["mutations"]), 0)
	assert_eq(int(s["toolCalls"]), 0)
	assert_true((s["byCategory"] as Dictionary).is_empty())
