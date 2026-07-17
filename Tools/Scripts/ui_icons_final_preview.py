# 합격 판정용 최종 미리보기
#  _preview_small.png     : 두 아이콘을 20px로 축소 (1:1 + 6x 확대 나란히)
#  _preview_cell_mock.png : GameApp.StageSelect.cs 실측값으로 88x88 셀 재현
#     루비 22x22 @ anchor(0.5,1) offset(0,-8) / 잠금 30x30 @ center / 셀 88x88
import os
from PIL import Image, ImageDraw, ImageFont

UI = r"C:\Users\minjae\UnityProjects\HangeulAdventure\Assets\Resources\Art\Ui"
OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\ui_icons"

PAPER = (245, 240, 227, 255)      # UiFactory.Paper
LOCKBG = (209, 204, 194, 255)     # 잠긴 셀 배경 (0.82,0.80,0.76)
INK = (41, 36, 31, 255)
DIM = (140, 133, 120)             # UiFactory.Dim  → 진행 잠금
GATE = (115, 110, 105)            # (0.45,0.43,0.41) → 자음 게이트
STAR = (242, 184, 31, 255)
SHEET_BG = (250, 249, 245, 255)


def tint(im, t):
    im = im.convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r * t[0] // 255, g * t[1] // 255, b * t[2] // 255, a)
    return im


def font(sz):
    for p in (r"C:\Windows\Fonts\malgun.ttf", r"C:\Windows\Fonts\arial.ttf"):
        if os.path.exists(p):
            return ImageFont.truetype(p, sz)
    return ImageFont.load_default()


def preview_small():
    """20px 축소 — 이게 합격 기준."""
    lock = Image.open(os.path.join(UI, "lock.png"))
    ruby = Image.open(os.path.join(UI, "ruby.png"))
    items = [("ruby 20px", ruby, None, PAPER),
             ("lock 20px (진행)", lock, DIM, LOCKBG),
             ("lock 20px (자음게이트)", lock, GATE, LOCKBG)]
    Z = 6
    cellw = 20 * Z
    sheet = Image.new("RGBA", (len(items) * (cellw + 12) + 12, cellw + 56), SHEET_BG)
    d = ImageDraw.Draw(sheet)
    f = font(11)
    x = 12
    for label, im, t, bg in items:
        s = tint(im.copy(), t) if t else im.convert("RGBA")
        s = s.resize((20, 20), Image.LANCZOS)
        c = Image.new("RGBA", (20, 20), bg)
        c.alpha_composite(s)
        sheet.paste(c.resize((cellw, cellw), Image.NEAREST), (x, 10))
        # 1:1 실제 크기도 옆에 (진짜 눈으로 보는 크기)
        sheet.paste(c, (x, cellw + 16))
        d.text((x + 26, cellw + 20), label, font=f, fill=INK)
        x += cellw + 12
    sheet.save(os.path.join(OUT, "_preview_small.png"))
    print("wrote _preview_small.png")


def cell_mock():
    """88x88 스테이지 셀 재현 (코드 실측값)."""
    lock = Image.open(os.path.join(UI, "lock.png"))
    ruby = Image.open(os.path.join(UI, "ruby.png"))
    f30, f20, f14 = font(26), font(15), font(11)

    def cell(kind):
        bg = PAPER if kind in ("ruby", "plain") else LOCKBG
        c = Image.new("RGBA", (88, 88), bg)
        d = ImageDraw.Draw(c)
        if kind in ("lock_prog", "lock_gate"):
            t = DIM if kind == "lock_prog" else GATE
            s = tint(lock.copy(), t).resize((30, 30), Image.LANCZOS)
            c.alpha_composite(s, (29, 29))            # center
        else:
            d.text((44, 44), "12", font=f30, fill=INK, anchor="mm")
            d.text((44, 74), "★★☆", font=f20, fill=STAR, anchor="mm")
            d.text((81, 78), "3", font=f14, fill=(140, 133, 120, 255), anchor="mm")
            if kind == "ruby":
                s = ruby.resize((22, 22), Image.LANCZOS)
                c.alpha_composite(s, (33, 8))          # anchor(0.5,1) offset(0,-8)
        return c

    kinds = [("일반 (루비 없음)", "plain"), ("루비 획득", "ruby"),
             ("진행 잠금", "lock_prog"), ("자음 게이트", "lock_gate")]
    Z = 3
    sheet = Image.new("RGBA", (len(kinds) * (88 * Z + 14) + 14, 88 * Z + 100), SHEET_BG)
    d = ImageDraw.Draw(sheet)
    f = font(13)
    x = 14
    for label, k in kinds:
        c = cell(k)
        sheet.paste(c.resize((88 * Z, 88 * Z), Image.NEAREST), (x, 14))
        sheet.paste(c, (x + 88, 88 * Z + 24))          # 1:1 실제 크기
        d.text((x, 88 * Z + 24), label, font=f, fill=INK)
        x += 88 * Z + 14
    sheet.save(os.path.join(OUT, "_preview_cell_mock.png"))
    print("wrote _preview_cell_mock.png")


if __name__ == "__main__":
    preview_small()
    cell_mock()
