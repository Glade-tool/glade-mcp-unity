using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Hierarchy
{
    public class GetGameObjectComponentsTool : ITool
    {
        public string Name => "get_gameobject_components";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            var components = obj.GetComponents<Component>();
            var names = new List<string>();
            int missingCount = 0;

            foreach (var comp in components)
            {
                if (comp == null)
                {
                    names.Add("MissingScript");
                    missingCount++;
                }
                else
                {
                    names.Add(comp.GetType().Name);
                }
            }

            var result = new Dictionary<string, object>
            {
                ["gameObjectPath"] = ToolUtils.EscapeJsonString(ToolUtils.GetGameObjectPath(obj)),
                ["componentCount"] = names.Count,
                ["missingCount"] = missingCount,
                ["components"] = names
            };

            return ToolUtils.SerializeDictToJson(result);
        }
    }
}
