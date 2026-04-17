using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GladeAgenticAI.Core.Tools;

namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class CreateTerrainTool : ITool
    {
        public string Name => "create_terrain";

        public string Execute(Dictionary<string, object> args)
        {
            string dataPath = args.ContainsKey("terrainDataPath") ? args["terrainDataPath"].ToString() : "Assets/TerrainData/TerrainData.asset";
            string name = args.ContainsKey("name") ? args["name"].ToString() : "Terrain";
            Vector3 position = args.ContainsKey("position") ? ToolUtils.ParseVector3(args["position"].ToString()) : Vector3.zero;
            Vector3 size = args.ContainsKey("size") ? ToolUtils.ParseVector3(args["size"].ToString()) : new Vector3(1000, 600, 1000);

            if (!dataPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                dataPath = "Assets/" + dataPath;

            string dir = System.IO.Path.GetDirectoryName(dataPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                ToolUtils.EnsureAssetFolder(dir);
            }

            TerrainData data = new TerrainData();
            data.size = size;
            AssetDatabase.CreateAsset(data, dataPath);
            AssetDatabase.SaveAssets();

            UnityEngine.GameObject terrainObj = UnityEngine.Terrain.CreateTerrainGameObject(data);
            terrainObj.name = name;
            terrainObj.transform.position = position;

            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(terrainObj) },
                { "terrainDataPath", dataPath }
            };
            
            return ToolUtils.CreateSuccessResponse($"Created Terrain '{name}'", extras);
        }
    }
}
