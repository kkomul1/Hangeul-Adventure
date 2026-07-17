"""Adopt the chosen ground chunks as ground_flat_05..08.  0 generations.

Order matters and is not arbitrary:
    key -> crop -> align -> PALETTE LOCK -> SOIL NORMALISE -> gate
  * palette lock uses a TERRAIN-ONLY reference (ground_flat_03 + boundary_bush). This is the one
    place the old recorded rule "팔레트 락은 지형에만" is exactly right: terrain wants the tight
    terrain palette, and locking to the wider master palette (which carries hanji cream) would let
    soil drift toward cream.
  * soil normalise runs AFTER the lock, never before. The lock snaps every pixel to its nearest
    palette entry and would knock the soil back off DirtColor; normalising last guarantees the
    bottom edge lands exactly on (125,91,80) so no band shows against the backfill quad.

Picks are by eye from _chunks_sheet.png / _terrain_cmp.png. The flatness gate (surf_std <= 4)
catches horizons but cannot catch small content errors, and these three escaped it:
    gc_moss__r0    std 1.6 -- two tiny human figures standing on the grass
    gc_roots__r0   std 1.7 -- the "soil" is a cobbled brick wall
    gc_plain__r3   std 2.3 -- soil is a visibly repeating pebble pattern, reads as tiled
so the final call stays visual.
"""
import os
import sys
import shutil
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from palette_lock import ref_palette, lock
from chunk_soil import normalize
from chunk_measure import measure

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art", "Forest", "Terrain")
T = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish", "terrain")
FINAL = os.path.join(T, "final")

TERRAIN_REFS = [os.path.join(ART, "ground_flat_03.png"), os.path.join(ART, "boundary_bush.png")]

# source -> adopted name  (why)
ADOPT = [
    ("ground_flat_05", os.path.join(T, "ground_flat_05.png"),
     "rescued ground_flat_04: its surface was always flat (std 1.8), only the margins + height were wrong"),
    ("ground_flat_06", os.path.join(T, "gc_plain__r1.png"),
     "short even grass, plain soil - closest sibling to 03, safe to repeat often"),
    ("ground_flat_07", os.path.join(T, "gc_stones__r2.png"),
     "grass with a darker base band, distinct rhythm from 06"),
    ("ground_flat_08", os.path.join(T, "gc_litter__r2.png"),
     "taller looser grass tufts, most visual difference from 03"),
]


def main():
    os.makedirs(FINAL, exist_ok=True)
    # ref_palette can hand back short/empty entries: PIL's getpalette() returns fewer than n*3
    # values when the source has fewer distinct colours than requested, and palette_lock slices
    # it blindly. Filter rather than touch the shared helper other scripts depend on.
    pal = [c for c in ref_palette(TERRAIN_REFS, 24) if len(c) == 3]
    print(f"terrain palette: {len(pal)} colours from ground_flat_03 + boundary_bush\n")
    rows = []
    for name, src, why in ADOPT:
        if not os.path.exists(src):
            print("MISSING", src)
            continue
        dst = os.path.join(FINAL, name + ".png")
        lock(src, dst, pal)                 # unify grass/earth hue with the shipped tile
        normalize(dst)                      # then force soil back onto DirtColor exactly
        r = measure(dst)
        import numpy as np
        bot = np.median(np.array(Image.open(dst).convert("RGB"))[-1], axis=0).astype(int)
        rows.append((name, r, tuple(bot)))
        print("  %-16s edge_d=%d seam_d=%.1f median=%d gaps=%d bottom=%s  %s" % (
            name, r["edge_delta"], r["seam_delta"], r["surface_median"],
            r["bottom_transparent_cols"], tuple(bot), "OK" if r["accept"] else "FAIL"))
    return rows


if __name__ == "__main__":
    main()
