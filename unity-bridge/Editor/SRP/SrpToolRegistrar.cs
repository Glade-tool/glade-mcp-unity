using GladeAgenticAI.Core.Tools.Implementations.Camera;
using GladeAgenticAI.Services;

namespace GladeAgenticAI.Core.Tools.SRP
{
    /// <summary>
    /// Auto-registers SRP-dependent tools with ToolExecutor.
    /// Lives in the GladeKit.Bridge.SRP assembly which only compiles when GLADE_SRP is defined
    /// (i.e. when URP or HDRP is installed).
    /// </summary>
    public static class SrpToolRegistrar
    {
        [UnityEditor.InitializeOnLoadMethod]
        private static void Register()
        {
            ToolExecutor.RegisterExternal(new SetPostProcessingTool());
        }
    }
}
