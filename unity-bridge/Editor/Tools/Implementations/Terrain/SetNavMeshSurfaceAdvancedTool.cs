using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if GLADE_AI_NAVIGATION
using Unity.AI.Navigation;
#endif
using UnityEngine.AI;
using GladeAgenticAI.Core.Tools;

#if GLADE_AI_NAVIGATION
namespace GladeAgenticAI.Core.Tools.Implementations.Terrain
{
    public class SetNavMeshSurfaceAdvancedTool : ITool
    {
        public string Name => "set_navmesh_surface_advanced";

        public string Execute(Dictionary<string, object> args)
        {
            string gameObjectPath = args.ContainsKey("gameObjectPath") ? args["gameObjectPath"].ToString() : "";
            if (string.IsNullOrEmpty(gameObjectPath))
            {
                return ToolUtils.CreateErrorResponse("gameObjectPath is required");
            }

            UnityEngine.GameObject obj = ToolUtils.FindGameObjectByPath(gameObjectPath);
            if (obj == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' not found");
            }

            var surface = obj.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                return ToolUtils.CreateErrorResponse($"GameObject '{gameObjectPath}' does not have a NavMeshSurface component. Use create_navmesh_surface first.");
            }

            // Use SerializedObject to access build settings properties that aren't directly exposed
            var serializedObject = new SerializedObject(surface);
            int agentTypeID = 0;
            bool buildHeightMesh = false;

            if (args.ContainsKey("agentTypeID"))
            {
                if (int.TryParse(args["agentTypeID"].ToString(), out int agentTypeIDValue))
                {
                    var settings = NavMesh.GetSettingsByID(agentTypeIDValue);
                    if (settings.agentTypeID == agentTypeIDValue)
                    {
                        var agentTypeIDProp = serializedObject.FindProperty("m_AgentTypeID");
                        if (agentTypeIDProp != null)
                        {
                            agentTypeIDProp.intValue = agentTypeIDValue;
                            agentTypeID = agentTypeIDValue;
                        }
                    }
                }
            }

            if (args.ContainsKey("buildHeightMesh"))
            {
                if (args["buildHeightMesh"] is bool b)
                {
                    var buildHeightMeshProp = serializedObject.FindProperty("m_BuildHeightMesh");
                    if (buildHeightMeshProp != null)
                    {
                        buildHeightMeshProp.boolValue = b;
                        buildHeightMesh = b;
                    }
                }
                else if (bool.TryParse(args["buildHeightMesh"].ToString(), out bool buildHeight))
                {
                    var buildHeightMeshProp = serializedObject.FindProperty("m_BuildHeightMesh");
                    if (buildHeightMeshProp != null)
                    {
                        buildHeightMeshProp.boolValue = buildHeight;
                        buildHeightMesh = buildHeight;
                    }
                }
            }

            if (args.ContainsKey("voxelSize"))
            {
                if (float.TryParse(args["voxelSize"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float voxelSize))
                {
                    var overrideVoxelSizeProp = serializedObject.FindProperty("m_OverrideVoxelSize");
                    var voxelSizeProp = serializedObject.FindProperty("m_VoxelSize");
                    if (overrideVoxelSizeProp != null)
                        overrideVoxelSizeProp.boolValue = true;
                    if (voxelSizeProp != null)
                        voxelSizeProp.floatValue = voxelSize;
                }
            }

            if (args.ContainsKey("minRegionArea"))
            {
                if (float.TryParse(args["minRegionArea"].ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float minRegionArea))
                {
                    var minRegionAreaProp = serializedObject.FindProperty("m_MinRegionArea");
                    if (minRegionAreaProp != null)
                        minRegionAreaProp.floatValue = minRegionArea;
                }
            }

            if (args.ContainsKey("overrideTileSize"))
            {
                if (args["overrideTileSize"] is bool b)
                {
                    var overrideTileSizeProp = serializedObject.FindProperty("m_OverrideTileSize");
                    if (overrideTileSizeProp != null)
                        overrideTileSizeProp.boolValue = b;
                }
                else if (bool.TryParse(args["overrideTileSize"].ToString(), out bool overrideTile))
                {
                    var overrideTileSizeProp = serializedObject.FindProperty("m_OverrideTileSize");
                    if (overrideTileSizeProp != null)
                        overrideTileSizeProp.boolValue = overrideTile;
                }
            }

            if (args.ContainsKey("tileSize"))
            {
                if (int.TryParse(args["tileSize"].ToString(), out int tileSize))
                {
                    var overrideTileSizeProp = serializedObject.FindProperty("m_OverrideTileSize");
                    var tileSizeProp = serializedObject.FindProperty("m_TileSize");
                    if (overrideTileSizeProp != null)
                        overrideTileSizeProp.boolValue = true;
                    if (tileSizeProp != null)
                        tileSizeProp.intValue = tileSize;
                }
            }

            if (args.ContainsKey("overrideVoxelSize"))
            {
                if (args["overrideVoxelSize"] is bool b)
                {
                    var overrideVoxelSizeProp = serializedObject.FindProperty("m_OverrideVoxelSize");
                    if (overrideVoxelSizeProp != null)
                        overrideVoxelSizeProp.boolValue = b;
                }
                else if (bool.TryParse(args["overrideVoxelSize"].ToString(), out bool overrideVoxel))
                {
                    var overrideVoxelSizeProp = serializedObject.FindProperty("m_OverrideVoxelSize");
                    if (overrideVoxelSizeProp != null)
                        overrideVoxelSizeProp.boolValue = overrideVoxel;
                }
            }

            // Note: preserveTilesOutsideBounds is not available on NavMeshSurface in this Unity version
            // It's a property of the build settings in the scene's NavMeshSettings, not on individual surfaces

            // Record object for undo before applying changes
            Undo.RecordObject(surface, $"Set NavMeshSurface Advanced Properties: {gameObjectPath}");

            // Apply changes
            serializedObject.ApplyModifiedProperties();

            // Read current values for response if not already set
            if (agentTypeID == 0)
            {
                var agentTypeIDProp = serializedObject.FindProperty("m_AgentTypeID");
                if (agentTypeIDProp != null)
                    agentTypeID = agentTypeIDProp.intValue;
            }
            
            if (!buildHeightMesh)
            {
                var buildHeightMeshProp = serializedObject.FindProperty("m_BuildHeightMesh");
                if (buildHeightMeshProp != null)
                    buildHeightMesh = buildHeightMeshProp.boolValue;
            }

            var extras = new Dictionary<string, object>
            {
                { "gameObjectPath", ToolUtils.GetGameObjectPath(obj) },
                { "agentTypeID", agentTypeID },
                { "buildHeightMesh", buildHeightMesh }
            };

            return ToolUtils.CreateSuccessResponse($"Updated advanced NavMeshSurface settings on '{gameObjectPath}'", extras);
        }
    }
}
#endif
