# Technical Architecture

<!-- Owner: fill or update with technical architecture details. -->
<!-- Agents reference this to understand how systems connect. -->

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
