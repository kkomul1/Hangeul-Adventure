"""Adopt the chosen roll per prop: palette lock -> tight crop -> final/.  0 generations.

The roll numbers below were chosen BY EYE from _rolls_0..3.png. Metrics cannot judge whether a
sprite reads as a fern rather than a shrub, or whether "exposed roots" came back as a whole tree
(r1/r3 did). The numbers only pre-filter the provably dead. Rejection notes are kept next to each
pick so a re-run does not silently re-adopt a roll that was rejected for a reason.

Tight crop to the alpha bbox is required: Unity places these with a BottomCenter pivot
(forest_import.py), so the sprite's bottom row must BE the object's base. Transparent padding
under the base is exactly the bug that made the backdrop trees float
("나무가 공중에 떠 있는 것처럼 보여") -- large had 31px of empty canvas under its trunk.
"""
import os
import shutil
import sys
from PIL import Image
import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from polish_palette import apply as palette_apply

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
POL = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish")
SRC = os.path.join(POL, "props")
FINAL = os.path.join(POL, "props", "final")

# name -> (roll, why)
PICKS = {
    # --- regen of the five the user called flimsy ---
    "prop_fern_tuft":        (1, "true fern fronds + pebble + litter; r3 drew fiddleheads as digits"),
    "prop_bush_cluster":     (2, "widest + raggedest silhouette w/ twigs; r0/r3 too dome-like"),
    "prop_mossy_boulder":    (3, "angular w/ crack + facets; r1/r2 came back round (the old bug)"),
    "prop_tree_stump":       (0, "peeling bark + mushroom + root grip, best material contrast"),
    "prop_fallen_log":       (1, "clear growth rings + moss; r3 washed out"),
    # --- new: nature ---
    "prop_mushroom_cluster": (1, "classic 5-cap cluster, cream stems read clearly"),
    "prop_wildflowers":      (2, "blossoms actually visible; r0/r3 read as bare leaves"),
    "prop_grass_tuft":       (2, "dense clean fan, good spiky silhouette"),
    "prop_stone_pile":       (0, "loose natural stack w/ moss in the gaps"),
    "prop_exposed_roots":    (0, "ONLY roll that obeyed 'roots, no tree' -- r1/r3 grew full canopies"),
    "prop_reeds":            (1, "fullest stand w/ pale seed plumes"),
    "prop_bamboo_clump":     (3, "reads as bamboo; r0 came back as palm trees, r2 went vivid blue"),
    "prop_vine_hanging":     (1, "leafiest, best taper"),
    "prop_boulder_small":    (2, "blocky w/ moss + pebbles; r1/r3 round"),
    "prop_boulder_large":    (2, "big slab, crack, moss ridge -- proper large tier"),
    # --- new: man-made (the category the praised sign belongs to) ---
    "prop_seonangdang_cairn": (0, "clearest stacked cairn + straw rope + moss base"),
    "prop_onggi_jar":        (0, "dark glaze + lid + highlight; r1 went multicolour"),
    "prop_water_jar":        (0, "on its side w/ dark mouth visible, as specified"),
    "prop_fence_wood":       (2, "posts + rails + broken end all readable"),
    "prop_jige":             (0, "closest to an A-frame rack w/ straw pad"),
    "prop_deungnong":        (1, "post planted in the ground -- r2/r3 float, which is the bug we are fixing"),
    "prop_straw_shoes":      (2, "pair + toe cords visible"),
    "prop_stepping_stones":  (1, "set into grass rather than floating"),
    "prop_sinmok_rope":      (2, "trunk-only w/ rope + hanging hanji strips"),
}


def tight(im):
    a = np.array(im)
    op = a[..., 3] > 8
    if not op.any():
        return im
    ys, xs = np.where(op)
    return im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))


def main():
    os.makedirs(FINAL, exist_ok=True)
    rows = []
    for name, (roll, why) in PICKS.items():
        src = os.path.join(SRC, f"{name}__r{roll}.png")
        if not os.path.exists(src):
            print("MISSING", src)
            continue
        tmp = os.path.join(FINAL, name + "_lock.tmp.png")
        palette_apply(src, tmp)
        im = tight(Image.open(tmp).convert("RGBA"))
        dst = os.path.join(FINAL, name + ".png")
        im.save(dst)
        os.remove(tmp)
        rows.append((name, roll, im.size))
        print("%-24s r%d  %3dx%-3d  %.2fu x %.2fu" % (
            name, roll, im.size[0], im.size[1], im.size[0] / 64, im.size[1] / 64))
    return rows


if __name__ == "__main__":
    main()
