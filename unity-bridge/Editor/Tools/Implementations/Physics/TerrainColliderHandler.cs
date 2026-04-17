using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Handles TerrainCollider creation and property read/write.
    /// Automatically links the TerrainData from a sibling Terrain component if present.
    /// TypeKey: "terrain"
    /// </summary>
    public class TerrainColliderHandler : IColliderHandler
    {
        public string TypeKey => "terrain";

        public bool AlreadyExists(UnityEngine.GameObject obj) => obj.GetComponent<TerrainCollider>() != null;

        public Collider AddComponent(UnityEngine.GameObject obj)
        {
            var tc = Undo.AddComponent<TerrainCollider>(obj);
            // Auto-link TerrainData from sibling Terrain component
            if (tc != null)
            {
                UnityEngine.Terrain terrain = obj.GetComponent<UnityEngine.Terrain>();
                if (terrain != null && terrain.terrainData != null)
                    tc.terrainData = terrain.terrainData;
            }
            return tc;
        }

        public void ApplyArgs(Collider collider, Dictionary<string, object> args)
        {
            if (collider is not TerrainCollider tc) return;

            if (args.ContainsKey("isTrigger"))
                collider.isTrigger = ToolUtils.ParseBool(args["isTrigger"]);

            // Manual TerrainData asset path
            if (args.ContainsKey("terrainDataPath") && !string.IsNullOrWhiteSpace(args["terrainDataPath"]?.ToString()))
            {
                string path = args["terrainDataPath"].ToString();
                if (!path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    path = "Assets/" + path;
                var data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (data != null) tc.terrainData = data;
            }
        }

        public void ApplyAutoAlign(Collider collider, Bounds bounds)
        {
            // TerrainCollider is sized by TerrainData — no manual sizing.
            // Auto-link from Terrain component if not already set.
            if (collider is TerrainCollider tc && tc.terrainData == null)
            {
                UnityEngine.Terrain terrain = tc.gameObject.GetComponent<UnityEngine.Terrain>();
                if (terrain != null && terrain.terrainData != null)
                    tc.terrainData = terrain.terrainData;
            }
        }

        public Dictionary<string, object> ReadProperties(Collider collider)
        {
            var props = new Dictionary<string, object>();
            if (collider is not TerrainCollider tc) return props;
            props["terrainDataPath"] = tc.terrainData != null
                ? AssetDatabase.GetAssetPath(tc.terrainData)
                : null;
            props["hasTerrainData"] = tc.terrainData != null;
            return props;
        }
    }
}
