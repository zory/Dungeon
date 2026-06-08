using Dungeon.Logic;
using UnityEngine;

namespace Dungeon.Visuals
{
    // Visual representation of a static (non-moving) WorldObject such as a building, prop, or chest.
    // Creates a WorldObject with Interactable on Awake.  No Locomotion — position is set at spawn.
    // The SpriteRenderer for the visual should be on this GameObject or a child.
    public class WorldObjectView : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private string _objectName = "Object";

        // TypeId must match the key used in ObjectDefinitionRegistry so the level editor
        // can save and reload this object correctly.
        [SerializeField] private string _typeId = "";

        public int    WorldObjectId { get; private set; } = -1;
        public string TypeId        => _typeId;

        private void Awake()
        {
            WorldObject obj = new WorldObject(_objectName, transform.position);
            obj.AddFeature(new Interactable());
            WorldObjectRegistry.Register(obj);
            WorldObjectId = obj.Id;
        }

        private void OnDestroy()
        {
            WorldObjectRegistry.Remove(WorldObjectId);
        }
    }
}
