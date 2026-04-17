using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetTerrainPropertiesTool : ITool
    {
        public string Name => "set_terrain_properties";

        public string Execute(Dictionary<string, object> args)
        {
            UnityEngine.Terrain terrain = null;
            if (args.ContainsKey("gameObjectPath"))
            {
                string path = args["gameObjectPath"].ToString();
                UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(path);
                if (obj != null) terrain = obj.GetComponent<UnityEngine.Terrain>();
            }

            TerrainData data = null;
            if (terrain != null) data = terrain.terrainData;

            if (data == null && args.ContainsKey("terrainDataPath"))
            {
                string dataPath = args["terrainDataPath"].ToString();
                if (!dataPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    dataPath = "Assets/" + dataPath;
                data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
            }

            if (data == null)
            {
                return ToolUtils.CreateErrorResponse("TerrainData not found");
            }

            if (args.ContainsKey("size"))
            {
                data.size = ToolUtils.ParseVector3(args["size"].ToString());
            }
            if (args.ContainsKey("heightmapResolution"))
            {
                if (int.TryParse(args["heightmapResolution"].ToString(), out int res))
                    data.heightmapResolution = res;
            }
            if (args.ContainsKey("baseMapResolution"))
            {
                if (int.TryParse(args["baseMapResolution"].ToString(), out int res))
                    data.baseMapResolution = res;
            }
            if (args.ContainsKey("detailResolution"))
            {
                if (int.TryParse(args["detailResolution"].ToString(), out int res))
                    data.SetDetailResolution(res, 16);
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            
            return ToolUtils.CreateSuccessResponse("Updated Terrain properties");
        }
    }
}
