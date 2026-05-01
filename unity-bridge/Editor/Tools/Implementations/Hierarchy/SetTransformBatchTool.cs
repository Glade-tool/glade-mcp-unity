using System.Collections.Generic;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class SetTransformBatchTool : ITool
    {
        public string Name => "set_transform_batch";

        public string Execute(Dictionary<string, object> args)
        {
            // Accept both "transforms" (new) and "items" (legacy) parameter names
            string itemsKey = args.ContainsKey("transforms") ? "transforms" : "items";
            if (!args.ContainsKey(itemsKey))
            {
                return ToolUtils.CreateErrorResponse("transforms is required");
            }

            int updated = 0;
            var itemsObj = args[itemsKey];

            // Re-hydrate JSON-array strings so transforms works whether it arrives
            // already-typed or string-encoded (e.g. via batch_execute).
            if (itemsObj is string itemsJson && ToolUtils.TryParseJsonArrayToList(itemsJson, out var parsedItems))
                itemsObj = parsedItems;

            var items = new List<Dictionary<string, object>>();
            if (itemsObj is List<object> list)
            {
                foreach (var item in list)
                {
                    if (item is Dictionary<string, object> dict) items.Add(dict);
                }
            }
            else
            {
                return ToolUtils.CreateErrorResponse($"{itemsKey} must be an array of objects");
            }

            var registry = new ToolRegistry();
            var setTransformTool = registry.GetTool("set_transform");
            if (setTransformTool == null)
                return ToolUtils.CreateErrorResponse("set_transform tool not available");

            foreach (var item in items)
            {
                string path = item.ContainsKey("gameObjectPath") ? item["gameObjectPath"].ToString() : "";
                if (string.IsNullOrEmpty(path)) continue;
                var callArgs = new Dictionary<string, object>(item);
                callArgs["gameObjectPath"] = path;
                var result = setTransformTool.Execute(callArgs);
                if (result.Contains("\"success\":true")) updated++;
            }

            var extras = new Dictionary<string, object>
            {
                { "updated", updated }
            };
            return ToolUtils.CreateSuccessResponse($"Updated {updated} object(s)", extras);
        }
    }
}
