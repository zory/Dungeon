using System;
using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct CharacterConfig
    {
        public string Name;
        public float WalkSpeed;
        public float InteractRange;
        public bool IsPlayerControlled;

        public Sprite SpriteDown;
        public Sprite SpriteUp;
        public Sprite SpriteLeft;
        public Sprite SpriteRight;

        // Initial position taken from the Authoring MonoBehaviour's transform.
        public Vector3 SpawnPosition;
    }

    public class CharacterService : IVisualService
    {
        private VisualWorld _world;
        private GridService _grid;
        private WorldObjectService _objects;

        private readonly List<CharacterInstance> _characters = new();

        public void Initialize(VisualWorld world)
        {
            _world = world;
            _grid = world.GetLogic<GridService>();
            _objects = world.GetLogic<WorldObjectService>();
        }

        public void AddCharacter(CharacterConfig config, SpriteRenderer spriteRenderer)
        {
            var obj = new WorldObject(config.Name, config.SpawnPosition);
            int id = _objects.Register(obj);
            obj.AddFeature(new Locomotion(obj, config.WalkSpeed));
            obj.AddFeature(new Interactable());
            obj.AddFeature(new Interactor(obj, config.InteractRange));

            _characters.Add(new CharacterInstance
            {
                ObjectId = id,
                Config = config,
                SpriteRenderer = spriteRenderer,
                Facing = Facing.Down,
            });

            ApplySprite(_characters[_characters.Count - 1]);
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _characters.Count; i++)
            {
                var ch = _characters[i];
                if (!_objects.TryGet(ch.ObjectId, out var obj)) continue;

                // Movement
                Vector2 input = ch.Config.IsPlayerControlled ? ReadPlayerInput() : Vector2.zero;
                if (input != Vector2.zero && obj.TryGetFeature<Locomotion>(out var loco))
                {
                    loco.Move(input, deltaTime, _grid.CellSize, _grid.XZOffset, _grid.Elevation);
                    UpdateFacing(ref ch, input);
                    ApplySprite(ch);
                    _characters[i] = ch;
                }

                // Interaction
                if (ch.Config.IsPlayerControlled)
                {
                    var kb = Keyboard.current;
                    if (kb != null && kb.eKey.wasPressedThisFrame)
                    {
                        obj.GetFeature<Interactor>()?.TryInteract(_objects);
                    }
                }

                // Sync visual transform to logic
                var p = obj.WorldPosition;
                var t = ch.SpriteRenderer.transform;
                t.position = new Vector3(p.x, t.position.y, p.z);
            }
        }

        private static Vector2 ReadPlayerInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;

            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                    - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                    - (kb.sKey.isPressed || kb.downArrowKey.isPressed   ? 1f : 0f);

            return new Vector2(x, z);
        }

        private static void UpdateFacing(ref CharacterInstance ch, Vector2 input)
        {
            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
                ch.Facing = input.x > 0f ? Facing.Right : Facing.Left;
            else
                ch.Facing = input.y > 0f ? Facing.Up : Facing.Down;
        }

        private static void ApplySprite(CharacterInstance ch)
        {
            var sprite = ch.Facing switch
            {
                Facing.Up    => ch.Config.SpriteUp,
                Facing.Left  => ch.Config.SpriteLeft,
                Facing.Right => ch.Config.SpriteRight,
                _            => ch.Config.SpriteDown,
            };
            if (sprite != null && ch.SpriteRenderer != null)
                ch.SpriteRenderer.sprite = sprite;
        }

        public void Dispose()
        {
            foreach (var ch in _characters)
                _objects?.Remove(ch.ObjectId);
            _characters.Clear();
        }

        private enum Facing { Down, Up, Left, Right }

        private struct CharacterInstance
        {
            public int ObjectId;
            public CharacterConfig Config;
            public SpriteRenderer SpriteRenderer;
            public Facing Facing;
        }
    }
}
