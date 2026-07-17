# lookcheck_b2 composite: 1280x720, integer scale, real 1:1 gameplay scale (PPU64 B-plan)
# Revision goals: continuous ground (no gaps), 2 floating platforms, unified dawn palette
import os, shutil
from PIL import Image, ImageDraw

B1 = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b"
D = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b2"
W, H = 1280, 720

# ---- copy reused assets into b2 for self-containment ----
for f in ["char_seonbi_east_180.png", "ladder_wood_48x128.png", "bg_ridge_200x120_v2_final.png"]:
    if not os.path.exists(os.path.join(D, f)):
        shutil.copy(os.path.join(B1, f), os.path.join(D, f))

def load(name):
    return Image.open(os.path.join(D, name)).convert("RGBA")

# ---- crop props tight, save versioned ----
sk = load("prop_seokdeung_b2.png")
sk = sk.crop(sk.split()[3].getbbox())            # 48x78
sk.save(os.path.join(D, "prop_seokdeung_b2_48x78.png"))
jg = load("prop_jangseung_b2.png")
jg = jg.crop(jg.split()[3].getbbox())            # 64x130
jg.save(os.path.join(D, "prop_jangseung_b2_64x130.png"))

# ---------- 1. dawn sky (blue-gray -> pale warm horizon) ----------
sky = Image.new("RGBA", (W, H))
stops = [(0.00, (116, 120, 146)), (0.45, (163, 164, 184)), (0.72, (206, 192, 182)), (1.00, (228, 209, 184))]
px = sky.load()
for y in range(H):
    t = y / (H - 1)
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0)
            col = tuple(round(a + (b - a) * f) for a, b in zip(c0, c1))
            break
    for x in range(W):
        px[x, y] = col + (255,)
canvas = sky
d = ImageDraw.Draw(canvas)

# ---------- 2. far ridges 3x + base fill ----------
bg = load("bg_ridge_200x120_v2_final.png")
bg3 = bg.resize((bg.width * 3, bg.height * 3), Image.NEAREST)   # 600x360
bg3m = bg3.transpose(Image.FLIP_LEFT_RIGHT)
BG_Y = 210
# base fill color: average of bottom opaque row of ridge art
bpx = bg.load()
cols = [bpx[x, bg.height - 1] for x in range(bg.width) if bpx[x, bg.height - 1][3] > 128]
base_col = tuple(sum(c[i] for c in cols) // len(cols) for i in range(3)) + (255,)
d.rectangle([0, BG_Y + 350, W, H], fill=base_col)
for i, tile in enumerate([bg3, bg3m, bg3]):
    canvas.alpha_composite(tile, (i * 600, BG_Y))

# ---------- 3. fog band ----------
fog = Image.new("RGBA", (W, H), (0, 0, 0, 0))
fpx = fog.load()
for y in range(400, H):
    a = int(115 * (y - 400) / 220) if y < 620 else 115
    for x in range(W):
        fpx[x, y] = (235, 232, 238, a)
canvas.alpha_composite(fog)

GROUND_WALK = 640          # walkable grass line

# ---------- 4. ladder (behind ground & platform) ----------
ladder = load("ladder_wood_48x128.png")
LAD_X = 165
for top in (452, 556):
    canvas.alpha_composite(ladder, (LAD_X, top))

# ---------- 5. continuous ground: terrain B chunks, overlapped, + dirt backfill ----------
terrB = load("terrain_flat_b_320x160_keyed.png")   # surface ~y71
terrBm = terrB.transpose(Image.FLIP_LEFT_RIGHT)
tpx = terrB.load()
dirt = tpx[160, 130][:3] + (255,)                  # interior dirt color
d.rectangle([0, 672, W, H], fill=dirt)
PITCH = 220
xs = list(range(-40, W + 40, PITCH))
for i, x in enumerate(xs):
    dy = (0, -2, 1, -1, 2, 0)[i % 6]
    canvas.alpha_composite(terrBm if i % 2 else terrB, (x, GROUND_WALK - 71 + dy))

# ---------- 6. hill (terrain A2) planted into ground, right side ----------
terrA = load("terrain_hill_a2_320x160_keyed.png")  # flank surface ~137, dome top ~58
HILL_X = 890
canvas.alpha_composite(terrA, (HILL_X, GROUND_WALK - 137))     # paste_y=503, dome top=561

# ---------- 7. floating platforms (2 tiers, same terrain style) ----------
plat_l = load("platform_float_large_160x64.png")   # content bbox (26,18)-(132,53)
plat_s = load("platform_float_small_96x48.png")    # content bbox (18,8)-(78,40)
canvas.alpha_composite(plat_l, (110, 430))         # tier 1, top surface ~448
canvas.alpha_composite(plat_s, (300, 330))         # tier 2, top surface ~338

# ---------- 8. props (regenerated, unified palette) ----------
canvas.alpha_composite(sk, (566, GROUND_WALK - sk.height + 2))          # seokdeung on ground
canvas.alpha_composite(jg, (1050 - jg.width // 2, 565 - jg.height))     # jangseung on hill dome

# ---------- 9. character (reused, feet on ground) ----------
char = load("char_seonbi_east_180.png").crop((58, 14, 121, 159))        # 63x145
canvas.alpha_composite(char, (400 - char.width // 2, GROUND_WALK - char.height))

# ---------- 10. firefly accents ----------
glow = (255, 233, 160)
for (fx, fy) in [(320, 430), (505, 380), (700, 470), (985, 300), (240, 560), (1120, 500)]:
    d.rectangle([fx, fy, fx + 2, fy + 2], fill=glow + (230,))
    d.rectangle([fx - 2, fy - 2, fx + 4, fy + 4], outline=glow + (60,))

out = os.path.join(D, "scene_composite_1280.png")
canvas.convert("RGB").save(out)
print("saved", out)
