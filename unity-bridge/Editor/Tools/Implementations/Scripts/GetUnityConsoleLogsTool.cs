using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GladeAgenticAI.Core.Tools;
using UnityEngine;
using UnityEditor;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    /// <summary>
    /// READ tool: retrieves Unity Editor Console window entries (errors, warnings, logs)
    /// via UnityEditorInternal.LogEntries. No Undo. Does not use Editor.log file.
    /// </summary>
    public class GetUnityConsoleLogsTool : ITool
    {
        private const int MaxEntriesCap = 2000;

        public string Name => "get_unity_console_logs";

        public string Execute(Dictionary<string, object> args)
        {
            var (entries, error) = TryGetLogEntriesViaReflection(MaxEntriesCap);
            if (error != null)
                return ToolUtils.CreateErrorResponse(error);
            if (entries != null)
            {
                var result = new Dictionary<string, object>
                {
                    ["entryCount"] = entries.Count,
                    ["entries"] = entries
                };
                return ToolUtils.SerializeDictToJson(result);
            }
            return ToolUtils.CreateErrorResponse("Could not read Unity Console (LogEntries API not available for this Unity version).");
        }

        /// <summary>
        /// Uses UnityEditorInternal.LogEntries via reflection. Returns (entries, null) on success,
        /// (null, null) if API not found, (null, errorMessage) on failure.
        /// </summary>
        private static (List<string> entries, string error) TryGetLogEntriesViaReflection(int maxEntries)
        {
            const BindingFlags staticAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            const BindingFlags instanceAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            try
            {
                Type logEntriesType = ResolveLogEntriesType();
                Type logEntryType = ResolveLogEntryType();
                if (logEntriesType == null)
                    return (null, "Unity Console API not found: LogEntries type missing. This Unity version may use a different internal API.");
                if (logEntryType == null)
                    return (null, "Unity Console API not found: LogEntry type missing.");

                MethodInfo getCountMethod = logEntriesType.GetMethod("GetCount", staticAny);
                MethodInfo getEntryMethod = ResolveGetEntryInternal(logEntriesType, logEntryType, staticAny);
                if (getCountMethod == null)
                    return (null, "Unity Console API not found: GetCount method missing on LogEntries.");
                if (getEntryMethod == null)
                    return (null, "Unity Console API not found: GetEntryInternal method missing or signature not matched.");

                int totalCount = (int)getCountMethod.Invoke(null, null);
                if (totalCount <= 0)
                {
                    return (new List<string> { "(Console is empty.)" }, null);
                }

                var entries = new List<string>();
                int startIndex = Math.Max(0, totalCount - maxEntries);
                int take = Math.Min(maxEntries, totalCount);

                for (int i = startIndex + take - 1; i >= startIndex && entries.Count < maxEntries; i--)
                {
                    object entryObj = Activator.CreateInstance(logEntryType);
                    object[] invokeArgs = new object[] { i, entryObj };
                    bool ok = (bool)getEntryMethod.Invoke(null, invokeArgs);
                    if (!ok) continue;
                    entryObj = invokeArgs[1];

                    string message = GetReflectionFieldString(logEntryType, entryObj, "message")
                        ?? GetReflectionFieldString(logEntryType, entryObj, "condition") ?? "";
                    string stackTrace = GetReflectionFieldString(logEntryType, entryObj, "stackTrace") ?? "";
                    int mode = 0;
                    var modeField = logEntryType.GetField("mode", instanceAny);
                    if (modeField != null && modeField.FieldType == typeof(int))
                        mode = (int)(modeField.GetValue(entryObj) ?? 0);

                    string typeLabel = "Log";
                    if ((mode & 1) != 0) typeLabel = "Error";
                    else if ((mode & 2) != 0) typeLabel = "Warning";
                    string line = $"[{typeLabel}] {message}";
                    if (!string.IsNullOrEmpty(stackTrace))
                        line += "\n" + stackTrace;
                    entries.Add(line);
                }

                return (entries, null);
            }
            catch (TargetInvocationException tex)
            {
                string msg = tex.InnerException?.Message ?? tex.Message;
                return (null, "Unity Console API error: " + msg);
            }
            catch (Exception ex)
            {
                return (null, "Unity Console API error: " + ex.Message);
            }
        }

        private static Type ResolveLogEntriesType()
        {
            var editorAssembly = typeof(AssetDatabase).Assembly;
            Type t = editorAssembly.GetType("UnityEditorInternal.LogEntries");
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType("UnityEditorInternal.LogEntries");
                if (t != null) return t;
            }
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    string name = asm.GetName().Name;
                    if (!name.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) && !name.Equals("UnityEditor", StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (Type type in asm.GetExportedTypes().Concat(asm.GetTypes()))
                    {
                        if (type.Name == "LogEntries" && type.GetMethod("GetCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
                            return type;
                    }
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private static Type ResolveLogEntryType()
        {
            var editorAssembly = typeof(AssetDatabase).Assembly;
            Type t = editorAssembly.GetType("UnityEditorInternal.LogEntry") ?? editorAssembly.GetType("UnityEditor.LogEntry");
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType("UnityEditorInternal.LogEntry") ?? asm.GetType("UnityEditor.LogEntry");
                if (t != null) return t;
            }
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    foreach (Type type in asm.GetExportedTypes().Concat(asm.GetTypes()))
                    {
                        if (type.Name == "LogEntry" && type.GetField("condition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null)
                            return type;
                    }
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private static MethodInfo ResolveGetEntryInternal(Type logEntriesType, Type logEntryType, BindingFlags staticAny)
        {
            MethodInfo m = logEntriesType.GetMethod("GetEntryInternal", staticAny, null, new Type[] { typeof(int), logEntryType }, null);
            if (m != null) return m;
            m = logEntriesType.GetMethod("GetEntryInternal", staticAny);
            if (m != null)
            {
                var ps = m.GetParameters();
                if (ps.Length == 2 && ps[0].ParameterType == typeof(int))
                    return m;
            }
            foreach (var method in logEntriesType.GetMethods(staticAny))
            {
                if (method.Name != "GetEntryInternal") continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 2) continue;
                if (parameters[0].ParameterType != typeof(int)) continue;
                Type second = parameters[1].ParameterType;
                if (second == logEntryType) return method;
                if (second.IsByRef && second.GetElementType() == logEntryType) return method;
            }
            return null;
        }

        private static string GetReflectionFieldString(Type type, object instance, string fieldName)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                var value = field.GetValue(instance);
                return value?.ToString();
            }
            return null;
        }
    }
}
