extends RefCounted

# Tracks scripts the bridge created during the current Godot session.
#
# Purpose: protects user code from AI clients that misread a "scaffold a new
# system" prompt as "extend an existing one" and then overwrite real user
# code with modify_script. modify_script consults this tracker; if the
# target script is NOT in the set, the modification is refused unless the
# caller passes confirm_existing_file_modification = true (the explicit
# user-intent gate).
#
# Mirrors GladeAgenticAI.Services.SessionTracker from the Unity bridge.
#
# State is process-local: when the editor restarts (or the addon hot-reloads
# enough to free this class), the tracker resets and every existing file
# becomes "pre-existing" again. That's the correct conservative default.

static var _created_paths: Dictionary = {}

# Per-CALL buffer of scripts freshly written during the tool currently
# executing. begin_call() resets it before each tool runs; take_recent_writes()
# drains it after, so the dispatcher can report exactly which scripts a single
# tool call created — even template/scaffolder tools that embed the script body
# internally (their path never appears in the caller's args). Distinct from
# _created_paths, which accumulates for the whole session to guard user code.
static var _recent_writes: Array = []


static func mark_created(script_path: String) -> void:
	if script_path.is_empty():
		return
	var normalized := _normalize(script_path)
	_created_paths[normalized] = true
	_recent_writes.append(normalized)


static func was_created_this_session(script_path: String) -> bool:
	return _created_paths.has(_normalize(script_path))


# Reset the per-call write buffer. Called by the dispatcher immediately before
# a tool's execute() so the buffer reflects only that one call.
static func begin_call() -> void:
	_recent_writes.clear()


# Return and clear the scripts written since the last begin_call(). Order is
# write-order; duplicates (a tool that wrote the same path twice) are collapsed.
static func take_recent_writes() -> Array:
	var seen: Dictionary = {}
	var out: Array = []
	for p in _recent_writes:
		if not seen.has(p):
			seen[p] = true
			out.append(p)
	_recent_writes.clear()
	return out


static func clear() -> void:
	_created_paths.clear()
	_recent_writes.clear()


static func _normalize(path: String) -> String:
	var p := path.strip_edges()
	if p.begins_with("res://") or p.begins_with("user://"):
		return p
	if p.begins_with("/"):
		p = p.substr(1)
	return "res://" + p


# ══════════════════════════════════════════════════════════════════════════
# Mutation log — the Godot twin of Unity SessionTracker's timeline half.
#
# Records every tool dispatch so the AI (via the get_session_summary tool)
# and the client's "What changed" panel can answer "what did you just do?"
# without re-reading scene state. Mirrors the Unity payload shape EXACTLY
# (camelCase keys, same field set) so the Proxy's _shape_session_summary
# formatter and the Electron SessionSummary type work unchanged.
#
# In-memory only; resets when the editor restarts or the addon reloads —
# "session" means the Godot process session, matching the script-guard
# state above and Unity's domain-reload semantics.
# ══════════════════════════════════════════════════════════════════════════

const _ReadOnlyGuard = preload("res://addons/com.gladekit.mcp-bridge/services/read_only_guard.gd")

const MAX_TIMELINE_ENTRIES := 500

static var _timeline: Array = []
static var _session_start_unix: float = 0.0
static var _total_tool_calls: int = 0
static var _mutation_success_count: int = 0
static var _mutation_error_count: int = 0

# Action classification by name prefix — same prefix sets as Unity's
# SessionTracker (destroy_ added; Godot deletes are delete_/remove_/clear_).
const _CREATE_PREFIXES := ["create_", "add_", "instantiate_", "duplicate_", "import_", "bake_"]
const _DESTROY_PREFIXES := ["destroy_", "delete_", "remove_", "clear_"]

# Target path fields, probed in args and result. Godot args are snake_case
# after ToolUtils.normalize_args, and success extras use the same convention.
const _TARGET_FIELDS := ["node_path", "script_path", "scene_path", "material_path", "path", "preset", "name"]


# Record one completed tool dispatch. Call with the FINAL result — for async
# tools that is the poll() payload, not the async_pending marker.
static func record_dispatch(tool_name: String, args: Dictionary, result: Dictionary) -> void:
	if tool_name.is_empty():
		return
	if _session_start_unix == 0.0:
		_session_start_unix = Time.get_unix_time_from_system()
	_total_tool_calls += 1

	# Read-only tools are counted as calls but never enter the mutation
	# timeline. The guard's READ_ONLY_TOOLS is the bridge's authoritative
	# read classification (pinned against the cloud + MCP sets by
	# proxy/tests/test_read_only_parity.py) — consulting it here instead of
	# keeping a second name list is what Unity's tracker could not do and
	# this one can.
	if tool_name in _ReadOnlyGuard.READ_ONLY_TOOLS:
		return

	var success := bool(result.get("success", false))
	if success:
		_mutation_success_count += 1
	else:
		_mutation_error_count += 1

	_timeline.append({
		"tool": tool_name,
		"action": classify_action(tool_name),
		"category": classify_category(tool_name),
		"target": extract_target(args, result),
		"summary": extract_summary(tool_name, result),
		"t_unix_ms": int(Time.get_unix_time_from_system() * 1000.0),
		"success": success,
	})
	if _timeline.size() > MAX_TIMELINE_ENTRIES:
		_timeline = _timeline.slice(_timeline.size() - MAX_TIMELINE_ENTRIES)


# Build the summary payload. Field names are camelCase ON PURPOSE — they are
# the wire contract shared with the Unity bridge, the Proxy formatter and the
# Electron SessionSummary type, not local style.
static func build_summary(max_timeline_return: int = 50) -> Dictionary:
	var by_category: Dictionary = {}
	for r in _timeline:
		if not bool(r["success"]):
			continue
		var cat := str(r["category"])
		if not by_category.has(cat):
			by_category[cat] = {
				"created": 0,
				"modified": 0,
				"destroyed": 0,
				"targets": [],
			}
		var bucket: Dictionary = by_category[cat]
		var key := "modified"
		if str(r["action"]) == "create":
			key = "created"
		elif str(r["action"]) == "destroy":
			key = "destroyed"
		bucket[key] = int(bucket[key]) + 1
		var target := str(r["target"])
		if not target.is_empty():
			var targets: Array = bucket["targets"]
			if not targets.has(target) and targets.size() < 25:
				targets.append(target)

	# Most-recent-first window, matching Unity.
	var timeline_out: Array = []
	var count := _timeline.size()
	var window: int = clampi(max_timeline_return, 0, MAX_TIMELINE_ENTRIES)
	for i in range(count - 1, -1, -1):
		if timeline_out.size() >= window:
			break
		var r: Dictionary = _timeline[i]
		var unix_ms: int = int(r["t_unix_ms"])
		timeline_out.append({
			"tMs": unix_ms & 0x7FFFFFFF,
			"tIso": Time.get_datetime_string_from_unix_time(int(unix_ms / 1000.0)) + "Z",
			"tool": r["tool"],
			"action": r["action"],
			"category": r["category"],
			"target": r["target"],
			"summary": r["summary"],
			"success": r["success"],
		})

	var start_unix: float = _session_start_unix if _session_start_unix > 0.0 else Time.get_unix_time_from_system()
	return {
		"sessionStartedAt": Time.get_datetime_string_from_unix_time(int(start_unix)) + "Z",
		"elapsedSeconds": int(Time.get_unix_time_from_system() - start_unix),
		"toolCalls": _total_tool_calls,
		"mutations": count,
		"successCount": _mutation_success_count,
		"errorCount": _mutation_error_count,
		"byCategory": by_category,
		"timeline": timeline_out,
		"timelineTruncated": count > window,
	}


# Reset the mutation log (test setup / eval-harness resets). Deliberately
# separate from clear(): the script-creation guard above protects user code
# and must not be droppable as a side effect of clearing a UI log.
static func reset_log() -> void:
	_timeline.clear()
	_total_tool_calls = 0
	_mutation_success_count = 0
	_mutation_error_count = 0
	_session_start_unix = 0.0


static func classify_action(tool_name: String) -> String:
	for p in _CREATE_PREFIXES:
		if tool_name.begins_with(p):
			return "create"
	for p in _DESTROY_PREFIXES:
		if tool_name.begins_with(p):
			return "destroy"
	return "modify"


# Category by name substring, mirroring Unity's approach but with Godot's
# vocabulary (nodes, not gameObjects). ORDER MATTERS: earlier rules win, so
# the specific domains (scripts, materials, animation…) are checked before
# the broad node-tool catch-all.
static func classify_category(tool_name: String) -> String:
	var t := tool_name.to_lower()
	if t.contains("script") or t.contains("compile"):
		return "scripts"
	if t.contains("material") or t.contains("shader"):
		return "materials"
	if t.contains("animation") or t.contains("blend_space") or t.contains("state_machine"):
		return "animation"
	if t.contains("particles"):
		return "particles"
	if t.contains("physics") or t.contains("collision") or t.contains("rigid"):
		return "physics"
	if t.contains("audio"):
		return "audio"
	if t.contains("control") or t.contains("ui_") or t.contains("theme") or t.contains("canvas"):
		return "ui"
	if t.contains("camera"):
		return "camera"
	if t.contains("light") or t.contains("environment"):
		return "lighting"
	if t.contains("navigation") or t.contains("navmesh"):
		return "navigation"
	if t.contains("signal"):
		return "signals"
	if t.contains("scene"):
		return "scenes"
	if t.contains("export"):
		return "export"
	if t.contains("asset") or t.contains("import"):
		return "assets"
	if t.contains("input_action") or t.contains("project") or t.contains("uid"):
		return "project"
	if (
		t.contains("node")
		or t.contains("transform")
		or t.contains("primitive")
		or t.contains("sprite")
		or t.contains("tilemap")
		or t.contains("parallax")
		or t.contains("parent")
		or t.contains("arrange")
		or t.contains("snap")
		or t.contains("platform")
	):
		return "nodes"
	return "misc"


static func extract_target(args: Dictionary, result: Dictionary) -> String:
	for key in _TARGET_FIELDS:
		var v = result.get(key)
		if v is String and not str(v).is_empty():
			return str(v)
	for key in _TARGET_FIELDS:
		var v = args.get(key)
		if v is String and not str(v).is_empty():
			return str(v)
	return ""


static func extract_summary(tool_name: String, result: Dictionary) -> String:
	var msg := str(result.get("message", ""))
	if msg.is_empty():
		var err := str(result.get("error", ""))
		if not err.is_empty():
			msg = "Error: " + err
	if msg.is_empty():
		msg = tool_name.replace("_", " ")
	if msg.length() > 160:
		msg = msg.substr(0, 160) + "..."
	return msg
