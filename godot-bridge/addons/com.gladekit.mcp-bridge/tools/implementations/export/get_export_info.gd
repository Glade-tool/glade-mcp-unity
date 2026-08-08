extends "res://addons/com.gladekit.mcp-bridge/tools/i_tool.gd"

# Read-only answer to "can this project be exported, and to what?"
#
# Call this BEFORE create_export_preset / export_project. Exporting has three
# preconditions that all fail with cryptic engine errors, and all three are
# cheap to check up front:
#   1. Export templates for the running engine version must be installed.
#      Without them every export dies on "No export template found"; the fix
#      is a GUI action (Editor > Manage Export Templates) that no tool can do
#      for the user, so the agent needs to say so rather than retry.
#   2. A preset with a matching name must exist in export_presets.cfg.
#      Godot matches the name EXACTLY and answers a miss with "Invalid export
#      preset name".
#   3. Some platforms have extra requirements — macOS/Android/iOS refuse to
#      export unless the project imports ETC2/ASTC textures.
#
# Args: none.
#
# Response payload:
#   engine_version           String — e.g. "4.6.3.stable"
#   templates_installed      bool   — templates for THIS engine version present
#   templates_version        String — the version directory we looked for
#   templates_dir            String — absolute path we looked in
#   installed_platforms      Array  — platforms whose templates are present
#   presets                  Array  — {name, platform, runnable, export_path,
#                                      templates_ready, blockers[]}
#   presets_file_exists      bool
#   exportable_presets       Array  — preset names that would build right now
#   supported_platforms      Array  — canonical platform strings for
#                                     create_export_preset
#   etc2_astc_enabled        bool   — gates macOS/Android/iOS
#   web_note                 String — how a Web build can be shared

const ToolUtils = preload("res://addons/com.gladekit.mcp-bridge/bridge/tool_utils.gd")
const ExportManager = preload("res://addons/com.gladekit.mcp-bridge/services/export_manager.gd")


func _init() -> void:
	tool_name = "get_export_info"
	requires_edit_mode = false  # pure read; safe while the game is running


func execute(_args: Dictionary) -> Dictionary:
	var templates_version := ExportManager.expected_template_version()
	var templates_dir := ExportManager.templates_dir()
	var installed: Array = ExportManager.installed_platforms(templates_dir)

	var presets_path := ExportManager.presets_abs_path()
	var read := ExportManager.read_presets(presets_path)
	if not bool(read.get("ok", false)):
		return ToolUtils.error_with_solutions(
			String(read.get("error", "Could not read export_presets.cfg")),
			[
				"Open the project in Godot and check Project > Export for a corrupt preset",
				"Or delete export_presets.cfg and re-create presets with create_export_preset",
			]
		)

	var etc2 := bool(ProjectSettings.get_setting(ExportManager.ETC2_ASTC_SETTING, false))

	var presets: Array = []
	var exportable: Array = []
	for p: Dictionary in read.get("presets", []):
		var platform := String(p.get("platform", ""))
		var blockers: Array = []
		var templates_ready := installed.has(platform)
		if not templates_ready:
			blockers.append(
				"Export templates for %s are not installed for Godot %s"
				% [platform, templates_version]
			)
		if ExportManager.PLATFORMS_NEEDING_ETC2_ASTC.has(platform) and not etc2:
			blockers.append(
				(
					"%s requires ETC2/ASTC texture import — enable Project Settings > "
					+ "Rendering > Textures > VRAM Compression > Import ETC2 ASTC"
				) % platform
			)
		var entry := {
			"name": p.get("name", ""),
			"platform": platform,
			"runnable": p.get("runnable", false),
			"export_path": p.get("export_path", ""),
			"excludes_gladekit_bridge": String(p.get("exclude_filter", "")).contains(
				ExportManager.BRIDGE_EXCLUDE_GLOB
			),
			"templates_ready": templates_ready,
			"blockers": blockers,
		}
		presets.append(entry)
		if blockers.is_empty():
			exportable.append(String(p.get("name", "")))

	var payload := {
		"engine_version": _engine_version_string(),
		"templates_installed": not installed.is_empty(),
		"templates_version": templates_version,
		"templates_dir": templates_dir,
		"installed_platforms": installed,
		"presets": presets,
		"presets_file_exists": bool(read.get("exists", false)),
		"exportable_presets": exportable,
		"supported_platforms": ExportManager.KNOWN_PLATFORMS,
		"etc2_astc_enabled": etc2,
		"web_note": ExportManager.WEB_SHAREABILITY_NOTE,
	}

	if installed.is_empty():
		# The one failure the agent genuinely cannot fix on the user's behalf.
		# Say so plainly instead of letting it retry an export six times.
		return ToolUtils.success(
			(
				"No export templates found for Godot %s. Exporting is blocked until "
				+ "they are installed — in the Godot editor use Editor > Manage Export "
				+ "Templates > Download and Install. Looked in: %s"
			) % [templates_version, templates_dir],
			payload
		)

	if presets.is_empty():
		return ToolUtils.success(
			(
				"Export templates are installed (%s), but this project has no export "
				+ "presets yet. Call create_export_preset to add one — platform=\"Web\" "
				+ "produces a build you can host anywhere."
			) % ", ".join(installed),
			payload
		)

	return ToolUtils.success(
		"%d export preset(s); %d ready to build. Templates installed for: %s"
		% [presets.size(), exportable.size(), ", ".join(installed)],
		payload
	)



func _engine_version_string() -> String:
	var info := Engine.get_version_info()
	return String(info.get("string", ExportManager.expected_template_version()))
