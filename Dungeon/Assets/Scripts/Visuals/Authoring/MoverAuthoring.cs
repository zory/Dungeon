using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    // Authoring component that marks a WorldObject as movable.
    // Add alongside WorldObjectAuthoring on a prefab.
    //
    // MaxSpeed: world units per second.
    // Acceleration: time constant in seconds (0 = instant response, 0.2 = slight inertia).
    //
    // Direction and Velocity are runtime state — set by AI, input, or other systems.
    [RequireComponent(typeof(WorldObjectAuthoring))]
    public class MoverAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _maxSpeed = 5f;

        [Tooltip("Time constant in seconds. 0 = instant response, higher = more inertia.")]
        [SerializeField] private float _acceleration = 0f;

        public float MaxSpeed => _maxSpeed;
        public float Acceleration => _acceleration;
    }
}
