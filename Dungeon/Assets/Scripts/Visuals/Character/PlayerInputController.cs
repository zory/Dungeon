using Dungeon.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Implements IMovementInput using WASD / arrow keys.
    // Drag this component into a CharacterView's Movement Input Provider slot.
    public class PlayerInputController : MonoBehaviour, IMovementInput
    {
        public Vector2 GetMovementInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed   ? 1f : 0f);

            return new Vector2(x, z);
        }
    }
}
