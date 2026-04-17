using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Camera
{
    public class SetPostProcessingTool : ITool
    {
        public string Name => "set_post_processing";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            string profilePath = args.ContainsKey("profilePath") ? args["profilePath"].ToString() : "";

            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            Volume volume = obj.GetComponent<Volume>() ?? Undo.AddComponent<Volume>(obj);

            if (!string.IsNullOrEmpty(profilePath))
            {
                if (!profilePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    profilePath = "Assets/" + profilePath;
                VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if (profile == null)
                {
                    return ToolUtils.CreateErrorResponse($"VolumeProfile not found at '{profilePath}'");
                }
                volume.profile = profile;
            }

            if (args.ContainsKey("isGlobal"))
            {
                if (args["isGlobal"] is bool b) volume.isGlobal = b;
                else if (bool.TryParse(args["isGlobal"].ToString(), out bool v)) volume.isGlobal = v;
            }
            if (args.ContainsKey("weight"))
            {
                if (float.TryParse(args["weight"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float w))
                    volume.weight = w;
            }

            return ToolUtils.CreateSuccessResponse($"Updated post-processing on '{gameObjectPath}'");
        }
    }
}
