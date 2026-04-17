using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    public class CreateReflectionProbeTool : ITool
    {
        public string Name => "create_reflection_probe";

        public string Execute(Dictionary<string, object> args)
        {
            string name = args.ContainsKey("name") ? args["name"].ToString() : "Reflection Probe";
            
            UnityEngine.GameObject probeObj = new UnityEngine.GameObject(name);
            ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
            
            // Set position
            if (args.ContainsKey("position"))
            {
                probeObj.transform.position = ToolUtils.ParseVector3(args["position"].ToString());
            }
            
            // Set size
            if (args.ContainsKey("size"))
            {
                probe.size = ToolUtils.ParseVector3(args["size"].ToString());
            }
            else
            {
                probe.size = new Vector3(10, 10, 10);
            }
            
            // Set mode
#if GLADE_SRP
            if (args.ContainsKey("mode"))
            {
                string modeStr = args["mode"].ToString().ToLower();
                probe.mode = modeStr switch
                {
                    "realtime" => UnityEngine.Rendering.ReflectionProbeMode.Realtime,
                    "custom" => UnityEngine.Rendering.ReflectionProbeMode.Custom,
                    _ => UnityEngine.Rendering.ReflectionProbeMode.Baked
                };
            }
#endif
            
            // Set resolution
            if (args.ContainsKey("resolution"))
            {
                int resolution = 128;
                if (args["resolution"] is int i) resolution = i;
                else if (args["resolution"] is float f) resolution = (int)f;
                else int.TryParse(args["resolution"].ToString(), out resolution);
                probe.resolution = resolution;
            }
            
            // Set intensity
            if (args.ContainsKey("intensity"))
            {
                float intensity = 1f;
                if (args["intensity"] is float f) intensity = f;
                else float.TryParse(args["intensity"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out intensity);
                probe.intensity = intensity;
            }
            
            // Set box projection
            if (args.ContainsKey("boxProjection"))
            {
                bool boxProjection = false;
                if (args["boxProjection"] is bool b) boxProjection = b;
                else bool.TryParse(args["boxProjection"].ToString(), out boxProjection);
                probe.boxProjection = boxProjection;
            }
            
            Undo.RegisterCreatedObjectUndo(probeObj, $"Create Reflection Probe: {name}");
            
            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(probeObj) }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Reflection Probe '{name}'", extras);
        }
    }
}
