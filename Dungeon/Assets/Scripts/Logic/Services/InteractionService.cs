using System.Collections.Generic;
using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    // System: tracks interactable cells and resolves interaction attempts.
    // An interactor can interact with an adjacent interactable cell when their TypeIds match.
    // When multiple interactables are adjacent, the interactor's facing direction is the tiebreaker.
    public class InteractionService : ILogicService
    {
        // Maps world cell → object ID for every registered interactable cell.
        private readonly Dictionary<Vector3Int, int> _interactableCells = new();

        private WorldObjectService _objects;

        // The four cardinal neighbour offsets (right, left, forward, back).
        private static readonly Vector3Int[] NEIGHBOURS =
        {
            new Vector3Int( 1, 0,  0),
            new Vector3Int(-1, 0,  0),
            new Vector3Int( 0, 0,  1),
            new Vector3Int( 0, 0, -1),
        };

        public void Initialize(LogicWorld world)
        {
            _objects = world.Get<WorldObjectService>();
        }

        public void Tick(float deltaTime) { }

        // Register all interactable cells for a WorldObject that has the Interactable feature.
        public void RegisterInteractable(WorldObject obj)
        {
            if (!obj.TryGetFeature<Interactable>(out Interactable interactable)) { return; }

            List<Vector3Int> worldCells = interactable.GetWorldCells(obj.CellCoords);
            for (int i = 0; i < worldCells.Count; i++)
            {
                _interactableCells[worldCells[i]] = obj.Id;
            }
        }

        // Remove all interactable cells belonging to the given object.
        public void UnregisterInteractable(int objectId)
        {
            var toRemove = new List<Vector3Int>();
            foreach (var kvp in _interactableCells)
            {
                if (kvp.Value == objectId)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                _interactableCells.Remove(toRemove[i]);
            }
        }

        // Attempt interaction for the given interactor WorldObject.
        // Checks adjacent cells for interactable objects with matching TypeIds.
        // Facing direction is used as tiebreaker when multiple candidates exist.
        // All matching TypeId pairs fire their callbacks.
        // Returns true if any interaction occurred.
        public bool TryInteract(WorldObject interactor)
        {
            if (!interactor.TryGetFeature<Interactor>(out Interactor interactorFeature)) { return false; }
            if (interactorFeature.Entries.Count == 0) { return false; }

            Vector3Int interactorCell = interactor.CellCoords;

            // Get facing for tiebreaking.
            Vector2 facing = Vector2.down;
            if (interactor.TryGetFeature<Mover>(out Mover mover))
            {
                facing = mover.Facing;
            }

            // Find the best adjacent target with at least one matching TypeId.
            int bestObjectId = -1;
            float bestDot = float.MinValue;

            for (int i = 0; i < NEIGHBOURS.Length; i++)
            {
                Vector3Int neighbourCell = interactorCell + NEIGHBOURS[i];

                if (!_interactableCells.TryGetValue(neighbourCell, out int objectId)) { continue; }

                // Self-interaction check.
                if (objectId == interactor.Id)
                {
                    WorldObject selfObj = _objects.Get(objectId);
                    if (selfObj == null) { continue; }
                    if (!selfObj.TryGetFeature<Interactable>(out Interactable selfInteractable)) { continue; }
                    if (!selfInteractable.AllowSelfInteraction) { continue; }
                }

                // Check if any TypeId matches between interactor and this target.
                WorldObject candidateObj = _objects.Get(objectId);
                if (candidateObj == null) { continue; }
                if (!candidateObj.TryGetFeature<Interactable>(out Interactable candidateInteractable)) { continue; }
                if (!HasAnyTypeMatch(interactorFeature, candidateInteractable)) { continue; }

                // Tiebreak by facing direction.
                Vector2 dirToNeighbour = new Vector2(NEIGHBOURS[i].x, NEIGHBOURS[i].z);
                float dot = Vector2.Dot(facing.normalized, dirToNeighbour.normalized);

                if (bestObjectId == -1 || dot > bestDot)
                {
                    bestObjectId = objectId;
                    bestDot = dot;
                }
            }

            if (bestObjectId == -1) { return false; }

            // Resolve: fire all matching TypeId pair callbacks on the chosen target.
            WorldObject target = _objects.Get(bestObjectId);
            if (target == null) { return false; }
            if (!target.TryGetFeature<Interactable>(out Interactable interactable)) { return false; }

            Debug.Log($"[Interaction] {interactor.Name} interacts with {target.Name}");
            return FireMatchingCallbacks(interactorFeature, interactable);
        }

        // Returns true if the interactor and interactable share at least one TypeId.
        private static bool HasAnyTypeMatch(Interactor interactor, Interactable interactable)
        {
            IReadOnlyList<InteractorEntry> torEntries = interactor.Entries;
            IReadOnlyList<InteractableEntry> tableEntries = interactable.Entries;

            for (int i = 0; i < torEntries.Count; i++)
            {
                for (int j = 0; j < tableEntries.Count; j++)
                {
                    if (torEntries[i].TypeId == tableEntries[j].TypeId) { return true; }
                }
            }
            return false;
        }

        // Fires callbacks for every matching TypeId pair. Returns true if any fired.
        private static bool FireMatchingCallbacks(Interactor interactor, Interactable interactable)
        {
            IReadOnlyList<InteractorEntry> torEntries = interactor.Entries;
            IReadOnlyList<InteractableEntry> tableEntries = interactable.Entries;
            bool fired = false;

            for (int i = 0; i < torEntries.Count; i++)
            {
                for (int j = 0; j < tableEntries.Count; j++)
                {
                    if (torEntries[i].TypeId == tableEntries[j].TypeId)
                    {
                        tableEntries[j].OnInteracted?.Invoke();
                        torEntries[i].OnInteract?.Invoke();
                        fired = true;
                    }
                }
            }
            return fired;
        }

        // Returns true if the given cell has an interactable registered.
        public bool IsInteractable(Vector3Int worldCell) => _interactableCells.ContainsKey(worldCell);

        public void Dispose()
        {
            _interactableCells.Clear();
        }
    }
}
