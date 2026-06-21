# TASK-013: Dynamic Shadow Casting

| Field | Value |
|-------|-------|
| **Status** | COMPLETED |
| **Priority** | HIGH |
| **Owner** | programmer |
| **Created** | 2026-06-20 |
| **Updated** | 2026-06-20 |

## Objective

Add dynamic 2D shadow casting to the light map system. Shadow casters block light from reaching areas behind them, creating hard shadow regions (fully unlit — same inversion rules apply).

## Relevant Files

- `Dungeon/Assets/Scripts/Visuals/Lighting/ShadowCaster2DCustom.cs` (new)
- `Dungeon/Assets/Scripts/Visuals/Lighting/LightMapRenderPass.cs` (modify — add shadow rendering step)
- `Dungeon/Assets/Shaders/` (shadow geometry shader if needed)

## Requirements

### ShadowCaster2DCustom Component (`Dungeon.Visuals` assembly)
- MonoBehaviour that defines a shadow-casting shape
- Serialized `Vector2[] Points` defining a polygon in local space (or auto-derive from a Collider2D/BoxCollider if present)
- The component marks the object as a shadow caster; the render pass queries these

### Shadow Geometry Computation
- For each active LightSource × nearby ShadowCaster pair:
  1. Determine which edges of the shadow polygon face away from the light
  2. Extrude those edges away from the light to a large distance (shadow fins)
  3. Build a mesh from the original polygon + extruded fins = shadow volume
- This is standard 2D shadow volume / shadow fin geometry

### Shadow Rendering in LightMapRenderPass
- After rendering all light circles (additive white), render shadow geometry
- Shadow geometry renders as **black** with **Min blend** (or multiply) so it subtracts light
- Or alternatively: per-light approach — for each light, render light circle, then render that light's shadows as black. Whichever is simpler.
- End result: areas behind shadow casters (relative to the light) are black in the light map

### Performance Considerations
- Only process shadow casters within range of each light (distance < light radius + caster extents)
- Shadow geometry can be computed on CPU and uploaded as a dynamic mesh each frame
- Cache shadow meshes when neither light nor caster has moved

## Acceptance Criteria

- [ ] ShadowCaster component can be added to GameObjects
- [ ] Shadow casters block light from LightSources
- [ ] Shadows are hard-edged (binary — fully lit or fully shadow)
- [ ] Multiple shadow casters work with multiple lights
- [ ] Objects in shadow show inverted rendering (same as unlit areas)
- [ ] Performance is acceptable with ~20 lights and ~50 shadow casters
- [ ] Project compiles
- [ ] No assembly boundary violations

## Tests

- Manual: place shadow caster between light and terrain, verify shadow appears

## Dependencies

- TASK-010 (LightMap Renderer Feature)
- TASK-011 or TASK-012 (at least one shader must sample the light map to see shadows)

## Result

Implemented dynamic shadow casting for the 2D light map system using a per-light shadow subtraction approach.

### Approach
For each active LightSource, after rendering its light circle (additive white), shadow geometry is rendered using `BlendOp Min` to write black where shadows fall. Shadow geometry is computed CPU-side each frame: back-facing polygon edges are extruded away from the light to create shadow fin quads, and the caster polygon itself is rendered as a solid occluder. All geometry is projected to NDC to match the existing identity view/projection rendering approach.

### Files Created
- `Dungeon/Assets/Scripts/Visuals/Lighting/ShadowCaster2DCustom.cs` — MonoBehaviour defining a shadow-casting polygon. Supports serialized `Vector2[] Points`, auto-generation from `BoxCollider`, `CastsShadows` toggle, and `GetWorldPoints()` for world-space polygon retrieval.
- `Dungeon/Assets/Shaders/ShadowGeometry.shader` — `Hidden/Dungeon/ShadowGeometry` shader. Solid black output with `BlendOp Min, Blend One One` (only darkens), `ZTest Always`, `ZWrite Off`, `Cull Off`.

### Files Modified
- `Dungeon/Assets/Scripts/Visuals/Lighting/LightMapRendererFeature.cs` — Added shadow material loading in `Create()`/`Dispose()`. Extended `PassData` with shadow material and per-light `LightShadowData` struct (light quad params + shadow mesh). `RecordRenderGraph` now queries `ShadowCaster2DCustom` components, builds shadow meshes per-light via `BuildShadowMeshForLight`, and renders them after each light circle. Added mesh lifecycle management to prevent leaks. Helper methods: `WorldXZToNDC`, `AddPolygonToMesh`, `AddShadowFins`, `EstimateCasterExtent`.

### Notes
- Shadow meshes are rebuilt every frame (no caching yet — noted for future optimization).
- Distance culling skips casters beyond `lightRadius + casterExtent + 100` units from the light.
- Polygon winding assumed CCW for back-face detection; works for both manual points and BoxCollider auto-generation.
