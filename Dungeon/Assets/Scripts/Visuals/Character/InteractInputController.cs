using Dungeon.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals
{
    // Routes E key presses from the visuals layer to the Interactor feature in logic.
    // Assign the CharacterView whose WorldObject should perform the interaction.
    public class InteractInputController : MonoBehaviour
    {
        [SerializeField] private CharacterView _target;

        private void Update()
        {
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;

            if (_target == null) return;
            if (!WorldObjectRegistry.TryGet(_target.WorldObjectId, out var obj)) return;

            obj.GetFeature<Interactor>()?.TryInteract();
        }
    }
}
