"""Prop prompts for the "flimsy" fix.  Style anchor + side clause owned here.

★ WHY THESE PROMPTS LOOK THE WAY THEY DO (measured -- see prop_diagnose.py)

The user praised spot_sign_hanji and called every nature prop flimsy. Measuring both explains it:

    sprite                lum spread   sat    detail   darkest px
    spot_sign_hanji           201     0.27     0.39        42     <- praised
    prop_bush_cluster          38     0.11     0.08       151     <- "flimsy"
    prop_fern_tuft             33     0.08     0.00       155     <- "flimsy"

The sign wins on three things, and they are all reproducible in a prompt:
  1. NAMED CONTRASTING MATERIALS. "pale cream hanji" (val 0.98) against "dark wood" (val 0.13)
     is a 201-step value range inside one object. "a low cluster of mossy bushes" names one
     material and no values, so the model had nothing to anchor to and copied the background.
  2. A DARK OUTLINE. The sign's darkest pixel is 42. The bush's is 151 -- it has NO outline at
     all, which is why it reads as a floating smudge with no weight.
  3. INTERNAL DETAIL (0.39 vs 0.00). fern_tuft is a flat stencil: zero neighbouring pixels differ.

So every prompt below is built as:
    ANCHOR + SUBJECT(with 3-4 named materials at named values) + SIDE + OUTLINE + SILHOUETTE
Rule kept from forest_prompts.py: SUBJECT must come immediately after ANCHOR, or the subject
gets pushed out and PixelLab returns a landscape. SIDE/GUARD stay short and go last.

MATERIALS is the contrast recipe: each prop names a dark anchor, a mid body, and a light accent.
"""

ANCHOR = ("Korean Joseon folk pixel art, muted dawn palette of desaturated mossy green, "
          "wet earth brown and misty blue-gray, soft dawn light from the left, "
          "dark warm brown single color outline (no pure black), gentle mid value contrast, "
          "tranquil ink-wash mood. ")

SIDE = ("Seen strictly from the side at eye level as a flat 2D platformer side elevation: no top "
        "plane is visible, moss reads edge-on as a thin fringe along the top silhouette, NOT a "
        "visible top surface. ")

# ★ the three measured traits of the prop the user liked, forced explicitly.
CRAFT = ("Drawn with a crisp dark warm brown outline all the way around and around every internal "
         "part, so the object reads as a solid crafted thing with real weight sitting on the "
         "ground. Full value range from deep shadow at the base to a bright lit edge on its upper "
         "left. Clearly readable silhouette with distinct notches and bumps, never a soft blob. ")

TAIL = SIDE + CRAFT + "Transparent background, nothing else in frame."

COMMON = dict(view="side", outline="single color outline", shading="medium shading",
              detail="high detail")

# ---------------------------------------------------------------------------
# Size hierarchy. PPU = 64, so 1 unit = 64 px. Canvas ~= final on-screen size:
# pixel art must be generated at its final resolution, never scaled afterwards.
#   S = <=1u    M = 1-2u    L = 2-3u
# ★ The shipped props have no hierarchy at all -- every one measures 1.0-1.6u
#   (sign 102x95, boulder 84x68, bush 87x87, fern 63x68, stump 74x68). A forest where every
#   object is the same size reads as flat and repetitive, which is part of "부실한 느낌".
TIER_CANVAS = {"S": (64, 64), "M": (128, 128), "L": (192, 192)}

# non-square silhouettes get an explicit canvas so the prompt's stated proportion is achievable
CANVAS = {
    "prop_fallen_log":       (128, 72),
    "prop_bush_cluster":     (128, 96),
    "prop_exposed_roots":    (128, 80),
    "prop_stepping_stones":  (128, 56),
    "prop_straw_shoes":      (72, 40),
    "prop_stone_pile":       (80, 64),
    "prop_boulder_small":    (64, 48),
    "prop_mushroom_cluster": (72, 56),
    "prop_wildflowers":      (72, 72),
    "prop_grass_tuft":       (64, 64),
    "prop_fern_tuft":        (112, 104),
    "prop_mossy_boulder":    (128, 96),
    "prop_tree_stump":       (112, 112),
    "prop_reeds":            (112, 152),
    "prop_bamboo_clump":     (128, 192),
    "prop_vine_hanging":     (96, 192),
    "prop_sinmok_rope":      (160, 192),
    "prop_seonangdang_cairn": (128, 192),
    "prop_deungnong":        (96, 168),
    "prop_jige":             (112, 144),
    "prop_fence_wood":       (144, 104),
    "prop_onggi_jar":        (80, 88),
    "prop_water_jar":        (88, 64),
    "prop_boulder_large":    (176, 160),
}


def canvas_for(name, tier):
    return CANVAS.get(name, TIER_CANVAS[tier])

# (name, subject, mask_fraction, size_tier)
#   size tiers at PPU 64:  S = <=1u (<=64px)   M = 1-2u (64-128px)   L = 2-3u (128-192px)
PROPS = [
    # ---------- REGEN: the five the user called flimsy ----------
    ("prop_fern_tuft", ANCHOR +
     "A dense tuft of forest ferns sprouting from the forest floor: several arching fronds of deep "
     "shadowed green with fine paired leaflets, two pale yellow-green young fronds curled into "
     "tight fiddlehead spirals at the centre, dry rust-brown fallen leaves and one small grey "
     "pebble tucked at the base. The fronds arch outward so the silhouette is spiky and separated, "
     "each leaflet edged in dark brown. " + TAIL, 0.5, "M"),

    ("prop_bush_cluster", ANCHOR +
     "A low wide cluster of forest undergrowth sitting on the ground, much wider than tall: dense "
     "dark blue-green foliage in three overlapping rounded masses, a scatter of lighter olive "
     "leaves catching the dawn light on the upper left, bare rust-brown twigs poking out of the "
     "top and right, and deep shadow underneath where it meets the earth. Ragged leafy "
     "silhouette. " + TAIL, 0.55, "M"),

    ("prop_mossy_boulder", ANCHOR +
     "One irregular angular weathered granite boulder resting on the forest floor, wider than it "
     "is tall: flat chipped facets of cool grey stone with a dark crack down the right side, a "
     "bright lit facet on the upper left, soft olive moss growing only along its top ridge and in "
     "the crack, and a dark shadow pooling where it meets the ground. Blocky broken silhouette "
     "with sharp corners - absolutely not a sphere, not round, not a ball, not a smooth lump. "
     + TAIL, 0.5, "M"),

    ("prop_tree_stump", ANCHOR +
     "An old cut pine stump standing on the forest floor: a stout trunk of warm grey-brown bark "
     "peeling away in curled strips to show pale cream sapwood underneath, a flat cut top edge "
     "seen edge-on as a thin pale line, concentric growth rings barely visible at that edge, olive "
     "moss up its shaded right side, two small cream mushrooms at its base and a gnarled root "
     "gripping the earth. " + TAIL, 0.5, "M"),

    ("prop_fallen_log", ANCHOR +
     "A fallen pine log lying flat and horizontal on the forest floor, a long low cylinder much "
     "wider than tall: warm grey-brown bark peeling in strips, one cut end facing the viewer "
     "showing pale cream growth rings, olive moss along its top length, a dark hollow knot in its "
     "side and deep shadow beneath it. " + TAIL, 0.55, "M"),

    # ---------- NEW: nature ----------
    ("prop_mushroom_cluster", ANCHOR +
     "A small cluster of five forest mushrooms growing from the leaf litter: rounded caps of dull "
     "russet brown with pale cream undersides and slender cream stems, one cap tipped and broken, "
     "growing out of dark rust-brown fallen leaves and a patch of olive moss. " + TAIL, 0.35, "S"),

    ("prop_wildflowers", ANCHOR +
     "A small patch of Korean wild forest flowers: slender olive-green stems with narrow leaves "
     "carrying a handful of tiny pale cream and dusty rose blossoms, a few unopened buds, rising "
     "from a low clump of dark moss. Delicate airy silhouette. " + TAIL, 0.35, "S"),

    ("prop_grass_tuft", ANCHOR +
     "A single small tuft of tall forest grass sprouting from the earth: a dozen thin blades of "
     "dull olive green fanning out and arching over, the outer blades dry and rust-brown, a few "
     "seed heads nodding at the tips, dark shadow at the base. Spiky separated silhouette. "
     + TAIL, 0.3, "S"),

    ("prop_stone_pile", ANCHOR +
     "A small natural pile of four weathered river stones resting on the forest floor: rounded "
     "cool grey stones of different sizes stacked loosely, the lit upper left faces pale grey, the "
     "shaded faces deep blue-grey, olive moss in the gaps between them and dark shadow beneath. "
     + TAIL, 0.4, "S"),

    ("prop_exposed_roots", ANCHOR +
     "A gnarled tangle of exposed tree roots arching out of the forest floor and back into it: "
     "thick ropes of warm grey-brown wood with deep shadowed hollows between them, olive moss on "
     "their upper curves, dark packed earth clinging underneath. Low and wide, knotted "
     "silhouette. " + TAIL, 0.5, "M"),

    ("prop_reeds", ANCHOR +
     "A stand of tall slender forest reeds growing from damp earth: straight olive-green stalks of "
     "varying height with narrow drooping leaves, dry pale cream seed plumes at the tops, a few "
     "stalks bent over, dark shadow pooling at the base. Tall narrow airy silhouette. "
     + TAIL, 0.55, "M"),

    ("prop_bamboo_clump", ANCHOR +
     "A clump of three slender bamboo culms growing straight up from the forest floor: smooth "
     "olive-green segmented stalks with dark rings at each joint, sparse narrow blue-green leaves "
     "near the top, one culm shorter and dry rust-brown, dark moss and fallen leaves at the base. "
     "Tall narrow silhouette. " + TAIL, 0.7, "L"),

    ("prop_vine_hanging", ANCHOR +
     "A hanging forest vine draping downward: a twisted rust-brown woody stem with dark blue-green "
     "heart-shaped leaves alternating along its length, a few pale olive tendrils curling off it, "
     "hanging free and tapering to a point at the bottom. Tall narrow silhouette, cut off flush at "
     "the top canvas edge. " + TAIL, 0.6, "M"),

    ("prop_boulder_small", ANCHOR +
     "One small angular chipped granite rock sitting on the forest floor, low and wide: cool grey "
     "stone with two flat broken facets, a bright lit edge on the upper left, a thin fringe of "
     "olive moss along its top edge and dark shadow where it meets the ground. Blocky silhouette, "
     "not round. " + TAIL, 0.3, "S"),

    ("prop_boulder_large", ANCHOR +
     "One big angular weathered granite boulder embedded in the forest floor, taller than a man: "
     "large flat chipped facets of cool grey stone split by a deep dark vertical crack, a bright "
     "pale lit facet on the upper left, thick olive moss along its top ridge and down the shaded "
     "right side, a small fern growing from the crack, dark shadow at its base. Blocky broken "
     "silhouette with sharp corners, not round. " + TAIL, 0.75, "L"),

    # ---------- NEW: man-made (the category the praised sign belongs to) ----------
    ("prop_seonangdang_cairn", ANCHOR +
     "A Korean seonangdang shrine cairn on the forest path: a tall loose tower of stacked flat "
     "grey stones of many sizes narrowing toward the top, the lit faces pale grey and the gaps "
     "between them deep shadow, a braided pale straw rope tied around its middle with two small "
     "strips of cream hanji paper knotted to it, olive moss at the base. Irregular stacked "
     "silhouette. " + TAIL, 0.75, "L"),

    ("prop_onggi_jar", ANCHOR +
     "A single Korean onggi earthenware jar standing on the forest floor: a rounded dark "
     "red-brown glazed clay body with a wide shoulder narrowing to a short neck, a bright curved "
     "highlight down its left side, a flat dark lid on top with a small knob, and a chipped "
     "cream-coloured scuff near its foot. Dark shadow beneath. " + TAIL, 0.4, "S"),

    ("prop_water_jar", ANCHOR +
     "A Korean earthenware water jar lying tipped on its side on the forest floor: a rounded dark "
     "brown clay body with a wide mouth facing the viewer showing a dark hollow interior, a bright "
     "highlight along its upper left curve, a braided straw carrying ring beside it, and a damp "
     "dark patch on the earth at its mouth. " + TAIL, 0.35, "S"),

    ("prop_fence_wood", ANCHOR +
     "A short broken section of rustic Korean wooden fence standing in the forest floor: three "
     "weathered grey-brown timber posts of uneven height with two horizontal rails lashed across "
     "them with pale braided straw rope, the rightmost post snapped off short and splintered, "
     "olive moss creeping up the bases. Open gaps between the posts. " + TAIL, 0.6, "M"),

    ("prop_jige", ANCHOR +
     "A Korean jige A-frame carrying rack leaning against nothing on the forest floor: two "
     "weathered grey-brown wooden legs joined by lashed cross-pieces of pale braided straw rope, "
     "a woven straw pad on the back, a short carrying stick propped beside it, standing tilted "
     "with its feet in the earth. Open triangular silhouette. " + TAIL, 0.6, "M"),

    ("prop_deungnong", ANCHOR +
     "A Korean deungnong lantern hanging from a short weathered wooden post planted in the forest "
     "floor: a small six-sided lantern with a dark wooden frame and glowing pale cream hanji paper "
     "panels, a tiny dark tiled cap on top, hung from the post's crook by a short braided straw "
     "rope, soft warm light spilling onto the post. " + TAIL, 0.6, "M"),

    ("prop_straw_shoes", ANCHOR +
     "A pair of Korean jipsin straw sandals resting on the forest floor: woven pale straw soles "
     "with fine braided texture and looped straw toe cords, one sandal lying flat and the other "
     "tipped on its side against it, a wisp of loose straw beside them, dark shadow beneath. Small "
     "and low. " + TAIL, 0.3, "S"),

    ("prop_stepping_stones", ANCHOR +
     "Three flat Korean stepping stones set into the forest floor in a row receding slightly: "
     "wide flat slabs of cool grey granite worn smooth on top, their lit edges pale and their "
     "sides deep shadow, packed dark earth and olive moss filling the gaps between them. Low and "
     "wide. " + TAIL, 0.4, "S"),

    ("prop_sinmok_rope", ANCHOR +
     "A sacred Korean sinmok tree trunk rising from the forest floor, seen as a section of trunk "
     "only: thick rugged grey-brown bark in deep vertical furrows, a heavy braided pale straw rope "
     "wound three times around it with knotted strips of cream hanji paper and small dusty rose "
     "cloth streamers hanging from it, olive moss up the shaded right side, gnarled roots gripping "
     "the earth. Cut off flush at the top canvas edge. " + TAIL, 0.8, "L"),
]
