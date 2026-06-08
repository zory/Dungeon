# Dungeon — Technical Architecture

## Guiding Principles

1. **Logic / Visuals / UI separation.** The Logic assembly is pure C# with no Unity rendering dependencies. The entire game state can advance without a renderer. Visuals read from Logic and push input into it. UI reads from both Visuals and Logic.
2. **Composition over inheritance.** Every entity is a `WorldObject`. Behaviour is attached as feature objects, not subclasses.
3. **Systems, not objects.** Game behaviour lives in stateless (or minimally stateful) systems that iterate over all objects with a relevant feature. Objects are data; systems are logic.
4. **Infinite world by default.** The grid is a sparse dictionary. Chunk streaming is the norm, not an afterthought.

---

## Assembly Structure

```
Dungeon.Logic         (pure C#, no UnityEngine.Rendering)
  └─ Grid             Grid, Cell, GridManager, ChunkLoader, TileType, WorldDataSource
  └─ WorldObject      WorldObject, WorldObjectRegistry, all Feature types
  └─ Systems          All game-update systems (movement, combat, energy, AI, …)
  └─ Serialisation    LevelData, SaveFile, JSON serialisers/deserialisers

Dungeon.Visuals       (depends on Dungeon.Logic + UnityEngine)
  └─ Grid             GridRenderer, ChunkRenderer, WorldRenderer, TileRegistry, TileSheet
  └─ Character        CharacterView, all visual character components
  └─ Camera           CameraDrag, CameraController
  └─ Input            PlayerInputController, InteractInputController, IMovementInput
  └─ LevelEditor      EditorToolController, EditorPalette, EditorState
                      (authoring tools live here; serialisation calls into Logic)

Dungeon.UI            (depends on both — skipped for now, inspector-based interim)
```

**Dependency rule:** `Logic` must never reference `Visuals` or `UI`. `Visuals` may reference `Logic`. `UI` may reference both.

---

## Entity System

### WorldObject
Every entity — characters, trees, traps, furniture, monsters, items — is a `WorldObject`. Position is stored as both a continuous `Vector3` (WorldPosition on XZ) and a discrete `Vector3Int` (CellCoords: X=col, Y=elevation, Z=row).

### Features (composition)
Features are plain C# classes attached to a WorldObject via `AddFeature<T>`. Systems query `HasFeature<T>` or `TryGetFeature<T>` to act on relevant objects.

| Feature | Description |
|---------|-------------|
| `Locomotion` | Can move through the world; holds speed, current velocity, pathfinding state |
| `Interactor` | Can initiate interactions with other objects |
| `Interactable` | Can be the target of interactions; holds interaction handler |
| `Health` | Has HP, can take damage, can die |
| `Energy` | Holds life-energy (dead brother specific); drains over time, replenished by kills |
| `Inventory` | Holds a list of items |
| `Fighter` | Can perform direct combat actions (attack, block) |
| `Faction` | Belongs to a faction; governs hostile/friendly relationships |
| `AIBrain` | Drives autonomous behaviour; holds behaviour state (explore, fight, flee, schedule-follow, etc.) |
| `WaypointRoute` | Ordered list of cell waypoints; used by patrol AI and NPC schedules |
| `Schedule` | Time-keyed waypoint + action list for NPC daily routines (villagers, adventurers) |
| `DialogueSpeaker` | Holds a list of speech lines triggered on proximity interaction; no branching for now |
| `TrapBehaviour` | Trigger condition + effect definition for trap objects |
| `Container` | Holds an item list; any character with `Interactor` can open and transfer items (shared chests, loot piles) |
| `ShopKeeper` | Extends Container with pricing and buy/sell rules (living brother's shop counter) |
| `PlayerControllable` | Marks this object as selectable and directly controllable by the player |
| `Mineable` | Can be mined; holds durability, yields item drops on depletion |

New behaviour = new feature + a system that processes it. No inheritance.

---

## Systems

Systems are updated each game tick by a central `GameLoop` (or Unity Update in Visuals). Each system receives relevant WorldObjects from `WorldObjectRegistry`.

| System | Responsibility |
|--------|----------------|
| `MovementSystem` | Advances positions of all Locomotion-bearing objects; resolves collisions with grid cells |
| `PathfindingSystem` | Computes paths on the Logic grid for AI and player move-to commands |
| `CombatSystem` | Processes Fighter attacks, applies damage via Health feature |
| `EnergySystem` | Drains dead brother energy per tick; triggers low-energy warnings |
| `AISystem` | Ticks AIBrain on each AI-controlled object; drives adventurer and monster behaviour |
| `InteractionSystem` | Resolves Interactor → Interactable pairs within range |
| `TrapSystem` | Evaluates TrapBehaviour trigger conditions each tick |
| `ScheduleSystem` | Advances NPC Schedule features; issues move-to and action commands at keyed times |
| `DialogueSystem` | Triggers DialogueSpeaker lines when an Interactor enters proximity |
| `ContainerSystem` | Handles open/close and item-transfer interactions between Interactor and Container |
| `ShopSystem` | Extends ContainerSystem with pricing, buy/sell transaction logic |
| `MiningSystem` | Handles mine actions; updates Cell tile type in the Grid, adds material drops |
| `ChunkSystem` | Determines which chunks need loading/unloading based on view position |
| `SpawnSystem` | Spawns adventurers at configured entry points on a time schedule |

---

## Grid & World

### Grid
- Sparse `Dictionary<Vector3Int, Cell>` where `Y` = elevation layer.
- `Cell` holds: `TileTypeId` (terrain), list of `WorldObject` IDs occupying it.
- Access via `GridManager` which owns the single `Grid` instance.

### Elevation Layers
- Each layer is a complete 2D XZ plane at a fixed integer Y.
- `Y=0` — surface / village.
- `Y=-1, -2, …` — underground dungeon floors.
- Layers are independent; cells at `(5, 0, 3)` and `(5, -1, 3)` are unrelated neighbours.
- The visual layer renders only the currently active elevation. Layer switch = camera teleport + render layer toggle.
- Stairs/portals are `Interactable` WorldObjects that, when used, call `SetActiveElevation(int)` on the view controller.

### Chunk Streaming
- `WorldConstants.ChunkSize` (currently implied 16) defines cells per chunk side.
- `ChunkLoader` (Logic) tracks loaded chunk coords and fires `OnChunksLoaded` events.
- `ChunkRenderer` (Visuals) listens and builds/destroys tile meshes.
- For authored level data, chunks are generated from the saved `LevelData` instead of `ProceduralWorldDataSource`.

---

## Save / Load System

### LevelData Format (JSON)
A level file covers a single elevation layer. Multiple files can be loaded at startup to populate all layers.

```json
{
  "metadata": { "name": "village", "elevation": 0, "version": 1 },
  "cells": [
    { "x": 0, "y": 0, "z": 0, "tileTypeId": 1 },
    ...
  ],
  "objects": [
    { "typeId": "chest_shared",    "x": 3,  "y": 0,  "z": 2,  "properties": {} },
    { "typeId": "stairs_down",     "x": 5,  "y": 0,  "z": 5,  "properties": { "targetElevation": -1, "targetX": 5, "targetZ": 5 } },
    { "typeId": "npc_villager",    "x": 10, "y": 0,  "z": 8,  "properties": {} },
    { "typeId": "brother_living",  "x": 6,  "y": 0,  "z": 3,  "properties": {} }
  ]
}
```

- `cells` is a flat list of non-default cells (sparse — empty/default cells are omitted).
- `objects` entries carry a `typeId` looked up in an `ObjectDefinitionRegistry` at load time; the registry provides the feature configuration for each type.
- Stairs/portals carry target elevation + XZ coords in `properties` so the `InteractionSystem` knows where to teleport.
- The file is written and read entirely by `Dungeon.Logic.Serialisation` — no Unity-specific types.

### MVP Level Files
| File | Elevation | Contents |
|------|-----------|----------|
| `village.json` | 0 | Village buildings, shop, cemetery, crypt entrance, shared chest, villager spawn, living brother spawn, adventurer entry point |
| `crypt_floor1.json` | -1 | Small starting room, stairs up, dead brother spawn — all surrounding cells are solid rock |

### Runtime Save (game state)
- Separate from level data. Stores dynamic state: object positions, inventories, energy, quest progress, etc.
- Same JSON approach with a `GameSaveData` root object.

---

## Level Editor

### Architecture
- The editor is a visual-layer overlay; it does NOT require a separate Unity scene.
- A single bool `EditorModeActive` on the `EditorState` (Visuals) switches tool overlays on/off.
- The normal play scene and the editor scene are identical; only the `EditorToolController` presence distinguishes them (can be on a separate GameObject toggled by a menu item or keyboard shortcut).

### Editor Data Flow
```
Designer clicks cell
  → EditorInputHandler (Visuals) reads mouse world position
  → Converts to CellCoords (Logic formula)
  → EditorState.PlaceObject(typeId, cellCoords) or PaintTile(tileTypeId, cellCoords)
  → Mutates a LevelData object in memory (Logic)
  → EditorVisualSync (Visuals) listens to LevelData changes, updates tile meshes/object visuals
  → Ctrl+S → LevelDataSerialiser.Save(levelData, filePath) writes JSON
```

### Loading Back at Runtime
```
SceneBootstrap reads level file path (from config or scene parameter)
  → LevelDataSerialiser.Load(filePath) → LevelData
  → LevelLoader.Apply(levelData, grid, worldObjectRegistry)
    → Sets cells in Grid
    → Instantiates WorldObjects via factory, adds features, registers in WorldObjectRegistry
  → ChunkRenderer builds meshes for populated chunks
  → Game starts
```

---

## Character Control & Brother Switching

- Both brothers have the `PlayerControllable` feature.
- `PlayerController` (Visuals) holds a reference to the currently active `WorldObject`.
- Switching brothers: click portrait → `PlayerController.SetControlTarget(WorldObject)` → camera follows new target, input routes to new target.
- All movement and actions are routed through `IMovementInput` / action interfaces so the same input stack works for any controllable entity.

---

## Work Plan

### Phase 1 — Foundation (current state)
- [x] Sparse infinite Grid (XZ + Y elevation)
- [x] Chunk loading & streaming
- [x] WorldObject + feature composition
- [x] Locomotion, Interactor, Interactable
- [x] CharacterView, PlayerInputController
- [x] Procedural world generation

### Phase 2 — World Persistence & Level Editor
- [ ] `LevelData` serialisation model (Logic)
- [ ] `LevelDataSerialiser` JSON read/write (Logic)
- [ ] `EditorState` + `EditorToolController` (Visuals)
- [ ] Tile paint tool (terrain per cell per elevation)
- [ ] Object placement tool (place/move/delete WorldObjects)
- [ ] Elevation layer switching in editor and play
- [ ] Scene bootstrap that loads a level file at startup

### Phase 3 — Core Character Systems
- [ ] `Health` feature + `CombatSystem`
- [ ] `Energy` feature + `EnergySystem` (dead brother drain/replenish)
- [ ] `Faction` feature
- [ ] `Inventory` feature
- [ ] Brother-switching `PlayerController`
- [ ] Elevation portal interaction (stairs)

### Phase 4 — AI & Characters
- [ ] `AIBrain` feature + `AISystem`
- [ ] Pathfinding (A* on Logic grid, single elevation layer)
- [ ] `Schedule` feature + `ScheduleSystem` (villager daily route)
- [ ] `DialogueSpeaker` feature + `DialogueSystem` (proximity speech lines)
- [ ] Adventurer spawn logic (`SpawnSystem`, time-based schedule)
- [ ] Adventurer behaviour: explore → fight → flee
- [ ] `WaypointRoute` + monster patrol AI

### Phase 5 — Gameplay Loops
- [ ] `MiningSystem` + mine action for dead brother (yields materials)
- [ ] `Container` feature + `ContainerSystem` (shared chests, loot piles)
- [ ] Loot flow: dead brother deposits → living brother withdraws
- [ ] `ShopKeeper` feature + `ShopSystem` (buy/sell at shop counter)
- [ ] Trap placement + `TrapSystem`
- [ ] Dead brother uses mined materials to place objects and traps

### Phase 6 — Story & Dialogue
- [ ] Dialogue system (data-driven, JSON or ScriptableObject)
- [ ] Narrative milestone triggers
- [ ] NPC schedules and conversations
- [ ] Story chapter progression

### Phase 7 — Polish
- [ ] Sound & music hooks
- [ ] Particle/VFX hooks
- [ ] UI layer (proper HUD replacing inspector-based interim)
- [ ] Save/load game state (runtime persistence)
- [ ] Settings, resolution, keybinding

---

## Coding Standards (from TechnicalNotes.md)

- Explicit types everywhere unless line becomes unwieldy.
- Explicit, understandable variable names.
- Do not remove existing comments; add comments where logic is non-obvious.
- Always use braces, even for single-line if/for bodies.
- Prefer longer lines over unnecessary splits (ultrawide monitor workflow).
- Public members: `PascalCase`
- Private members: `_lowerCamelCaseWithLeadingUnderscore`
- Constants: `SCREAMING_SNAKE_CASE`
