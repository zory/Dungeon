# Per-Layer Rendering Migration TODO

The dual-grid renderer now uses per-layer rendering (one sub-mesh per terrain type)
instead of the old primary/secondary two-color blend. You need new atlas textures
and a small Unity inspector update before it works.

Delete each section as you complete it.

---

## 1. Draw Shape Atlas Texture

The old atlas combined fill + outline + alpha-for-blending into one texture.
The new system uses alpha purely for terrain presence (1 = terrain here, 0 = see-through).

Create a new texture with the same 8-tile layout your current atlas uses
(same grid dimensions, same bitmask-to-slot mapping configured in the DualGridAtlas asset).

Each tile is just a solid silhouette:

- **Alpha = 1** where the terrain shape is filled
- **Alpha = 0** where it is not (transparent)
- **RGB does not matter** (shader ignores it; white is fine)
- **No outlines** in this texture

The 8 tiles and their bitmask shapes (NW=1, NE=2, SW=4, SE=8):

```
Tile 0 - bitmask 15 (FULL)        Tile 1 - bitmask 2 (NE corner)
+--------+                        +--------+
|XXXXXXXX|                        |    XXXX|
|XXXXXXXX|                        |      XX|
|XXXXXXXX|                        |        |
|XXXXXXXX|                        |        |
+--------+                        +--------+

Tile 2 - bitmask 1 (NW corner)    Tile 3 - bitmask 3 (N edge)
+--------+                        +--------+
|XXXX    |                        |XXXXXXXX|
|XX      |                        |XXXXXXXX|
|        |                        |        |
|        |                        |        |
+--------+                        +--------+

Tile 4 - bitmask 4 (SW corner)    Tile 5 - bitmask 10 (E edge)
+--------+                        +--------+
|        |                        |    XXXX|
|        |                        |    XXXX|
|XX      |                        |    XXXX|
|XXXX    |                        |    XXXX|
+--------+                        +--------+

Tile 6 - bitmask 6 (NE+SW diag)   Tile 7 - bitmask 7 (all except SE)
+--------+                        +--------+
|    XXXX|                        |XXXXXXXX|
|      XX|                        |XXXXXXXX|
|XX      |                        |XX      |
|XXXX    |                        |XXXX    |
+--------+                        +--------+
```

X = alpha 1 (opaque), blank = alpha 0 (transparent).

The complement trick still works: the shader inverts alpha at runtime for the
8 undrawn bitmasks (0, 13, 14, 12, 11, 5, 9, 8), so you only draw these 8.

Corner tiles (1, 2, 4) should use a smooth quarter-circle curve, not a hard diagonal.

Save as PNG with transparency. Assign to the DualGridAtlas asset's **Atlas** slot
(replacing the old texture).

---

## 2. Draw Outline Atlas Texture (optional, skip if you don't want outlines yet)

Same 8-tile grid layout as the shape atlas. Each tile has thin border lines
(1-2 px) at the transition edge between filled and empty regions.

- **RGB = outline colour** (typically black: 0,0,0)
- **Alpha = 1** on border pixels, **alpha = 0** everywhere else
- Draw the border on **BOTH sides** of the transition edge (1 px inside + 1 px outside
  the shape boundary). This is needed because the complement trick inverts the shape
  but NOT the outline -- having pixels on both sides ensures each layer sees its
  outline within its own filled region.

```
Example: Tile 3 (N edge) outline

+--------+
|        |   <- inside the shape (alpha=1 in shape atlas)
|        |
|========|   <- border line: 1px above + 1px below the midline, both alpha=1
|        |
|        |   <- outside the shape (alpha=0 in shape atlas)
+--------+
```

Save as PNG with transparency. Assign to the DualGridAtlas asset's **Outline Atlas** slot.

If you leave this slot empty, terrains render as flat colour fills with no borders
(a 1x1 transparent fallback is used automatically).

---

## 3. Update DualGridAtlas Asset in Inspector

Open `Dungeon/Assets/Data/DualGridAtlas.asset` in the Unity inspector:

- **Atlas** slot: assign your new shape atlas texture
- **Outline Atlas** slot: assign your outline atlas texture (or leave empty)
- **Columns / Rows**: keep matching your texture grid (currently 5x5, adjust if changed)
- **Tile Bitmasks**: no change needed (same 8 bitmasks: 15, 2, 1, 3, 4, 10, 6, 7)

---

## 4. Check Material

The material using shader `Dungeon/DualGridTile` should auto-pick up both textures
from the atlas asset at runtime. But verify in the inspector that:

- Shader is still set to `Dungeon/DualGridTile`
- If the material inspector shows `Shape Atlas` and `Outline Atlas` slots,
  you can leave them empty -- the C# code sets them from the DualGridAtlas asset

---

## 5. Verify TileColorRegistry Priorities

Priority order now determines layer stacking (lowest = bottom, highest = top).
Previously priority only decided which type "won" at a two-type merge. Now every
type is visible, and priority controls which draws on top at overlaps.

Open `Dungeon/Assets/Data/TileRegistry.asset` and confirm the priority values
produce the visual layering you want. For example:

- Water (low priority) should be below Grass
- Rock/walls (high priority) should be above everything

No changes needed if your existing priorities already reflect the desired z-order.
