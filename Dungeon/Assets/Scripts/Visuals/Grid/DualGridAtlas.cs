using UnityEngine;

namespace Dungeon.Visuals
{
    // Wraps the dual-grid autotile atlas and maps 4-bit bitmasks to atlas tile UVs.
    //
    // BITMASK BITS (which of the 4 sampled cells match the primary type):
    //   bit 0 = 1  NW cell
    //   bit 1 = 2  NE cell
    //   bit 2 = 4  SW cell
    //   bit 3 = 8  SE cell
    //
    // COMPLEMENT TRICK
    // ────────────────
    // You only need 8 tiles to cover all 16 bitmask combinations.
    // Each tile handles its bitmask directly AND its complement (15 − bitmask)
    // with primary/secondary colours swapped.
    //
    // CONFIGURATION
    // ─────────────
    // Set _tileBitmasks[i] to the bitmask that atlas tile i represents.
    // The array index is the tile's linear position in the atlas texture
    // (left-to-right, top-to-bottom, starting at 0).
    //
    // Default layout (matching the user's tileset):
    //   Tile 0 = 15  (FULL — all 4 corners filled)
    //   Tile 1 = 2   (NE corner only)
    //   Tile 2 = 1   (NW corner only)
    //   Tile 3 = 3   (NW + NE — north edge)
    //   Tile 4 = 4   (SW corner only)
    //   Tile 5 = 10  (NE + SE — east edge)
    //   Tile 6 = 6   (NE + SW — diagonal)
    //   Tile 7 = 7   (NW + NE + SW — all except SE)
    [CreateAssetMenu(menuName = "Dungeon/Dual Grid Atlas", fileName = "DualGridAtlas")]
    public class DualGridAtlas : ScriptableObject
    {
        [SerializeField] private Texture2D _atlas;
        [Tooltip("Optional outline texture with the same tile layout. " +
                 "Alpha = where border lines appear, RGB = outline colour. " +
                 "Draw borders on both sides of each transition edge.")]
        [SerializeField] private Texture2D _outlineAtlas;
        [SerializeField] [Min(1)] private int _columns = 5;
        [SerializeField] [Min(1)] private int _rows    = 5;

        [Tooltip("The 4-bit bitmask (0–15) that each atlas tile represents. " +
                 "Index = linear tile position in the texture. " +
                 "Bits: NW=1, NE=2, SW=4, SE=8.")]
        [SerializeField] private int[] _tileBitmasks = { 15, 2, 1, 3, 4, 10, 6, 7 };

        public Texture2D Atlas        => _atlas;
        public Texture2D OutlineAtlas => _outlineAtlas;

        // Precomputed lookup tables (bitmask 0-15 → atlas slot + swap flag).
        private int[]  _slotLookup;
        private bool[] _swapLookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _slotLookup = new int[16];
            _swapLookup = new bool[16];

            // First pass: assign bitmasks directly represented by a tile.
            var assigned = new bool[16];
            for (int slot = 0; slot < _tileBitmasks.Length; slot++)
            {
                int bm = _tileBitmasks[slot];
                if (bm < 0 || bm > 15) continue;
                _slotLookup[bm] = slot;
                _swapLookup[bm] = false;
                assigned[bm] = true;
            }

            // Second pass: unassigned bitmasks use their complement tile with swap.
            for (int bm = 0; bm < 16; bm++)
            {
                if (assigned[bm]) continue;
                int complement = 15 - bm;
                if (assigned[complement])
                {
                    _slotLookup[bm] = _slotLookup[complement];
                    _swapLookup[bm] = true;
                }
                // else: fall through to slot 0, no swap (shouldn't happen with 8 valid pairs)
            }
        }

        // Returns the UV rect for the given bitmask and whether to swap primary/secondary.
        public Rect GetUVRect(int bitmask, out bool swap)
        {
            if (_slotLookup == null) BuildLookup();
            int clamped = bitmask < 0 ? 0 : (bitmask > 15 ? 15 : bitmask);
            swap = _swapLookup[clamped];
            return SlotToUVRect(_slotLookup[clamped]);
        }

        private Rect SlotToUVRect(int slot)
        {
            int   col = slot % _columns;
            int   row = slot / _columns;
            float w   = 1f / _columns;
            float h   = 1f / _rows;
            float u   = col * w;
            float v   = 1f - (row + 1) * h;   // V=0 is bottom in Unity; row 0 = top of image
            return new Rect(u, v, w, h);
        }
    }
}
