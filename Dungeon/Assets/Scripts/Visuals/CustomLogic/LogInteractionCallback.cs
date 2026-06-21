using UnityEngine;

namespace Dungeon.Visuals.CustomLogic
{
    // Simple interaction callback that logs a message to the console.
    // Useful for testing that interactions are wired correctly.
    public class LogInteractionCallback : InteractionCallback
    {
        [SerializeField] private string _message = "Interacted!";

        public override void Execute()
        {
            Debug.Log($"[Interaction] {_message} (on {gameObject.name})");
        }
    }
}
