using UnityEngine;

namespace Dungeon.Logic
{
    // Abstraction for movement intent on the XZ plane.
    // Implement for player keyboard, AI, replayed input, network, etc.
    public interface IMovementInput
    {
        // Direction on the XZ plane.  Components are in [-1, 1].
        // Locomotion normalises vectors whose magnitude exceeds 1.
        Vector2 GetMovementInput();
    }
}
