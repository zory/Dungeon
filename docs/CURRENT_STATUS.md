# Current Status

Last updated: 2026-06-20

## What Is Done

- Assembly structure: Logic / Visuals / UI with proper dependency boundaries
- Sparse infinite grid with chunk loading and streaming
- WorldObject entity system with feature composition (Locomotion, Interactor, Interactable)
- Dual-grid autotile rendering system (16 asymmetric tiles per terrain, color tinting, single draw call per chunk)
- TerrainAtlas ScriptableObject (replaces old DualGridAtlas + TileColorRegistry)
- Custom binary lighting system:
  - LightMapRendererFeature (screen-space light map, hard-edged circles)
  - LightSource component (radius, offset, enable/disable)
  - Binary lit/unlit rule in terrain shader (lit = normal, unlit = inverted RGB)
  - LitObject shader for sprites/objects (same binary lighting)
  - Dynamic shadow casting (ShadowCaster2DCustom, shadow fin extrusion, BlendOp Min)
- Camera drag panning
- Procedural world generation
- Basic character system (LivingBrother)
- CI pipeline (GitHub Actions with game-ci, Discord notifications)
- Agent workflow structure
