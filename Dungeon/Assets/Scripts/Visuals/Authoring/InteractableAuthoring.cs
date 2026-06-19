using System;
using System.Collections.Generic;
using Dungeon.Visuals.CustomLogic;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    // Authoring component that marks a WorldObject's cells as interactable.
    // Each entry has a TypeId that must match an Interactor's TypeId for interaction to work.
    // Optionally assign an InteractionCallback MonoBehaviour to run custom logic on interact.
    //
    // Vector2Int X = grid X offset, Y = grid Z offset relative to origin.
    [RequireComponent(typeof(WorldObjectAuthoring))]
    public class InteractableAuthoring : MonoBehaviour
    {
        [Header("Interactable cells — local offsets that can be interacted with")]
        [SerializeField] private List<Vector2Int> _interactableCells = new() { Vector2Int.zero };

        [Header("Self-interaction")]
        [SerializeField] private bool _allowSelfInteraction = false;

        [Header("Interaction entries — TypeId filter + optional callback")]
        [SerializeField] private List<InteractableEntryData> _entries = new();

        public List<Vector2Int> InteractableCells => _interactableCells;
        public bool AllowSelfInteraction => _allowSelfInteraction;
        public List<InteractableEntryData> Entries => _entries;

        [Serializable]
        public struct InteractableEntryData
        {
            public int TypeId;
            public InteractionCallback Callback;
        }
    }
}
