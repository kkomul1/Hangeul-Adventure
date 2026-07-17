# 실제 표시 크기 축소 미리보기 — 합격 판정용
# 루비 22px / 잠금 30px / 맵스팟 20px 로 줄여서 읽히는지 본다.
# usage: ui_icons_preview.py <png> [<png> ...]
import os, sys
from PIL import Image

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\ui_icons"

# (라벨, 표시크기, 배경색) — 배경은 실제 UI 색
CASES = [
    ("20px", 20),
    ("22px", 22),
    ("30px", 30),
]
ZOOM = 5


def contact(path, tint=None, bg=(209, 204, 194)):
    """축소 → 확대해서 나란히 붙인 대비 시트."""
    src = Image.open(path).convert("RGBA")
    if tint:
        px = src.load()
        for y in range(src.height):
            for x in range(src.width):
                r, g, b, a = px[x, y]
                px[x, y] = (r * tint[0] // 255, g * tint[1] // 255, b * tint[2] // 255, a)
    pad = 8
    tiles = []
    for label, size in CASES:
        small = src.resize((size, size), Image.LANCZOS)
        canvas = Image.new("RGBA", (size, size), bg + (255,))
        canvas.alpha_composite(small)
        tiles.append(canvas.resize((size * ZOOM, size * ZOOM), Image.NEAREST))
    W = sum(t.width for t in tiles) + pad * (len(tiles) + 1)
    H = max(t.height for t in tiles) + pad * 2
    sheet = Image.new("RGBA", (W, H), (245, 243, 238, 255))
    x = pad
    for t in tiles:
        sheet.paste(t, (x, pad))
        x += t.width + pad
    return sheet


def actual_size_row(path, tint=None, bg=(209, 204, 194)):
    """1:1 실제 크기 (확대 없음) — 진짜 눈으로 보는 크기."""
    src = Image.open(path).convert("RGBA")
    if tint:
        px = src.load()
        for y in range(src.height):
            for x in range(src.width):
                r, g, b, a = px[x, y]
                px[x, y] = (r * tint[0] // 255, g * tint[1] // 255, b * tint[2] // 255, a)
    sheet = Image.new("RGBA", (120, 40), (245, 243, 238, 255))
    x = 6
    for label, size in CASES:
        small = src.resize((size, size), Image.LANCZOS)
        cell = Image.new("RGBA", (size, size), bg + (255,))
        cell.alpha_composite(small)
        sheet.paste(cell, (x, (40 - size) // 2))
        x += size + 8
    return sheet


if __name__ == "__main__":
    for p in sys.argv[1:]:
        name = os.path.splitext(os.path.basename(p))[0]
        tint = (140, 133, 120) if "lock" in name else None   # UiFactory.Dim 근사
        sheet = contact(p, tint=tint)
        sheet.save(os.path.join(OUT, f"_preview_small_{name}.png"))
        actual_size_row(p, tint=tint).save(os.path.join(OUT, f"_actual_{name}.png"))
        print("wrote", f"_preview_small_{name}.png")
