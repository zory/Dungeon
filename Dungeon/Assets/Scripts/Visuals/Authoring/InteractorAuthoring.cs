using System;
using System.Collections.Generic;
using Dungeon.Visuals.CustomLogic;
using UnityEngine;

namespace Dungeon.Visuals.Authoring
{
    // Authoring component that marks a WorldObject or Character as capable of interacting.
    // Each entry has a TypeId — the object can interact with any Interactable that shares the same TypeId.
    // Optionally assign an InteractionCallback to run custom logic when this interactor interacts.
    //
    // Can be placed alongside WorldObjectAuthoring (static objects) or CharacterAuthoring (characters).
    public class InteractorAuthoring : MonoBehaviour
    {
        [Header("Interactor entries — TypeId capabilities + optional callback")]
        [SerializeField] private List<InteractorEntryData> _entries = new();

        public List<InteractorEntryData> Entries => _entries;

        [Serializable]
        public struct InteractorEntryData
        {
            public int TypeId;
            public InteractionCallback Callback;
        }
    }
}
