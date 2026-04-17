using System.Collections.Generic;

namespace GladeAgenticAI.Core.Tools
{
    /// <summary>
    /// Interface for all Unity editor tools executed by the AI.
    /// Replaces the monolithic switch statement in ToolExecutor.
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// The unique name of the tool (e.g., "create_primitive", "list_assets").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the tool logic.
        /// </summary>
        /// <param name="args">Dictionary of arguments parsed from the JSON payload.</param>
        /// <returns>A JSON string representing the result.</returns>
        string Execute(Dictionary<string, object> args);
    }
}
