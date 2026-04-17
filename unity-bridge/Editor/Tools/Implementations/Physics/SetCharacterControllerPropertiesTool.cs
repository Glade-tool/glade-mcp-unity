using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class SetCharacterControllerPropertiesTool : ITool
    {
        public string Name => "set_character_controller_properties";

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

            CharacterController charController = obj.GetComponent<CharacterController>();
            if (charController == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' has no CharacterController component");
            }

            // Check for conflicts
            var conflictInfo = ToolUtils.CheckColliderConflicts(obj);
            bool hasColliders = conflictInfo.ContainsKey("hasColliders") && conflictInfo["hasColliders"] is bool hasCol && hasCol;
            var existingColliders = conflictInfo.ContainsKey("existingColliders") ? conflictInfo["existingColliders"] as List<string> : new List<string>();

            Undo.RecordObject(charController, "Set CharacterController Properties");
            ApplyCharacterControllerProperties(charController, args);

            var responseExtras = new Dictionary<string, object>
            {
                { "radius", charController.radius },
                { "height", charController.height },
                { "center", $"{charController.center.x},{charController.center.y},{charController.center.z}" }
            };

            if (hasColliders)
            {
                responseExtras["warnings"] = $"GameObject has {existingColliders.Count} other collider(s) in addition to CharacterController. This may cause conflicts.";
                responseExtras["existingColliders"] = existingColliders;
            }

            return ToolUtils.CreateSuccessResponse($"Updated CharacterController properties on '{gameObjectPath}'", responseExtras);
        }

        private static void ApplyCharacterControllerProperties(CharacterController charController, Dictionary<string, object> args)
        {
            if (args.ContainsKey("radius") && float.TryParse(args["radius"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float radius))
                charController.radius = radius;
            if (args.ContainsKey("height") && float.TryParse(args["height"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float height))
                charController.height = height;
            if (args.ContainsKey("center"))
                charController.center = ToolUtils.ParseVector3(args["center"].ToString());
            if (args.ContainsKey("slopeLimit") && float.TryParse(args["slopeLimit"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float slopeLimit))
                charController.slopeLimit = slopeLimit;
            if (args.ContainsKey("stepOffset") && float.TryParse(args["stepOffset"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float stepOffset))
                charController.stepOffset = stepOffset;
            if (args.ContainsKey("skinWidth") && float.TryParse(args["skinWidth"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float skinWidth))
                charController.skinWidth = skinWidth;
            if (args.ContainsKey("minMoveDistance") && float.TryParse(args["minMoveDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float minMoveDistance))
                charController.minMoveDistance = minMoveDistance;

            EnsureValidCharacterController(charController);
        }

        /// <summary>
        /// Ensures CharacterController has valid values so it stays active and avoids
        /// "Step Offset must be positive" and "Move called on inactive controller" errors.
        /// </summary>
        private static void EnsureValidCharacterController(CharacterController cc)
        {
            if (cc.radius <= 0f) cc.radius = 0.5f;
            float minHeight = cc.radius * 2f;
            if (cc.height < minHeight) cc.height = Mathf.Max(2f, minHeight);
            if (cc.stepOffset <= 0f) cc.stepOffset = 0.3f;
            if (cc.skinWidth <= 0f) cc.skinWidth = 0.08f;
        }
    }
}
