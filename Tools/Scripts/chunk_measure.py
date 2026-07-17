"""Measure a ground chunk's surface line and decide accept/reject.  0 generations.

★ THE PROBLEM THIS GATES
Only ground_flat_03 is usable as a repeating ground tile, so the forest floor visibly repeats.
02 and 04 exist but cannot be tiled: PixelLab does not honour "same height at both canvas edges",
and their surface line sits 20px / 11px lower at the edges than mid-span, so butting two copies
together produces a visible step.

Acceptance (matches how the tiles are actually placed -- edge to edge at PPU 64):
  edge_delta   = |surface(left col) - surface(right col)|          must be <= 3
  seam_delta   = |mean(surface, first 8 cols) - mean(last 8 cols)| must be <= 3   (a single-column
                 match can be luck; the seam is what the eye reads)
  bottom gaps  = columns whose bottom row is transparent            must be 0     (soil must be solid)
There is no seed, so the only workable method is: roll many, measure, keep the flat ones.
"""
import json
import os
import sys
from PIL import Image
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MAN = os.path.join(ROOT, "ArtDrop", "Generated", "forest_main", "chunk_manifest.json")

EDGE_TOL = 3
SEAM_TOL = 3


def profile(path):
    a = np.array(Image.open(path).convert("RGBA"))
    op = a[..., 3] > 128
    h, w = op.shape
    prof = np.full(w, h, dtype=int)
    for x in range(w):
        nz = np.nonzero(op[:, x])[0]
        if len(nz):
            prof[x] = nz[0]
    return a, op, prof


def measure(path):
    a, op, prof = profile(path)
    h, w = op.shape
    left, right = int(prof[0]), int(prof[-1])
    seam_l, seam_r = float(prof[:8].mean()), float(prof[-8:].mean())
    gaps = int((~op[h - 1]).sum())
    med = int(np.median(prof))
    r = dict(
        file=os.path.basename(path), w=w, h=h,
        surface_left=left, surface_right=right,
        edge_delta=abs(left - right),
        seam_delta=round(abs(seam_l - seam_r), 1),
        surface_median=med,
        edge_vs_mid=int(round((left + right) / 2 - med)),   # the 02/04 killer: edges sag vs middle
        surface_min=int(prof.min()), surface_max=int(prof.max()),
        bottom_transparent_cols=gaps,
    )
    r["accept"] = bool(r["edge_delta"] <= EDGE_TOL and r["seam_delta"] <= SEAM_TOL and gaps == 0)
    return r


def record(path, key=None):
    r = measure(path)
    man = json.load(open(MAN)) if os.path.exists(MAN) else {}
    man[key or r["file"]] = r
    with open(MAN, "w") as f:
        json.dump(man, f, indent=1)
    return r


if __name__ == "__main__":
    for p in sys.argv[1:]:
        r = measure(p)
        flag = "ACCEPT" if r["accept"] else "reject"
        print("%-44s %s  edge_d=%2d seam_d=%4.1f edge_vs_mid=%+3d gaps=%d" % (
            r["file"], flag, r["edge_delta"], r["seam_delta"], r["edge_vs_mid"],
            r["bottom_transparent_cols"]))
