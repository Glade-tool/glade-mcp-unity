using System.Collections.Generic;
using UnityEditor;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Assets
{
    public class RefreshAssetDatabaseTool : ITool
    {
        public string Name => "refresh_asset_database";

        public string Execute(Dictionary<string, object> args)
        {
            AssetDatabase.Refresh();
            return ToolUtils.CreateSuccessResponse("AssetDatabase refreshed");
        }
    }
}
