"""Normalise a ground chunk's soil to ground_flat_03's soil.  0 generations.

★ WHY THIS IS MANDATORY, NOT COSMETIC
SideWorld.Build.cs paints a flat backfill quad behind the chunks:
    DirtColor = new Color(125/255, 91/255, 80/255)     // = #7D5B50
and its own comment records the failure mode: "흙 백필 색 ... 다른 색을 쓰면 청크가 끝나는 y에
수평 띠가 드러난다(실측 확인)" -- if a chunk's soil is not that exact colour, a horizontal band
appears where the chunk's bottom edge meets the backfill.

Measured:  ground_flat_03 bottom row = (125, 91, 80)  -- exactly DirtColor, which is why it works.
           ground_flat_04 bottom row = ( 83, 57, 61)  -- a dark plum; it would band.
PixelLab has no seed, so every fresh roll lands on its own soil colour. Any chunk that is not
normalised is unusable no matter how flat its surface is. This is why "roll until one matches"
was never going to work: the surface and the soil colour would both have to come up right at once.

Method: an additive shift in the soil zone only, chosen so the soil's median lands on DirtColor.
Additive (not a palette snap) because it preserves the soil's internal texture and shading ramp;
a palette snap sends (83,57,61) to its nearest neighbour, which is another dark colour, not
DirtColor. The grass zone is untouched.
"""
import os
import sys
from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from chunk_crop import surface_profile

DIRT = np.array([125, 91, 80], dtype=np.int16)   # must equal SideWorld.Build.cs DirtColor
GRASS_KEEP = 26          # rows below the surface line left alone (grass fringe + root zone)
SOIL_SAMPLE = 45         # start sampling the soil median this far below the surface


def normalize(src, dst=None):
    dst = dst or src
    a = np.array(Image.open(src).convert("RGBA")).astype(np.int16)
    op = a[..., 3] > 128
    h, w = op.shape
    prof = surface_profile(op)
    valid = prof[prof < h]
    if not len(valid):
        return None
    surf = int(np.median(valid))

    y0 = min(h - 1, surf + SOIL_SAMPLE)
    sample = a[y0:, :, :3][op[y0:]]
    if not len(sample):
        return None
    med = np.median(sample, axis=0).astype(np.int16)
    delta = DIRT - med

    zone = np.zeros_like(op)
    zone[min(h - 1, surf + GRASS_KEEP):] = True
    zone &= op
    a[..., :3][zone] = np.clip(a[..., :3][zone] + delta, 0, 255)

    Image.fromarray(a.astype(np.uint8)).save(dst)
    return dict(surface=surf, soil_before=tuple(int(v) for v in med),
                soil_after=tuple(int(v) for v in np.median(
                    np.array(Image.open(dst).convert("RGB"))[-1], axis=0).astype(int)),
                delta=tuple(int(v) for v in delta))


if __name__ == "__main__":
    for p in sys.argv[1:]:
        print(os.path.basename(p), normalize(p))
