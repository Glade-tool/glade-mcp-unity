using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

#if GLADE_UGUI
namespace GladeAgenticAI.Core.Tools.Implementations.UI
{
    internal static class UIHelpers
    {
        public static bool IsTextMeshProAvailable()
        {
            return System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;
        }

        /// <summary>
        /// Result of TMP availability check for UI actions.
        /// </summary>
        public class TMPCheckResult
        {
            public bool IsAvailable { get; set; }
            public bool InstallationStarted { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Checks if TextMeshPro is available for UI actions. 
        /// If not available, opens the import dialog and returns early with instructions.
        /// This should be called at the start of all UI tool Execute methods.
        /// </summary>
        public static TMPCheckResult EnsureTMPForUIActions()
        {
            // Check if Essential Resources are actually imported (check for Assets/TextMesh Pro/Resources folder)
            // Only check Resources folder - ignore type availability (types can exist without Resources)
            string resourcesPath = System.IO.Path.Combine(Application.dataPath, "TextMesh Pro", "Resources");
            bool essentialResourcesImported = System.IO.Directory.Exists(resourcesPath);
            
            // Also verify it has the Fonts & Materials subfolder and files
            if (essentialResourcesImported)
            {
                string fontsPath = System.IO.Path.Combine(resourcesPath, "Fonts & Materials");
                bool fontsExist = System.IO.Directory.Exists(fontsPath);
                bool hasFiles = false;
                try
                {
                    var files = System.IO.Directory.GetFiles(resourcesPath, "*", System.IO.SearchOption.TopDirectoryOnly);
                    hasFiles = files.Length > 0;
                }
                catch { }
                
                if (fontsExist && hasFiles)
                {
                    return new TMPCheckResult { IsAvailable = true, InstallationStarted = false, Message = null };
                }
            }

            // TMP not available - open import dialog and return early with instructions
            UnityEngine.Debug.LogError("[GladeAI] TMP Essential Resources not found - opening import dialog");
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            
            return new TMPCheckResult 
            { 
                IsAvailable = false, 
                InstallationStarted = true, 
                Message = "TextMeshPro Essential Resources are not installed. " +
                         "I've opened the import dialog in Unity Editor. " +
                         "Please click the 'Import' button in the dialog, wait for Unity to finish importing, " +
                         "then retry your UI action request. This is a one-time setup." 
            };
        }

        /// <summary>
        /// Adds TMP text component to a GameObject. Uses EnsureTMPForUIActions to ensure TMP is available.
        /// Returns true if TMP was added, false if TMP is not available.
        /// </summary>
        public static bool TryAddTmpText(UnityEngine.GameObject obj, Dictionary<string, object> args)
        {
            // Ensure TMP is available (this will auto-import if package is installed)
            var tmpCheck = EnsureTMPForUIActions();
            if (!tmpCheck.IsAvailable)
            {
                UnityEngine.Debug.LogError($"[GladeAI] TMP not available: {tmpCheck.Message}");
                return false;
            }

            var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType == null)
            {
                UnityEngine.Debug.LogError("[GladeAI] TMP type not found even though TMP check passed.");
                return false;
            }

            var tmpComponent = obj.AddComponent(tmpType);
            TrySetTmpTextProperties(tmpComponent, args);
            return true; // TMP was successfully added
        }

        public static void TrySetTmpTextProperties(Component tmpComponent, Dictionary<string, object> args)
        {
            if (tmpComponent == null || args == null)
            {
                return;
            }

            var tmpType = tmpComponent.GetType();
            if (args.ContainsKey("text"))
            {
                var textProp = tmpType.GetProperty("text");
                textProp?.SetValue(tmpComponent, args["text"].ToString(), null);
            }
            if (args.ContainsKey("color"))
            {
                var colorProp = tmpType.GetProperty("color");
                colorProp?.SetValue(tmpComponent, ToolUtils.ParseColor(args["color"].ToString()), null);
            }
            if (args.ContainsKey("fontSize") && float.TryParse(args["fontSize"].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float size))
            {
                var sizeProp = tmpType.GetProperty("fontSize");
                sizeProp?.SetValue(tmpComponent, size, null);
            }
            if (args.ContainsKey("alignment"))
            {
                var alignmentProp = tmpType.GetProperty("alignment");
                if (alignmentProp != null)
                {
                    var alignmentType = alignmentProp.PropertyType;
                    try
                    {
                        var alignmentValue = System.Enum.Parse(alignmentType, args["alignment"].ToString(), true);
                        alignmentProp.SetValue(tmpComponent, alignmentValue, null);
                    }
                    catch
                    {
                        // Ignore invalid alignment values.
                    }
                }
            }
        }

        public static void CollectUiChildrenJson(UnityEngine.Transform parent, StringBuilder sb, int depth)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.GetComponent<RectTransform>() == null) continue;

                if (i > 0 || depth > 0) sb.Append(",");
                sb.Append("{");
                sb.Append($"\"name\":\"{ToolUtils.EscapeJsonString(child.name)}\",");
                sb.Append($"\"path\":\"{ToolUtils.EscapeJsonString(ToolUtils.GetGameObjectPath(child.gameObject))}\",");
                sb.Append($"\"active\":{(child.gameObject.activeSelf ? "true" : "false")},");
                sb.Append("\"children\":[");
                CollectUiChildrenJson(child, sb, depth + 1);
                sb.Append("]}");
            }
        }
    }
}
#endif
