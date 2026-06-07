using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Visual representation of a character WorldObject.
    // Creates the WorldObject and attaches Locomotion / Interactor / Interactable features on Awake.
    // Drives movement each frame via an IMovementInput provider.
    // Swaps sprites to reflect the current movement direction (4-directional).
    //
    // "Logic State" inspector fields are display-only snapshots — written from logic, never read back.
    public class CharacterView : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private string _characterName = "Character";
        [SerializeField] private float  _walkSpeed     = 5f;
        [SerializeField] private float  _interactRange = 2f;

        [Tooltip("MonoBehaviour that implements IMovementInput.  Leave empty for no movement.")]
        [SerializeField] private MonoBehaviour _movementInputProvider;

        [Header("Grid Settings — must match GridRenderer")]
        [SerializeField] private GridRenderer _gridRenderer;

        [Header("Sprites — 4 directions")]
        [SerializeField] private Sprite _spriteDown;   // facing -Z  (default / idle)
        [SerializeField] private Sprite _spriteUp;     // facing +Z
        [SerializeField] private Sprite _spriteLeft;   // facing -X
        [SerializeField] private Sprite _spriteRight;  // facing +X

        // WorldObject ID — use this in InteractInputController to reference this character.
        public int WorldObjectId { get; private set; } = -1;

        // ── Inspector display (read-only) — sourced from WorldObject, never authoritative ──
        [Header("Logic State (display only)")]
        [SerializeField] private string    _displayName;
        [SerializeField] private Vector3   _displayWorldPosition;
        [SerializeField] private Vector3Int _displayCellCoords;
        [SerializeField] private float     _displayWalkSpeed;
        // ─────────────────────────────────────────────────────────────────────────────────

        private SpriteRenderer  _spriteRenderer;
        private IMovementInput  _movementInput;

        private enum Facing { Down, Up, Left, Right }
        private Facing _facing = Facing.Down;

        private void Awake()
        {
            _movementInput = _movementInputProvider as IMovementInput;

            var obj = new WorldObject(_characterName, transform.position);
            WorldObjectId = obj.Id;
            obj.AddFeature(new Locomotion(obj, _walkSpeed));
            obj.AddFeature(new Interactable());
            obj.AddFeature(new Interactor(obj, _interactRange));
            WorldObjectRegistry.Register(obj);

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            ApplySprite();
        }

        private void OnDestroy()
        {
            WorldObjectRegistry.Remove(WorldObjectId);
        }

        private void Update()
        {
            if (!WorldObjectRegistry.TryGet(WorldObjectId, out var obj)) return;

            // ── Movement ─────────────────────────────────────────────────────────────────
            var input = _movementInput?.GetMovementInput() ?? Vector2.zero;
            if (input != Vector2.zero && obj.TryGetFeature<Locomotion>(out var loco))
            {
                float cellSize  = _gridRenderer != null ? _gridRenderer.CellSize       : 1f;
                var   xzOffset  = _gridRenderer != null ? _gridRenderer.XZOffset       : Vector2.zero;
                int   elevation = _gridRenderer != null ? _gridRenderer.ElevationLayer : 0;

                loco.Move(input, Time.deltaTime, cellSize, xzOffset, elevation);
                UpdateFacing(input);
                ApplySprite();
            }

            // ── Sync visual transform to logic ───────────────────────────────────────────
            var p = obj.WorldPosition;
            transform.position = new Vector3(p.x, transform.position.y, p.z);

            // ── Refresh inspector display ────────────────────────────────────────────────
            _displayName          = obj.Name;
            _displayWorldPosition = obj.WorldPosition;
            _displayCellCoords    = obj.CellCoords;
            _displayWalkSpeed     = obj.TryGetFeature<Locomotion>(out var l) ? l.WalkSpeed : 0f;
        }

        private void UpdateFacing(Vector2 input)
        {
            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
                _facing = input.x > 0f ? Facing.Right : Facing.Left;
            else
                _facing = input.y > 0f ? Facing.Up : Facing.Down;
        }

        private void ApplySprite()
        {
            var sprite = _facing switch
            {
                Facing.Up    => _spriteUp,
                Facing.Left  => _spriteLeft,
                Facing.Right => _spriteRight,
                _            => _spriteDown,
            };
            if (sprite != null)
                _spriteRenderer.sprite = sprite;
        }
    }
}
