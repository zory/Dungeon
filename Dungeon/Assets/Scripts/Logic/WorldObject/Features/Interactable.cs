using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: this WorldObject can be interacted with by adjacent Interactors.
    //
    // LocalCells defines WHERE interaction can happen (cells relative to origin).
    // Entries define WHAT interactions are available — each has a TypeId and a callback.
    // An Interactor must have a matching TypeId for the interaction to fire.
    //
    // AllowSelfInteraction: if true, the same object can interact with itself (default false).
    public class Interactable
    {
        private readonly List<Vector2Int> _localCells;
        private readonly List<InteractableEntry> _entries = new();

        public IReadOnlyList<Vector2Int> LocalCells => _localCells;
        public IReadOnlyList<InteractableEntry> Entries => _entries;
        public bool AllowSelfInteraction { get; set; }

        public Interactable(List<Vector2Int> localCells, bool allowSelfInteraction = false)
        {
            _localCells = localCells != null && localCells.Count > 0
                ? new List<Vector2Int>(localCells)
                : new List<Vector2Int> { Vector2Int.zero };
            AllowSelfInteraction = allowSelfInteraction;
        }

        // Add an interaction entry. TypeId must match an Interactor's entry for the interaction to fire.
        public void AddEntry(int typeId, Action onInteracted = null)
        {
            _entries.Add(new InteractableEntry { TypeId = typeId, OnInteracted = onInteracted });
        }

        // Returns world cell positions for the interactable cells.
        public List<Vector3Int> GetWorldCells(Vector3Int originCell)
        {
            var result = new List<Vector3Int>(_localCells.Count);
            for (int i = 0; i < _localCells.Count; i++)
            {
                Vector2Int local = _localCells[i];
                result.Add(new Vector3Int(originCell.x + local.x, originCell.y, originCell.z + local.y));
            }
            return result;
        }

        // Returns true if this interactable has at least one entry with the given TypeId.
        public bool HasTypeId(int typeId)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TypeId == typeId) { return true; }
            }
            return false;
        }
    }

    public struct InteractableEntry
    {
        public int TypeId;
        public Action OnInteracted;
    }
}
