using System.Collections.Generic;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    public class SetCollisionMatrixTool : ITool
    {
        public string Name => "set_collision_matrix";

        public string Execute(Dictionary<string, object> args)
        {
            string layer1Name = args.ContainsKey("layer1") ? args["layer1"].ToString() : "";
            string layer2Name = args.ContainsKey("layer2") ? args["layer2"].ToString() : "";

            if (string.IsNullOrEmpty(layer1Name))
                return ToolUtils.CreateErrorResponse("layer1 is required (layer name or index)");
            if (string.IsNullOrEmpty(layer2Name))
                return ToolUtils.CreateErrorResponse("layer2 is required (layer name or index)");

            int layer1 = ResolveLayer(layer1Name);
            int layer2 = ResolveLayer(layer2Name);

            if (layer1 < 0)
                return ToolUtils.CreateErrorResponse($"Layer '{layer1Name}' not found");
            if (layer2 < 0)
                return ToolUtils.CreateErrorResponse($"Layer '{layer2Name}' not found");

            bool ignore = true;
            if (args.ContainsKey("ignore") && bool.TryParse(args["ignore"].ToString(), out bool ig))
                ignore = ig;

            UnityEngine.Physics.IgnoreLayerCollision(layer1, layer2, ignore);

            string l1Display = LayerMask.LayerToName(layer1);
            string l2Display = LayerMask.LayerToName(layer2);
            if (string.IsNullOrEmpty(l1Display)) l1Display = layer1.ToString();
            if (string.IsNullOrEmpty(l2Display)) l2Display = layer2.ToString();

            var extras = new Dictionary<string, object>
            {
                { "layer1", l1Display },
                { "layer2", l2Display },
                { "ignoreCollision", ignore }
            };

            string action = ignore ? "now ignoring" : "now colliding";
            return ToolUtils.CreateSuccessResponse($"Layers '{l1Display}' and '{l2Display}' are {action}", extras);
        }

        private static int ResolveLayer(string nameOrIndex)
        {
            if (int.TryParse(nameOrIndex, out int index) && index >= 0 && index < 32)
                return index;

            int layer = LayerMask.NameToLayer(nameOrIndex);
            return layer; // returns -1 if not found
        }
    }
}
