"""Ground chunk variants: generate wide -> crop a flush window -> align -> palette lock -> gate.

★ WHY THE FOREST FLOOR REPEATS
SideWorld.Build.cs ships `GroundChunks = { "ground_flat_03" }` -- one tile, laid at GroundPitch
318/64 with a 320px sprite, i.e. butted with only 2px of overlap. One tile at a 5u pitch is a
visible 5u rhythm. 02 and 04 were rejected, and re-measuring shows the recorded reason
("edge surface line 11-20px lower than mid-span") is not the real one:

    chunk           median  std  range90  side margins   verdict
    ground_flat_02      69  8.6       27     23px/21px   genuinely bumpy AND an island
    ground_flat_03      63  1.7        5       0px/0px   the one in use
    ground_flat_04      83  1.8        4     10px/10px   FLAT -- only mis-placed and margined

04's surface is as flat as 03's (std 1.8 vs 1.7). It was thrown away for two defects that are
both fixable offline: transparent side margins, and a surface sitting 20px too low. So the tile
budget was never one -- it was two, plus whatever we roll now.

★ METHOD
PixelLab will not honour "cut off flush at both canvas edges" (it was already in the prompt that
produced the islands). So stop asking: generate at 400px wide, then CHOOSE a 320px window whose
two seam columns are opaque top-to-bottom and whose surface heights match. Flushness becomes a
property of where we cut. Then shift vertically so the surface lands on 63, matching 03 exactly,
so any tile is interchangeable with any other.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import polish_prompts as PP

ANCHOR = PP.ANCHOR
SIDE = ("Seen strictly from the side as a flat side elevation: no top plane is visible anywhere, "
        "the mossy grass reads edge-on as a thin fringe along the top silhouette line, NOT a "
        "visible top surface. ")

# ★ BODY is the _GROUND clause from forest_prompts.py, near-verbatim. It is the wording that
#   actually produced real ground chunks (02/03/04), so it is not up for creative improvement.
#
#   A rewrite with heavy negations -- "NOT an island, NOT a floating platform, NOT a hill, with no
#   rounded or tapered ends" -- was measurably WORSE: it returned sunset landscape paintings with
#   distant hills, a sun and tiny human figures. Two known failure modes fired at once:
#     (a) the recorded trap "long text between ANCHOR and SUBJECT pushes the subject out and you
#         get figures/clouds landscapes" -- the negation pile did exactly that; and
#     (b) naming a thing to forbid it still conditions on it. "NOT a hill" drew hills.
#   Flushness is NOT solved by asking. It is solved by generating wide and cropping (chunk_crop).
BODY = ("Continuous ground terrain chunk for a side-scrolling platformer: a solid mass of packed "
        "brown earth topped with a walkable layer of short mossy grass. The grass surface line "
        "runs nearly flat with only soft subtle undulation, sitting about 40% down from the top of "
        "the canvas at both the left and right canvas edges at exactly the same height. Below that "
        "line the earth body is deep and massive and completely fills the entire lower half of the "
        "canvas all the way down to the bottom edge - a deep cross-section of soil, never a thin "
        "strip. ")

GUARD = ("Moss and grass are desaturated dull olive, never vivid green; earth is soft muted "
         "greyish brown, never red or rust. Nothing rises above the grass line except what is "
         "described. Nothing floats. ")

W, H = 400, 160   # generate wide; crop_align takes the best flush 320x160 window out of it

# (name, feature clause)
CHUNKS = [
    ("gc_plain", "The top carries only a few sparse tufts of short grass - the plainest variant, "
                 "meant to repeat often without drawing attention. "),
    ("gc_pebbles", "A scatter of small weathered grey pebbles rests on the grass, and a few more "
                   "stones are embedded in the soil below. "),
    ("gc_roots", "Two low gnarled tree roots arch out of the soil and back into it, staying close "
                 "to the ground. No trunk, no branches and no leaves rise above the ground - only "
                 "the low roots. "),
    ("gc_moss", "Soft patches of darker moss and three small clumps of low fern spread across the "
                "grass, with fine roots threading the soil below. "),
    ("gc_stones", "Two half-buried weathered grey boulders sit low in the earth with their tops "
                  "just breaking the grass line, moss on their upper edges. "),
    ("gc_litter", "A drift of dry rust-brown fallen leaves and two small pinecones lie scattered "
                  "across the grass. "),
]


def prompts():
    out = []
    for name, feat in CHUNKS:
        out.append((name, ANCHOR + BODY + feat + SIDE + GUARD, None, None))
    return out


if __name__ == "__main__":
    for n, d, _, _ in prompts():
        print("==", n, "\n", d, "\n")
