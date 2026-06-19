using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals.Services
{
    // Visual service that reads keyboard input and feeds it to one controlled WorldObject.
    // Handles movement direction (WASD/arrows) and interaction (E key).
    // The controlled object can be switched at runtime via SetControlledObject.
    public class PlayerInputService : IVisualService
    {
        private WorldObjectService _objects;
        private InteractionService _interaction;
        private int _controlledObjectId = -1;

        public int ControlledObjectId => _controlledObjectId;

        public void Initialize(VisualWorld world)
        {
            _objects     = world.GetLogic<WorldObjectService>();
            _interaction = world.GetLogic<InteractionService>();
        }

        // Set which WorldObject this handler controls.
        // The object must have a Mover feature for movement and Interactor for interaction.
        public void SetControlledObject(int objectId)
        {
            // Clear direction on the old object so it stops moving.
            if (_controlledObjectId >= 0 && _objects.TryGet(_controlledObjectId, out WorldObject oldObj))
            {
                if (oldObj.TryGetFeature<Mover>(out Mover oldMover))
                {
                    oldMover.Direction = Vector2.zero;
                }
            }

            _controlledObjectId = objectId;
        }

        public void Tick(float deltaTime)
        {
            if (_controlledObjectId < 0) { return; }
            if (!_objects.TryGet(_controlledObjectId, out WorldObject obj)) { return; }

            // Movement input.
            if (obj.TryGetFeature<Mover>(out Mover mover))
            {
                mover.Direction = ReadMovementInput();
            }

            // Interaction input.
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame)
            {
                _interaction.TryInteract(obj);
            }
        }

        private static Vector2 ReadMovementInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) { return Vector2.zero; }

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);

            return new Vector2(x, z);
        }

        public void Dispose() { }
    }
}
