using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Handles MeshCollider creation (both non-convex "mesh" and convex "convex" alias).
    /// Auto-finds the mesh from MeshFilter or SkinnedMeshRenderer on the GameObject or its children.
    /// TypeKey: "mesh"  (also aliased as "convex" in ColliderHandlerRegistry)
    /// </summary>
    public class MeshColliderHandler : IColliderHandler
    {
        public string TypeKey => "mesh";

        public bool AlreadyExists(UnityEngine.GameObject obj) => obj.GetComponent<MeshCollider>() != null;

        public Collider AddComponent(UnityEngine.GameObject obj) => Undo.AddComponent<MeshCollider>(obj);

        public void ApplyArgs(Collider collider, Dictionary<string, object> args)
        {
            if (collider is not MeshCollider mesh) return;

            if (args.ContainsKey("isTrigger"))
                collider.isTrigger = ToolUtils.ParseBool(args["isTrigger"]);

            // convex flag — also auto-set when colliderType=="convex"
            if (args.ContainsKey("convex") && bool.TryParse(args["convex"]?.ToString(), out bool convex))
                mesh.convex = convex;

            // Explicit mesh path
            if (args.ContainsKey("meshPath") && !string.IsNullOrWhiteSpace(args["meshPath"]?.ToString()))
            {
                string meshPath = args["meshPath"].ToString();
                if (!meshPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    meshPath = "Assets/" + meshPath;
                var sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (sharedMesh != null) mesh.sharedMesh = sharedMesh;
            }
            else
            {
                // Auto-find mesh from the GameObject or its children
                AutoAssignMesh(mesh);
            }
        }

        public void ApplyAutoAlign(Collider collider, Bounds bounds)
        {
            // MeshCollider uses the mesh itself — no manual sizing needed.
            // Just ensure the mesh is assigned.
            if (collider is MeshCollider mesh && mesh.sharedMesh == null)
                AutoAssignMesh(mesh);
        }

        public Dictionary<string, object> ReadProperties(Collider collider)
        {
            var props = new Dictionary<string, object>();
            if (collider is not MeshCollider mesh) return props;
            props["convex"]   = mesh.convex;
            props["meshPath"] = mesh.sharedMesh != null ? AssetDatabase.GetAssetPath(mesh.sharedMesh) : null;
            return props;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void AutoAssignMesh(MeshCollider mesh)
        {
            UnityEngine.GameObject go = mesh.gameObject;

            // Direct MeshFilter
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) { mesh.sharedMesh = mf.sharedMesh; return; }

            // Direct SkinnedMeshRenderer
            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null) { mesh.sharedMesh = smr.sharedMesh; return; }

            // Children MeshFilter
            MeshFilter childMf = go.GetComponentInChildren<MeshFilter>();
            if (childMf != null && childMf.sharedMesh != null) { mesh.sharedMesh = childMf.sharedMesh; return; }

            // Children SkinnedMeshRenderer
            SkinnedMeshRenderer childSmr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (childSmr != null && childSmr.sharedMesh != null) { mesh.sharedMesh = childSmr.sharedMesh; }
        }
    }
}
