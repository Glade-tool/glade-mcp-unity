extends "res://addons/com.gladekit.mcp-bridge/tools/i_tool.gd"

# Build the project into a distributable artifact — the terminal step of every
# game, and the one thing GladeKit could not do before this tool.
#
# Spawns a second, headless Godot on the same project with --export-release
# (or --export-debug). The editor stays alive and responsive throughout; this
# follows the i_tool async protocol (execute() returns an "async_pending"
# marker, the bridge polls poll()), the same shape import_asset uses for
# downloads and run_project uses for play sessions.
#
# Behaviors that exist because they were MEASURED against Godot 4.6.3, each of
# which otherwise produces a confusing failure:
#
#   * Godot does NOT create the output directory. Exporting to a path whose
#     folder is missing fails with "Target folder does not exist or is
#     inaccessible" after doing all the packing work. We mkdir first.
#   * Godot exports what is ON DISK. A "create the node, now export" chain
#     silently ships the pre-edit scene, so the edited scene is saved first
#     (auto_save, mirroring run_project's F5-like default).
#   * The export subprocess boots the project's editor plugins, so OUR OWN
#     bridge tries to bind :8766, finds the live editor holding it, and logs
#     `ERROR: Port 8766 is already in use`. That is benign and filtered out of
#     the reported log — otherwise an agent scanning for "ERROR" concludes a
#     good build failed.
#   * Success is decided by ARTIFACT + ERROR SCAN, not by an exit code.
#     execute_with_pipe cannot report an exit code (see PlaySessionManager),
#     and Godot has historically returned 0 on exports that produced nothing —
#     so we check that the file actually exists and is non-empty, and that the
#     log carries no fatal marker. That is strictly stronger than trusting $?.
#
# Args:
#   preset:        String (required) — preset name from export_presets.cfg.
#                        Matched EXACTLY (Godot's own behavior); call
#                        get_export_info to list them.
#   output_path:   String — override the preset's export_path. Relative
#                        resolves against the project root.
#   debug:         bool  — build a debug export (--export-debug). Default false
#                        (release). Debug builds include the debugger and are
#                        for testing, not distribution.
#   auto_save:     bool  — save the edited scene before building. Default true.
#   timeout_seconds: int — abandon the build after this long. Default 600.
#                        Large projects with texture compression are slow; a
#                        trivial project exports in ~3s.
#
# Response payload:
#   preset, platform, output_path, output_exists, size_bytes, total_bytes,
#   size_human, sibling_files[], build_seconds, errors[], log_tail,
#   auto_saved_scene, share_hint

const ToolUtils = preload("res://addons/com.gladekit.mcp-bridge/bridge/tool_utils.gd")
const ExportManager = preload("res://addons/com.gladekit.mcp-bridge/services/export_manager.gd")

const DEFAULT_TIMEOUT_SECONDS := 600
const DRAIN_CHUNK_BYTES := 65536

var _job: Dictionary = {}
var _finalized := false
var _final_result: Dictionary = {}


func _init() -> void:
	tool_name = "export_project"
	# Exporting reads the project from disk into a separate process; it does
	# not touch the live scene tree, so it is safe while the game is running.
	requires_edit_mode = false


func execute(args: Dictionary) -> Dictionary:
	_job = {}
	_finalized = false
	_final_result = {}

	var preset_name := ToolUtils.parse_string_arg(args, "preset")
	if preset_name.strip_edges().is_empty():
		return ToolUtils.error_with_solutions(
			"preset is required",
			[
				"Call get_export_info to list this project's export presets",
				"Call create_export_preset(platform=\"Web\") if there are none yet",
			]
		)

	var presets_path := ExportManager.presets_abs_path()
	var read := ExportManager.read_presets(presets_path)
	if not bool(read.get("ok", false)):
		return ToolUtils.error(String(read.get("error", "Could not read export_presets.cfg")))

	var presets: Array = read.get("presets", [])
	if presets.is_empty():
		return ToolUtils.error_with_solutions(
			"This project has no export presets (%s)" % (
				"export_presets.cfg not found" if not bool(read.get("exists", false))
				else "export_presets.cfg has no presets"
			),
			[
				"Call create_export_preset(platform=\"Web\") to add one",
				"Call get_export_info to check export templates first",
			]
		)

	var index := ExportManager.find_preset_index(presets, preset_name)
	if index < 0:
		var names: Array = []
		for p: Dictionary in presets:
			names.append(String(p.get("name", "")))
		return ToolUtils.error_with_solutions(
			"No export preset named '%s'" % preset_name,
			[
				"Godot matches preset names exactly. Available: %s" % ", ".join(names),
				"Call create_export_preset to add '%s'" % preset_name,
			]
		)

	var preset: Dictionary = {}
	for p: Dictionary in presets:
		if int(p.get("index", -1)) == index:
			preset = p
			break
	# Use the preset's stored name so the CLI arg matches byte-for-byte even
	# when the caller differed in case.
	var canonical_name := String(preset.get("name", preset_name))
	var platform := String(preset.get("platform", ""))

	# ── Preconditions (cheap to check, expensive to discover mid-build) ──
	var templates_version := ExportManager.expected_template_version()
	var templates_dir := ExportManager.templates_dir()
	var installed: Array = ExportManager.installed_platforms(templates_dir)
	if not installed.is_empty() and not installed.has(platform):
		return ToolUtils.error_with_solutions(
			"Export templates for %s are not installed (Godot %s)" % [platform, templates_version],
			[
				"In the Godot editor: Editor > Manage Export Templates > Download and Install",
				"Installed platforms: %s" % ", ".join(installed),
			]
		)
	if ExportManager.PLATFORMS_NEEDING_ETC2_ASTC.has(platform):
		if not bool(ProjectSettings.get_setting(ExportManager.ETC2_ASTC_SETTING, false)):
			return ToolUtils.error_with_solutions(
				"%s cannot be exported while ETC2/ASTC texture import is disabled" % platform,
				[
					"Enable Project Settings > Rendering > Textures > VRAM Compression > "
					+ "Import ETC2 ASTC, then retry",
					"Note: enabling it triggers a re-import of the project's textures",
				]
			)

	# ── Resolve the output path and create its directory ──
	var project_root := ProjectSettings.globalize_path("res://")
	if project_root.is_empty():
		return ToolUtils.error("Could not resolve the project root via res://")

	var requested := ToolUtils.parse_string_arg(args, "output_path")
	var rel_or_abs := requested if not requested.strip_edges().is_empty() else String(preset.get("export_path", ""))
	rel_or_abs = rel_or_abs.replace("res://", "").strip_edges()
	if rel_or_abs.is_empty():
		return ToolUtils.error_with_solutions(
			"Preset '%s' has no export_path and no output_path was given" % canonical_name,
			[
				"Pass output_path explicitly, e.g. \"build/web/index.html\"",
				"Or re-create the preset with create_export_preset so it gets a default path",
			]
		)
	var output_abs := rel_or_abs if rel_or_abs.is_absolute_path() else project_root.path_join(rel_or_abs)
	output_abs = output_abs.simplify_path()

	var out_dir := output_abs.get_base_dir()
	if not DirAccess.dir_exists_absolute(out_dir):
		var mk := DirAccess.make_dir_recursive_absolute(out_dir)
		if mk != OK:
			return ToolUtils.error_with_solutions(
				"Could not create the output directory '%s' (error %d)" % [out_dir, mk],
				[
					"Check the path is writable",
					"Pass a different output_path inside the project",
				]
			)

	# ── Save the edited scene so we export what the agent just built ──
	var auto_saved_scene := ""
	if ToolUtils.parse_bool_arg(args, "auto_save", true):
		var root: Node = EditorInterface.get_edited_scene_root()
		if root != null and not root.scene_file_path.is_empty():
			var save_err := EditorInterface.save_scene()
			if save_err != OK:
				return ToolUtils.error_with_solutions(
					"Failed to save the edited scene before exporting (error %d)" % save_err,
					[
						"Pass auto_save=false to export the last-saved state on disk",
						"Or call save_scene explicitly, then retry",
					]
				)
			auto_saved_scene = root.scene_file_path

	# ── Spawn the headless exporter ──
	var godot_exe := OS.get_executable_path()
	if godot_exe.is_empty():
		return ToolUtils.error("Could not resolve the running Godot executable path")

	var debug := ToolUtils.parse_bool_arg(args, "debug", false)
	var export_flag := "--export-debug" if debug else "--export-release"
	var cli_args: PackedStringArray = [
		"--headless",
		"--path", project_root,
		export_flag, canonical_name,
		output_abs,
	]

	var pipe := OS.execute_with_pipe(godot_exe, cli_args, false)
	if pipe.is_empty() or not pipe.has("pid"):
		return ToolUtils.error_with_solutions(
			"Could not start the export subprocess",
			[
				"Check that the Godot executable is still on disk: %s" % godot_exe,
				"Retry; if it persists, export once from Project > Export to rule out a project issue",
			]
		)

	var timeout_seconds := ToolUtils.parse_int_arg(args, "timeout_seconds", DEFAULT_TIMEOUT_SECONDS)
	if timeout_seconds <= 0:
		timeout_seconds = DEFAULT_TIMEOUT_SECONDS
	# Tell the bridge to allow this job the full build window. The default
	# async ceiling is sized for asset downloads and would abandon a large
	# export mid-build (leaving an orphan process behind).
	async_timeout_msec = timeout_seconds * 1000

	_job = {
		"pid": int(pipe["pid"]),
		"stdio": pipe.get("stdio", null),
		"stderr": pipe.get("stderr", null),
		"log": "",
		"preset": canonical_name,
		"platform": platform,
		"output_abs": output_abs,
		"output_rel": rel_or_abs,
		"debug": debug,
		"auto_saved_scene": auto_saved_scene,
		"started_msec": Time.get_ticks_msec(),
		"command": "%s %s" % [godot_exe, " ".join(cli_args)],
	}

	return ToolUtils.success(
		"Exporting '%s' (%s)…" % [canonical_name, platform],
		{"async_pending": true, "preset": canonical_name, "platform": platform}
	)


# Polled once per editor tick by the bridge. {} while the subprocess runs; the
# final result once it has exited and the artifact has been inspected.
func poll() -> Dictionary:
	if _finalized:
		return _final_result
	if _job.is_empty():
		return {}

	# Drain both pipes every tick. This is not optional: the OS pipe buffer is
	# finite and a Godot export emits hundreds of "Storing File:" lines, so an
	# undrained pipe would fill and block the child forever.
	_drain("stdio")
	_drain("stderr")

	if OS.is_process_running(int(_job["pid"])):
		return {}

	# Exited — take one last drain to catch whatever it wrote on the way out.
	_drain("stdio")
	_drain("stderr")

	_final_result = _finalize()
	_finalized = true
	return _final_result


# Kill the subprocess if the bridge abandons this dispatch, so a timed-out
# export never leaves an orphaned headless Godot holding project files open.
func cancel() -> void:
	if _job.is_empty() or _finalized:
		return
	var pid := int(_job.get("pid", 0))
	if pid > 0 and OS.is_process_running(pid):
		OS.kill(pid)
	_finalized = true


func _drain(pipe_key: String) -> void:
	var pipe = _job.get(pipe_key, null)
	if pipe == null or not (pipe is FileAccess):
		return
	var bytes: PackedByteArray = pipe.get_buffer(DRAIN_CHUNK_BYTES)
	if bytes.is_empty():
		return
	_job["log"] = String(_job["log"]) + bytes.get_string_from_utf8()


func _finalize() -> Dictionary:
	var log_text := String(_job.get("log", ""))
	var output_abs := String(_job["output_abs"])
	var preset_name := String(_job["preset"])
	var platform := String(_job["platform"])
	var build_seconds := float(Time.get_ticks_msec() - int(_job["started_msec"])) / 1000.0

	var errors := ExportManager.extract_errors(log_text)
	var artifacts := ExportManager.inspect_artifacts(output_abs)
	var exists := bool(artifacts.get("exists", false))
	var size_bytes := int(artifacts.get("size_bytes", 0))
	var total_bytes := int(artifacts.get("total_bytes", 0))

	var payload := {
		"preset": preset_name,
		"platform": platform,
		"output_path": output_abs,
		"output_exists": exists,
		"size_bytes": size_bytes,
		"total_bytes": total_bytes,
		"size_human": ExportManager.human_size(total_bytes),
		"sibling_files": artifacts.get("sibling_files", []),
		"build_seconds": snappedf(build_seconds, 0.01),
		"errors": errors,
		"log_tail": ExportManager.log_tail(log_text),
		"auto_saved_scene": _job.get("auto_saved_scene", ""),
		"debug_build": bool(_job.get("debug", false)),
	}

	# An artifact that exists but is empty is a failed build that happened to
	# create the file, so require non-empty as well.
	if not exists or size_bytes <= 0:
		var reason := "no output file was produced"
		if exists:
			reason = "the output file is empty"
		return ToolUtils.error_with_solutions(
			"Export of preset '%s' failed — %s at %s" % [preset_name, reason, output_abs],
			_solutions_for(errors, platform),
			payload
		)

	# The artifact exists, but Godot still logged something fatal — report it
	# as a failure rather than handing back a build that may be incomplete.
	if not errors.is_empty():
		var fatal := false
		for e: String in errors:
			for marker: String in ExportManager.FATAL_LOG_MARKERS:
				if e.contains(marker):
					fatal = true
					break
		if fatal:
			return ToolUtils.error_with_solutions(
				"Export of preset '%s' reported errors; the artifact may be incomplete" % preset_name,
				_solutions_for(errors, platform),
				payload
			)

	if platform == ExportManager.PLATFORM_WEB:
		payload["share_hint"] = (
			"%s Serve the folder over HTTP (e.g. `python3 -m http.server` in %s) — "
			+ "opening index.html directly with file:// will not work, browsers block "
			+ "the wasm fetch."
		) % [ExportManager.WEB_SHAREABILITY_NOTE, output_abs.get_base_dir()]

	var message := "Exported '%s' (%s) to %s — %s in %.1fs" % [
		preset_name,
		platform,
		output_abs,
		ExportManager.human_size(total_bytes),
		build_seconds,
	]
	if not errors.is_empty():
		message += " (with %d non-fatal warning(s))" % errors.size()

	return ToolUtils.success(message, payload)


# Turn whatever Godot complained about into things the agent can actually do.
func _solutions_for(errors: Array, platform: String) -> Array:
	var joined := " ".join(errors)
	var out: Array = []
	if joined.contains("No export template found") or joined.contains("Export templates"):
		out.append(
			"Install export templates: Editor > Manage Export Templates > Download and Install"
		)
	if joined.contains("Invalid export preset name"):
		out.append("Call get_export_info to list the exact preset names")
	if joined.contains("Target folder does not exist"):
		out.append("The output directory is missing — pass an output_path inside the project")
	if joined.contains("ETC2 ASTC"):
		out.append(
			"Enable Project Settings > Rendering > Textures > VRAM Compression > Import ETC2 ASTC"
		)
	if joined.contains("configuration errors"):
		out.append(
			"Open Project > Export in Godot and check preset '%s' for a red configuration warning"
			% platform
		)
	if out.is_empty():
		out.append("Read `errors` and `log_tail` in this response for the engine's own message")
		out.append("Call get_export_info to re-check templates and preset configuration")
	return out

