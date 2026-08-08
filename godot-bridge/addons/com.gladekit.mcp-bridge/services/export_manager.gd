@tool
extends RefCounted

# Shared helpers behind the three export tools — get_export_info,
# create_export_preset, and export_project.
#
# Everything that does not need EditorInterface is a STATIC pure function so
# the GUT suite exercises it headlessly in CI. (Same split that made
# set_node_transform_batch's contract testable: GUT's runner can't drive the
# editor, so anything worth pinning has to be reachable without it.)
#
# Every constant below was measured against a real Godot 4.6.3 headless
# export, not read off the docs. The surprising ones are called out inline —
# they are the difference between "export works" and "the agent gets a
# cryptic failure and flails".

# Godot resolves a relative export_path against the project root, so the
# presets file always lives beside project.godot.
const PRESETS_RES_PATH := "res://export_presets.cfg"

# Canonical `platform=` strings Godot matches in export_presets.cfg.
# Verified by round-tripping each through headless --export-release on 4.6.3:
# Web, Linux and "Windows Desktop" produced real binaries (cross-compiled from
# macOS); macOS/Android/iOS are recognised but carry extra setup — see
# PLATFORM_PRECONDITIONS.
const PLATFORM_WEB := "Web"
const PLATFORM_WINDOWS := "Windows Desktop"
const PLATFORM_LINUX := "Linux"
const PLATFORM_MACOS := "macOS"
const PLATFORM_ANDROID := "Android"
const PLATFORM_IOS := "iOS"

const KNOWN_PLATFORMS := [
	PLATFORM_WEB,
	PLATFORM_WINDOWS,
	PLATFORM_LINUX,
	PLATFORM_MACOS,
	PLATFORM_ANDROID,
	PLATFORM_IOS,
]

# What a user (or a model paraphrasing one) actually types. Godot matches the
# platform string EXACTLY and answers a miss with "Invalid export preset", so
# without this table "export to windows" silently authors a dead preset.
const PLATFORM_ALIASES := {
	"web": PLATFORM_WEB,
	"html": PLATFORM_WEB,
	"html5": PLATFORM_WEB,
	"wasm": PLATFORM_WEB,
	"webgl": PLATFORM_WEB,
	"browser": PLATFORM_WEB,
	"itch": PLATFORM_WEB,
	"windows": PLATFORM_WINDOWS,
	"windows desktop": PLATFORM_WINDOWS,
	"win": PLATFORM_WINDOWS,
	"win32": PLATFORM_WINDOWS,
	"win64": PLATFORM_WINDOWS,
	"pc": PLATFORM_WINDOWS,
	"exe": PLATFORM_WINDOWS,
	"linux": PLATFORM_LINUX,
	# Godot 3 spelled it "Linux/X11". 4.6 still accepts the legacy string, but
	# we normalize to the modern one so authored presets match the editor UI.
	"linux/x11": PLATFORM_LINUX,
	"x11": PLATFORM_LINUX,
	"macos": PLATFORM_MACOS,
	"mac": PLATFORM_MACOS,
	"mac os": PLATFORM_MACOS,
	"osx": PLATFORM_MACOS,
	"darwin": PLATFORM_MACOS,
	"android": PLATFORM_ANDROID,
	"apk": PLATFORM_ANDROID,
	"ios": PLATFORM_IOS,
	"iphone": PLATFORM_IOS,
	"ipad": PLATFORM_IOS,
}

# Default output filename per platform. Godot infers the exporter's file layout
# from the extension, so these are not cosmetic: a Web export written to a path
# without `.html` still emits index.* siblings but names them after the stem.
const PLATFORM_DEFAULT_FILE := {
	PLATFORM_WEB: "index.html",
	PLATFORM_WINDOWS: "%s.exe",
	PLATFORM_LINUX: "%s.x86_64",
	PLATFORM_MACOS: "%s.zip",
	PLATFORM_ANDROID: "%s.apk",
	PLATFORM_IOS: "%s.ipa",
}

const PLATFORM_DEFAULT_SUBDIR := {
	PLATFORM_WEB: "web",
	PLATFORM_WINDOWS: "windows",
	PLATFORM_LINUX: "linux",
	PLATFORM_MACOS: "macos",
	PLATFORM_ANDROID: "android",
	PLATFORM_IOS: "ios",
}

# One template file per platform that proves the template set is installed.
# Web lists two because 4.3+ ships threads and no-threads variants and either
# satisfies an export (the no-threads one is what a default Web preset picks —
# see WEB_SHAREABILITY_NOTE).
const PLATFORM_TEMPLATE_FILES := {
	PLATFORM_WEB: ["web_nothreads_release.zip", "web_release.zip"],
	PLATFORM_WINDOWS: ["windows_release_x86_64.exe"],
	PLATFORM_LINUX: ["linux_release.x86_64"],
	PLATFORM_MACOS: ["macos.zip"],
	PLATFORM_ANDROID: ["android_release.apk"],
	PLATFORM_IOS: ["ios.zip"],
}

# Measured on 4.6.3: a default Web preset links the `web_nothreads_release`
# template (exported wasm byte-matched that zip's godot.wasm, NOT the threaded
# one). That is the shareable variant — it boots from any static host. The
# threaded build needs COOP/COEP cross-origin-isolation headers, which GitHub
# Pages, S3 and most static hosts do not send, so it would fail to start.
# Reported to the agent so "put this online" advice stays true.
const WEB_SHAREABILITY_NOTE := (
	"Web builds use Godot's no-threads template by default, so this runs on any "
	+ "plain static host (GitHub Pages, S3, itch.io) with no special headers."
)

# The GladeKit bridge is EDITOR-ONLY tooling, but Godot packs res://addons/**
# into the game like any other resource. Measured on the dogfood project: the
# shipped .pck went 658,400 -> 69,120 bytes (9.5x) once this was excluded.
# Shipping our own dev bridge inside a user's game is a defect, so every preset
# we author excludes it by default.
const BRIDGE_EXCLUDE_GLOB := "addons/com.gladekit.mcp-bridge/*"

# macOS (and the arm64 mobile targets) refuse to export unless the project
# imports ETC2/ASTC textures. The engine's own words:
#   "Cannot export for universal or arm64 if ETC2 ASTC texture format is
#    disabled. Enable it in the Project Settings"
# Cheap to detect up front; otherwise the agent burns a full build to find out.
const ETC2_ASTC_SETTING := "rendering/textures/vram_compression/import_etc2_astc"
const PLATFORMS_NEEDING_ETC2_ASTC := [PLATFORM_MACOS, PLATFORM_ANDROID, PLATFORM_IOS]

# Log lines the export subprocess emits that are NOT export failures. The
# subprocess boots the project's editor plugins, so OUR OWN bridge tries to
# bind :8766, finds the live editor already holding it, and logs an ERROR.
# An agent that greps the build log for "ERROR" would conclude a perfectly
# good build failed, so these are filtered out of the reported log.
# The bind failure is ONE push_error carrying a SEVEN-LINE message plus Godot's
# backtrace scaffolding — not the single line it looks like at the call site.
# The first version of this list matched only two of those lines, and a live
# export against the dogfood project leaked the rest ("Fix: set the
# GLADEKIT_GODOT_BRIDGE_PORT environment variable...", a GDScript backtrace into
# plugin.gd) straight into log_tail. Keep this matched to the real emitted text
# in ws_server._handle_bind_failure, not to what that function reads like.
const BENIGN_LOG_MARKERS := [
	"[GladeKit MCP Bridge]",
	"is already in use",
	"_handle_bind_failure",
	"Godot uses 8766 by default",
	# The multi-line remediation advice attached to the bind failure.
	"GLADEKIT_GODOT_BRIDGE_PORT",
	"if you also run the GladeKit Unity bridge",
	# Backtrace frames pointing into our own addon. Safe to drop wholesale in a
	# BUILD log: the addon is excluded from the export and never runs in the
	# shipped game, so anything it says here is tooling noise rather than the
	# user's project failing. A frame in the USER's own code is untouched.
	"addons/com.gladekit.mcp-bridge",
	# push_error's own scaffolding — the scaffolding ONLY. The message line it
	# decorates is still kept, so a user @tool script's push_error keeps its
	# signal and loses just the boilerplate.
	"GDScript backtrace (most recent call first)",
	"at: push_error (core/variant/variant_utility.cpp",
	"at: push_warning (core/variant/variant_utility.cpp",
	# ConfigFile chatter when a preset has no [preset.N.options] section.
	# We always write one key to avoid emitting this ourselves, but a
	# hand-authored preset can still trigger it.
	"Cannot get keys from nonexistent section",
]

# Lines that mean the export genuinely failed.
const FATAL_LOG_MARKERS := [
	"Project export for preset",
	"Cannot export project with preset",
	"Invalid export preset name",
	"No export template found",
	"Target folder does not exist",
]


# ── Platform naming ────────────────────────────────────────────────────────

# Resolve whatever the caller typed to a canonical Godot platform string.
# Returns "" when there is no confident match, so callers can emit a listing
# error instead of authoring a preset Godot will never match.
static func normalize_platform(raw: String) -> String:
	var trimmed := raw.strip_edges()
	if trimmed.is_empty():
		return ""
	for known: String in KNOWN_PLATFORMS:
		if known.to_lower() == trimmed.to_lower():
			return known
	return String(PLATFORM_ALIASES.get(trimmed.to_lower(), ""))


# Default `export_path` for a platform, relative to the project root (which is
# how Godot resolves a non-absolute export_path).
static func default_export_path(platform: String, project_name: String) -> String:
	var subdir := String(PLATFORM_DEFAULT_SUBDIR.get(platform, "export"))
	var pattern := String(PLATFORM_DEFAULT_FILE.get(platform, "%s"))
	var stem := _sanitize_stem(project_name)
	var filename := pattern if not pattern.contains("%s") else pattern % stem
	return "build/%s/%s" % [subdir, filename]


# Strip a project name down to something safe for a filename. Project names
# routinely contain spaces and punctuation ("My Game (Demo)"); an unsanitized
# stem produces shell-hostile and platform-illegal output paths.
const _STEM_SAFE_CHARS := "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

static func _sanitize_stem(project_name: String) -> String:
	var out := ""
	for ch in project_name.strip_edges():
		if _STEM_SAFE_CHARS.contains(ch):
			out += ch
		elif ch == " " or ch == "-" or ch == "_" or ch == ".":
			out += "_"
	while out.contains("__"):
		out = out.replace("__", "_")
	out = out.trim_prefix("_").trim_suffix("_")
	return out if not out.is_empty() else "game"


# ── Export templates ───────────────────────────────────────────────────────

# The templates directory name Godot expects for the running engine, e.g.
# "4.6.3.stable". Godot omits the patch segment when it is 0 ("4.4.stable").
static func expected_template_version() -> String:
	return _expected_template_version()


# Absolute path to the export-template directory for the running engine, e.g.
# "~/Library/Application Support/Godot/export_templates/4.6.3.stable".
# Returns "" outside the editor.
#
# Two API facts, both established by a live run rather than assumed:
#   * EditorPaths has NO get_export_templates_dir(). Calling it raises
#     "Nonexistent function 'get_export_templates_dir' in base 'EditorPaths'".
#     Templates live under <data_dir>/export_templates/<version>/, so derive
#     the path from get_data_dir() — which correctly honors self-contained
#     mode and the per-OS location.
#   * EditorInterface is accessed directly, matching every other tool in this
#     bridge; the Engine.get_singleton("EditorInterface") route does not
#     resolve here.
const TEMPLATES_SUBDIR := "export_templates"

static func templates_dir() -> String:
	var root := templates_root()
	if root.is_empty():
		return ""
	return root.path_join(_expected_template_version())


# The version-independent template root — <editor data dir>/export_templates.
static func templates_root() -> String:
	if not Engine.is_editor_hint():
		return ""
	var paths: EditorPaths = EditorInterface.get_editor_paths()
	if paths == null:
		return ""
	var data_dir: String = paths.get_data_dir()
	if data_dir.is_empty():
		return ""
	return data_dir.path_join(TEMPLATES_SUBDIR)


static func _expected_template_version() -> String:
	var info := Engine.get_version_info()
	var major := int(info.get("major", 0))
	var minor := int(info.get("minor", 0))
	var patch := int(info.get("patch", 0))
	var status := String(info.get("status", "stable"))
	if patch == 0:
		return "%d.%d.%s" % [major, minor, status]
	return "%d.%d.%d.%s" % [major, minor, patch, status]


# Which platforms have their templates installed in `templates_root`.
# Pure over the filesystem so a test can point it at a fixture directory.
static func installed_platforms(templates_root: String) -> Array:
	var out: Array = []
	if templates_root.is_empty() or not DirAccess.dir_exists_absolute(templates_root):
		return out
	for platform: String in KNOWN_PLATFORMS:
		var candidates: Array = PLATFORM_TEMPLATE_FILES.get(platform, [])
		for filename: String in candidates:
			if FileAccess.file_exists(templates_root.path_join(filename)):
				out.append(platform)
				break
	return out


# ── export_presets.cfg ─────────────────────────────────────────────────────

# Absolute path to the project's export_presets.cfg (it need not exist yet).
static func presets_abs_path() -> String:
	return ProjectSettings.globalize_path(PRESETS_RES_PATH)


# Read every preset. Returns
#   {"ok": true, "exists": bool, "presets": Array[Dictionary]}
# or {"ok": false, "error": String}. A missing file is NOT an error — it is
# the normal state of a project that has never been exported.
static func read_presets(abs_path: String) -> Dictionary:
	if not FileAccess.file_exists(abs_path):
		return {"ok": true, "exists": false, "presets": []}
	var cfg := ConfigFile.new()
	var err := cfg.load(abs_path)
	if err != OK:
		return {
			"ok": false,
			"error": "Could not parse export_presets.cfg (error %d)" % err,
		}
	var presets: Array = []
	for section: String in cfg.get_sections():
		# Preset bodies are "preset.N"; their options live in "preset.N.options".
		if not section.begins_with("preset.") or section.ends_with(".options"):
			continue
		var index := int(section.trim_prefix("preset."))
		presets.append({
			"index": index,
			"name": String(cfg.get_value(section, "name", "")),
			"platform": String(cfg.get_value(section, "platform", "")),
			"runnable": bool(cfg.get_value(section, "runnable", false)),
			"export_path": String(cfg.get_value(section, "export_path", "")),
			"exclude_filter": String(cfg.get_value(section, "exclude_filter", "")),
		})
	presets.sort_custom(func(a, b): return int(a["index"]) < int(b["index"]))
	return {"ok": true, "exists": true, "presets": presets}


# Find a preset by name (case-insensitive). Returns its index, or -1.
static func find_preset_index(presets: Array, preset_name: String) -> int:
	for p: Dictionary in presets:
		if String(p.get("name", "")).to_lower() == preset_name.to_lower():
			return int(p.get("index", -1))
	return -1


# Lowest unused preset index.
static func next_preset_index(presets: Array) -> int:
	var used: Dictionary = {}
	for p: Dictionary in presets:
		used[int(p.get("index", -1))] = true
	var i := 0
	while used.has(i):
		i += 1
	return i


# Write (or overwrite) one preset in export_presets.cfg, preserving any other
# presets already in the file.
#
# Two measured requirements are encoded here and must not be "simplified":
#   * include_filter / exclude_filter have NO defaults in Godot's exporter.
#     Omitting them produces "Couldn't find the given section ... and key
#     'include_filter', and no default was given" and the export dies.
#   * The [preset.N.options] section must contain at least one key. An empty
#     or absent section makes ConfigFile log "Cannot get keys from nonexistent
#     section" on every export — harmless, but it trains the agent to ignore
#     the log it needs to read.
static func write_preset(
	abs_path: String,
	index: int,
	preset_name: String,
	platform: String,
	export_path: String,
	runnable: bool,
	exclude_filter: String,
	extra_options: Dictionary = {}
) -> Dictionary:
	var cfg := ConfigFile.new()
	if FileAccess.file_exists(abs_path):
		var load_err := cfg.load(abs_path)
		if load_err != OK:
			return {
				"ok": false,
				"error": "Could not parse existing export_presets.cfg (error %d)" % load_err,
			}
	var section := "preset.%d" % index
	cfg.set_value(section, "name", preset_name)
	cfg.set_value(section, "platform", platform)
	cfg.set_value(section, "runnable", runnable)
	cfg.set_value(section, "advanced_options", false)
	cfg.set_value(section, "dedicated_server", false)
	cfg.set_value(section, "custom_features", "")
	cfg.set_value(section, "export_filter", "all_resources")
	cfg.set_value(section, "include_filter", "")
	cfg.set_value(section, "exclude_filter", exclude_filter)
	cfg.set_value(section, "export_path", export_path)
	cfg.set_value(section, "encryption_include_filters", "")
	cfg.set_value(section, "encryption_exclude_filters", "")
	cfg.set_value(section, "encrypt_pck", false)
	cfg.set_value(section, "encrypt_directory", false)

	var options_section := section + ".options"
	# Always at least one key (see the doc comment above).
	cfg.set_value(options_section, "custom_template/debug", "")
	cfg.set_value(options_section, "custom_template/release", "")
	for key: String in extra_options:
		cfg.set_value(options_section, key, extra_options[key])

	var save_err := cfg.save(abs_path)
	if save_err != OK:
		return {"ok": false, "error": "Could not write export_presets.cfg (error %d)" % save_err}
	return {"ok": true}


# ── Build-log interpretation ───────────────────────────────────────────────

# True for log lines that look alarming but do not indicate export failure.
static func is_benign_log_line(line: String) -> bool:
	for marker: String in BENIGN_LOG_MARKERS:
		if line.contains(marker):
			return true
	return false


# Pull the lines that actually explain a failure out of a noisy build log.
# Godot prints progress as "[  n% ] ..." which we drop, and emits its real
# complaints as ERROR:/fatal-marker lines.
static func extract_errors(log_text: String) -> Array:
	var out: Array = []
	for raw_line: String in log_text.split("\n"):
		var line := _strip_ansi(raw_line).strip_edges()
		if line.is_empty():
			continue
		if is_benign_log_line(line):
			continue
		var is_error := line.begins_with("ERROR:") or line.begins_with("USER ERROR:")
		if not is_error:
			for marker: String in FATAL_LOG_MARKERS:
				if line.contains(marker):
					is_error = true
					break
		if is_error and not out.has(line):
			out.append(line)
	return out


# Godot colorizes its console output; the escape codes make the log unreadable
# once it is embedded in a JSON tool result.
static var ESC := String.chr(27)

static func _strip_ansi(text: String) -> String:
	var out := ""
	var i := 0
	while i < text.length():
		var ch := text[i]
		if ch == ESC:
			# Skip through the terminating letter of the CSI sequence.
			i += 1
			while i < text.length():
				var c2 := text[i]
				if (c2 >= "a" and c2 <= "z") or (c2 >= "A" and c2 <= "Z"):
					i += 1
					break
				i += 1
			continue
		out += ch
		i += 1
	return out


# Trim a build log down to something worth putting in a tool response.
static func log_tail(log_text: String, max_lines: int = 40) -> String:
	var kept: Array = []
	for raw_line: String in log_text.split("\n"):
		var line := _strip_ansi(raw_line).strip_edges()
		if line.is_empty():
			continue
		# Per-file progress spam ("[  3% ] savepack | Storing File: ...") is
		# hundreds of lines and says nothing the agent can act on.
		if line.begins_with("[") and line.contains("%") and line.contains("]"):
			continue
		if is_benign_log_line(line):
			continue
		kept.append(line)
	if kept.size() > max_lines:
		kept = kept.slice(kept.size() - max_lines, kept.size())
	return "\n".join(kept)


# ── Artifact inspection ────────────────────────────────────────────────────

# Measure what an export actually produced. Godot writes siblings next to the
# main artifact (a Web export emits index.wasm/.pck/.js beside index.html), so
# a bare exists() check under-reports the build.
static func inspect_artifacts(main_abs_path: String) -> Dictionary:
	var dir_path := main_abs_path.get_base_dir()
	var stem := main_abs_path.get_file().get_basename()
	var main_exists := FileAccess.file_exists(main_abs_path)
	var main_size := 0
	if main_exists:
		var f := FileAccess.open(main_abs_path, FileAccess.READ)
		if f != null:
			main_size = f.get_length()
			f.close()
	var siblings: Array = []
	var total := main_size
	var dir := DirAccess.open(dir_path)
	if dir != null:
		dir.list_dir_begin()
		var entry := dir.get_next()
		while entry != "":
			if not dir.current_is_dir() and entry.get_basename().begins_with(stem):
				var abs := dir_path.path_join(entry)
				if abs != main_abs_path:
					var sf := FileAccess.open(abs, FileAccess.READ)
					if sf != null:
						total += sf.get_length()
						sf.close()
					siblings.append(entry)
			entry = dir.get_next()
		dir.list_dir_end()
	siblings.sort()
	return {
		"exists": main_exists,
		"size_bytes": main_size,
		"total_bytes": total,
		"sibling_files": siblings,
	}


static func human_size(num_bytes: int) -> String:
	if num_bytes < 1024:
		return "%d B" % num_bytes
	var kb := float(num_bytes) / 1024.0
	if kb < 1024.0:
		return "%.1f KB" % kb
	var mb := kb / 1024.0
	if mb < 1024.0:
		return "%.1f MB" % mb
	return "%.2f GB" % (mb / 1024.0)
