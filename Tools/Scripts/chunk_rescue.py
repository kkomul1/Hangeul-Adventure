"""Rescue ground_flat_04 into a usable 320x160 tile.  0 generations.

Re-measuring 04 (see polish_chunks.py) shows it was discarded for the wrong reason. Its surface is
as flat as the tile we actually ship:  04 std 1.8 / range90 4   vs   03 std 1.7 / range90 5.
Its two real defects are both geometry, not drawing, and both are fixable offline:
    cols 0..9 and 310..319 are fully transparent  (tapered island ends)
    bottom row is opaque only across cols 20..299 (tapered bottom corners)
    -> the genuinely solid core is cols 20..299 = 280px
    surface sits at median 83; 03 sits at 63, so the two are not interchangeable

Fix: take the 280px solid core, mirror-pad 20px on each side back up to 320, then shift the
surface to 63. Mirror-padding keeps the seam flush (the padded edge column is a real column from
the chunk, so its surface height is known and matched) and avoids the vertical streaking that
edge-clamping would produce on grass. The mirror seam lands 20px inside the tile, where the grass
is irregular enough to hide it -- and the neighbouring tile overlaps the outer 2px anyway.

Only emitted if it passes the same gate as any fresh roll (chunk_measure.measure).
"""
import os
import sys
from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from chunk_crop import surface_profile, SURFACE_Y

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Assets", "Resources", "Art", "Forest", "Terrain", "ground_flat_04.png")
OUT = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish", "terrain")
CORE0, CORE1 = 20, 300      # measured solid core [20, 300)
PAD = 20


def rescue(dst=None):
    dst = dst or os.path.join(OUT, "ground_flat_05.png")
    os.makedirs(OUT, exist_ok=True)
    a = np.array(Image.open(SRC).convert("RGBA"))
    core = a[:, CORE0:CORE1]                      # 280 wide, solid
    left = core[:, :PAD][:, ::-1]                 # mirror of cols 20..39  -> new col 0 == old col 39
    right = core[:, -PAD:][:, ::-1]               # mirror of cols 280..299 -> new col 319 == old col 280
    wide = np.concatenate([left, core, right], axis=1)
    assert wide.shape[1] == 320, wide.shape

    op = wide[..., 3] > 128
    prof = surface_profile(op)
    med = int(np.median(prof[prof < wide.shape[0]]))
    shift = SURFACE_Y - med

    h = 160
    out = np.zeros((h, 320, 4), dtype=np.uint8)
    for y in range(h):
        sy = y - shift
        if 0 <= sy < wide.shape[0]:
            out[y] = wide[sy]
    # backfill soil below the surface so no column is hollow after the shift
    earth = wide[min(wide.shape[0] - 1, med + 60), 160]
    for x in range(320):
        nz = np.nonzero(out[:, x, 3] > 128)[0]
        if not len(nz):
            continue
        for y in range(nz[0], h):
            if out[y, x, 3] < 128:
                out[y, x] = earth
    Image.fromarray(out).save(dst)
    return dst


if __name__ == "__main__":
    p = rescue()
    from chunk_measure import measure
    r = measure(p)
    print(p)
    print("  edge_delta=%d seam_delta=%.1f median=%d gaps=%d -> %s" % (
        r["edge_delta"], r["seam_delta"], r["surface_median"], r["bottom_transparent_cols"],
        "ACCEPT" if r["accept"] else "REJECT"))
