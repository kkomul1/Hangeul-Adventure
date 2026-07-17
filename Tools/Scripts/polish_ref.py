"""Build ref_forest_192.png -- the style reference for prop inpainting.  0 generations.

★ ROOT CAUSE this fixes (measured, see prop_diagnose.py):
  The old ref_style_192.png was ~62% flat pastel SKY. Every prop's inpainting mask is centred
  on the canvas, so 67-86% of each mask landed on that sky. PixelLab "matches the style" of what
  surrounds the mask -> weakly-described organic props inherited the sky wholesale:
      ref sky band mean = (161, 161, 178)
      prop_fern_tuft dominant = (155, 156, 177)   <- the fern IS the sky colour, within 6/255
  Props with strongly-named contrasting materials (spot_sign_hanji: "pale cream hanji" + "dark
  wood") resisted it. That is the entire difference between the prop the user liked and the ones
  they called flimsy.

FIX: keep the proven ground line at 62% (masks still straddle the ground), but replace the sky
with a dense forest thicket built from boundary_bush.png. boundary_bush and ground_flat_03 came
from the same palette-locked batch (they share #96C36A and #52755F exactly), so the whole ref is
one consistent palette with real dark values (thicket lum p5=63, vs sky p5=155).
A prop inpainted against this inherits dark outline + moss green + earth brown instead of haze.
"""
import os
from PIL import Image, ImageEnhance

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art", "Forest")
OUT = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish")
os.makedirs(OUT, exist_ok=True)

W = H = 192
GROUND_Y = 120          # 62% -- proven: masks straddle the ground line (forest_ref.py finding)
SURFACE_IN_CHUNK = 63   # ground_flat_03 measured surface line (chunk_manifest.json)


def build():
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    # --- backdrop: dense thicket (replaces the sky that poisoned every prop) ---
    bush = Image.open(os.path.join(ART, "Terrain", "boundary_bush.png")).convert("RGBA")
    bw, bh = bush.size
    thicket = Image.new("RGBA", (W, H), (57, 63, 80, 255))  # #393F50 = bush's own dominant
    for tx in range(0, W + bw, bw):
        for ty in range(0, GROUND_Y + bh, bh):
            thicket.alpha_composite(bush, (tx, ty))
            thicket.alpha_composite(bush.transpose(Image.FLIP_LEFT_RIGHT), (tx + bw, ty))
    # push it back a touch so it reads as "behind" without losing its dark values
    thicket = ImageEnhance.Brightness(thicket).enhance(0.92)
    canvas.alpha_composite(thicket.crop((0, 0, W, H)))

    # --- ground: shipped ground_flat_03, surface aligned to GROUND_Y ---
    g = Image.open(os.path.join(ART, "Terrain", "ground_flat_03.png")).convert("RGBA")
    crop = g.crop((64, 0, 64 + W, g.size[1]))
    canvas.alpha_composite(crop, (0, GROUND_Y - SURFACE_IN_CHUNK))

    # backfill soil to the bottom edge with the chunk's own earth colour
    earth = crop.getpixel((96, 140))[:3] + (255,)
    bot = GROUND_Y - SURFACE_IN_CHUNK + g.size[1]
    if bot < H:
        Image.new("RGBA", (W, H - bot), earth)
        canvas.paste(Image.new("RGBA", (W, H - bot), earth), (0, bot))

    p = os.path.join(OUT, "ref_forest_192.png")
    canvas.convert("RGB").save(p)
    print("wrote", p, "ground_y=", GROUND_Y)
    return p


if __name__ == "__main__":
    build()
