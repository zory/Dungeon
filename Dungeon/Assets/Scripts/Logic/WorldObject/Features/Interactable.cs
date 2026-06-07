using System;

namespace Dungeon.Logic
{
    // Feature: this WorldObject can be interacted with by an Interactor.
    // Subscribe to OnInteracted to react when something interacts with this object.
    public class Interactable
    {
        public event Action<WorldObject> OnInteracted;

        // Called by Interactor — internal so external code cannot trigger interactions directly.
        internal void NotifyInteracted(WorldObject initiator) =>
            OnInteracted?.Invoke(initiator);
    }
}
