extends GutTest

# Pure tests for export_manager.gd — no editor access, so GUT actually runs
# them headlessly in CI. (The tools themselves need EditorInterface and a real
# subprocess; those paths are covered by the live driver.)
#
# The behaviors pinned here are the ones measured against a live Godot 4.6.3
# export. Each is a real failure mode: a wrong platform string makes Godot
# answer "Invalid export preset"; a missing include_filter/exclude_filter key
# aborts the export outright; an options section with no keys spams the build
# log an agent has to read; and a preset file rewritten without preserving its
# siblings would silently delete the user's other export targets.

const ExportManager = preload("res://addons/com.gladekit.mcp-bridge/services/export_manager.gd")


func _tmp(name: String) -> String:
	var dir := OS.get_cache_dir().path_join("gladekit_export_tests")
	DirAccess.make_dir_recursive_absolute(dir)
	return dir.path_join(name)


# ── Platform normalization ─────────────────────────────────────────────────
# Godot matches `platform=` EXACTLY, so every alias a user or model might type
# has to resolve or the authored preset is dead on arrival.

func test_canonical_platform_names_pass_through() -> void:
	for p: String in ExportManager.KNOWN_PLATFORMS:
		assert_eq(ExportManager.normalize_platform(p), p, "canonical '%s' must round-trip" % p)


func test_common_aliases_normalize() -> void:
	var cases := {
		"web": "Web", "html5": "Web", "browser": "Web", "itch": "Web",
		"WEB": "Web", "  web  ": "Web",
		"windows": "Windows Desktop", "win64": "Windows Desktop", "exe": "Windows Desktop",
		"linux": "Linux", "Linux/X11": "Linux",
		"mac": "macOS", "osx": "macOS", "MACOS": "macOS",
		"apk": "Android", "iphone": "iOS",
	}
	for raw: String in cases:
		assert_eq(
			ExportManager.normalize_platform(raw),
			String(cases[raw]),
			"alias '%s'" % raw
		)


func test_unknown_platform_returns_empty_not_a_guess() -> void:
	# A wrong-but-plausible guess would author a preset Godot silently never
	# matches; "" lets the caller error with the valid list instead.
	assert_eq(ExportManager.normalize_platform("nintendo64"), "")
	assert_eq(ExportManager.normalize_platform(""), "")
	assert_eq(ExportManager.normalize_platform("   "), "")


# ── Default output paths ───────────────────────────────────────────────────

func test_default_export_path_per_platform() -> void:
	assert_eq(ExportManager.default_export_path("Web", "My Game"), "build/web/index.html")
	assert_eq(
		ExportManager.default_export_path("Windows Desktop", "My Game"),
		"build/windows/My_Game.exe"
	)
	assert_eq(ExportManager.default_export_path("Linux", "My Game"), "build/linux/My_Game.x86_64")


func test_project_name_is_sanitized_into_the_filename() -> void:
	# Real project names carry spaces and punctuation; an unsanitized stem
	# produces a platform-illegal or shell-hostile path.
	var p := ExportManager.default_export_path("Windows Desktop", "My Game: The Sequel! (v2)")
	assert_true(p.ends_with(".exe"), "keeps the extension, got %s" % p)
	assert_false(p.contains(":"), "colon must not survive: %s" % p)
	assert_false(p.contains("!"), "bang must not survive: %s" % p)
	assert_false(p.contains(" "), "space must not survive: %s" % p)
	assert_false(p.contains("__"), "runs must collapse: %s" % p)


func test_empty_project_name_still_yields_a_usable_path() -> void:
	assert_eq(ExportManager.default_export_path("Linux", "!!!"), "build/linux/game.x86_64")


# ── Template version directory ─────────────────────────────────────────────

func test_expected_template_version_matches_the_running_engine() -> void:
	# The templates directory is named after the engine version; getting this
	# wrong reports "templates not installed" on a machine that has them.
	var info := Engine.get_version_info()
	var v := ExportManager.expected_template_version()
	assert_true(
		v.begins_with("%d.%d" % [int(info["major"]), int(info["minor"])]),
		"version dir '%s' should start with the engine major.minor" % v
	)
	assert_true(v.ends_with(String(info.get("status", "stable"))), "should carry the status suffix")
	# Godot drops the patch segment when it is 0 ("4.4.stable", not "4.4.0.stable").
	if int(info.get("patch", 0)) == 0:
		assert_false(v.contains(".0."), "patch 0 must be omitted, got %s" % v)


func test_installed_platforms_is_empty_for_a_missing_templates_dir() -> void:
	assert_eq(ExportManager.installed_platforms("/nonexistent/path/xyz"), [])
	assert_eq(ExportManager.installed_platforms(""), [])


# ── export_presets.cfg round-trip ──────────────────────────────────────────

func test_missing_presets_file_is_not_an_error() -> void:
	# A project that has never been exported is the normal starting state.
	var r := ExportManager.read_presets(_tmp("does_not_exist.cfg"))
	assert_true(bool(r["ok"]), "missing file must read ok")
	assert_false(bool(r["exists"]))
	assert_eq(r["presets"], [])


func test_write_then_read_round_trips() -> void:
	var path := _tmp("roundtrip.cfg")
	DirAccess.remove_absolute(path)
	var w := ExportManager.write_preset(
		path, 0, "Web", "Web", "build/web/index.html", true,
		ExportManager.BRIDGE_EXCLUDE_GLOB
	)
	assert_true(bool(w["ok"]), "write failed: %s" % w.get("error", ""))

	var r := ExportManager.read_presets(path)
	assert_true(bool(r["ok"]))
	assert_true(bool(r["exists"]))
	assert_eq(r["presets"].size(), 1)
	var p: Dictionary = r["presets"][0]
	assert_eq(p["name"], "Web")
	assert_eq(p["platform"], "Web")
	assert_eq(p["export_path"], "build/web/index.html")
	assert_true(bool(p["runnable"]))
	DirAccess.remove_absolute(path)


func test_written_preset_carries_the_keys_the_exporter_has_no_default_for() -> void:
	# Measured: omitting include_filter/exclude_filter aborts the export with
	# "Couldn't find the given section ... and key 'include_filter', and no
	# default was given". These are not optional.
	var path := _tmp("required_keys.cfg")
	DirAccess.remove_absolute(path)
	ExportManager.write_preset(path, 0, "Web", "Web", "build/web/index.html", true, "")
	var cfg := ConfigFile.new()
	assert_eq(cfg.load(path), OK)
	assert_true(cfg.has_section_key("preset.0", "include_filter"), "include_filter required")
	assert_true(cfg.has_section_key("preset.0", "exclude_filter"), "exclude_filter required")
	assert_true(cfg.has_section_key("preset.0", "export_filter"), "export_filter required")
	DirAccess.remove_absolute(path)


func test_options_section_is_never_empty() -> void:
	# An absent/empty [preset.N.options] makes every export log "Cannot get
	# keys from nonexistent section" — noise in the one log the agent must read.
	var path := _tmp("options.cfg")
	DirAccess.remove_absolute(path)
	ExportManager.write_preset(path, 0, "Web", "Web", "build/web/index.html", true, "")
	var cfg := ConfigFile.new()
	assert_eq(cfg.load(path), OK)
	assert_true(cfg.has_section("preset.0.options"), "options section must exist")
	assert_gt(cfg.get_section_keys("preset.0.options").size(), 0, "must have >=1 key")
	DirAccess.remove_absolute(path)


func test_writing_one_preset_preserves_the_others() -> void:
	# Rewriting the file must never silently drop a user's other export targets.
	var path := _tmp("multi.cfg")
	DirAccess.remove_absolute(path)
	ExportManager.write_preset(path, 0, "Web", "Web", "build/web/index.html", true, "")
	ExportManager.write_preset(path, 1, "Win", "Windows Desktop", "build/win/g.exe", true, "")
	# Update preset 0 in place.
	ExportManager.write_preset(path, 0, "Web", "Web", "build/other/index.html", false, "")

	var r := ExportManager.read_presets(path)
	assert_eq(r["presets"].size(), 2, "the sibling preset must survive")
	var by_name := {}
	for p: Dictionary in r["presets"]:
		by_name[p["name"]] = p
	assert_eq(by_name["Web"]["export_path"], "build/other/index.html", "updated in place")
	assert_false(bool(by_name["Web"]["runnable"]), "runnable was updated")
	assert_eq(by_name["Win"]["platform"], "Windows Desktop", "sibling untouched")
	DirAccess.remove_absolute(path)


func test_find_preset_index_is_case_insensitive_and_reports_misses() -> void:
	var presets := [
		{"index": 0, "name": "Web"},
		{"index": 3, "name": "Windows"},
	]
	assert_eq(ExportManager.find_preset_index(presets, "Web"), 0)
	assert_eq(ExportManager.find_preset_index(presets, "web"), 0)
	assert_eq(ExportManager.find_preset_index(presets, "WINDOWS"), 3)
	assert_eq(ExportManager.find_preset_index(presets, "Nope"), -1)


func test_next_preset_index_fills_the_lowest_gap() -> void:
	assert_eq(ExportManager.next_preset_index([]), 0)
	assert_eq(ExportManager.next_preset_index([{"index": 0}]), 1)
	# A file whose presets were hand-edited can have holes; reuse them rather
	# than growing the indices forever.
	assert_eq(ExportManager.next_preset_index([{"index": 0}, {"index": 2}]), 1)


# ── Build-log interpretation ───────────────────────────────────────────────

func test_our_own_port_collision_is_treated_as_benign() -> void:
	# The export subprocess boots the project's editor plugins, so THIS bridge
	# tries to bind :8766 while the live editor holds it and logs an ERROR.
	# Reporting that as a build failure would be wrong every single time.
	assert_true(ExportManager.is_benign_log_line(
		"ERROR: [GladeKit MCP Bridge] Port 8766 is already in use."
	))
	assert_true(ExportManager.is_benign_log_line(
		"   at: _handle_bind_failure (res://addons/com.gladekit.mcp-bridge/bridge/ws_server.gd:1383)"
	))
	assert_true(ExportManager.is_benign_log_line(
		'ERROR: Cannot get keys from nonexistent section "preset.0.options".'
	))


# Verbatim from a live `export_project` run against godot-project on
# 2026-08-07, bridge 0.7.12. The bind failure is ONE push_error whose message
# is seven lines, plus Godot's backtrace scaffolding — the first version of the
# filter matched only two of them and leaked the rest into log_tail, where an
# agent reading "Fix: set the GLADEKIT_GODOT_BRIDGE_PORT environment variable"
# would chase a config problem on a build that succeeded in 3.2s.
const _LIVE_BIND_FAILURE_LOG := """Godot Engine v4.6.3.stable.official.7d41c59c4 - https://godotengine.org
[ DONE ] first_scan_filesystem
Fix: set the GLADEKIT_GODOT_BRIDGE_PORT environment variable to a free port
and restart the Godot editor. Example: GLADEKIT_GODOT_BRIDGE_PORT=8868
Note: if you also run the GladeKit Unity bridge on this machine, that uses
at: push_error (core/variant/variant_utility.cpp:1024)
GDScript backtrace (most recent call first):
[1] start (res://addons/com.gladekit.mcp-bridge/bridge/ws_server.gd:198)
[2] _enter_tree (res://addons/com.gladekit.mcp-bridge/plugin.gd:22)
[ DONE ] savepack"""


func test_live_bind_failure_noise_is_fully_filtered_from_log_tail() -> void:
	var tail := log_tail_of(_LIVE_BIND_FAILURE_LOG)
	for leaked: String in [
		"GLADEKIT_GODOT_BRIDGE_PORT",
		"restart the Godot editor",
		"GladeKit Unity bridge",
		"GDScript backtrace",
		"push_error",
		"com.gladekit.mcp-bridge",
	]:
		assert_false(
			tail.contains(leaked),
			"our own port-collision noise leaked into log_tail: '%s'\n---\n%s" % [leaked, tail]
		)


func test_live_bind_failure_does_not_register_as_an_export_error() -> void:
	# The success verdict keys off extract_errors, so this failing would turn a
	# good build into a reported failure.
	assert_eq(ExportManager.extract_errors(_LIVE_BIND_FAILURE_LOG), [])


func test_a_user_tool_script_error_still_survives_the_filter() -> void:
	# The filter drops push_error SCAFFOLDING, never the message it decorates,
	# and never a frame in the user's own code.
	var log_text := "\n".join([
		"ERROR: Something broke in the user's build script",
		"at: push_error (core/variant/variant_utility.cpp:1024)",
		"GDScript backtrace (most recent call first):",
		"[0] _build_hook (res://scripts/my_export_hook.gd:12)",
	])
	var errors := ExportManager.extract_errors(log_text)
	assert_eq(errors.size(), 1, "the user's error message must survive: %s" % str(errors))
	assert_string_contains(errors[0], "Something broke")
	var tail := log_tail_of(log_text)
	assert_string_contains(tail, "res://scripts/my_export_hook.gd", "user frames must survive")


# Small indirection so the two tests above read cleanly.
func log_tail_of(text: String) -> String:
	return ExportManager.log_tail(text)


func test_real_export_failures_are_not_benign() -> void:
	assert_false(ExportManager.is_benign_log_line("ERROR: Invalid export preset name: Nope."))
	assert_false(ExportManager.is_benign_log_line(
		'ERROR: Project export for preset "Web" failed.'
	))


func test_extract_errors_keeps_failures_and_drops_our_noise() -> void:
	var log_text := "\n".join([
		"[   0% ] savepack | Started Packing (102 steps)",
		"ERROR: [GladeKit MCP Bridge] Port 8766 is already in use.",
		"[  50% ] savepack | Storing File: res://main.tscn",
		'ERROR: Project export for preset "Web" failed.',
		"",
	])
	var errors := ExportManager.extract_errors(log_text)
	assert_eq(errors.size(), 1, "got: %s" % str(errors))
	assert_string_contains(errors[0], "Project export for preset")


func test_extract_errors_dedupes_repeated_lines() -> void:
	var log_text := "ERROR: Invalid export preset name: X.\nERROR: Invalid export preset name: X."
	assert_eq(ExportManager.extract_errors(log_text).size(), 1)


func test_log_tail_drops_per_file_progress_spam() -> void:
	# A real export emits hundreds of "Storing File:" lines; none are actionable.
	var lines: Array = []
	for i in range(200):
		lines.append("[  %d%% ] savepack | Storing File: res://file_%d.tres" % [i % 100, i])
	lines.append("ERROR: something real")
	var tail := ExportManager.log_tail("\n".join(PackedStringArray(lines)))
	assert_string_contains(tail, "something real")
	assert_false(tail.contains("Storing File"), "progress spam must be dropped")


func test_log_tail_is_bounded() -> void:
	var lines: Array = []
	for i in range(500):
		lines.append("ERROR: distinct failure %d" % i)
	var tail := ExportManager.log_tail("\n".join(PackedStringArray(lines)), 40)
	assert_lt(tail.split("\n").size(), 41, "tail must respect max_lines")


func test_ansi_colour_codes_are_stripped() -> void:
	var esc := String.chr(27)
	var coloured := "%s[91mERROR: Project export for preset \"Web\" failed.%s[0m" % [esc, esc]
	var errors := ExportManager.extract_errors(coloured)
	assert_eq(errors.size(), 1)
	assert_false(errors[0].contains(esc), "escape codes must not reach the tool result")


# ── Size formatting ────────────────────────────────────────────────────────

func test_human_size() -> void:
	assert_eq(ExportManager.human_size(512), "512 B")
	assert_eq(ExportManager.human_size(2048), "2.0 KB")
	assert_string_contains(ExportManager.human_size(38_000_000), "MB")


func test_inspect_artifacts_reports_absent_output() -> void:
	var r := ExportManager.inspect_artifacts(_tmp("never_built/index.html"))
	assert_false(bool(r["exists"]))
	assert_eq(int(r["size_bytes"]), 0)


func test_inspect_artifacts_sums_the_sibling_files() -> void:
	# A Web export writes index.wasm/.pck/.js beside index.html, so reporting
	# only the main file's size would understate the build by ~1000x.
	var dir := _tmp("artifacts")
	DirAccess.make_dir_recursive_absolute(dir)
	for entry: Array in [["index.html", "html"], ["index.wasm", "wasmwasm"], ["index.pck", "pck"]]:
		var f := FileAccess.open(dir.path_join(String(entry[0])), FileAccess.WRITE)
		f.store_string(String(entry[1]))
		f.close()

	var r := ExportManager.inspect_artifacts(dir.path_join("index.html"))
	assert_true(bool(r["exists"]))
	assert_eq(int(r["size_bytes"]), 4, "main file only")
	assert_eq(int(r["total_bytes"]), 15, "main + siblings")
	assert_eq(r["sibling_files"], ["index.pck", "index.wasm"])

	for name: String in ["index.html", "index.wasm", "index.pck"]:
		DirAccess.remove_absolute(dir.path_join(name))
