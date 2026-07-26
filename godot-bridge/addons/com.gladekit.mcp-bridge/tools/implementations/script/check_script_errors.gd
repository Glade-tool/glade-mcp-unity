extends "res://addons/com.gladekit.mcp-bridge/tools/i_tool.gd"

# Validates GDScript files and reports parse/analysis errors with file and line
# numbers. This is the verification step to run after writing or editing a
# script — the equivalent of a compile check in a statically compiled language.
#
# GDScript is often described as needing no compile step. That is true of the
# edit-run cycle, but not of correctness: the engine still runs a full parser
# and type analyser over every .gd file, and a script that fails either one is
# dead at load time. Writing a broken script otherwise produces no signal at
# all — the write succeeds, the file lands on disk, and the failure only shows
# up later as a scene that will not start, with nothing pointing at the cause.
#
# The check runs the engine's own parser in a short-lived headless process
# (`--check-only`), so the diagnostics are exactly the ones Godot itself would
# report, and it catches more than syntax:
#   - syntax errors            ("Expected ":" after "if" condition.")
#   - static type violations   ("Cannot assign a value of type String ... int")
#   - unknown members/calls    ("Function "no_such_method()" not found in base self.")
#
# Read-only: it never writes to the project and is safe during play mode.
#
# Args:
#   script_paths: Array[String] — res:// paths to check. A bare String is
#                 accepted for a single file.
#   script_path:  String — convenience alias for one path.
#
# Response payload:
#   checked:      [String] — paths that were checked
#   error_count:  int      — total diagnostics across all files
#   errors:       [{script_path, line, message}]
#   clean:        bool     — true when error_count == 0

const ToolUtils = preload("res://addons/com.gladekit.mcp-bridge/bridge/tool_utils.gd")

# Each file costs one short-lived engine process (~0.2s). Cap the batch so a
# caller passing a large list cannot stall the editor's main thread; the bridge
# has its own stall watchdog and we want to stay well inside it.
const MAX_FILES := 12

# Matches the `at:` line the engine prints under each diagnostic, from which we
# recover the source line: `   at: GDScript::reload (res://player.gd:42)`
const AT_LINE_PREFIX := "at: GDScript::reload ("


func _init() -> void:
	tool_name = "check_script_errors"
	requires_edit_mode = false


func execute(args: Dictionary) -> Dictionary:
	var paths := _collect_paths(args)
	if paths.is_empty():
		return ToolUtils.error(
			"script_paths is required — pass the res:// path(s) of the script(s) you just wrote."
		)
	if paths.size() > MAX_FILES:
		paths = paths.slice(0, MAX_FILES)

	var exe := OS.get_executable_path()
	if exe.is_empty():
		return ToolUtils.error("Could not resolve the Godot executable path to run the script check.")
	var project_root := ProjectSettings.globalize_path("res://")

	var errors: Array = []
	var checked: Array = []
	var missing: Array = []

	for p in paths:
		var path := String(p)
		if not FileAccess.file_exists(path):
			missing.append(path)
			continue
		checked.append(path)
		errors.append_array(_check_one(exe, project_root, path))

	if checked.is_empty():
		return ToolUtils.error(
			"None of the given scripts exist: %s" % ", ".join(missing)
		)

	var payload := {
		"checked": checked,
		"error_count": errors.size(),
		"errors": errors,
		"clean": errors.is_empty(),
	}
	if not missing.is_empty():
		payload["missing"] = missing

	if errors.is_empty():
		return ToolUtils.success(
			"No script errors in %d file(s) — parses and type-checks clean." % checked.size(),
			payload
		)

	# Report failure as a successful tool call carrying a failing result: the
	# caller needs the diagnostics to act on, and an error envelope invites a
	# retry of the check rather than a fix of the script.
	var lines: Array = []
	for e in errors:
		lines.append("%s:%d — %s" % [e["script_path"], e["line"], e["message"]])
	return ToolUtils.success(
		"Found %d script error(s):\n%s" % [errors.size(), "\n".join(lines)],
		payload
	)


# Collects res:// paths from either `script_paths` (Array or single String) or
# the `script_path` alias, skipping blanks and de-duplicating.
func _collect_paths(args: Dictionary) -> Array:
	var out: Array = []
	var raw = args.get("script_paths", null)
	if raw == null:
		raw = args.get("script_path", null)
	if raw == null:
		return out

	var candidates: Array = []
	if raw is Array:
		candidates = raw
	else:
		candidates = [raw]

	for c in candidates:
		if c == null:
			continue
		var s := String(c).strip_edges()
		if s.is_empty():
			continue
		if not s.begins_with("res://"):
			s = "res://" + s.trim_prefix("/")
		if s.get_extension().is_empty():
			s += ".gd"
		if not out.has(s):
			out.append(s)
	return out


# Runs one file through the engine's parser and returns its diagnostics.
func _check_one(exe: String, project_root: String, path: String) -> Array:
	var output: Array = []
	var argv := ["--headless", "--check-only", "--script", path, "--path", project_root]
	var exit_code := OS.execute(exe, argv, output, true)

	# exit_code 0 with no diagnostics is the clean case. A non-zero exit without
	# parseable diagnostics still means something went wrong, so surface it
	# rather than silently reporting the script as clean.
	var found := _parse_diagnostics(output, path)
	if found.is_empty() and exit_code != 0:
		found.append({
			"script_path": path,
			"line": 0,
			"message": "Script failed to load (engine exit code %d) but reported no parse diagnostics." % exit_code,
		})
	return found


# Extracts {script_path, line, message} triples from the engine's stderr.
# Format (one diagnostic = two lines):
#   SCRIPT ERROR: Parse Error: Expected ":" after "if" condition.
#      at: GDScript::reload (res://player.gd:5)
func _parse_diagnostics(output: Array, fallback_path: String) -> Array:
	var text := ""
	for chunk in output:
		text += String(chunk)
	# The engine writes CRLF on Windows; normalise before splitting.
	var lines: PackedStringArray = text.replace("\r\n", "\n").split("\n", false)

	var found: Array = []
	var pending_message := ""
	for raw_line in lines:
		var line := String(raw_line).strip_edges()

		if line.begins_with("SCRIPT ERROR:"):
			# Flush a diagnostic that never got its `at:` line.
			if not pending_message.is_empty():
				found.append({"script_path": fallback_path, "line": 0, "message": pending_message})
			pending_message = line.trim_prefix("SCRIPT ERROR:").strip_edges()
			pending_message = pending_message.trim_prefix("Parse Error:").strip_edges()
			continue

		if pending_message.is_empty():
			continue

		if line.begins_with(AT_LINE_PREFIX):
			var loc := line.trim_prefix(AT_LINE_PREFIX).trim_suffix(")")
			var sep := loc.rfind(":")
			var file_part := loc.substr(0, sep) if sep > 0 else fallback_path
			var line_no := int(loc.substr(sep + 1)) if sep > 0 else 0
			found.append({
				"script_path": file_part,
				"line": line_no,
				"message": pending_message,
			})
			pending_message = ""

	if not pending_message.is_empty():
		found.append({"script_path": fallback_path, "line": 0, "message": pending_message})
	return found
