using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: this WorldObject can move through the world.
    // CharacterView calls Move each frame after reading its IMovementInput.
    public class Locomotion
    {
        private readonly WorldObject _owner;

        public float WalkSpeed { get; set; }

        public Locomotion(WorldObject owner, float walkSpeed = 5f)
        {
            _owner    = owner;
            WalkSpeed = walkSpeed;
        }

        // Apply normalised XZ input scaled by WalkSpeed and deltaTime.
        public void Move(Vector2 input, float deltaTime, float cellSize, Vector2 xzOffset, int elevation)
        {
            if (input == Vector2.zero) return;
            var dir = input.magnitude > 1f ? input.normalized : input;
            _owner.SetPosition(
                _owner.WorldPosition + new Vector3(dir.x, 0f, dir.y) * WalkSpeed * deltaTime,
                cellSize, xzOffset, elevation);
        }
    }
}
