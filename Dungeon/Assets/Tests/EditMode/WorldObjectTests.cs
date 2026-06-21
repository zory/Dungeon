using System.Collections.Generic;
using Dungeon.Logic;
using NUnit.Framework;
using UnityEngine;

namespace Dungeon.Tests.EditMode
{
    public class WorldObjectTests
    {
        [Test]
        public void AddFeature_And_GetFeature_RoundTrips()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);
            var mover = new Mover(5f);
            obj.AddFeature(mover);

            Mover retrieved = obj.GetFeature<Mover>();

            Assert.AreSame(mover, retrieved);
        }

        [Test]
        public void GetFeature_ReturnsNull_WhenNotAdded()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);

            Mover result = obj.GetFeature<Mover>();

            Assert.IsNull(result);
        }

        [Test]
        public void HasFeature_ReturnsTrueOnlyWhenAdded()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);

            Assert.IsFalse(obj.HasFeature<Mover>());

            obj.AddFeature(new Mover(5f));

            Assert.IsTrue(obj.HasFeature<Mover>());
        }

        [Test]
        public void TryGetFeature_ReturnsTrueAndFeature_WhenPresent()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);
            var mover = new Mover(3f);
            obj.AddFeature(mover);

            bool found = obj.TryGetFeature<Mover>(out Mover result);

            Assert.IsTrue(found);
            Assert.AreSame(mover, result);
        }

        [Test]
        public void TryGetFeature_ReturnsFalseAndNull_WhenAbsent()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);

            bool found = obj.TryGetFeature<Mover>(out Mover result);

            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void SetPosition_UpdatesCellCoords()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);

            // cellSize=1, offset=0, elevation=0 → cell = FloorToInt(pos)
            obj.SetPosition(new Vector3(2.5f, 0f, 3.7f), 1f, Vector2.zero, 0);

            Assert.AreEqual(new Vector3Int(2, 0, 3), obj.CellCoords);
        }

        [Test]
        public void SetPosition_WithOffset_ShiftsCellCoords()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);

            // pos=2.5, offset=1.0 → (2.5-1.0)/1 = 1.5 → FloorToInt = 1
            obj.SetPosition(new Vector3(2.5f, 0f, 3.7f), 1f, new Vector2(1f, 1f), 0);

            Assert.AreEqual(new Vector3Int(1, 0, 2), obj.CellCoords);
        }

        [Test]
        public void SetPosition_Elevation_SetsCellY()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);

            obj.SetPosition(new Vector3(0.5f, 0f, 0.5f), 1f, Vector2.zero, 3);

            Assert.AreEqual(3, obj.CellCoords.y);
        }

        [Test]
        public void MultipleFeatures_CoexistIndependently()
        {
            var obj = new WorldObject("TestObj", Vector3.zero);
            var mover = new Mover(5f);
            var obstacle = new Obstacle(new List<Vector2Int> { Vector2Int.zero });

            obj.AddFeature(mover);
            obj.AddFeature(obstacle);

            Assert.IsTrue(obj.HasFeature<Mover>());
            Assert.IsTrue(obj.HasFeature<Obstacle>());
            Assert.IsFalse(obj.HasFeature<Footprint>());
        }
    }
}
