extends "res://addons/com.gladekit.mcp-bridge/tools/i_tool.gd"

# Author (or update) an export preset in export_presets.cfg so export_project
# has something to build. Idempotent by name: calling it twice with the same
# `name` rewrites that preset in place rather than stacking duplicates, so a
# retrying agent can't fill the file with "Web", "Web 2", "Web 3".
#
# Godot normally writes this file from the Project > Export dialog. Everything
# the dialog fills in that the exporter has no default for is written here —
# see write_preset() in export_manager.gd for the two measured requirements
# (include_filter/exclude_filter must exist; the options section needs a key).
#
# Two defaults are opinionated and reported back rather than hidden:
#   * exclude_gladekit_bridge=true — res://addons/com.gladekit.mcp-bridge/ is
#     editor-only tooling, but Godot packs it into the game like any other
#     resource. Measured on the dogfood project: shipped .pck 658,400 -> 69,120
#     bytes once excluded. Shipping our own dev bridge in a user's game is a
#     defect, so it is excluded unless explicitly asked for.
#   * export_path defaults to build/<platform>/<project>.<ext> — a conventional
#     spot inside the project, which keeps the artifact discoverable and the
#     path relative (Godot resolves a relative export_path against the project
#     root).
#
# Args:
#   name:        String — preset name. export_project matches it EXACTLY.
#                         Default: the platform name.
#   platform:    String (required) — "Web" | "Windows Desktop" | "Linux" |
#                         "macOS" | "Android" | "iOS". Common aliases are
#                         accepted and normalized ("web"/"html5" -> "Web",
#                         "windows"/"win64" -> "Windows Desktop").
#   export_path: String — output path. Relative resolves against the project
#                         root. Default: build/<platform>/<project>.<ext>.
#   runnable:    bool   — mark the preset runnable in the editor. Default true.
#   exclude_gladekit_bridge: bool — keep the GladeKit addon out of the build.
#                         Default true. Only set false if you have a reason.
#   exclude_filter: String — extra comma-separated globs to exclude, appended
#                         to the bridge exclusion.
#
# Response payload:
#   preset, platform, export_path, created (bool — false = updated in place),
#   presets_file, exclude_filter, blockers[], next_step

const ToolUtils = preload("res://addons/com.gladekit.mcp-bridge/bridge/tool_utils.gd")
const ExportManager = preload("res://addons/com.gladekit.mcp-bridge/services/export_manager.gd")


func _init() -> void:
	tool_name = "create_export_preset"
	requires_edit_mode = true


func execute(args: Dictionary) -> Dictionary:
	var raw_platform := ToolUtils.parse_string_arg(args, "platform")
	if raw_platform.is_empty():
		return ToolUtils.error_with_solutions(
			"platform is required",
			[
				'Pass platform="Web" for a build you can host anywhere',
				'Other options: "Windows Desktop", "Linux", "macOS", "Android", "iOS"',
			]
		)

	var platform := ExportManager.normalize_platform(raw_platform)
	if platform.is_empty():
		return ToolUtils.error_with_solutions(
			"Unknown export platform '%s'" % raw_platform,
			[
				"Use one of: %s" % ", ".join(ExportManager.KNOWN_PLATFORMS),
				"Call get_export_info to see which platforms have templates installed",
			]
		)

	var preset_name := ToolUtils.parse_string_arg(args, "name", platform)
	if preset_name.strip_edges().is_empty():
		preset_name = platform

	var project_name := String(ProjectSettings.get_setting("application/config/name", "game"))
	var export_path := ToolUtils.parse_string_arg(args, "export_path")
	if export_path.strip_edges().is_empty():
		export_path = ExportManager.default_export_path(platform, project_name)
	# res:// is meaningful to Godot's resource loader but NOT to the exporter's
	# export_path, which wants a filesystem path (relative = project root).
	export_path = export_path.replace("res://", "")

	var exclude_bridge := ToolUtils.parse_bool_arg(args, "exclude_gladekit_bridge", true)
	var extra_excludes := ToolUtils.parse_string_arg(args, "exclude_filter")
	var exclude_parts: Array = []
	if exclude_bridge:
		exclude_parts.append(ExportManager.BRIDGE_EXCLUDE_GLOB)
	if not extra_excludes.strip_edges().is_empty():
		exclude_parts.append(extra_excludes.strip_edges())
	var exclude_filter := ", ".join(exclude_parts)

	var runnable := ToolUtils.parse_bool_arg(args, "runnable", true)

	var presets_path := ExportManager.presets_abs_path()
	var read := ExportManager.read_presets(presets_path)
	if not bool(read.get("ok", false)):
		return ToolUtils.error_with_solutions(
			String(read.get("error", "Could not read export_presets.cfg")),
			[
				"Open Project > Export in Godot to inspect the presets file",
				"Or delete export_presets.cfg and retry to start clean",
			]
		)

	var presets: Array = read.get("presets", [])
	var existing_index := ExportManager.find_preset_index(presets, preset_name)
	var created := existing_index < 0
	var index := existing_index if not created else ExportManager.next_preset_index(presets)

	var write := ExportManager.write_preset(
		presets_path,
		index,
		preset_name,
		platform,
		export_path,
		runnable,
		exclude_filter
	)
	if not bool(write.get("ok", false)):
		return ToolUtils.error_with_solutions(
			String(write.get("error", "Could not write export_presets.cfg")),
			[
				"Check that the project directory is writable",
				"Check that export_presets.cfg is not read-only or locked by version control",
			]
		)

	# Surface the same preconditions get_export_info reports, so a
	# create -> export chain fails here (cheap) rather than after a build.
	var blockers := _blockers_for(platform)

	var verb := "Created" if created else "Updated"
	var message := "%s export preset '%s' (%s) -> %s" % [verb, preset_name, platform, export_path]
	if not blockers.is_empty():
		message += ". Not buildable yet: %s" % " ".join(blockers)

	var next_step := "Call export_project(preset=\"%s\") to build it." % preset_name
	if not blockers.is_empty():
		next_step = "Resolve the blockers above, then call export_project(preset=\"%s\")." % preset_name

	return ToolUtils.success(message, {
		"preset": preset_name,
		"platform": platform,
		"export_path": export_path,
		"created": created,
		"presets_file": presets_path,
		"exclude_filter": exclude_filter,
		"excludes_gladekit_bridge": exclude_bridge,
		"blockers": blockers,
		"next_step": next_step,
		"web_note": ExportManager.WEB_SHAREABILITY_NOTE if platform == ExportManager.PLATFORM_WEB else "",
	})


# Preconditions that would make export_project fail for this platform.
func _blockers_for(platform: String) -> Array:
	var blockers: Array = []
	var version := ExportManager.expected_template_version()
	var templates_dir := ExportManager.templates_dir()
	var installed: Array = ExportManager.installed_platforms(templates_dir)
	if installed.is_empty():
		blockers.append(
			(
				"No export templates installed for Godot %s (install via Editor > "
				+ "Manage Export Templates)."
			) % version
		)
	elif not installed.has(platform):
		blockers.append(
			"Export templates for %s are missing (installed: %s)."
			% [platform, ", ".join(installed)]
		)
	if ExportManager.PLATFORMS_NEEDING_ETC2_ASTC.has(platform):
		if not bool(ProjectSettings.get_setting(ExportManager.ETC2_ASTC_SETTING, false)):
			blockers.append(
				(
					"%s needs ETC2/ASTC texture import — enable Project Settings > Rendering "
					+ "> Textures > VRAM Compression > Import ETC2 ASTC."
				) % platform
			)
	return blockers

