using System.Collections.Generic;
using Dungeon.Logic;
using NUnit.Framework;
using UnityEngine;

namespace Dungeon.Tests.EditMode
{
    public class FootprintTests
    {
        [Test]
        public void DefaultsToSingleCell_WhenNullPassed()
        {
            var footprint = new Footprint(null);

            Assert.AreEqual(1, footprint.LocalCells.Count);
            Assert.AreEqual(Vector2Int.zero, footprint.LocalCells[0]);
        }

        [Test]
        public void DefaultsToSingleCell_WhenEmptyListPassed()
        {
            var footprint = new Footprint(new List<Vector2Int>());

            Assert.AreEqual(1, footprint.LocalCells.Count);
            Assert.AreEqual(Vector2Int.zero, footprint.LocalCells[0]);
        }

        [Test]
        public void GetWorldCells_OffsetsFromOrigin()
        {
            var footprint = new Footprint(new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
            });

            Vector3Int origin = new Vector3Int(10, 2, 20);
            List<Vector3Int> worldCells = footprint.GetWorldCells(origin);

            Assert.AreEqual(3, worldCells.Count);
            Assert.AreEqual(new Vector3Int(10, 2, 20), worldCells[0]);
            Assert.AreEqual(new Vector3Int(11, 2, 20), worldCells[1]);
            Assert.AreEqual(new Vector3Int(10, 2, 21), worldCells[2]);
        }

        [Test]
        public void GetWorldCells_NonAllocating_ClearsPreviousResults()
        {
            var footprint = new Footprint(new List<Vector2Int> { Vector2Int.zero });
            var results = new List<Vector3Int> { new Vector3Int(99, 99, 99) };

            footprint.GetWorldCells(Vector3Int.zero, results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(Vector3Int.zero, results[0]);
        }
    }

    public class ObstacleTests
    {
        [Test]
        public void DefaultsToSingleCell_WhenNullPassed()
        {
            var obstacle = new Obstacle(null);

            Assert.AreEqual(1, obstacle.BlockedLocalCells.Count);
            Assert.AreEqual(Vector2Int.zero, obstacle.BlockedLocalCells[0]);
        }

        [Test]
        public void GetBlockedWorldCells_OffsetsFromOrigin()
        {
            var obstacle = new Obstacle(new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(2, 0),
            });

            Vector3Int origin = new Vector3Int(5, 0, 5);
            List<Vector3Int> blocked = obstacle.GetBlockedWorldCells(origin);

            Assert.AreEqual(2, blocked.Count);
            Assert.AreEqual(new Vector3Int(5, 0, 5), blocked[0]);
            Assert.AreEqual(new Vector3Int(7, 0, 5), blocked[1]);
        }
    }

    public class MoverTests
    {
        [Test]
        public void Constructor_SetsDefaults()
        {
            var mover = new Mover(10f, 0.5f);

            Assert.AreEqual(10f, mover.MaxSpeed);
            Assert.AreEqual(0.5f, mover.Acceleration);
            Assert.AreEqual(Vector2.zero, mover.Direction);
            Assert.AreEqual(Vector2.zero, mover.Velocity);
            Assert.AreEqual(new Vector2(0f, -1f), mover.Facing);
        }

        [Test]
        public void DefaultAcceleration_IsZero()
        {
            var mover = new Mover(5f);

            Assert.AreEqual(0f, mover.Acceleration);
        }
    }

    public class InteractableTests
    {
        [Test]
        public void AddEntry_And_HasTypeId()
        {
            var interactable = new Interactable(null);

            interactable.AddEntry(5);
            interactable.AddEntry(10);

            Assert.IsTrue(interactable.HasTypeId(5));
            Assert.IsTrue(interactable.HasTypeId(10));
            Assert.IsFalse(interactable.HasTypeId(0));
        }

        [Test]
        public void Callback_IsInvoked_WhenSet()
        {
            var interactable = new Interactable(null);
            bool callbackFired = false;
            interactable.AddEntry(0, () => callbackFired = true);

            interactable.Entries[0].OnInteracted.Invoke();

            Assert.IsTrue(callbackFired);
        }

        [Test]
        public void AllowSelfInteraction_DefaultsFalse()
        {
            var interactable = new Interactable(null);

            Assert.IsFalse(interactable.AllowSelfInteraction);
        }

        [Test]
        public void GetWorldCells_DefaultsToSingleCell()
        {
            var interactable = new Interactable(null);

            List<Vector3Int> cells = interactable.GetWorldCells(new Vector3Int(3, 0, 4));

            Assert.AreEqual(1, cells.Count);
            Assert.AreEqual(new Vector3Int(3, 0, 4), cells[0]);
        }
    }

    public class InteractorTests
    {
        [Test]
        public void AddEntry_And_HasTypeId()
        {
            var interactor = new Interactor();

            interactor.AddEntry(7);
            interactor.AddEntry(3);

            Assert.IsTrue(interactor.HasTypeId(7));
            Assert.IsTrue(interactor.HasTypeId(3));
            Assert.IsFalse(interactor.HasTypeId(0));
        }

        [Test]
        public void Callback_IsInvoked_WhenSet()
        {
            var interactor = new Interactor();
            bool callbackFired = false;
            interactor.AddEntry(0, () => callbackFired = true);

            interactor.Entries[0].OnInteract.Invoke();

            Assert.IsTrue(callbackFired);
        }
    }
}
