using System.Collections.Generic;
using System.Linq;
using Dungeon.Logic;
using Dungeon.Logic.Core;
using Dungeon.Logic.Services;
using NUnit.Framework;
using UnityEngine;

namespace Dungeon.Tests.EditMode
{
    // Helper: spins up a minimal LogicWorld with the services needed for testing.
    internal static class TestWorldFactory
    {
        public static LogicWorld CreateWithAllServices()
        {
            LogicWorld world = new LogicWorld();
            world.Register(new GridService(GridConfig.Default));
            world.Register(new WorldGenerationService(WorldGenerationConfig.Default, System.Array.Empty<TerrainGenerationRule>()));
            world.Register(new WorldObjectService());
            world.Register(new ObstacleService());
            world.Register(new UndergroundService());
            world.Register(new InteractionService());
            world.Register(new MovementService());
            world.InitializeAll();
            return world;
        }

        // Creates a WorldObject at a cell position, registered and with a Footprint.
        public static WorldObject CreateRegisteredObject(LogicWorld world, string name, Vector3Int cell)
        {
            GridService grid = world.Get<GridService>();
            WorldObjectService objects = world.Get<WorldObjectService>();

            Vector3 worldPos = grid.CellCenter(cell);
            var obj = new WorldObject(name, worldPos);
            obj.SetPosition(worldPos, grid.CellSize, grid.XZOffset, grid.Elevation);
            obj.AddFeature(new Footprint(new List<Vector2Int> { Vector2Int.zero }));
            objects.Register(obj);
            objects.OccupyCells(obj);
            return obj;
        }
    }

    // ── WorldObjectService Tests ──────────────────────────────────────────────────

    public class WorldObjectServiceTests
    {
        [Test]
        public void Register_AssignsIncrementingIds()
        {
            var service = new WorldObjectService();
            var obj1 = new WorldObject("A", Vector3.zero);
            var obj2 = new WorldObject("B", Vector3.zero);

            int id1 = service.Register(obj1);
            int id2 = service.Register(obj2);

            Assert.AreEqual(1, id1);
            Assert.AreEqual(2, id2);
            Assert.AreEqual(id1, obj1.Id);
            Assert.AreEqual(id2, obj2.Id);
        }

        [Test]
        public void Get_ReturnsRegisteredObject()
        {
            var service = new WorldObjectService();
            var obj = new WorldObject("A", Vector3.zero);
            int id = service.Register(obj);

            WorldObject result = service.Get(id);

            Assert.AreSame(obj, result);
        }

        [Test]
        public void Get_ReturnsNull_ForUnknownId()
        {
            var service = new WorldObjectService();

            Assert.IsNull(service.Get(999));
        }

        [Test]
        public void OccupyCells_And_GetObjectAtCell()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            WorldObjectService objects = world.Get<WorldObjectService>();

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Box", new Vector3Int(5, 0, 5));

            WorldObject found = objects.GetObjectAtCell(new Vector3Int(5, 0, 5));

            Assert.AreSame(obj, found);
        }

        [Test]
        public void VacateCells_RemovesFromSpatialIndex()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            WorldObjectService objects = world.Get<WorldObjectService>();

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Box", new Vector3Int(5, 0, 5));
            objects.VacateCells(obj);

            Assert.IsNull(objects.GetObjectAtCell(new Vector3Int(5, 0, 5)));
        }

        [Test]
        public void Remove_VacatesCellsAndUnregisters()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            WorldObjectService objects = world.Get<WorldObjectService>();

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Box", new Vector3Int(3, 0, 3));
            int id = obj.Id;
            objects.Remove(id);

            Assert.IsNull(objects.Get(id));
            Assert.IsNull(objects.GetObjectAtCell(new Vector3Int(3, 0, 3)));
        }

        [Test]
        public void GetObjectIdAtCell_ReturnsMinusOne_WhenEmpty()
        {
            var service = new WorldObjectService();

            Assert.AreEqual(-1, service.GetObjectIdAtCell(new Vector3Int(0, 0, 0)));
        }
    }

    // ── ObstacleService Tests ─────────────────────────────────────────────────────

    public class ObstacleServiceTests
    {
        [Test]
        public void RegisterObstacle_BlocksCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            ObstacleService obstacles = world.Get<ObstacleService>();

            WorldObject wall = TestWorldFactory.CreateRegisteredObject(world, "Wall", new Vector3Int(5, 0, 5));
            wall.AddFeature(new Obstacle(new List<Vector2Int> { Vector2Int.zero }));
            obstacles.RegisterObstacle(wall);

            Assert.IsTrue(obstacles.IsBlocked(new Vector3Int(5, 0, 5)));
            Assert.IsFalse(obstacles.IsBlocked(new Vector3Int(6, 0, 5)));
        }

        [Test]
        public void UnregisterObstacle_UnblocksCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            ObstacleService obstacles = world.Get<ObstacleService>();

            WorldObject wall = TestWorldFactory.CreateRegisteredObject(world, "Wall", new Vector3Int(5, 0, 5));
            wall.AddFeature(new Obstacle(new List<Vector2Int> { Vector2Int.zero }));
            obstacles.RegisterObstacle(wall);
            obstacles.UnregisterObstacle(wall.Id);

            Assert.IsFalse(obstacles.IsBlocked(new Vector3Int(5, 0, 5)));
            Assert.AreEqual(0, obstacles.ObstacleCount);
        }

        [Test]
        public void MultiCellObstacle_BlocksAllCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            ObstacleService obstacles = world.Get<ObstacleService>();

            WorldObject wall = TestWorldFactory.CreateRegisteredObject(world, "LongWall", new Vector3Int(0, 0, 0));
            wall.AddFeature(new Obstacle(new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
            }));
            obstacles.RegisterObstacle(wall);

            Assert.IsTrue(obstacles.IsBlocked(new Vector3Int(0, 0, 0)));
            Assert.IsTrue(obstacles.IsBlocked(new Vector3Int(1, 0, 0)));
            Assert.IsTrue(obstacles.IsBlocked(new Vector3Int(2, 0, 0)));
            Assert.IsFalse(obstacles.IsBlocked(new Vector3Int(3, 0, 0)));
        }
    }

    // ── InteractionService Tests ──────────────────────────────────────────────────

    public class InteractionServiceTests
    {
        [Test]
        public void TryInteract_FiresCallbacks_WhenTypeIdMatches()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            InteractionService interactions = world.Get<InteractionService>();

            // Interactable at (5,0,5)
            WorldObject chest = TestWorldFactory.CreateRegisteredObject(world, "Chest", new Vector3Int(5, 0, 5));
            var interactable = new Interactable(null);
            bool interactableFired = false;
            interactable.AddEntry(0, () => interactableFired = true);
            chest.AddFeature(interactable);
            interactions.RegisterInteractable(chest);

            // Interactor at (4,0,5) — adjacent to chest
            WorldObject player = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(4, 0, 5));
            var interactor = new Interactor();
            bool interactorFired = false;
            interactor.AddEntry(0, () => interactorFired = true);
            player.AddFeature(interactor);
            // Give the player a Mover so facing is available (facing right toward chest).
            var mover = new Mover(5f);
            mover.Facing = new Vector2(1f, 0f);
            player.AddFeature(mover);

            bool result = interactions.TryInteract(player);

            Assert.IsTrue(result);
            Assert.IsTrue(interactableFired);
            Assert.IsTrue(interactorFired);
        }

        [Test]
        public void TryInteract_ReturnsFalse_WhenNoMatchingTypeId()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            InteractionService interactions = world.Get<InteractionService>();

            // Interactable with TypeId=5
            WorldObject chest = TestWorldFactory.CreateRegisteredObject(world, "Chest", new Vector3Int(5, 0, 5));
            var interactable = new Interactable(null);
            interactable.AddEntry(5);
            chest.AddFeature(interactable);
            interactions.RegisterInteractable(chest);

            // Interactor with TypeId=0 — no match
            WorldObject player = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(4, 0, 5));
            var interactor = new Interactor();
            interactor.AddEntry(0);
            player.AddFeature(interactor);

            bool result = interactions.TryInteract(player);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryInteract_ReturnsFalse_WhenNotAdjacent()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            InteractionService interactions = world.Get<InteractionService>();

            // Interactable at (10,0,10)
            WorldObject chest = TestWorldFactory.CreateRegisteredObject(world, "Chest", new Vector3Int(10, 0, 10));
            var interactable = new Interactable(null);
            interactable.AddEntry(0);
            chest.AddFeature(interactable);
            interactions.RegisterInteractable(chest);

            // Interactor at (0,0,0) — far away
            WorldObject player = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(0, 0, 0));
            var interactor = new Interactor();
            interactor.AddEntry(0);
            player.AddFeature(interactor);

            bool result = interactions.TryInteract(player);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryInteract_ReturnsFalse_WhenNoInteractorFeature()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            InteractionService interactions = world.Get<InteractionService>();

            WorldObject player = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(0, 0, 0));
            // No Interactor feature added.

            bool result = interactions.TryInteract(player);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryInteract_BlocksSelfInteraction_ByDefault()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            InteractionService interactions = world.Get<InteractionService>();

            // Object is both interactable and interactor at the same cell.
            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "SelfObj", new Vector3Int(5, 0, 5));
            var interactable = new Interactable(null, allowSelfInteraction: false);
            interactable.AddEntry(0);
            obj.AddFeature(interactable);
            interactions.RegisterInteractable(obj);

            var interactor = new Interactor();
            interactor.AddEntry(0);
            obj.AddFeature(interactor);

            bool result = interactions.TryInteract(obj);

            Assert.IsFalse(result);
        }

        [Test]
        public void UnregisterInteractable_RemovesCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            InteractionService interactions = world.Get<InteractionService>();

            WorldObject chest = TestWorldFactory.CreateRegisteredObject(world, "Chest", new Vector3Int(5, 0, 5));
            var interactable = new Interactable(null);
            interactable.AddEntry(0);
            chest.AddFeature(interactable);
            interactions.RegisterInteractable(chest);

            Assert.IsTrue(interactions.IsInteractable(new Vector3Int(5, 0, 5)));

            interactions.UnregisterInteractable(chest.Id);

            Assert.IsFalse(interactions.IsInteractable(new Vector3Int(5, 0, 5)));
        }
    }

    // ── MovementService Tests ─────────────────────────────────────────────────────

    public class MovementServiceTests
    {
        [Test]
        public void Tick_MovesMoverInDirection()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(5, 0, 5));
            var mover = new Mover(10f);
            mover.Direction = new Vector2(1f, 0f); // Move right
            obj.AddFeature(mover);

            Vector3 startPos = obj.WorldPosition;
            world.TickAll(0.1f); // 10 units/sec * 0.1s = 1 unit

            Assert.Greater(obj.WorldPosition.x, startPos.x);
            Assert.AreEqual(startPos.z, obj.WorldPosition.z, 0.001f);
        }

        [Test]
        public void Tick_StopsAtObstacle()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            ObstacleService obstacles = world.Get<ObstacleService>();

            // Player at cell (5,0,5), moving right toward wall at cell (6,0,5).
            WorldObject player = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(5, 0, 5));
            var mover = new Mover(10f);
            mover.Direction = new Vector2(1f, 0f);
            player.AddFeature(mover);

            WorldObject wall = TestWorldFactory.CreateRegisteredObject(world, "Wall", new Vector3Int(6, 0, 5));
            wall.AddFeature(new Obstacle(new List<Vector2Int> { Vector2Int.zero }));
            obstacles.RegisterObstacle(wall);

            // Tick multiple times — player should never enter cell (6,0,5).
            for (int i = 0; i < 100; i++)
            {
                world.TickAll(0.016f);
            }

            Assert.Less(player.CellCoords.x, 6, "Player should not pass through the wall.");
        }

        [Test]
        public void Tick_ZeroDirection_StopsMovement()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(5, 0, 5));
            var mover = new Mover(10f);
            obj.AddFeature(mover);

            // Move right for a bit.
            mover.Direction = new Vector2(1f, 0f);
            world.TickAll(0.1f);

            // Stop.
            mover.Direction = Vector2.zero;
            world.TickAll(0.1f);

            Assert.AreEqual(Vector2.zero, mover.Velocity);
        }

        [Test]
        public void Tick_UpdatesFacing_WhenMoving()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Player", new Vector3Int(5, 0, 5));
            var mover = new Mover(10f);
            mover.Direction = new Vector2(0f, 1f); // Move forward (positive Z)
            obj.AddFeature(mover);

            world.TickAll(0.1f);

            // Facing should point upward (positive Z = positive Y in Vector2).
            Assert.Greater(mover.Facing.y, 0f);
        }
    }

    // ── UndergroundService Tests ──────────────────────────────────────────────────

    public class UndergroundServiceTests
    {
        [Test]
        public void IsImplicitlyBlocked_ReturnsFalse_ForSurfaceCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();

            Assert.IsFalse(underground.IsImplicitlyBlocked(new Vector3Int(5, 0, 5)));
        }

        [Test]
        public void IsImplicitlyBlocked_ReturnsTrue_ForUndugUndergroundCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();

            Assert.IsTrue(underground.IsImplicitlyBlocked(new Vector3Int(5, -1, 5)));
        }

        [Test]
        public void IsImplicitlyBlocked_ReturnsFalse_AfterDig()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            GridService grid = world.Get<GridService>();

            // Pre-populate the grid cell so DigCell can update it.
            Vector3Int cell = new Vector3Int(5, -1, 5);
            grid.Grid.SetCell(cell, new Cell(4));

            underground.DigCell(cell);

            Assert.IsFalse(underground.IsImplicitlyBlocked(cell));
        }

        [Test]
        public void IsRevealed_ReturnsFalse_BeforeDig()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();

            Assert.IsFalse(underground.IsRevealed(new Vector3Int(5, -1, 5)));
        }

        [Test]
        public void IsDug_ReturnsFalse_BeforeDig()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();

            Assert.IsFalse(underground.IsDug(new Vector3Int(5, -1, 5)));
        }

        [Test]
        public void DigCell_RevealsAdjacentCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            GridService grid = world.Get<GridService>();

            Vector3Int cell = new Vector3Int(5, -1, 5);
            grid.Grid.SetCell(cell, new Cell(4));

            // Pre-populate all 8 neighbors with real terrain (Rock = 5).
            Vector3Int[] neighbors = new Vector3Int[]
            {
                new Vector3Int(6, -1, 5),
                new Vector3Int(4, -1, 5),
                new Vector3Int(5, -1, 6),
                new Vector3Int(5, -1, 4),
                new Vector3Int(6, -1, 6),
                new Vector3Int(4, -1, 6),
                new Vector3Int(6, -1, 4),
                new Vector3Int(4, -1, 4),
            };
            foreach (Vector3Int n in neighbors)
            {
                grid.Grid.SetCell(n, new Cell(5));
            }

            underground.DigCell(cell);

            // The dug cell and all 8 neighbors should be revealed.
            Assert.IsTrue(underground.IsRevealed(cell));
            foreach (Vector3Int n in neighbors)
            {
                Assert.IsTrue(underground.IsRevealed(n), $"Neighbor {n} should be revealed");
            }
        }

        [Test]
        public void DigCell_UpdatesGridTileId_ToTypeBelowDugCell()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            GridService grid = world.Get<GridService>();
            WorldGenerationService generation = world.Get<WorldGenerationService>();

            Vector3Int cell = new Vector3Int(5, -1, 5);
            grid.Grid.SetCell(cell, new Cell(4));

            underground.DigCell(cell);

            // After digging, the cell's TileId should be the type from one level below.
            // If that level is empty (0), falls back to the cell's own generated type.
            int expectedFloorType = generation.GetTileId(cell.x, cell.y - 1, cell.z);
            if (expectedFloorType == 0)
            {
                expectedFloorType = generation.GetTileId(cell.x, cell.y, cell.z);
            }
            Assert.AreEqual(expectedFloorType, grid.Grid.GetCell(cell).TileId);
        }

        [Test]
        public void DigCell_DoesNotModifyCellAbove()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            GridService grid = world.Get<GridService>();

            Vector3Int cell = new Vector3Int(5, -1, 5);
            Vector3Int cellAbove = new Vector3Int(5, 0, 5);
            grid.Grid.SetCell(cell, new Cell(4));
            grid.Grid.SetCell(cellAbove, new Cell(3));

            underground.DigCell(cell);

            // Cell above keeps its original tile — rendering handles fog-of-war visually.
            Assert.AreEqual(3, grid.Grid.GetCell(cellAbove).TileId);
        }

        [Test]
        public void DigCell_CreatesObstacles_ForRevealedNonDugCells()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            ObstacleService obstacles = world.Get<ObstacleService>();
            GridService grid = world.Get<GridService>();

            Vector3Int cell = new Vector3Int(5, -1, 5);
            grid.Grid.SetCell(cell, new Cell(4));

            // Pre-populate neighbors with a real terrain type (Rock = 5).
            Vector3Int neighbor = new Vector3Int(6, -1, 5);
            grid.Grid.SetCell(neighbor, new Cell(5));

            underground.DigCell(cell);

            // The neighbor is revealed but not dug — it should have an obstacle.
            Assert.IsTrue(obstacles.IsBlocked(neighbor), "Revealed non-dug neighbor should be blocked by obstacle");
        }

        [Test]
        public void DigCell_RemovesObstacle_AtDugCell()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            ObstacleService obstacles = world.Get<ObstacleService>();
            GridService grid = world.Get<GridService>();

            // Dig cell A to reveal cell B and create an obstacle at B.
            Vector3Int cellA = new Vector3Int(5, -1, 5);
            Vector3Int cellB = new Vector3Int(6, -1, 5);
            grid.Grid.SetCell(cellA, new Cell(4));
            grid.Grid.SetCell(cellB, new Cell(5));

            underground.DigCell(cellA);
            Assert.IsTrue(obstacles.IsBlocked(cellB), "B should have obstacle after A is dug");

            // Now dig cell B — its obstacle should be removed.
            underground.DigCell(cellB);
            Assert.IsFalse(obstacles.IsBlocked(cellB), "B should not be blocked after being dug");
        }

        [Test]
        public void DigCell_FiresOnCellsRevealed()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            UndergroundService underground = world.Get<UndergroundService>();
            GridService grid = world.Get<GridService>();

            Vector3Int cell = new Vector3Int(5, -1, 5);
            grid.Grid.SetCell(cell, new Cell(4));

            IReadOnlyList<Vector3Int> revealedCells = null;
            underground.OnCellsRevealed += (cells) => revealedCells = cells;

            underground.DigCell(cell);

            Assert.IsNotNull(revealedCells, "OnCellsRevealed should have fired");
            Assert.Greater(revealedCells.Count, 0, "Should have revealed at least one cell");
            Assert.IsTrue(revealedCells.Contains(cell), "Revealed cells should include the dug cell");
        }

        [Test]
        public void MovementService_BlocksUndergroundMovement()
        {
            LogicWorld world = TestWorldFactory.CreateWithAllServices();
            GridService grid = world.Get<GridService>();

            // Set elevation to underground so movement resolves at y=-1.
            grid.Elevation = -1;

            // Place a mover underground at a dug cell.
            Vector3Int dugCell = new Vector3Int(5, -1, 5);
            Vector3Int undugCell = new Vector3Int(6, -1, 5);
            grid.Grid.SetCell(dugCell, new Cell(4));
            grid.Grid.SetCell(undugCell, new Cell(4));

            // Dig only the starting cell so the mover can be placed there.
            UndergroundService underground = world.Get<UndergroundService>();
            underground.DigCell(dugCell);

            WorldObject obj = TestWorldFactory.CreateRegisteredObject(world, "Digger", dugCell);
            var mover = new Mover(10f);
            mover.Direction = new Vector2(1f, 0f); // Move toward undug cell
            obj.AddFeature(mover);

            // Tick multiple times — should not enter the undug cell.
            for (int i = 0; i < 50; i++)
            {
                world.TickAll(0.016f);
            }

            Assert.Less(obj.CellCoords.x, 6, "Mover should not enter undug underground cell");
        }
    }
}
