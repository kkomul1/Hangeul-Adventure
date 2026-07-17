# 캐릭터 시트만 다시 굽는다 (Char/ 전용).
#
# ★ forest_import.py를 쓰지 말 것: 그건 Assets/Resources/Art/Forest 전체를 rmtree 한다.
#   Terrain/Props/Backdrop은 다른 작업자의 영역이라 날리면 안 된다. 여기선 Char/*.png만 덮어쓴다.
#   프레임 수(Idle 4/Walk 6/Run 6/Jump 6/Climb 5)를 그대로 유지하므로 기존 .meta 슬라이싱·피벗(y=0.1176) 유효.
#
# usage: forest_char_sheets.py bake | check
import os, sys
from PIL import Image

ROOT = r"C:\Users\minjae\UnityProjects\HangeulAdventure"
SRC = os.path.join(ROOT, r"ArtDrop\Generated\forest_main\char")
DST = os.path.join(ROOT, r"Assets\Resources\Art\Forest\Char")
CELL = 136

# 선비 애니 소스 (책보 통일본). check()도 이 경로를 쓴다.
SEONBI_SRC = "seonbi_chaekbo/Seonbi_Chaekbo"

# (캐릭터폴더, 시트이름, 소스 애니 경로, 채택 프레임 인덱스 | None=전부)
SHEETS = [
    # ★ 소스 교체: 베이스에 책보(冊褓)를 허리로 옮긴 create_character_state 후보 B(seonbi_chaekbo)에서
    #   5개 애니를 v3로 전부 재생성했다. 구 소스(seonbi_hanbok)는 소품이 손/허리로 제각각이라 폐기.
    #   도포·갓 팔레트는 use_color_palette_from_reference로 원본과 동일(새 색 0). 프레임 수 불변.
    (SEONBI_SRC, "SeonbiHanbok", [
        ("idle/east",   "Idle",  None),
        ("walk/east",   "Walk",  None),
        ("run/east",    "Run",   None),
        ("jump/east",   "Jump",  None),
        ("climb/north", "Climb", None),
    ]),
    ("player_modern/Player_Modern_Clothes", "PlayerModern", [
        ("idle/east", "Idle", None),
        ("walk/east", "Walk", None),
    ]),
]


def frames(char_rel, anim_rel, pick):
    d = os.path.join(SRC, *char_rel.split("/"), "animations", *anim_rel.split("/"))
    fs = sorted(f for f in os.listdir(d) if f.endswith(".png"))
    if pick is not None:
        fs = [fs[i] for i in pick]
    ims = []
    for f in fs:
        im = Image.open(os.path.join(d, f)).convert("RGBA")
        if im.size != (CELL, CELL):
            raise SystemExit(f"[치명] {d}/{f}: 캔버스 {im.size} != {CELL}")
        ims.append(im)
    return ims


def bake():
    for char_rel, dst_name, anims in SHEETS:
        for anim_rel, sheet_name, pick in anims:
            ims = frames(char_rel, anim_rel, pick)
            sheet = Image.new("RGBA", (CELL * len(ims), CELL), (0, 0, 0, 0))
            for i, im in enumerate(ims):
                sheet.alpha_composite(im, (i * CELL, 0))
            p = os.path.join(DST, dst_name, sheet_name + ".png")
            os.makedirs(os.path.dirname(p), exist_ok=True)
            sheet.save(p)
            print(f"  {dst_name}/{sheet_name}: {len(ims)}f {sheet.size} <- {anim_rel}")


# ---- 검수: 대조 시트 + 도포 하단 실측 ----
def _teal(p):
    r, g, b, a = p
    return a > 128 and g > r + 8 and b > r + 4 and 90 < g < 210 and r < 190


def _blk(p):
    r, g, b, a = p
    return a > 128 and r < 60 and g < 60 and b < 60


def measure(im):
    """도포 하단 y / 알파 하단 y / 갓 폭(최상단 검은덩어리 12행 내 최대폭)."""
    px = im.load()
    W, H = im.size
    tb = ab = -1
    for y in range(H):
        for x in range(W):
            if px[x, y][3] > 128:
                ab = y
            if _teal(px[x, y]):
                tb = y
    top = next((y for y in range(H) if any(_blk(px[x, y]) for x in range(W))), None)
    gw = 0
    if top is not None:
        for y in range(top, min(top + 12, H)):
            xs = [x for x in range(W) if _blk(px[x, y])]
            if xs:
                gw = max(gw, max(xs) - min(xs) + 1)
    return tb, ab, gw


def check():
    """세로로 Idle/Walk/Run/Jump/Climb를 쌓은 대조 시트 + 수치."""
    rows = [("Idle", "idle/east"), ("Walk", "walk/east"), ("Run", "run/east"),
            ("Jump", "jump/east"), ("Climb", "climb/north")]
    char_rel = SEONBI_SRC
    sets = [(n, frames(char_rel, r, None)) for n, r in rows]
    cols = max(len(f) for _, f in sets)
    BG = (245, 244, 240, 255)
    sheet = Image.new("RGBA", (CELL * cols, CELL * len(sets)), BG)
    print(f"{'anim':6} {'f':>2} {'robeBot':>7} {'alphaBot':>8} {'legGap':>6} {'gatW':>5}")
    stats = {}
    for r, (name, ims) in enumerate(sets):
        gaps, gws = [], []
        for c, im in enumerate(ims):
            sheet.alpha_composite(im, (c * CELL, r * CELL))
            tb, ab, gw = measure(im)
            gaps.append(ab - tb)
            gws.append(gw)
            print(f"{name:6} {c:2} {tb:7} {ab:8} {ab - tb:6} {gw:5}")
        stats[name] = (gaps, gws)
    for r in range(1, len(sets)):
        for x in range(sheet.width):
            sheet.putpixel((x, r * CELL), (200, 120, 120, 255))
    p = os.path.join(ROOT, r"ArtDrop\Generated\forest_main\_costume_check.png")
    sheet.save(p)
    print("\n대조 시트:", p)
    print(f"\n{'anim':6} {'legGap(도포밑단→발끝)':>24} {'gatW':>14}")
    for n, (g, w) in stats.items():
        print(f"{n:6} {str(g):>24} {str(w):>14}")
    allg = [v for g, _ in stats.values() for v in g]
    allw = [v for _, w in stats.values() for v in w]
    print(f"\nlegGap 범위 {min(allg)}~{max(allg)} (편차 {max(allg) - min(allg)})")
    print(f"gatW   범위 {min(allw)}~{max(allw)} (편차 {max(allw) - min(allw)})")


if __name__ == "__main__":
    {"bake": bake, "check": check}[sys.argv[1]]()
