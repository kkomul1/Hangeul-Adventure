"""Build + apply the master forest palette.  0 generations.

WHY: the user asked for "물체들 간의 일관성". Prompts cannot deliver that -- create_map_object has
no seed, so colour drifts every call (that is why palette_lock.py exists at all). Inpainting was
the previous answer, but the probe proved it trades one bug for another: a prop inpainted against
sky becomes sky-coloured, and against a thicket it becomes thicket-shaped. Either way it absorbs
the donor.

So: generate props in BASIC mode (transparent background -> clean silhouette, no bleed, no mask
rectangle) and enforce consistency offline by snapping every prop to one shared palette.

★ The old trap (recorded): palette-locking props destroyed them -- hanji cream turned grass-green,
pine needles turned grey. Cause: the lock palette was sampled from TERRAIN ONLY, so it contained
no cream and no neutral grey; every cream pixel's nearest neighbour was a green. The fix is not
"never lock props", it is "lock to a palette that spans every material the props actually use".
This palette is sampled from all six approved assets, covering: cream hanji, dark outline, stone
grey, earth brown, moss/leaf green, slate shadow.
"""
import os
import sys
from PIL import Image
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art", "Forest")
OUT = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish")

# Every approved asset, so the palette spans every material family the props need.
# Each source is quantised SEPARATELY to its own n slots, then merged. Pooling the pixels first
# does not work: spot_sign_hanji is 37% cream by area, so a global median-cut spent 6 of 32 slots
# on near-identical creams (#FBF3CA/#FAF3CD/#FBF3CD/...) and starved the greens and greys.
# Per-source quantisation guarantees each material family its own share regardless of area.
SOURCES = [
    (os.path.join(ART, "Terrain", "ground_flat_03.png"), 7),      # earth brown + grass green
    (os.path.join(ART, "Terrain", "boundary_bush.png"), 7),       # slate shadow, moss, dark
    (os.path.join(ART, "Props", "spot_sign_hanji.png"), 6),       # ★ cream hanji + dark wood
    (os.path.join(ART, "Props", "prop_jangseung.png"), 5),        # weathered timber
    (os.path.join(ART, "Props", "prop_seokdeung.png"), 5),        # stone grey
    (os.path.join(ART, "Props", "prop_tree_stump.png"), 5),       # bark brown
]
MERGE_DIST = 10.0   # LAB dE below which two colours are treated as duplicates
PAL_PATH = os.path.join(OUT, "master_palette.json")


def srgb_to_lab_arr(rgb):
    c = rgb.astype(np.float64) / 255.0
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ m.T
    xyz /= np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16,
                     500 * (f[..., 0] - f[..., 1]),
                     200 * (f[..., 1] - f[..., 2])], -1)


def _quant(path, n):
    a = np.array(Image.open(path).convert("RGBA"))
    op = a[..., 3] > 128
    c = a[..., :3][op]
    tmp = Image.new("RGB", (len(c), 1))
    tmp.putdata([tuple(v) for v in c])
    q = tmp.quantize(colors=n, method=Image.MEDIANCUT)
    pal = q.getpalette()[:n * 3]
    return [tuple(pal[i * 3:i * 3 + 3]) for i in range(n)]


def build():
    cand = []
    for p, n in SOURCES:
        got = _quant(p, n)
        cand += got
        print("%-24s -> %s" % (os.path.basename(p), " ".join("#%02X%02X%02X" % c for c in got)))

    # merge perceptual duplicates so no family wastes slots
    cols = []
    for c in cand:
        lc = srgb_to_lab_arr(np.array([c]))[0]
        if all(np.linalg.norm(lc - srgb_to_lab_arr(np.array([k]))[0]) > MERGE_DIST for k in cols):
            cols.append(c)

    import json
    with open(PAL_PATH, "w") as f:
        json.dump(cols, f)

    order = sorted(cols, key=lambda c: 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2])
    sw = Image.new("RGB", (len(order) * 24, 48))
    d = sw.load()
    for i, c in enumerate(order):
        for x in range(i * 24, i * 24 + 24):
            for y in range(48):
                d[x, y] = c
    sw.save(os.path.join(OUT, "master_palette.png"))
    print("\nmaster palette: %d colours -> %s" % (len(cols), PAL_PATH))
    for c in order:
        print("  #%02X%02X%02X" % c)
    return cols


def load():
    import json
    with open(PAL_PATH) as f:
        return [tuple(c) for c in json.load(f)]


def apply(src, dst, pal=None, protect_dark=True):
    """Snap opaque pixels to the master palette. Luminance-weighted so shading survives."""
    pal = pal or load()
    a = np.array(Image.open(src).convert("RGBA"))
    op = a[..., 3] > 128
    rgb = a[..., :3]
    uniq, inv = np.unique(rgb[op].reshape(-1, 3), axis=0, return_inverse=True)
    lu = srgb_to_lab_arr(uniq)
    lp = srgb_to_lab_arr(np.array(pal))
    # weight L heavily -> preserves the shading ramp, pulls only hue/chroma to the palette
    d = (2.2 * (lu[:, None, 0] - lp[None, :, 0]) ** 2
         + (lu[:, None, 1] - lp[None, :, 1]) ** 2
         + (lu[:, None, 2] - lp[None, :, 2]) ** 2)
    best = np.array(pal)[np.argmin(d, 1)]
    out = rgb.copy()
    out[op] = best[inv]
    a[..., :3] = out
    Image.fromarray(a).save(dst)
    return dst


if __name__ == "__main__":
    if len(sys.argv) == 1:
        build()
    else:
        apply(sys.argv[1], sys.argv[2])
