using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

namespace GladeAgenticAI.Services
{
    /// <summary>
    /// Tracks compilation errors and learning patterns to improve AI responses.
    /// Also maintains an in-memory list of errors from the most recent compilation round,
    /// with source code context, so compile_scripts can inject them into AI retry prompts.
    /// </summary>
    [InitializeOnLoad]
    public static class ErrorTracker
    {
        private static readonly string ErrorLogPath = Path.Combine(Application.dataPath, "..", "Library", "GladeAI_ErrorLog.json");
        private const int MaxStoredErrors = 100;
        private const int MaxErrorsPerCompilation = 50;
        private const int SourceContextLines = 10; // lines above/below error

        // In-memory errors from the most recent compilation round.
        // Cleared when a new compilation starts; populated as assemblies finish.
        private static readonly List<CompilationError> _lastCompilationErrors = new List<CompilationError>();

        /// <summary>
        /// Structured compilation error with source code context for AI injection.
        /// </summary>
        public class CompilationError
        {
            public string File;       // Assets-relative path
            public int Line;
            public int Column;
            public string Message;    // e.g. "CS0246: The type 'Rigidbody2D' could not be found"
            public string SourceContext; // ±10 lines around the error, with >>> marker
        }

        static ErrorTracker()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private static void OnCompilationStarted(object context)
        {
            // Clear in-memory errors at the start of each new compile round
            lock (_lastCompilationErrors)
            {
                _lastCompilationErrors.Clear();
            }
        }

        /// <summary>
        /// Returns errors from the most recent compilation round with source context.
        /// Returns an empty list if the last compilation was clean.
        /// </summary>
        public static List<CompilationError> GetLastCompilationErrors()
        {
            lock (_lastCompilationErrors)
            {
                return new List<CompilationError>(_lastCompilationErrors);
            }
        }

        private static string GetAssetsRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return "(unknown)";
            try
            {
                string full = Path.GetFullPath(absolutePath).Replace("\\", "/");
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
                if (full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
                    return full.Substring(root.Length + 1);
            }
            catch { }
            return absolutePath.Replace("\\", "/");
        }

        private static string ExtractSourceContext(string absoluteFilePath, int errorLine)
        {
            try
            {
                if (string.IsNullOrEmpty(absoluteFilePath) || !File.Exists(absoluteFilePath))
                    return null;

                string[] lines = File.ReadAllLines(absoluteFilePath);
                int startLine = Math.Max(0, errorLine - SourceContextLines - 1); // convert to 0-indexed
                int endLine = Math.Min(lines.Length - 1, errorLine + SourceContextLines - 1);

                var sb = new StringBuilder();
                for (int i = startLine; i <= endLine; i++)
                {
                    string marker = (i == errorLine - 1) ? ">>>" : "   ";
                    sb.AppendLine($"{marker} {i + 1,4}: {lines[i]}");
                }
                return sb.ToString().TrimEnd();
            }
            catch
            {
                return null;
            }
        }

        [Serializable]
        private class ErrorLog
        {
            public string scriptPath;
            public string errorMessage;
            public string errorType; // "Compilation", "Runtime", "Validation"
            public string timestamp;
            public string userQuery; // What the user asked for
            public bool wasFixed;
            public string fixApplied; // How it was fixed (if known)
        }

        [Serializable]
        private class ErrorLogCollection
        {
            public List<ErrorLog> errors = new List<ErrorLog>();
        }

        /// <summary>
        /// Logs a compilation error for learning
        /// </summary>
        public static void LogError(string scriptPath, string errorMessage, string userQuery = null, string errorType = "Compilation")
        {
            try
            {
                var logs = LoadErrors();
                
                var error = new ErrorLog
                {
                    scriptPath = scriptPath,
                    errorMessage = errorMessage,
                    errorType = errorType,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    userQuery = userQuery,
                    wasFixed = false
                };

                logs.errors.Add(error);

                // Keep only recent errors
                if (logs.errors.Count > MaxStoredErrors)
                {
                    logs.errors = logs.errors
                        .OrderByDescending(e => e.timestamp)
                        .Take(MaxStoredErrors)
                        .ToList();
                }

                SaveErrors(logs);
                Debug.Log($"[ErrorTracker] Logged error for {scriptPath}: {errorMessage.Substring(0, Mathf.Min(100, errorMessage.Length))}...");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ErrorTracker] Failed to log error: {e.Message}");
            }
        }

        /// <summary>
        /// Marks an error as fixed
        /// </summary>
        public static void MarkFixed(string scriptPath, string fixDescription = null)
        {
            try
            {
                var logs = LoadErrors();
                var recentErrors = logs.errors
                    .Where(e => e.scriptPath == scriptPath && !e.wasFixed)
                    .OrderByDescending(e => e.timestamp)
                    .Take(1)
                    .ToList();

                foreach (var error in recentErrors)
                {
                    error.wasFixed = true;
                    error.fixApplied = fixDescription;
                }

                SaveErrors(logs);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ErrorTracker] Failed to mark error as fixed: {e.Message}");
            }
        }

        /// <summary>
        /// Gets recent errors to include in RAG context
        /// </summary>
        public static string GetRecentErrorsContext(int maxErrors = 10)
        {
            try
            {
                var logs = LoadErrors();
                var recentErrors = logs.errors
                    .Where(e => !e.wasFixed) // Only unfixed errors
                    .OrderByDescending(e => e.timestamp)
                    .Take(maxErrors)
                    .ToList();

                if (recentErrors.Count == 0)
                    return "";

                var context = new System.Text.StringBuilder();
                context.AppendLine("## Recent Errors to Avoid");
                context.AppendLine("These errors occurred recently. Avoid repeating them:");
                context.AppendLine();

                foreach (var error in recentErrors)
                {
                    context.AppendLine($"### {error.scriptPath}");
                    if (!string.IsNullOrEmpty(error.userQuery))
                        context.AppendLine($"User Request: {error.userQuery}");
                    context.AppendLine($"Error: {error.errorMessage}");
                    context.AppendLine($"Time: {error.timestamp}");
                    context.AppendLine();
                }

                return context.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets patterns from fixed errors (what worked)
        /// </summary>
        public static string GetFixedErrorsContext(int maxErrors = 5)
        {
            try
            {
                var logs = LoadErrors();
                var fixedErrors = logs.errors
                    .Where(e => e.wasFixed && !string.IsNullOrEmpty(e.fixApplied))
                    .OrderByDescending(e => e.timestamp)
                    .Take(maxErrors)
                    .ToList();

                if (fixedErrors.Count == 0)
                    return "";

                var context = new System.Text.StringBuilder();
                context.AppendLine("## Successful Fixes (Learn from these)");
                context.AppendLine("These errors were fixed successfully:");
                context.AppendLine();

                foreach (var error in fixedErrors)
                {
                    context.AppendLine($"### {error.scriptPath}");
                    context.AppendLine($"Original Error: {error.errorMessage}");
                    context.AppendLine($"Fix Applied: {error.fixApplied}");
                    context.AppendLine();
                }

                return context.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Gets all error context for RAG
        /// </summary>
        public static string GetAllErrorContext()
        {
            var context = new System.Text.StringBuilder();
            
            string recent = GetRecentErrorsContext(10);
            if (!string.IsNullOrEmpty(recent))
            {
                context.AppendLine(recent);
                context.AppendLine();
            }

            string fixedErrors = GetFixedErrorsContext(5);
            if (!string.IsNullOrEmpty(fixedErrors))
            {
                context.AppendLine(fixedErrors);
            }

            return context.ToString();
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
                return;

            int loggedCount = 0;
            foreach (var compilerMessage in messages)
            {
                if (compilerMessage.type != CompilerMessageType.Error)
                    continue;

                if (loggedCount >= MaxErrorsPerCompilation)
                    break;

                string normalizedPath = NormalizeScriptPath(compilerMessage.file);
                string formattedMessage = $"({compilerMessage.line},{compilerMessage.column}) {compilerMessage.message}";
                LogError(normalizedPath, formattedMessage, errorType: "Compilation");

                // Also accumulate in-memory for AI injection via compile_scripts
                var error = new CompilationError
                {
                    File = GetAssetsRelativePath(compilerMessage.file),
                    Line = compilerMessage.line,
                    Column = compilerMessage.column,
                    Message = compilerMessage.message,
                    SourceContext = ExtractSourceContext(compilerMessage.file, compilerMessage.line),
                };
                lock (_lastCompilationErrors)
                {
                    _lastCompilationErrors.Add(error);
                }

                loggedCount++;
            }
        }

        private static string NormalizeScriptPath(string scriptPath)
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
                return "(unknown)";

            try
            {
                string fullScriptPath = Path.GetFullPath(scriptPath).Replace("\\", "/");
                string fullProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
                if (fullScriptPath.StartsWith(fullProjectRoot + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return fullScriptPath.Substring(fullProjectRoot.Length + 1);
                }
            }
            catch
            {
                // Fall back to the original path if normalization fails.
            }

            return scriptPath.Replace("\\", "/");
        }

        private static ErrorLogCollection LoadErrors()
        {
            try
            {
                if (File.Exists(ErrorLogPath))
                {
                    string json = File.ReadAllText(ErrorLogPath);
                    return JsonUtility.FromJson<ErrorLogCollection>(json) ?? new ErrorLogCollection();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ErrorTracker] Failed to load errors: {e.Message}");
            }

            return new ErrorLogCollection();
        }

        private static void SaveErrors(ErrorLogCollection logs)
        {
            try
            {
                string directory = Path.GetDirectoryName(ErrorLogPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(logs, true);
                File.WriteAllText(ErrorLogPath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ErrorTracker] Failed to save errors: {e.Message}");
            }
        }

        /// <summary>
        /// Clears all error logs (for testing or reset)
        /// </summary>
        [MenuItem("Window/GladeKit/Clear Error Log")]
        public static void ClearErrorLog()
        {
            if (EditorUtility.DisplayDialog("Clear Error Log", 
                "Are you sure you want to clear all error logs? This cannot be undone.", 
                "Clear", "Cancel"))
            {
                try
                {
                    if (File.Exists(ErrorLogPath))
                        File.Delete(ErrorLogPath);
                    
                    Debug.Log("[ErrorTracker] Error log cleared");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ErrorTracker] Failed to clear error log: {e.Message}");
                }
            }
        }
    }
}

