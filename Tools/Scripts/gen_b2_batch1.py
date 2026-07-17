# lookcheck_b2 batch 1: terrain A/B + floating platforms (4 generations)
import json, time
from mcp import call_tool, tool_text

ANCHOR = ("Korean Joseon folk pixel art, muted dawn palette of desaturated mossy green, "
          "wet earth brown and misty blue-gray, soft dawn light from the left, "
          "dark warm brown single color outline (no pure black), gentle mid value contrast, "
          "tranquil ink-wash mood. ")

JOBS = [
    ("terrain_hill_a_320x160", {
        "description": ANCHOR + (
            "Continuous ground terrain chunk for a side-scrolling platformer, side view: "
            "a solid mass of packed brown earth topped with a walkable layer of short mossy grass. "
            "The grass surface line starts at the LEFT canvas edge about one quarter down from the top, "
            "rises into one gentle rounded hill in the middle, then returns to EXACTLY the same height "
            "at the RIGHT canvas edge. The soil body is filled completely solid all the way down to the "
            "bottom edge of the canvas and is cut off flush at both left and right canvas edges so that "
            "copies placed side by side connect seamlessly. A few weathered gray stones and thin roots "
            "embedded in the earth, small grass tufts on top. No gaps, no floating pieces, no background."),
        "width": 320, "height": 160,
        "view": "side",
        "outline": "single color outline",
        "shading": "medium shading",
        "detail": "high detail",
    }),
    ("terrain_flat_b_320x160", {
        "description": ANCHOR + (
            "Continuous ground terrain chunk for a side-scrolling platformer, side view: "
            "a solid mass of packed brown earth topped with a walkable layer of short mossy grass. "
            "The grass surface line runs nearly flat with only soft subtle undulation, sitting about one "
            "quarter down from the top at BOTH the left and right canvas edges at exactly the same height. "
            "On top sit one half-buried mossy boulder and a few short grass tufts. The soil body is filled "
            "completely solid down to the bottom edge of the canvas and is cut off flush at both left and "
            "right canvas edges so copies placed side by side connect seamlessly. A few weathered gray "
            "stones embedded in the earth. No gaps, no floating pieces, no background."),
        "width": 320, "height": 160,
        "view": "side",
        "outline": "single color outline",
        "shading": "medium shading",
        "detail": "high detail",
    }),
    ("platform_float_large_160x64", {
        "description": ANCHOR + (
            "A floating earth platform drifting in the air for a side-scrolling platformer, side view: "
            "flat walkable top of short mossy grass over a compact body of packed brown earth with a few "
            "embedded weathered gray stones, gently rounded underside with two or three short hanging "
            "roots and a small moss patch. Silhouette clearly wider than tall. Same material and style as "
            "grassy ground terrain of the same world. Transparent background."),
        "width": 160, "height": 64,
        "view": "side",
        "outline": "single color outline",
        "shading": "medium shading",
        "detail": "high detail",
    }),
    ("platform_float_small_96x48", {
        "description": ANCHOR + (
            "A small floating earth platform drifting in the air for a side-scrolling platformer, side "
            "view: flat walkable top of short mossy grass over a small compact lump of packed brown earth "
            "with one embedded gray stone, gently rounded underside with one short hanging root. "
            "Same material and style as grassy ground terrain of the same world. Transparent background."),
        "width": 96, "height": 48,
        "view": "side",
        "outline": "single color outline",
        "shading": "medium shading",
        "detail": "high detail",
    }),
]

ids = {}
for name, args in JOBS:
    for attempt in range(6):
        r = call_tool("create_map_object", args)
        txt = tool_text(r)
        if "rate limit" in txt.lower():
            print(f"{name}: rate limited, wait 30s (attempt {attempt})")
            time.sleep(30)
            continue
        oid = None
        for line in txt.splitlines():
            if line.strip().lower().startswith("id:"):
                oid = line.split(":", 1)[1].strip()
        ids[name] = oid
        print(f"{name}: id={oid}")
        if oid is None:
            print("  RAW:", txt[:400])
        break
    time.sleep(15)

with open("b2_batch1_ids.json", "w") as f:
    json.dump(ids, f, indent=1)
print("queued:", ids)
