# Technical Architecture

Avoid statics is possible as they are breaking testing
There is bootloader monobehaviour which creates world and ticks services.
World contains Services which are not monobehaviours but they can be accessed though world like world.GetService. All services knows their world. Services are managers whichs has stuff like update.
Services can have state and initial parameters, but they are provided from monobehaviours or scriptable objects which is synced between each other.
Entire architecture is split into logic (non unity, no monobehaviours) entire game logic and state should be here. Entire game should be possible to run only with logic if input replaced with some kind of cammand line for example.
Input and visuals are in visuals which basically registers to logic and calls logic public api
UI basically registers to logic and visuals and calls their public api.
Each system have service which controls its instances. WorldObject can contain multiple types and can belong to multiple systems. One system can reference another system, but preferably not
Most of the things should be quite generic. For example character should basically be same character just one character is controlled by some script of ai, while another by player, but via same interfaces.
World editor is important part of the game authoring. Almost everything should be authorable and created in unity inspector, have its own parameters for authoring, images dragged and so on and from there it is serialized into save file and can be fully restored into game state. Any moment in game can be fully recreated with save file.
For dialogs I will use inkle. Most dialog system will be ui popups with images.
Game will be moddable so most of the things will be converted to lua later on, but this is final strech goal.

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

Grid is endless. It can have different elevations. One elevation at a time is enought to see. Grid can be generated or drawn in editor or deserialized from save file.
Each grid cell can have world object associated. One object can be in multiple cells at once (bigger object), but not vise versa. It might be different later on, but for now one object at one time. Also cell can have 2 types (two layers) of ground. One ground is native ground like water, grass, and on top of that special layer like stone path.

## Save / Load

Any logical state can be saved or be loaded into logic. From any logic state visuals and ui can be loaded.

## Level Editor

Authoring is super important. Creating worldObjects, drawing world, changing elevation, creating dialogs, basically entire game should have authoring tools. Inspector and editor windows are enough for now, dont need UI for everything