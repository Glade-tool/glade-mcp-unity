extends "res://addons/com.gladekit.mcp-bridge/tools/i_tool.gd"

# Returns a grouped summary of every mutation recorded by SessionTracker's
# mutation log since the Godot editor session started (or since the last
# reset). The Godot twin of the Unity bridge's GetSessionSummaryTool.
#
# Read-only. Lets the AI answer "what did you just do?" / "what changed?"
# without re-reading scene state, and lets the client's "What changed" panel
# show engine-side stats — which were Unity-only until this tool existed
# (GodotBridgeClient.getSessionSummary returned null unconditionally).
#
# The payload shape is the SHARED WIRE CONTRACT with the Unity bridge
# (camelCase keys: sessionStartedAt, elapsedSeconds, toolCalls, mutations,
# successCount, errorCount, byCategory, timeline, timelineTruncated) so the
# Proxy's _shape_session_summary formatter and the Electron SessionSummary
# type consume both engines through one code path.
#
# Args:
#   max_timeline_entries: int — max recent mutation entries to include.
#                               Default 50, clamped to [0, 500].

const SessionTracker = preload("res://addons/com.gladekit.mcp-bridge/bridge/session_tracker.gd")


func _init() -> void:
	tool_name = "get_session_summary"
	requires_edit_mode = false


func execute(args: Dictionary) -> Dictionary:
	var max_timeline := 50
	if args.has("max_timeline_entries"):
		max_timeline = int(str(args.get("max_timeline_entries")).to_int())
	max_timeline = clampi(max_timeline, 0, SessionTracker.MAX_TIMELINE_ENTRIES)

	var summary := SessionTracker.build_summary(max_timeline)
	summary["success"] = true
	summary["message"] = "Session summary: %d mutation(s) across %d tool call(s)" % [
		int(summary.get("mutations", 0)),
		int(summary.get("toolCalls", 0)),
	]
	return summary
