using System.Collections.Generic;
using UnityEngine;

namespace GladeAgenticAI.Core.Tools.Implementations.Physics
{
    /// <summary>
    /// Strategy interface for per-collider-type logic.
    /// Each collider type (Box, Sphere, Capsule, Mesh, Wheel, Terrain, ...) implements this
    /// interface and is registered in ColliderHandlerRegistry.
    /// To add a new collider type: create a class implementing IColliderHandler, then call
    /// ColliderHandlerRegistry.Register() in its static constructor or in ColliderHandlerRegistry.
    /// </summary>
    public interface IColliderHandler
    {
        /// <summary>
        /// The lowercase key used to look up this handler (e.g. "box", "sphere", "wheel").
        /// Must match the colliderType values passed by the AI tools (lowercased).
        /// </summary>
        string TypeKey { get; }

        /// <summary>
        /// Returns true if a collider of this type already exists on the GameObject.
        /// </summary>
        bool AlreadyExists(UnityEngine.GameObject obj);

        /// <summary>
        /// Adds the collider component to the GameObject (should use Undo.AddComponent).
        /// Returns the new Collider, or null on failure.
        /// </summary>
        Collider AddComponent(UnityEngine.GameObject obj);

        /// <summary>
        /// Applies tool arguments (from the AI call) to the collider.
        /// Handles isTrigger plus all type-specific properties.
        /// </summary>
        void ApplyArgs(Collider collider, Dictionary<string, object> args);

        /// <summary>
        /// Sizes/positions the collider automatically from mesh bounds.
        /// Called only when autoAlign=true and no explicit size args were provided.
        /// </summary>
        void ApplyAutoAlign(Collider collider, Bounds bounds);

        /// <summary>
        /// Reads all type-specific properties from the collider into a dictionary
        /// for inclusion in the tool response.
        /// </summary>
        Dictionary<string, object> ReadProperties(Collider collider);
    }
}
