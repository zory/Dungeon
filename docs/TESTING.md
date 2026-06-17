# Testing Strategy

## Test Assemblies

| Assembly | Type | References |
|----------|------|------------|
| `Dungeon.Tests.EditMode` | Editor-only tests | Dungeon.Logic |
| `Dungeon.Tests.PlayMode` | Runtime tests | Dungeon.Logic |

## Priority Areas

1. Pure Logic tests (Grid, Cell, WorldObject, Features)
2. Serialization (LevelData round-trip)
3. Procedural generation determinism
4. System behaviour (movement, interaction, combat)

## Running Tests

- **Local:** `scripts/run-unity-tests.ps1 -TestMode editmode`
- **CI:** Automatic on push/PR to main via GitHub Actions

## Coverage

<!-- Tester: update as test coverage grows. -->

## Current Test Files

- `Assets/Tests/EditMode/SampleEditModeTests.cs` — placeholder
- `Assets/Tests/PlayMode/SamplePlayModeTests.cs` — placeholder
