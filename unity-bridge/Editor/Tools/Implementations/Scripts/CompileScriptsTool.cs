using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using GladeAgenticAI.Core.Tools;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.Implementations.Scripts
{
    public class CompileScriptsTool : ITool
    {
        public string Name => "compile_scripts";

        public string Execute(Dictionary<string, object> args)
        {
            bool isCompiling = EditorApplication.isCompiling;

            if (isCompiling)
            {
                var extras = new Dictionary<string, object>
                {
                    { "isCompiling", true },
                    { "status", "compiling" }
                };
                return ToolUtils.CreateSuccessResponse(
                    "Unity is still compiling scripts. Call compile_scripts again to check when it finishes. Do NOT call add_component until compilation is complete.",
                    extras);
            }

            // Not compiling — trigger a refresh in case new files haven't been picked up
            AssetDatabase.Refresh(ImportAssetOptions.Default);

            // Check again after refresh
            if (EditorApplication.isCompiling)
            {
                var extras = new Dictionary<string, object>
                {
                    { "isCompiling", true },
                    { "status", "compiling" }
                };
                return ToolUtils.CreateSuccessResponse(
                    "Compilation started after refresh. Call compile_scripts again to check when it finishes.",
                    extras);
            }

            // Compilation is idle — check for errors from the last round
            var errors = ErrorTracker.GetLastCompilationErrors();
            if (errors.Count > 0)
            {
                var message = BuildErrorMessage(errors);
                var errorExtras = new Dictionary<string, object>
                {
                    { "isCompiling", false },
                    { "status", "idle" },
                    { "hasErrors", true },
                    { "errorCount", errors.Count }
                };
                return ToolUtils.CreateSuccessResponse(message, errorExtras);
            }

            var doneExtras = new Dictionary<string, object>
            {
                { "isCompiling", false },
                { "status", "idle" },
                { "hasErrors", false }
            };
            return ToolUtils.CreateSuccessResponse(
                "Compilation complete. All script types are ready to use with add_component.",
                doneExtras);
        }

        private static string BuildErrorMessage(List<ErrorTracker.CompilationError> errors)
        {
            const int maxErrors = 10; // cap to avoid extremely long tool responses
            var sb = new StringBuilder();
            int shown = System.Math.Min(errors.Count, maxErrors);

            sb.AppendLine($"Compilation finished with {errors.Count} error(s). Fix these errors before calling add_component or proceeding.");
            sb.AppendLine();

            for (int i = 0; i < shown; i++)
            {
                var err = errors[i];
                sb.AppendLine($"Error {i + 1}: {err.File} (line {err.Line}, col {err.Column})");
                sb.AppendLine(err.Message);

                if (!string.IsNullOrEmpty(err.SourceContext))
                {
                    sb.AppendLine();
                    sb.AppendLine("Source context:");
                    sb.AppendLine(err.SourceContext);
                }

                if (i < shown - 1)
                    sb.AppendLine();
            }

            if (errors.Count > maxErrors)
                sb.AppendLine($"... and {errors.Count - maxErrors} more error(s). Fix the above first.");

            return sb.ToString().TrimEnd();
        }
    }
}
