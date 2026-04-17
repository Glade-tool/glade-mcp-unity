using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

#if ENABLE_LEGACY_INPUT_MANAGER
namespace GladeAgenticAI.Core.Tools.Implementations.InputLegacy
{
    public class ListLegacyInputAxesTool : ITool
    {
        public string Name => "list_legacy_input_axes";

        public string Execute(Dictionary<string, object> args)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (assets == null || assets.Length == 0)
                return ToolUtils.CreateErrorResponse("ProjectSettings/InputManager.asset not found.");

            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty axesProp = so.FindProperty("m_Axes");
            if (axesProp == null || !axesProp.isArray)
                return ToolUtils.CreateErrorResponse("InputManager m_Axes not found.");

            var axesList = new List<Dictionary<string, object>>();
            for (int i = 0; i < axesProp.arraySize; i++)
            {
                SerializedProperty axis = axesProp.GetArrayElementAtIndex(i);
                var ax = new Dictionary<string, object>
                {
                    { "name", axis.FindPropertyRelative("m_Name").stringValue },
                    { "positiveButton", axis.FindPropertyRelative("positiveButton").stringValue },
                    { "negativeButton", axis.FindPropertyRelative("negativeButton").stringValue },
                    { "altPositiveButton", axis.FindPropertyRelative("altPositiveButton").stringValue },
                    { "altNegativeButton", axis.FindPropertyRelative("altNegativeButton").stringValue },
                    { "gravity", axis.FindPropertyRelative("gravity").floatValue },
                    { "dead", axis.FindPropertyRelative("dead").floatValue },
                    { "sensitivity", axis.FindPropertyRelative("sensitivity").floatValue },
                    { "snap", axis.FindPropertyRelative("snap").intValue != 0 },
                    { "invert", axis.FindPropertyRelative("invert").intValue != 0 },
                    { "type", axis.FindPropertyRelative("type").intValue },
                    { "axis", axis.FindPropertyRelative("axis").intValue },
                    { "joyNum", axis.FindPropertyRelative("joyNum").intValue }
                };
                axesList.Add(ax);
            }

            var sb = new StringBuilder();
            sb.Append("{\"success\":true,\"message\":\"List of legacy input axes\",\"axes\":[");
            for (int i = 0; i < axesList.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(SerializeAxisToJson(axesList[i]));
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string SerializeAxisToJson(Dictionary<string, object> ax)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"name\":\"{ToolUtils.EscapeJsonString((string)ax["name"])}\"");
            sb.Append($",\"positiveButton\":\"{ToolUtils.EscapeJsonString((string)ax["positiveButton"])}\"");
            sb.Append($",\"negativeButton\":\"{ToolUtils.EscapeJsonString((string)ax["negativeButton"])}\"");
            sb.Append($",\"altPositiveButton\":\"{ToolUtils.EscapeJsonString((string)ax["altPositiveButton"])}\"");
            sb.Append($",\"altNegativeButton\":\"{ToolUtils.EscapeJsonString((string)ax["altNegativeButton"])}\"");
            sb.Append($",\"gravity\":{ax["gravity"]}");
            sb.Append($",\"dead\":{ax["dead"]}");
            sb.Append($",\"sensitivity\":{ax["sensitivity"]}");
            sb.Append($",\"snap\":{((bool)ax["snap"] ? "true" : "false")}");
            sb.Append($",\"invert\":{((bool)ax["invert"] ? "true" : "false")}");
            sb.Append($",\"type\":{ax["type"]}");
            sb.Append($",\"axis\":{ax["axis"]}");
            sb.Append($",\"joyNum\":{ax["joyNum"]}");
            sb.Append("}");
            return sb.ToString();
        }
    }
}
#endif
