using UnityEngine;

namespace Dungeon.Logic
{
    // Feature: this WorldObject can initiate interactions.
    // TryInteract finds the closest Interactable WorldObject within Range and triggers it.
    public class Interactor
    {
        private readonly WorldObject _owner;

        public float Range { get; set; } = 2f;

        public Interactor(WorldObject owner, float range = 2f)
        {
            _owner = owner;
            Range  = range;
        }

        public void TryInteract()
        {
            WorldObject closest     = null;
            float       closestDist = float.MaxValue;

            foreach (var obj in WorldObjectRegistry.All.Values)
            {
                if (obj == _owner) continue;
                if (!obj.HasFeature<Interactable>()) continue;

                float dist = Vector3.Distance(_owner.WorldPosition, obj.WorldPosition);
                if (dist <= Range && dist < closestDist)
                {
                    closest     = obj;
                    closestDist = dist;
                }
            }

            if (closest == null) return;

            Debug.Log($"{_owner.Name} interacts with {closest.Name}");
            closest.GetFeature<Interactable>().NotifyInteracted(_owner);
        }
    }
}
