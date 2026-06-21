# TASK-010: LightMap Renderer Feature + LightSource Component

| Field | Value |
|-------|-------|
| **Status** | COMPLETED |
| **Priority** | HIGH |
| **Owner** | programmer |
| **Created** | 2026-06-20 |
| **Updated** | 2026-06-20 |

## Objective

Create a custom URP ScriptableRendererFeature that renders all light sources into a screen-space binary light map texture. This is the foundation for the game's custom 2D lighting system where every pixel is either fully lit or fully unlit.

## Relevant Files

- `Dungeon/Assets/Scripts/Visuals/` (new files go here)
- `Dungeon/Assets/Shaders/` (light rendering shader)

## Requirements

### LightSource Component (`Dungeon.Visuals` assembly)
- MonoBehaviour with serialized fields: `float Radius`, `Vector2 Offset`
- Position is derived from transform.position (XZ plane, Y ignored for 2D)
- Light shape is a hard-edged circle (no falloff — fully lit inside radius, fully dark outside)
- Public property to enable/disable at runtime

### LightMapRendererFeature (`Dungeon.Visuals` assembly)
- URP `ScriptableRendererFeature` + `ScriptableRenderPass`
- Allocates a screen-sized `RTHandle` for the light map (R8 or R16 format — only needs one channel)
- Render pass executes **before** main geometry rendering (`RenderPassEvent.BeforeRenderingOpaques`)
- Steps:
  1. Clear light map to black (all unlit)
  2. Find all active `LightSource` components in scene
  3. For each light: render a hard white circle at the light's screen position with screen-projected radius
  4. Set the result as global shader texture `_LightMap` via `Shader.SetGlobalTexture`
  5. Also set `_LightMapParams` (float4) with screen dimensions for UV calculation

### Light Circle Shader (`Dungeon/Assets/Shaders/LightCircle.shader`)
- Simple unlit shader used internally by the render pass
- Renders a quad as a hard circle: `step(length(uv - 0.5) , 0.5)` → white inside, transparent outside
- Additive blending (multiple lights accumulate — clamped to 1 by the R8 format or saturate)

### Integration
- The feature must be addable to the URP Renderer via the Unity Editor (standard ScriptableRendererFeature workflow)
- When no LightSource exists in the scene, the light map should be all black (everything unlit) or all white (everything lit) — use all white as default so the game works without lights during development
- The global `_LightMap` texture and `_LightMapParams` must be available to any shader that declares them

## Acceptance Criteria

- [ ] LightSource component can be added to any GameObject
- [ ] LightMapRendererFeature can be added to the URP Renderer asset
- [ ] Light map RT is created and cleared each frame
- [ ] Each LightSource renders a hard circle into the light map
- [ ] `_LightMap` is set as a global texture accessible by all shaders
- [ ] Multiple lights accumulate correctly (overlapping = still lit)
- [ ] No light sources in scene → light map defaults to white (fully lit fallback)
- [ ] Project compiles
- [ ] No assembly boundary violations

## Tests

- Manual: add LightSource to a GameObject, verify light map output via Frame Debugger

## Dependencies

None — this is the foundation.

## Human Dependencies

- Owner must add the LightMapRendererFeature to the URP Renderer asset after implementation

## Result

Implemented all three files for the custom 2D light map system.

### Files Created

- `Dungeon/Assets/Scripts/Visuals/Lighting/LightSource.cs` — MonoBehaviour with `Radius`, `Offset`, `Enabled` fields. Derives XZ position from transform. Namespace `Dungeon.Visuals.Lighting`.
- `Dungeon/Assets/Scripts/Visuals/Lighting/LightMapRendererFeature.cs` — `ScriptableRendererFeature` + inner `LightMapPass` (`ScriptableRenderPass`). Uses URP RenderGraph API (`AddUnsafePass`) to allocate a screen-sized R8_UNorm RT, clear it, draw light circles, and expose it as global `_LightMap` + `_LightMapParams`. Runs at `BeforeRenderingOpaques`.
- `Dungeon/Assets/Shaders/LightCircle.shader` — URP-compatible unlit shader rendering a hard circle via `step(length(uv-0.5), 0.5)`. Additive blend (One One), ZTest Always, no depth write.

### Key Design Decisions

- Used `AddUnsafePass` (RenderGraph API) rather than deprecated `Execute` path, matching Unity 6 / URP 17 best practices.
- Light world-to-screen projection uses camera right vector projected onto XZ plane for correct radius measurement regardless of camera angle.
- Quad mesh is drawn in NDC space with identity view/projection matrices for pixel-accurate placement.
- No-lights fallback clears to white (fully lit) so the game works during development without any LightSource in scene.

### Human Action Required

- Owner must add `LightMapRendererFeature` to the URP Renderer asset (`Dungeon/Assets/Settings/Renderer2D.asset`) via Unity Editor.
- Unity will auto-generate `.meta` files for the new `.cs` and `.shader` files on next import.
