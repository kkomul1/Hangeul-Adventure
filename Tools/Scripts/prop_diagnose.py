"""Diagnose why spot_sign_hanji reads as "good" and the nature props read as "flimsy".

Measures, per sprite (opaque pixels only):
  - distinct colors, and colors per 1k opaque px (detail/palette richness)
  - luminance p5..p95 spread and stddev  (contrast)
  - mean saturation                       (chroma strength)
  - content bbox fill ratio               (how much canvas is used = effective resolution)
  - silhouette compactness  P/sqrt(A)     (silhouette complexity; circle ~3.5, ragged >>)
  - internal detail density               (fraction of opaque px whose color differs from right/below neighbor)

Usage: python Tools/Scripts/prop_diagnose.py
"""
import os
import sys
from PIL import Image
import numpy as np

PROPS = os.path.join("Assets", "Resources", "Art", "Forest", "Props")

TARGETS = [
    ("spot_sign_hanji", "GOOD (user praised)"),
    ("exit_gate_jangseung", "artificial ref"),
    ("prop_jangseung", "artificial ref"),
    ("prop_seokdeung", "artificial ref"),
    ("prop_mossy_boulder", "flimsy"),
    ("prop_tree_stump", "flimsy"),
    ("prop_fallen_log", "flimsy"),
    ("prop_bush_cluster", "flimsy"),
    ("prop_fern_tuft", "flimsy"),
]


def rgb_to_hsv_arr(rgb):
    r, g, b = rgb[..., 0] / 255.0, rgb[..., 1] / 255.0, rgb[..., 2] / 255.0
    mx = np.max(rgb[..., :3], axis=-1) / 255.0
    mn = np.min(rgb[..., :3], axis=-1) / 255.0
    diff = mx - mn
    sat = np.where(mx > 0, diff / np.maximum(mx, 1e-6), 0.0)
    return sat, mx


def luminance(rgb):
    return 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]


def analyze(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im)
    alpha = a[..., 3]
    op = alpha > 128
    n = int(op.sum())
    if n == 0:
        return None
    rgb = a[..., :3].astype(np.float32)

    # distinct colors among opaque
    cols = a[..., :3][op]
    distinct = len(np.unique(cols.reshape(-1, 3), axis=0))

    lum = luminance(rgb)[op]
    p5, p95 = np.percentile(lum, 5), np.percentile(lum, 95)
    sat, val = rgb_to_hsv_arr(a[..., :3].astype(np.float32))
    msat = float(sat[op].mean())

    ys, xs = np.where(op)
    bbox = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1)
    bw, bh = bbox[2] - bbox[0], bbox[3] - bbox[1]
    H, W = op.shape
    canvas_use = (bw * bh) / float(W * H)
    fill = n / float(W * H)

    # silhouette perimeter: opaque px with >=1 non-opaque 4-neighbour
    pad = np.pad(op, 1, constant_values=False)
    nb = (pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
    perim = int((op & ~nb).sum())
    compact = perim / np.sqrt(max(n, 1))

    # internal detail density: opaque px whose colour differs (>12) from right/below opaque neighbour
    q = a[..., :3].astype(np.int16)
    dr = np.zeros_like(op)
    dd = np.zeros_like(op)
    dr[:, :-1] = (np.abs(q[:, :-1] - q[:, 1:]).sum(-1) > 12) & op[:, :-1] & op[:, 1:]
    dd[:-1, :] = (np.abs(q[:-1, :] - q[1:, :]).sum(-1) > 12) & op[:-1, :] & op[1:, :]
    detail = float((dr | dd).sum()) / max(n, 1)

    return dict(
        canvas=f"{W}x{H}",
        opaque=n,
        content=f"{bw}x{bh}",
        canvas_use=canvas_use,
        fill=fill,
        distinct=distinct,
        col_per_1k=distinct / (n / 1000.0),
        lum_p5=float(p5),
        lum_p95=float(p95),
        lum_spread=float(p95 - p5),
        lum_std=float(lum.std()),
        sat=msat,
        compact=float(compact),
        detail=detail,
    )


def main():
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    rows = []
    for name, tag in TARGETS:
        p = os.path.join(root, PROPS, name + ".png")
        if not os.path.exists(p):
            print("MISSING", p)
            continue
        r = analyze(p)
        r["name"] = name
        r["tag"] = tag
        rows.append(r)

    hdr = (f"{'sprite':<24}{'tag':<20}{'content':>9}{'use%':>7}{'fill%':>7}"
           f"{'cols':>6}{'c/1k':>7}{'lumP5':>7}{'lumP95':>7}{'spread':>7}{'lstd':>6}"
           f"{'sat':>6}{'compact':>8}{'detail':>7}")
    print(hdr)
    print("-" * len(hdr))
    for r in rows:
        print(f"{r['name']:<24}{r['tag']:<20}{r['content']:>9}{r['canvas_use']*100:>7.1f}"
              f"{r['fill']*100:>7.1f}{r['distinct']:>6}{r['col_per_1k']:>7.1f}"
              f"{r['lum_p5']:>7.0f}{r['lum_p95']:>7.0f}{r['lum_spread']:>7.0f}{r['lum_std']:>6.1f}"
              f"{r['sat']:>6.2f}{r['compact']:>8.2f}{r['detail']:>7.2f}")


if __name__ == "__main__":
    main()
