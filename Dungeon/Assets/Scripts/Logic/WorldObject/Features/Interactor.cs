using System;
using System.Collections.Generic;

namespace Dungeon.Logic
{
    // Feature: this WorldObject can interact with adjacent Interactables.
    //
    // Entries define which interaction TypeIds this object can initiate.
    // Each entry has a TypeId and an optional callback that fires when interaction succeeds.
    // A pickaxe with TypeId=10 can only interact with Interactables that also have TypeId=10.
    public class Interactor
    {
        private readonly List<InteractorEntry> _entries = new();

        public IReadOnlyList<InteractorEntry> Entries => _entries;

        // Add an interactor capability. TypeId must match an Interactable's entry for interaction.
        public void AddEntry(int typeId, Action onInteract = null)
        {
            _entries.Add(new InteractorEntry { TypeId = typeId, OnInteract = onInteract });
        }

        // Returns true if this interactor has at least one entry with the given TypeId.
        public bool HasTypeId(int typeId)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].TypeId == typeId) { return true; }
            }
            return false;
        }
    }

    public struct InteractorEntry
    {
        public int TypeId;
        public Action OnInteract;
    }
}
