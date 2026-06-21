# TASK-011: Binary Lighting in Terrain Shader

| Field | Value |
|-------|-------|
| **Status** | COMPLETED |
| **Priority** | HIGH |
| **Owner** | programmer |
| **Created** | 2026-06-20 |
| **Updated** | 2026-06-20 |

## Objective

Update the terrain shader (`DualGridTile.shader`) to sample the global `_LightMap` texture and apply binary lit/unlit rendering. When lit: normal appearance (tile × color). When unlit: inverted (white outlines on black — `1.0 - tile.rgb`, no color tint).

## Relevant Files

- `Dungeon/Assets/Shaders/DualGridTile.shader` (modify)

## Requirements

### Light Map Sampling
- Declare `TEXTURE2D(_LightMap)` and `float4 _LightMapParams` (NOT in CBUFFER — these are global textures set by the renderer feature)
- Convert fragment's clip-space position to screen UV: `screenUV = positionCS.xy / _LightMapParams.xy` (where _LightMapParams.xy = screen width/height)
- Sample the light map: `half lit = step(0.5h, SAMPLE_TEXTURE2D(_LightMap, sampler_point_clamp, screenUV).r)`

### Binary Output
- `lit == 1`: current behavior — `tile.rgb * UnpackRGB(color)` composited via source-over
- `lit == 0`: inverted — `1.0 - tile.rgb` (no color multiplication), composited via source-over with same alpha
- The `step()` ensures no gradients — every pixel is fully one or the other

### Graceful Fallback
- If `_LightMap` is not set (renderer feature not active), the shader should default to fully lit. Unity sets unassigned global textures to white, so sampling white → `step(0.5, 1.0)` = 1 = lit. This works automatically.

### No Changes to Vertex Data
- The light map is sampled using screen-space position, not vertex attributes. No changes to ChunkRenderer.cs or vertex layout needed.

## Acceptance Criteria

- [ ] Terrain pixels in lit areas show normal tinted appearance
- [ ] Terrain pixels in unlit areas show inverted tile (white outlines, black fill)
- [ ] Transition is binary — no gradients or partial lighting
- [ ] Without the renderer feature active, terrain renders normally (fully lit fallback)
- [ ] Project compiles
- [ ] No assembly boundary violations

## Tests

- Manual: place a LightSource near terrain, verify lit/unlit boundary

## Dependencies

- TASK-010 (LightMap Renderer Feature) — provides `_LightMap` global texture

## Result

Added binary lighting support to `DualGridTile.shader`. Changes:

1. Declared global `TEXTURE2D(_LightMap)` and `float4 _LightMapParams` outside the CBUFFER (set by renderer feature, not per-material).
2. After the layer compositing loop, compute screen UV from `SV_POSITION` pixel coordinates divided by `_LightMapParams.xy` (screen dimensions).
3. Sample light map with `step(0.5)` for binary result — no gradients.
4. Lit pixels keep normal tinted appearance; unlit pixels get inverted RGB (`1.0 - result.rgb`), producing white outlines on black.
5. Reuses existing `sampler_point_clamp`. No new samplers needed.
6. Graceful fallback: unassigned `_LightMap` defaults to white texture, `step(0.5, 1.0) = 1` = fully lit.

No changes to vertex data layout, `Attributes`, `Varyings`, vertex shader, or any C# files.

**Files modified:**
- `Dungeon/Assets/Shaders/DualGridTile.shader`
