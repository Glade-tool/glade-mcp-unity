using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Lighting
{
    public class SetRenderSettingsTool : ITool
    {
        public string Name => "set_render_settings";

        public string Execute(Dictionary<string, object> args)
        {
            // Fog settings
            if (args.ContainsKey("fogEnabled"))
            {
                bool fogEnabled = false;
                if (args["fogEnabled"] is bool b) fogEnabled = b;
                else bool.TryParse(args["fogEnabled"].ToString(), out fogEnabled);
                RenderSettings.fog = fogEnabled;
            }
            
            if (args.ContainsKey("fogColor"))
            {
                RenderSettings.fogColor = ToolUtils.ParseColor(args["fogColor"].ToString());
            }
            
            if (args.ContainsKey("fogMode"))
            {
                string fogModeStr = args["fogMode"].ToString().ToLower();
                RenderSettings.fogMode = fogModeStr switch
                {
                    "linear" => FogMode.Linear,
                    "exponential" => FogMode.Exponential,
                    "exponentialsquared" => FogMode.ExponentialSquared,
                    _ => FogMode.Linear
                };
            }
            
            if (args.ContainsKey("fogDensity"))
            {
                float density = RenderSettings.fogDensity;
                if (args["fogDensity"] is float f) density = f;
                else float.TryParse(args["fogDensity"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out density);
                RenderSettings.fogDensity = density;
            }
            
            if (args.ContainsKey("fogStartDistance"))
            {
                float startDist = RenderSettings.fogStartDistance;
                if (args["fogStartDistance"] is float f) startDist = f;
                else float.TryParse(args["fogStartDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out startDist);
                RenderSettings.fogStartDistance = startDist;
            }
            
            if (args.ContainsKey("fogEndDistance"))
            {
                float endDist = RenderSettings.fogEndDistance;
                if (args["fogEndDistance"] is float f) endDist = f;
                else float.TryParse(args["fogEndDistance"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out endDist);
                RenderSettings.fogEndDistance = endDist;
            }
            
            // Ambient settings
#if GLADE_SRP
            if (args.ContainsKey("ambientMode"))
            {
                string ambientModeStr = args["ambientMode"].ToString().ToLower();
                RenderSettings.ambientMode = ambientModeStr switch
                {
                    "skybox" => UnityEngine.Rendering.AmbientMode.Skybox,
                    "trilight" => UnityEngine.Rendering.AmbientMode.Trilight,
                    "flat" => UnityEngine.Rendering.AmbientMode.Flat,
                    _ => UnityEngine.Rendering.AmbientMode.Skybox
                };
            }
#endif
            
            if (args.ContainsKey("ambientColor"))
            {
                RenderSettings.ambientLight = ToolUtils.ParseColor(args["ambientColor"].ToString());
            }
            
            if (args.ContainsKey("ambientIntensity"))
            {
                float intensity = RenderSettings.ambientIntensity;
                if (args["ambientIntensity"] is float f) intensity = f;
                else float.TryParse(args["ambientIntensity"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out intensity);
                RenderSettings.ambientIntensity = intensity;
            }
            
            // Skybox
            if (args.ContainsKey("skyboxMaterial"))
            {
                string skyboxPath = args["skyboxMaterial"].ToString();
                if (!skyboxPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    skyboxPath = "Assets/" + skyboxPath;
                    
                Material skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                if (skybox != null)
                {
                    RenderSettings.skybox = skybox;
                }
                else
                {
                    return ToolUtils.CreateErrorResponse($"Skybox material not found at '{skyboxPath}'");
                }
            }
            
            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            return ToolUtils.CreateSuccessResponse("Updated render settings");
        }
    }
}
