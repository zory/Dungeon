# TASK-012: Lit Object Shader

| Field | Value |
|-------|-------|
| **Status** | COMPLETED |
| **Priority** | HIGH |
| **Owner** | programmer |
| **Created** | 2026-06-20 |
| **Updated** | 2026-06-20 |

## Objective

Create a new URP shader `Dungeon/LitObject` for sprites and non-terrain objects (obstacles, characters, items). Same binary lighting rules as the terrain shader: lit = normal sprite, unlit = inverted RGB (white outlines, black fill). This shader replaces the default sprite shader for all game objects.

## Relevant Files

- `Dungeon/Assets/Shaders/` (new shader: `LitObject.shader`)

## Requirements

### Shader: `Dungeon/LitObject`
- URP-compatible, SRP Batcher friendly
- Transparent blend mode (SrcAlpha OneMinusSrcAlpha), ZWrite Off, Cull Off
- Properties: `_MainTex` (sprite texture), `_Color` (tint color, default white)
- Vertex input: POSITION, TEXCOORD0 (UV), COLOR (vertex color — Unity sprites use this)

### Light Map Sampling
- Same approach as terrain shader: declare global `_LightMap` + `_LightMapParams`
- Convert clip position to screen UV, sample light map
- `lit = step(0.5, lightMapSample.r)`

### Binary Output
- `lit == 1`: `sprite.rgba * _Color * vertexColor` (standard sprite rendering)
- `lit == 0`: `half4(1.0 - sprite.rgb, sprite.a) * vertexColor.a` — inverted RGB, preserve alpha for shape. No color tint in darkness.
- Binary step, no gradients

### Graceful Fallback
- Without `_LightMap` set → defaults to white → fully lit → normal sprite rendering

## Acceptance Criteria

- [ ] New shader file compiles
- [ ] Sprites using this shader render normally when lit
- [ ] Sprites using this shader show inverted RGB when unlit
- [ ] Vertex color from Unity's sprite system is respected
- [ ] Without renderer feature, sprites render normally
- [ ] Project compiles
- [ ] No assembly boundary violations

## Tests

- Manual: assign shader to a sprite material, place near LightSource, verify lit/unlit

## Dependencies

- TASK-010 (LightMap Renderer Feature) — provides `_LightMap` global texture

## Result

### Created
- `Dungeon/Assets/Shaders/LitObject.shader` — new URP shader `Dungeon/LitObject`

### Summary
Created a simple sprite shader with binary lighting from the global light map. The shader:
- Accepts `_MainTex` (sprite texture) and `_Color` (tint) as material properties inside `CBUFFER_START(UnityPerMaterial)` for SRP Batcher compatibility.
- Reads vertex COLOR semantic for Unity sprite vertex colour/alpha.
- Samples the global `_LightMap` using screen-space UV (SV_POSITION pixel coords / `_LightMapParams.xy`).
- Lit regions: standard sprite rendering with texture, tint, and vertex colour applied.
- Unlit regions: inverted RGB from raw texture only (no colour tint), alpha preserved from texture and vertex colour for shape visibility.
- Graceful fallback: without the LightMap renderer feature, Unity provides a white default texture, so `step(0.5, 1.0) = 1` and sprites render normally.

### Notes
- `_LightMap` and `_LightMapParams` are declared outside the CBUFFER as global shader variables, matching the terrain shader pattern.
- Uses `sampler_point_clamp` for both sprite and light map sampling, consistent with the pixel-art style.
- `LightMode` tag set to `SRPDefaultUnlit` to match the terrain shader pass.
