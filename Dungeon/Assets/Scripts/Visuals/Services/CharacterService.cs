using System;
using System.Collections.Generic;
using Dungeon.Logic;
using Dungeon.Logic.Core;
using Dungeon.Logic.Services;
using Dungeon.Visuals.Core;
using UnityEngine;

namespace Dungeon.Visuals.Services
{
    [Serializable]
    public struct CharacterConfig
    {
        public string Name;
        public float WalkSpeed;
        public bool IsPlayerControlled;

        public Sprite SpriteDown;
        public Sprite SpriteUp;
        public Sprite SpriteLeft;
        public Sprite SpriteRight;

        // Initial position taken from the Authoring MonoBehaviour's transform.
        public Vector3 SpawnPosition;
    }

    // Visual service: creates character entities and syncs their sprites/transforms.
    // Input is handled by PlayerInputService — this service only does visuals.
    public class CharacterService : IVisualService
    {
        private WorldObjectService _objects;
        private GridService _grid;

        private readonly List<CharacterInstance> _characters = new();

        public void Initialize(VisualWorld world)
        {
            _objects = world.GetLogic<WorldObjectService>();
            _grid    = world.GetLogic<GridService>();
        }

        // Creates a character WorldObject with a Mover feature.
        // Interactor/Interactable are added by the bootstrapper from authoring components.
        // rootTransform is the character's root GameObject — used for position sync.
        // SpriteRenderer can live on a child object.
        // Returns the assigned object ID (used to wire PlayerInputService).
        public int AddCharacter(CharacterConfig config, SpriteRenderer spriteRenderer, Transform rootTransform)
        {
            var obj = new WorldObject(config.Name, config.SpawnPosition);
            obj.SetPosition(config.SpawnPosition, _grid.CellSize, _grid.XZOffset, _grid.Elevation);
            int id = _objects.Register(obj);
            obj.AddFeature(new Mover(config.WalkSpeed));

            _characters.Add(new CharacterInstance
            {
                ObjectId = id,
                Config = config,
                SpriteRenderer = spriteRenderer,
                RootTransform = rootTransform,
                CurrentFacing = Facing.Down,
            });

            ApplySprite(_characters[_characters.Count - 1]);
            return id;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _characters.Count; i++)
            {
                CharacterInstance ch = _characters[i];
                if (!_objects.TryGet(ch.ObjectId, out WorldObject obj)) { continue; }

                // Update sprite facing from Mover.Facing (set by MovementService).
                if (obj.TryGetFeature<Mover>(out Mover mover) && mover.Velocity.sqrMagnitude > 0.001f)
                {
                    Facing newFacing = VectorToFacing(mover.Facing);
                    if (newFacing != ch.CurrentFacing)
                    {
                        ch.CurrentFacing = newFacing;
                        ApplySprite(ch);
                        _characters[i] = ch;
                    }
                }

                // Position sync handled by WorldObjectVisualSyncService for all movers.
            }
        }

        private static Facing VectorToFacing(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            {
                return dir.x > 0f ? Facing.Right : Facing.Left;
            }
            return dir.y > 0f ? Facing.Up : Facing.Down;
        }

        private static void ApplySprite(CharacterInstance ch)
        {
            Sprite sprite = ch.CurrentFacing switch
            {
                Facing.Up    => ch.Config.SpriteUp,
                Facing.Left  => ch.Config.SpriteLeft,
                Facing.Right => ch.Config.SpriteRight,
                _            => ch.Config.SpriteDown,
            };
            if (sprite != null && ch.SpriteRenderer != null)
            {
                ch.SpriteRenderer.sprite = sprite;
            }
        }

        public void Dispose()
        {
            foreach (CharacterInstance ch in _characters)
            {
                _objects?.Remove(ch.ObjectId);
            }
            _characters.Clear();
        }

        private enum Facing { Down, Up, Left, Right }

        private struct CharacterInstance
        {
            public int ObjectId;
            public CharacterConfig Config;
            public SpriteRenderer SpriteRenderer;
            public Transform RootTransform;
            public Facing CurrentFacing;
        }
    }
}
