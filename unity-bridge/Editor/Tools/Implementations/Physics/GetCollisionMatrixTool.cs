using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class GetCollisionMatrixTool : ITool
    {
        public string Name => "get_collision_matrix";

        public string Execute(Dictionary<string, object> args)
        {
            // Collect all named layers
            var layers = new Dictionary<string, object>();
            var collisionPairs = new List<string>();

            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName)) continue;

                layers[i.ToString()] = layerName;

                // Check collision pairs with other named layers
                for (int j = i; j < 32; j++)
                {
                    string otherName = LayerMask.LayerToName(j);
                    if (string.IsNullOrEmpty(otherName)) continue;

                    bool ignores = UnityEngine.Physics.GetIgnoreLayerCollision(i, j);
                    if (ignores)
                        collisionPairs.Add($"{layerName}({i}) ignores {otherName}({j})");
                }
            }

            var extras = new Dictionary<string, object>
            {
                { "layers", ToolUtils.SerializeDictToJson(layers) },
                { "ignoredPairs", collisionPairs.Count > 0 ? string.Join("; ", collisionPairs) : "none" },
                { "ignoredPairCount", collisionPairs.Count }
            };

            return ToolUtils.CreateSuccessResponse("Layer collision matrix retrieved", extras);
        }
    }
}
