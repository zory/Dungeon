using Dungeon.Logic.Core;
using UnityEngine;

namespace Dungeon.Logic.Services
{
    // System: processes all WorldObjects that have the Mover feature.
    // Each tick it applies acceleration/deceleration, computes new positions,
    // and resolves collisions against ObstacleService so movers cannot enter blocked cells.
    public class MovementService : ILogicService
    {
        private WorldObjectService _objects;
        private ObstacleService _obstacles;
        private GridService _grid;
        private UndergroundService _underground;

        public void Initialize(LogicWorld world)
        {
            _objects      = world.Get<WorldObjectService>();
            _obstacles    = world.Get<ObstacleService>();
            _grid         = world.Get<GridService>();
            _underground  = world.Get<UndergroundService>();
        }

        public void Tick(float deltaTime)
        {
            foreach (var kvp in _objects.All)
            {
                WorldObject obj = kvp.Value;
                if (obj.TryGetFeature<Mover>(out Mover mover))
                {
                    ProcessMover(obj, mover, deltaTime);
                }
            }
        }

        private void ProcessMover(WorldObject obj, Mover mover, float deltaTime)
        {
            // Compute target velocity from desired direction.
            Vector2 direction = mover.Direction;
            if (direction.sqrMagnitude > 1f)
            {
                direction = direction.normalized;
            }
            Vector2 targetVelocity = direction * mover.MaxSpeed;

            // Accelerate toward target velocity.
            if (mover.Acceleration <= 0f)
            {
                // Instant response — no inertia.
                mover.Velocity = targetVelocity;
            }
            else
            {
                // Smooth transition. Acceleration = time constant in seconds.
                // 0.2 means ~63% of target reached in 0.2s. Higher = more sluggish.
                float t = Mathf.Clamp01(deltaTime / mover.Acceleration);
                mover.Velocity = Vector2.Lerp(mover.Velocity, targetVelocity, t);
            }

            // Dead-zone — stop tiny drifts.
            if (mover.Velocity.sqrMagnitude < 0.0001f)
            {
                mover.Velocity = Vector2.zero;
                return;
            }

            // Compute candidate position.
            Vector3 currentPos = obj.WorldPosition;
            Vector3 candidatePos = currentPos + new Vector3(mover.Velocity.x, 0f, mover.Velocity.y) * deltaTime;

            // Resolve obstacle collisions.
            Vector3 resolvedPos = ResolveCollision(currentPos, candidatePos);

            // If collision stopped movement on an axis, zero that velocity component.
            if (Mathf.Approximately(resolvedPos.x, currentPos.x) && !Mathf.Approximately(candidatePos.x, currentPos.x))
            {
                mover.Velocity = new Vector2(0f, mover.Velocity.y);
            }
            if (Mathf.Approximately(resolvedPos.z, currentPos.z) && !Mathf.Approximately(candidatePos.z, currentPos.z))
            {
                mover.Velocity = new Vector2(mover.Velocity.x, 0f);
            }

            // Update facing to match movement direction.
            if (mover.Velocity.sqrMagnitude > 0.001f)
            {
                mover.Facing = mover.Velocity.normalized;
            }

            obj.SetPosition(resolvedPos, _grid.CellSize, _grid.XZOffset, _grid.Elevation);
        }

        // Returns true if a cell is blocked by any obstacle or by unrevealed underground.
        private bool IsCellBlocked(Vector3Int cell)
        {
            return _obstacles.IsBlocked(cell) || _underground.IsImplicitlyBlocked(cell);
        }

        // Per-axis collision resolution for wall sliding.
        // Tries full movement first; if blocked, tries each axis independently.
        private Vector3 ResolveCollision(Vector3 current, Vector3 candidate)
        {
            float cellSize = _grid.CellSize;
            Vector2 offset = _grid.XZOffset;
            int elevation = _grid.Elevation;

            // Fast path: target cell is clear — allow full movement.
            Vector3Int candidateCell = WorldObject.ComputeCell(candidate, cellSize, offset, elevation);
            if (!IsCellBlocked(candidateCell))
            {
                return candidate;
            }

            // Target blocked — try sliding along each axis independently.
            float finalX = candidate.x;
            float finalZ = candidate.z;

            // X axis only.
            Vector3Int xCell = WorldObject.ComputeCell(new Vector3(candidate.x, current.y, current.z), cellSize, offset, elevation);
            if (IsCellBlocked(xCell))
            {
                finalX = current.x;
            }

            // Z axis only.
            Vector3Int zCell = WorldObject.ComputeCell(new Vector3(current.x, current.y, candidate.z), cellSize, offset, elevation);
            if (IsCellBlocked(zCell))
            {
                finalZ = current.z;
            }

            // Final diagonal check — if the combined result is still blocked, don't move.
            Vector3 resolved = new Vector3(finalX, current.y, finalZ);
            Vector3Int resolvedCell = WorldObject.ComputeCell(resolved, cellSize, offset, elevation);
            if (IsCellBlocked(resolvedCell))
            {
                return current;
            }

            return resolved;
        }

        public void Dispose() { }
    }
}
