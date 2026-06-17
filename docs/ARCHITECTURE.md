# Technical Architecture

Avoid statics is possible as they are breaking testing
There is bootloader monobehaviour which creates world and ticks services.
World contains Services which are not monobehaviours but they can be accessed though world like world.GetService. All services knows their world. Services are managers whichs has stuff like update.
Services can have state and initial parameters, but they are provided from monobehaviours or scriptable objects which is synced between each other.
Entire architecture is split into logic (non unity, no monobehaviours) entire game logic and state should be here. Entire game should be possible to run only with logic if input replaced with some kind of cammand line for example.
Input and visuals are in visuals which basically registers to logic and calls logic public api
UI basically registers to logic and visuals and calls their public api.

## Guiding Principles

1. **Logic / Visuals / UI separation.** Logic is pure C# with no Unity rendering dependencies.
2. **Composition over inheritance.** Every entity is a `WorldObject` with feature components.
3. **Systems, not objects.** Game behaviour lives in stateless systems that iterate over objects.
4. **Infinite world by default.** Sparse dictionary grid, chunk streaming is the norm.

## Assembly Structure

```
Dungeon.Logic         (pure C#, no UnityEngine.Rendering)
  └─ Grid             Grid, Cell, TileType
  └─ WorldObject      WorldObject, Features
  └─ Services         GridService, WorldGenerationService, ChunkLoadingService, WorldObjectService
  └─ Serialisation    LevelData, serializers

Dungeon.Visuals       (depends on Dungeon.Logic + UnityEngine)
  └─ Grid             ChunkRenderer, DualGridAtlas, TileRegistry, TileSheet
  └─ Authoring        MonoBehaviour authorings for Logic services
  └─ Services         CameraService, CharacterService, EditorService, GridRenderService, etc.
  └─ LevelEditor      LevelDataSerializer, ObjectDefinitionRegistry

Dungeon.UI            (depends on both — currently empty)
```

## Entity System

<!-- Describe WorldObject, Features, and how systems process them. -->

## Grid & World

<!-- Describe the sparse grid, elevation layers, chunk streaming. -->

## Save / Load

<!-- Describe LevelData format, level files, runtime save. -->

## Level Editor

<!-- Describe editor data flow, tools, serialization. -->
