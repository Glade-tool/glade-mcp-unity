using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

#if ENABLE_LEGACY_INPUT_MANAGER
namespace GladeAgenticAI.Core.Tools.Implementations.InputLegacy
{
    public class EnsureLegacyInputAxesTool : ITool
    {
        public string Name => "ensure_legacy_input_axes";

        public string Execute(Dictionary<string, object> args)
        {
            if (!args.ContainsKey("axes") || !(args["axes"] is List<object> axesList))
                return ToolUtils.CreateErrorResponse("axes (array of axis objects with at least 'name') is required.");

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (assets == null || assets.Length == 0)
                return ToolUtils.CreateErrorResponse("ProjectSettings/InputManager.asset not found.");

            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty axesProp = so.FindProperty("m_Axes");
            if (axesProp == null || !axesProp.isArray)
                return ToolUtils.CreateErrorResponse("InputManager m_Axes not found.");

            var createdOrUpdated = new List<string>();
            foreach (var item in axesList)
            {
                if (!(item is Dictionary<string, object> axisDict))
                    continue;
                string name = axisDict.ContainsKey("name") ? axisDict["name"]?.ToString() : null;
                if (string.IsNullOrEmpty(name))
                    continue;

                int existingIndex = FindAxisIndex(axesProp, name);
                SerializedProperty axisProp;
                if (existingIndex >= 0)
                {
                    axisProp = axesProp.GetArrayElementAtIndex(existingIndex);
                    ApplyAxisProperties(axisProp, axisDict);
                    createdOrUpdated.Add(name);
                }
                else
                {
                    axesProp.arraySize++;
                    so.ApplyModifiedProperties();
                    axisProp = axesProp.GetArrayElementAtIndex(axesProp.arraySize - 1);
                    SetAxisDefaults(axisProp);
                    axisProp.FindPropertyRelative("m_Name").stringValue = name;
                    ApplyAxisProperties(axisProp, axisDict);
                    createdOrUpdated.Add(name);
                }
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            var extras = new Dictionary<string, object>
            {
                { "createdOrUpdated", createdOrUpdated }
            };
            return ToolUtils.CreateSuccessResponse($"Ensured {createdOrUpdated.Count} legacy input axis/axes.", extras);
        }

        private static int FindAxisIndex(SerializedProperty axesProp, string name)
        {
            for (int i = 0; i < axesProp.arraySize; i++)
            {
                var el = axesProp.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("m_Name").stringValue == name)
                    return i;
            }
            return -1;
        }

        private static void SetAxisDefaults(SerializedProperty axisProp)
        {
            axisProp.FindPropertyRelative("descriptiveName").stringValue = "";
            axisProp.FindPropertyRelative("descriptiveNegativeName").stringValue = "";
            axisProp.FindPropertyRelative("negativeButton").stringValue = "";
            axisProp.FindPropertyRelative("positiveButton").stringValue = "";
            axisProp.FindPropertyRelative("altNegativeButton").stringValue = "";
            axisProp.FindPropertyRelative("altPositiveButton").stringValue = "";
            axisProp.FindPropertyRelative("gravity").floatValue = 3f;
            axisProp.FindPropertyRelative("dead").floatValue = 0.001f;
            axisProp.FindPropertyRelative("sensitivity").floatValue = 3f;
            axisProp.FindPropertyRelative("snap").intValue = 1;
            axisProp.FindPropertyRelative("invert").intValue = 0;
            axisProp.FindPropertyRelative("type").intValue = 0;
            axisProp.FindPropertyRelative("axis").intValue = 0;
            axisProp.FindPropertyRelative("joyNum").intValue = 0;
        }

        private static void ApplyAxisProperties(SerializedProperty axisProp, Dictionary<string, object> axisDict)
        {
            if (axisDict.ContainsKey("positiveButton"))
                axisProp.FindPropertyRelative("positiveButton").stringValue = axisDict["positiveButton"]?.ToString() ?? "";
            if (axisDict.ContainsKey("negativeButton"))
                axisProp.FindPropertyRelative("negativeButton").stringValue = axisDict["negativeButton"]?.ToString() ?? "";
            if (axisDict.ContainsKey("altPositiveButton"))
                axisProp.FindPropertyRelative("altPositiveButton").stringValue = axisDict["altPositiveButton"]?.ToString() ?? "";
            if (axisDict.ContainsKey("altNegativeButton"))
                axisProp.FindPropertyRelative("altNegativeButton").stringValue = axisDict["altNegativeButton"]?.ToString() ?? "";
            if (axisDict.ContainsKey("gravity") && ParseFloat(axisDict["gravity"], out float g))
                axisProp.FindPropertyRelative("gravity").floatValue = g;
            if (axisDict.ContainsKey("dead") && ParseFloat(axisDict["dead"], out float d))
                axisProp.FindPropertyRelative("dead").floatValue = d;
            if (axisDict.ContainsKey("sensitivity") && ParseFloat(axisDict["sensitivity"], out float sens))
                axisProp.FindPropertyRelative("sensitivity").floatValue = sens;
            if (axisDict.ContainsKey("snap"))
                axisProp.FindPropertyRelative("snap").intValue = ParseBool(axisDict["snap"]) ? 1 : 0;
            if (axisDict.ContainsKey("invert"))
                axisProp.FindPropertyRelative("invert").intValue = ParseBool(axisDict["invert"]) ? 1 : 0;
            if (axisDict.ContainsKey("type") && ParseInt(axisDict["type"], out int t))
                axisProp.FindPropertyRelative("type").intValue = t;
            if (axisDict.ContainsKey("axis") && ParseInt(axisDict["axis"], out int a))
                axisProp.FindPropertyRelative("axis").intValue = a;
            if (axisDict.ContainsKey("joyNum") && ParseInt(axisDict["joyNum"], out int j))
                axisProp.FindPropertyRelative("joyNum").intValue = j;
        }

        private static bool ParseFloat(object o, out float v)
        {
            v = 0f;
            if (o == null) return false;
            if (o is float f) { v = f; return true; }
            if (o is double d) { v = (float)d; return true; }
            if (o is int i) { v = i; return true; }
            return float.TryParse(o.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v);
        }

        private static bool ParseInt(object o, out int v)
        {
            v = 0;
            if (o == null) return false;
            if (o is int i) { v = i; return true; }
            if (o is long l) { v = (int)l; return true; }
            if (o is float f) { v = (int)f; return true; }
            return int.TryParse(o.ToString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out v);
        }

        private static bool ParseBool(object o)
        {
            if (o == null) return false;
            if (o is bool b) return b;
            if (o is string s) return bool.TryParse(s, out var b2) && b2;
            return false;
        }
    }
}
#endif
