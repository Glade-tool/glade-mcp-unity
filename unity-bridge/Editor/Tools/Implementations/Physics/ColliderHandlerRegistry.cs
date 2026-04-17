using System;
using System.Collections.Generic;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Central registry for collider handlers.
    /// All collider type logic is routed through here — add new collider types by
    /// implementing IColliderHandler and calling Register() here.
    /// </summary>
    public static class ColliderHandlerRegistry
    {
        private static readonly Dictionary<string, IColliderHandler> _handlers =
            new Dictionary<string, IColliderHandler>(StringComparer.OrdinalIgnoreCase);

        static ColliderHandlerRegistry()
        {
            Register(new BoxColliderHandler());
            Register(new SphereColliderHandler());
            Register(new CapsuleColliderHandler());

            // MeshCollider handles both "mesh" (non-convex) and "convex" (convex mesh)
            var meshHandler = new MeshColliderHandler();
            Register(meshHandler);
            _handlers["convex"] = meshHandler; // alias

            Register(new WheelColliderHandler());
            Register(new TerrainColliderHandler());
        }

        /// <summary>Registers or replaces a handler for its TypeKey.</summary>
        public static void Register(IColliderHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[handler.TypeKey] = handler;
        }

        /// <summary>
        /// Attempts to get the handler for the given collider type key (case-insensitive).
        /// Returns false if the type is unknown.
        /// </summary>
        public static bool TryGet(string typeKey, out IColliderHandler handler)
            => _handlers.TryGetValue(typeKey ?? "box", out handler);

        /// <summary>
        /// Gets the handler for the given key, falling back to BoxColliderHandler if not found.
        /// </summary>
        public static IColliderHandler GetOrDefault(string typeKey)
            => _handlers.TryGetValue(typeKey ?? "box", out var h) ? h : _handlers["box"];

        /// <summary>All registered type keys.</summary>
        public static IEnumerable<string> AllTypeKeys => _handlers.Keys;
    }
}
