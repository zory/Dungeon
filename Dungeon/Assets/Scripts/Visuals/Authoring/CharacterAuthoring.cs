using Dungeon.Visuals.Services;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterAuthoring : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private string _characterName = "Character";
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _interactRange = 2f;
        [SerializeField] private bool _isPlayerControlled = false;

        [Header("Sprites — 4 directions")]
        [SerializeField] private Sprite _spriteDown;
        [SerializeField] private Sprite _spriteUp;
        [SerializeField] private Sprite _spriteLeft;
        [SerializeField] private Sprite _spriteRight;

        public SpriteRenderer SpriteRenderer => GetComponent<SpriteRenderer>();

        public CharacterConfig GetConfig() => new CharacterConfig
        {
            Name = _characterName,
            WalkSpeed = _walkSpeed,
            InteractRange = _interactRange,
            IsPlayerControlled = _isPlayerControlled,
            SpriteDown = _spriteDown,
            SpriteUp = _spriteUp,
            SpriteLeft = _spriteLeft,
            SpriteRight = _spriteRight,
            SpawnPosition = transform.position,
        };
    }
}
