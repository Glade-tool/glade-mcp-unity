using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Prefabs
{
    /// <summary>
    /// Tool to set transform properties (position, rotation, scale) on a prefab asset's root object.
    /// This edits the prefab asset directly, affecting all future instantiations.
    /// </summary>
    public class SetPrefabTransformTool : ITool
    {
        public string Name => "set_prefab_transform";

        public string Execute(Dictionary<string, object> args)
        {
            string prefabPath = args.ContainsKey("prefabPath") ? args["prefabPath"].ToString() : "";
            if (string.IsNullOrEmpty(prefabPath))
                return ToolUtils.CreateErrorResponse("prefabPath is required");

            // Normalize path
            if (!prefabPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                prefabPath = "Assets/" + prefabPath;

            // Load prefab asset
            UnityEngine.GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                // Try case-insensitive search
                prefabAsset = ToolUtils.LoadAssetAtPathCaseInsensitive<UnityEngine.GameObject>(prefabPath);
                if (prefabAsset == null)
                    return ToolUtils.CreateErrorResponse($"Prefab not found at '{prefabPath}'");
            }

            // Get root transform
            UnityEngine.Transform rootTransform = prefabAsset.transform;
            if (rootTransform == null)
                return ToolUtils.CreateErrorResponse($"Prefab '{prefabPath}' has no root transform");

            // Capture previous state
            var prevPos = rootTransform.localPosition;
            var prevRot = rootTransform.localRotation;
            var prevRotEuler = prevRot.eulerAngles;
            var prevScale = rootTransform.localScale;

            string operation = args.ContainsKey("operation") ? args["operation"].ToString() : "set";

            // Use SerializedObject to edit prefab asset
            SerializedObject serializedTransform = new SerializedObject(rootTransform);

            if (args.ContainsKey("position"))
            {
                var pos = ToolUtils.ParseVector3(args["position"].ToString());
                if (operation == "add")
                    pos = prevPos + pos;
                else if (operation == "multiply")
                    pos = new Vector3(prevPos.x * pos.x, prevPos.y * pos.y, prevPos.z * pos.z);

                SerializedProperty localPositionProp = serializedTransform.FindProperty("m_LocalPosition");
                if (localPositionProp != null)
                {
                    localPositionProp.vector3Value = pos;
                }
            }

            if (args.ContainsKey("rotation"))
            {
                var rot = ToolUtils.ParseVector3(args["rotation"].ToString());
                Quaternion eulerRot = Quaternion.Euler(rot);
                if (operation == "add")
                    eulerRot = prevRot * eulerRot;
                else if (operation == "multiply")
                {
                    var current = prevRotEuler;
                    var newRot = new Vector3(current.x * rot.x, current.y * rot.y, current.z * rot.z);
                    eulerRot = Quaternion.Euler(newRot);
                }

                SerializedProperty localRotationProp = serializedTransform.FindProperty("m_LocalRotation");
                if (localRotationProp != null)
                {
                    localRotationProp.quaternionValue = eulerRot;
                }
            }

            if (args.ContainsKey("scale"))
            {
                var scale = ToolUtils.ParseVector3(args["scale"].ToString());
                if (operation == "add")
                    scale = prevScale + scale;
                else if (operation == "multiply")
                    scale = new Vector3(prevScale.x * scale.x, prevScale.y * scale.y, prevScale.z * scale.z);

                SerializedProperty localScaleProp = serializedTransform.FindProperty("m_LocalScale");
                if (localScaleProp != null)
                {
                    localScaleProp.vector3Value = scale;
                }
            }

            // Apply changes
            serializedTransform.ApplyModifiedProperties();

            // Mark prefab as dirty and save
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                ["previousState"] = new Dictionary<string, object>
                {
                    ["position"] = $"{prevPos.x},{prevPos.y},{prevPos.z}",
                    ["rotation"] = $"{prevRotEuler.x},{prevRotEuler.y},{prevRotEuler.z}",
                    ["scale"] = $"{prevScale.x},{prevScale.y},{prevScale.z}"
                }
            };

            return ToolUtils.CreateSuccessResponse($"Updated transform on prefab '{prefabPath}' (operation: {operation}). Changes apply to all future instantiations.", extras);
        }
    }
}
