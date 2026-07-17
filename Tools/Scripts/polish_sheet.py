"""Contact sheets.  0 generations.

  rolls   -- every roll of every prop, grouped by prop, for picking the keeper by eye
  final   -- the adopted set at true relative scale (PPU 64), on the real ground, for judging
             tone consistency and the size hierarchy in one glance

Metrics cannot pick the keeper: they cannot tell whether the thing is actually a fern. They only
rule out the ones that are provably dead (no outline, no chroma, no internal detail). So the sheet
is the acceptance artifact and the numbers are the pre-filter.
"""
import os
import sys
import glob
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import polish_prompts as P

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
POL = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish")
PROPS = os.path.join(POL, "props")
BG = (58, 63, 80, 255)
INK = (232, 228, 214, 255)


def _label(d, x, y, text, fill=INK):
    d.text((x, y), text, fill=fill)


def rolls_sheet(dst=None, scale=2, pattern="__r*"):
    dst = dst or os.path.join(POL, "_rolls_sheet.png")
    names = [n for n, _, _, _ in P.PROPS]
    rows = []
    for n in names:
        fs = sorted(glob.glob(os.path.join(PROPS, n + pattern + ".png")))
        fs = [f for f in fs if "_locked" not in f]
        if fs:
            rows.append((n, fs))
    if not rows:
        print("no rolls yet")
        return None

    cell_w = max(max(Image.open(f).size[0] for f in fs) for _, fs in rows) * scale + 8
    cell_h = max(max(Image.open(f).size[1] for f in fs) for _, fs in rows) * scale + 8
    cell_w = min(cell_w, 420)
    cell_h = min(cell_h, 420)
    ncol = max(len(fs) for _, fs in rows)
    W = 190 + ncol * cell_w
    H = sum(cell_h + 14 for _ in rows) + 10

    im = Image.new("RGBA", (W, H), BG)
    d = ImageDraw.Draw(im)
    y = 6
    for n, fs in rows:
        _label(d, 6, y + cell_h // 2 - 4, n.replace("prop_", ""))
        for i, f in enumerate(fs):
            t = Image.open(f).convert("RGBA")
            t = t.resize((t.width * scale, t.height * scale), Image.NEAREST)
            if t.width > cell_w - 8 or t.height > cell_h - 8:
                t.thumbnail((cell_w - 8, cell_h - 8), Image.NEAREST)
            x = 190 + i * cell_w + (cell_w - t.width) // 2
            im.alpha_composite(t, (x, y + (cell_h - t.height) // 2))
            _label(d, 190 + i * cell_w + 3, y + 1, os.path.basename(f).split("__")[-1][:-4],
                   (150, 158, 175, 255))
        y += cell_h + 14
        d.line([(0, y - 7), (W, y - 7)], fill=(40, 44, 56, 255))
    im.save(dst)
    print("->", dst, im.size)
    return dst


def final_sheet(adopted, dst=None, scale=2):
    """adopted: list of (name, path). Drawn standing on the real ground tile at true scale."""
    dst = dst or os.path.join(POL, "_contact_sheet.png")
    ground = Image.open(os.path.join(ROOT, "Assets", "Resources", "Art", "Forest",
                                     "Terrain", "ground_flat_03.png")).convert("RGBA")
    GY = 63  # ground_flat_03 surface line

    items = [(n, Image.open(p).convert("RGBA")) for n, p in adopted]
    pad = 14
    maxh = max(i.height for _, i in items)
    rowh = (maxh + 46) * scale
    perrow = 6
    rows = (len(items) + perrow - 1) // perrow
    colw = max(max(i.width for _, i in items) + pad * 2, 110) * scale
    W, H = colw * perrow, rowh * rows

    im = Image.new("RGBA", (W, H), (44, 48, 62, 255))
    for r in range(rows):
        band = Image.new("RGBA", (W, rowh), (0, 0, 0, 0))
        # ground strip along the bottom of each row
        gy = rowh - (ground.height - GY) * scale - 10
        g = ground.resize((ground.width * scale, ground.height * scale), Image.NEAREST)
        for gx in range(0, W + g.width, g.width):
            band.alpha_composite(g, (gx, gy - GY * scale))
        im.alpha_composite(band, (0, r * rowh))

    d = ImageDraw.Draw(im)
    for k, (n, t) in enumerate(items):
        r, c = k // perrow, k % perrow
        t2 = t.resize((t.width * scale, t.height * scale), Image.NEAREST)
        baseline = r * rowh + rowh - (ground.height - GY) * scale - 10
        x = c * colw + (colw - t2.width) // 2
        im.alpha_composite(t2, (x, baseline - t2.height))
        u = "%.2fu x %.2fu" % (t.width / 64.0, t.height / 64.0)
        _label(d, c * colw + 6, r * rowh + 6, n.replace("prop_", ""))
        _label(d, c * colw + 6, r * rowh + 18, u, (150, 158, 175, 255))
    im.save(dst)
    print("->", dst, im.size)
    return dst


if __name__ == "__main__":
    if sys.argv[1] == "rolls":
        rolls_sheet()
