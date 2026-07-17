"""Turn a raw generated ground chunk into a tileable one by CROPPING.  0 generations.

★ WHY CROP INSTEAD OF RE-PROMPTING
The recorded note says 02/04 were unusable because "the edge surface line sits 11-20px lower than
mid-span". Re-measuring shows that is not what happened:
    ground_flat_02  opaque cols  23..298 of 320   -> 23px / 21px of FULLY TRANSPARENT margin
    ground_flat_04  opaque cols  10..309 of 320   -> 10px / 10px
    ground_flat_03  opaque cols   0..319 of 320   -> flush, which is why it is the only usable one
PixelLab drew 02 and 04 as free-floating islands with tapered rounded ends. They were never ground
chunks. No amount of "cut off flush at both canvas edges" fixes that -- the wording was already in
the prompt and was ignored. It is not a prompt problem, so more rolls will not solve it.

So: generate WIDER than needed and choose the crop window. Cropping makes flushness a property of
where we cut, not of what the model decided to draw. A window is accepted only if every one of its
columns is opaque top-to-bottom at the seam and the surface heights at its two edges match, which
makes the seam correct by construction rather than by luck.

Also aligns the surface line to SURFACE_Y so every tile is interchangeable with ground_flat_03.
"""
import os
import sys
from PIL import Image
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUTW, OUTH = 320, 160
SURFACE_Y = 63          # ground_flat_03's measured surface line -- all tiles must match it
EDGE_TOL = 3


def surface_profile(op):
    h, w = op.shape
    prof = np.full(w, h, dtype=int)
    for x in range(w):
        nz = np.nonzero(op[:, x])[0]
        if len(nz):
            prof[x] = nz[0]
    return prof


def best_window(path, width=OUTW, tol=EDGE_TOL):
    """Slide a `width` window; return (x0, score, info) for the flattest-seam solid window."""
    a = np.array(Image.open(path).convert("RGBA"))
    op = a[..., 3] > 128
    h, w = op.shape
    if w < width:
        return None
    prof = surface_profile(op)
    solid_bottom = op[h - 1]                     # column reaches the bottom edge
    cands = []
    for x0 in range(0, w - width + 1):
        x1 = x0 + width - 1
        # both seam columns must be real ground: opaque at the bottom and with a surface above
        if not (solid_bottom[x0] and solid_bottom[x1]):
            continue
        if prof[x0] >= h or prof[x1] >= h:
            continue
        seam_l = prof[max(0, x0):x0 + 8].mean()
        seam_r = prof[x1 - 7:x1 + 1].mean()
        edge_d = abs(int(prof[x0]) - int(prof[x1]))
        seam_d = abs(seam_l - seam_r)
        if edge_d > tol or seam_d > tol:
            continue
        inner = prof[x0:x1 + 1]
        if (inner >= h).any():                   # no transparent column anywhere inside
            continue
        gaps = int((~op[h - 1, x0:x1 + 1]).sum())
        if gaps:
            continue
        # prefer a flat seam, then a chunk whose surface sits near SURFACE_Y already
        score = edge_d * 2 + seam_d + abs(np.median(inner) - SURFACE_Y) * 0.15
        cands.append((score, x0, dict(edge_d=edge_d, seam_d=round(float(seam_d), 2),
                                      median=int(np.median(inner)))))
    if not cands:
        return None
    cands.sort()
    score, x0, info = cands[0]
    return x0, score, info


def crop_align(path, dst, width=OUTW, height=OUTH):
    r = best_window(path, width)
    if not r:
        return None
    x0, score, info = r
    a = np.array(Image.open(path).convert("RGBA"))
    op = a[..., 3] > 128
    prof = surface_profile(op)
    win = a[:, x0:x0 + width]
    med = int(np.median(prof[x0:x0 + width]))

    # vertical align: put the surface line at SURFACE_Y, then backfill soil to the bottom edge
    shift = SURFACE_Y - med
    out = np.zeros((height, width, 4), dtype=np.uint8)
    src_h = win.shape[0]
    for y in range(height):
        sy = y - shift
        if 0 <= sy < src_h:
            out[y] = win[sy]
    earth = win[min(src_h - 1, med + 60), width // 2]
    if earth[3] < 128:
        earth = win[src_h - 1, width // 2]
    # fill any column that is transparent below the surface with the chunk's own earth colour
    for x in range(width):
        col = out[:, x, 3] > 128
        nz = np.nonzero(col)[0]
        if not len(nz):
            continue
        top = nz[0]
        for y in range(top, height):
            if out[y, x, 3] < 128:
                out[y, x] = earth
    Image.fromarray(out).save(dst)
    info["x0"] = x0
    return info


if __name__ == "__main__":
    src, dst = sys.argv[1], sys.argv[2]
    print(crop_align(src, dst))
