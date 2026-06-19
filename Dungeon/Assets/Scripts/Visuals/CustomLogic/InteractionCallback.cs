using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Visuals.CustomLogic
{
    // Base class for interaction callbacks assignable in the Inspector.
    // Create a concrete subclass, override Execute(), and drag it onto a prefab.
    // Assign it in InteractableAuthoring or InteractorAuthoring entries.
    //
    // Inside Execute() you have access to LogicWorld — from there you can
    // get any service, look up objects by ID, and modify game state.
    //
    // Example:
    //   public class PrintMessageCallback : InteractionCallback
    //   {
    //       [SerializeField] private string _message = "Hello!";
    //       public override void Execute() => Debug.Log(_message);
    //   }
    public abstract class InteractionCallback : MonoBehaviour
    {
        // Called when the interaction fires.
        public abstract void Execute();

        // Access to the logic world for querying/modifying game state.
        protected LogicWorld LogicWorld => GameBootstrapper.Instance.LogicWorld;
    }
}
