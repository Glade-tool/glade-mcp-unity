using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class CreatePhysicsMaterialTool : ITool
    {
        public string Name => "create_physics_material";

        public string Execute(Dictionary<string, object> args)
        {
            string materialPath = args.ContainsKey("materialPath") ? args["materialPath"].ToString() : "";
            if (string.IsNullOrEmpty(materialPath))
            {
                return ToolUtils.CreateErrorResponse("materialPath is required");
            }

            if (!materialPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                materialPath = "Assets/" + materialPath;

            // Check if asset already exists
            var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(materialPath);
            if (existing != null)
            {
                return ToolUtils.CreateErrorResponse($"PhysicMaterial already exists at '{materialPath}'. Use a different path or delete the existing asset first.");
            }

            string dir = System.IO.Path.GetDirectoryName(materialPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }

            var mat = new PhysicsMaterial(System.IO.Path.GetFileNameWithoutExtension(materialPath));
            if (args.ContainsKey("dynamicFriction") && float.TryParse(args["dynamicFriction"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dynamicFriction))
                mat.dynamicFriction = dynamicFriction;
            if (args.ContainsKey("staticFriction") && float.TryParse(args["staticFriction"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float staticFriction))
                mat.staticFriction = staticFriction;
            if (args.ContainsKey("bounciness") && float.TryParse(args["bounciness"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float bounciness))
                mat.bounciness = bounciness;

            if (args.ContainsKey("frictionCombine"))
            {
                if (System.Enum.TryParse(args["frictionCombine"].ToString(), true, out PhysicsMaterialCombine combine))
                    mat.frictionCombine = combine;
            }
            if (args.ContainsKey("bounceCombine"))
            {
                if (System.Enum.TryParse(args["bounceCombine"].ToString(), true, out PhysicsMaterialCombine combine))
                    mat.bounceCombine = combine;
            }

            AssetDatabase.CreateAsset(mat, materialPath);
            AssetDatabase.SaveAssets();
            
            var extras = new Dictionary<string, object>
            {
                { "materialPath", materialPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created PhysicMaterial at '{materialPath}'", extras);
        }
    }
}
