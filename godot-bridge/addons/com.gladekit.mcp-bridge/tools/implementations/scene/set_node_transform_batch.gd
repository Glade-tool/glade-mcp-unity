extends "res://addons/com.gladekit.mcp-bridge/tools/i_tool.gd"

# Applies transforms to MANY nodes in ONE call — the batch counterpart of
# set_node_transform for arbitrary per-node placement.
#
# Placing a set of nodes one at a time costs one request per node, and an AI
# client pays for each of those round-trips in both latency and tokens. Laying
# out a few dozen props that way is slow enough to be the dominant cost of the
# whole request. arrange_nodes already covers PATTERNED layout (row / column /
# grid from a single anchor + spacing); this covers everything else — explicit
# per-node positions, rotations, scales, or a mix of all three.
#
# Each entry is exactly the arg set set_node_transform accepts, and this tool
# DELEGATES to that implementation rather than re-deriving the math. The two
# paths therefore cannot drift: whatever the single tool does for Node2D vs
# Node3D, local vs global space, and set/add/multiply, the batch does
# identically.
#
# Args:
#   transforms: Array (required) — entries of
#               {node_path, position?, rotation?, scale?, space?, operation?}.
#               Each entry needs node_path plus at least one of
#               position / rotation / scale.
#   space:      String — batch-wide default ("local" | "global"); an entry's own
#                        `space` wins. Default "local".
#   operation:  String — batch-wide default ("set" | "add" | "multiply"); an
#                        entry's own `operation` wins. Default "set".
#
# Response payload:
#   updated: [{node_path, previous_state}] — previous_state is set_node_transform's,
#            so an undo can be reconstructed per node.
#   failed:  [{node_path, error}] — entries that did not apply (bad path, wrong
#            node class, no transform component given).
#   count:   number updated.
#
# Partial failures are reported, not swallowed: a batch where 3 of 5 nodes
# resolved returns success with `failed` populated and the counts in the
# message. A batch where NOTHING resolved is an error — reporting success for a
# call that moved nothing is exactly the trust bug the single tool's
# "needs at least one of position/rotation/scale" refusal exists to prevent.

const ToolUtils = preload("res://addons/com.gladekit.mcp-bridge/bridge/tool_utils.gd")
const SetNodeTransformTool = preload("res://addons/com.gladekit.mcp-bridge/tools/implementations/scene/set_node_transform.gd")

# Inherited batch-wide defaults. An entry that names either one wins over these.
const _INHERITED_KEYS := ["space", "operation"]


func _init() -> void:
	tool_name = "set_node_transform_batch"
	requires_edit_mode = true


func execute(args: Dictionary) -> Dictionary:
	var raw = args.get("transforms")
	if not (raw is Array) or (raw as Array).is_empty():
		return empty_batch_error()

	var single := SetNodeTransformTool.new()
	var updated: Array = []
	var failed: Array = []

	for raw_entry in raw:
		if not (raw_entry is Dictionary):
			failed.append({
				"node_path": str(raw_entry),
				"error": "Entry is not an object — expected {node_path, position/rotation/scale}",
			})
			continue

		var entry: Dictionary = prepare_entry(raw_entry as Dictionary, args)
		var node_path := ToolUtils.parse_string_arg(entry, "node_path")
		var result: Dictionary = single.execute(entry)
		if result.get("success", false):
			updated.append({
				"node_path": node_path,
				"previous_state": result.get("previous_state", {}),
			})
		else:
			failed.append({
				"node_path": node_path,
				"error": result.get("error", result.get("message", "unknown error")),
			})

	return build_result(updated, failed)


# ── Pure helpers ───────────────────────────────────────────────────────────
# Static and editor-free on purpose: everything except the delegation loop is
# then exercisable by the HEADLESS unit suite. Integration tests here need a
# live EditorInterface, which GUT's play_custom_scene runner cannot provide
# (see tests/README.md), so a batch tool whose whole logic lived inside
# execute() would ship with no test that CI actually runs.

static func prepare_entry(raw_entry: Dictionary, batch_args: Dictionary) -> Dictionary:
	"""Normalize one entry and fold in the batch-wide defaults it didn't set.

	ws_server normalizes camelCase only at the TOP level of the payload, so a
	model writing {"nodePath": ...} inside an entry would otherwise land on an
	unread key and get a silent no-op. An entry's own space/operation always
	wins over the batch-wide value."""
	var entry: Dictionary = ToolUtils.normalize_args(raw_entry)
	for key in _INHERITED_KEYS:
		if not entry.has(key) and batch_args.has(key):
			entry[key] = batch_args[key]
	return entry


static func empty_batch_error() -> Dictionary:
	return ToolUtils.error_with_solutions(
		"set_node_transform_batch needs a non-empty transforms array",
		[
			"Pass one entry per node: transforms=[{\"node_path\": \"Coin1\", \"position\": \"0,1,0\"}, {\"node_path\": \"Coin2\", \"position\": \"2,1,0\"}]",
			"To move a SINGLE node, use set_node_transform instead",
			"For a row / column / grid from one anchor + spacing, arrange_nodes does the layout math for you",
		]
	)


static func build_result(updated: Array, failed: Array) -> Dictionary:
	"""Summarize the fan-out. Nothing updated is an ERROR, not an empty success:
	a batch that reports success while moving nothing leaves the agent acting on
	positions it never set."""
	if updated.is_empty():
		var first_error: String = str(failed[0].get("error", "")) if not failed.is_empty() else ""
		return ToolUtils.error_with_solutions(
			"No node in the batch could be updated (%d entr%s failed). First error: %s" % [
				failed.size(), "y" if failed.size() == 1 else "ies", first_error,
			],
			[
				"Check the paths with get_scene_tree — they are scene-relative, not absolute",
				"Transforms apply to Node2D / Node3D only; Control (UI) nodes use set_control_anchors / set_control_size",
				"Every entry needs at least one of position, rotation, or scale",
			]
		)

	var miss_note := ""
	if not failed.is_empty():
		miss_note = " (%d failed)" % failed.size()

	return ToolUtils.success(
		"Updated transforms on %d node%s%s in one call — save the scene to persist" % [
			updated.size(), "" if updated.size() == 1 else "s", miss_note,
		],
		{
			"updated": updated,
			"failed": failed,
			"count": updated.size(),
		}
	)
