# map_101 실좌표 1:1 합성 씬 (60u 중 20u 구간 3장). compose_b2.py 패턴 계승.
#
# 좌표계 (확정 사항 반영)
#   PPU 64, orthographicSize 5.625 + Pixel Perfect(ref 1280x720)  -> 화면 = 20u x 11.25u = 1280x720 (1:1)
#   맵 세로 9u(world y -0.5..8.5)가 화면 11.25u보다 작다 -> SnapCamera가 세로 중앙 고정 (center y=4.0)
#   따라서 보이는 y = 4.0 +- 5.625 = -1.625 .. 9.625
#   world->screen: sx = (wx - x0) * 64 ,  sy = (9.625 - wy) * 64
#   칸 규약: 칸 y의 윗변 = y + 0.5  -> 지면(y=0) 상면 = world y 0.5 = screen y 584
import json, os
from PIL import Image, ImageDraw

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
B2 = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b2"
MAN = json.load(open(os.path.join(OUT, "chunk_manifest.json")))
PPU = 64
W, H = 1280, 720
TOP_W = 9.625  # 화면 상단의 world y


def sy(wy):
    return round((TOP_W - wy) * PPU)


def load(n, d=OUT, suf="_final"):
    return Image.open(os.path.join(d, n + suf + ".png")).convert("RGBA")


def tight(im):
    b = im.split()[3].getbbox()
    return im.crop(b) if b else im


def pad_down(im, extra):
    """청크의 마지막 불투명 행을 아래로 연장 (흙 몸체를 화면 하단까지 이어 이음새 제거).
    ★근거: 생성기가 좌우 flush를 못 지켜 청크가 섬 모양으로 나온다. 겹침만으로는 흙 몸체에
    계단식 이음새가 남는다 -> 각 청크의 흙을 아래로 늘려 연속 덩어리로 만든다. 생성 0회."""
    w, h = im.size
    out = Image.new("RGBA", (w, h + extra), (0, 0, 0, 0))
    out.alpha_composite(im, (0, 0))
    px = out.load()
    for x in range(w):
        last = None
        for y in range(h):
            if px[x, y][3] > 200:
                last = px[x, y]
        if last:
            for y in range(h, h + extra):
                px[x, y] = last
    return out


# ---- 에셋 ----
GROUND = ["ground_flat_03_320x160", "ground_flat_02_320x160", "ground_flat_04_320x160"]
mound_a = load("derived_step_mound_a_256x128")
mound_b = load("derived_step_mound_b_256x128")
plat_mid = load("platform_earth_mid_192x96")
plat_cap = load("derived_platform_cap_L_96x96")
ladder = load("ladder_body_seg_96x128")
ladder_cap = load("ladder_top_cap_96x64")
bush = load("boundary_bush_96x256")
tree_l = load("tree_pine_large_320x384")
tree_s = load("tree_pine_small_192x256")
sign = tight(load("spot_sign_hanji"))
gate = tight(load("exit_gate_jangseung"))
boulder = tight(load("prop_mossy_boulder"))
stump = tight(load("prop_tree_stump"))
log = tight(load("prop_fallen_log"))
ridge = Image.open(os.path.join(B2, "bg_ridge_200x120_v2_final.png")).convert("RGBA")
jangseung = Image.open(os.path.join(B2, "prop_jangseung_b2_64x130.png")).convert("RGBA")
seokdeung = Image.open(os.path.join(B2, "prop_seokdeung_b2_48x78.png")).convert("RGBA")

CH = os.path.join(OUT, "char")
hanbok_walk = Image.open(os.path.join(CH, "seonbi_hanbok/Seonbi_Hanbok_Forest/animations/walk/east/frame_002.png")).convert("RGBA")
hanbok_climb = Image.open(os.path.join(CH, "seonbi_hanbok/Seonbi_Hanbok_Forest/animations/climb/north/frame_002.png")).convert("RGBA")
modern_walk = Image.open(os.path.join(CH, "player_modern/Player_Modern_Clothes/animations/walk/east/frame_002.png")).convert("RGBA")

# map_101 실데이터
SPOTS = [(6, 1, "61"), (12, 1, "62"), (18, 1, "63"), (28, 1, "64"),
         (38, 1, "65"), (43, 1, "66"), (50, 1, "67"), (50, 5, "68")]
EXIT_X = 57
DECOS = [(9.0, boulder), (16.0, stump), (26.0, log), (36.5, boulder),
         (41.0, stump), (53.0, log), (20.5, seokdeung), (55.5, jangseung)]
TREES_L = [4.5, 24.5, 44.5]
TREES_S = [15.0, 34.0, 54.0]


def sky_layer():
    stops = [(0.00, (116, 120, 146)), (0.45, (163, 164, 184)), (0.72, (206, 192, 182)), (1.00, (228, 209, 184))]
    im = Image.new("RGBA", (W, H))
    px = im.load()
    for y in range(H):
        t = y / (H - 1)
        for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
            if t0 <= t <= t1:
                f = (t - t0) / (t1 - t0)
                col = tuple(round(a + (b - a) * f) for a, b in zip(c0, c1))
                break
        for x in range(W):
            px[x, y] = col + (255,)
    return im


def section(x0, idx):
    c = sky_layer()
    d = ImageDraw.Draw(c)

    def X(wx):
        return round((wx - x0) * PPU)

    # 1) 원경 능선 (승인본 ×3 NEAREST — 승인 합성과 동일 기법). parallax 0.25 근사
    r3 = ridge.resize((ridge.width * 3, ridge.height * 3), Image.NEAREST)
    off = -round(x0 * PPU * 0.25) % r3.width
    for i in range(-1, W // r3.width + 2):
        c.alpha_composite(r3, (i * r3.width + off - r3.width, sy(6.0)))

    # 2) 안개 띠
    fog = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    fp = fog.load()
    y_top, y_bot = sy(4.2), sy(0.5)
    for y in range(y_top, min(H, y_bot + 40)):
        a = int(110 * min(1.0, (y - y_top) / max(1, (y_bot - y_top))))
        for x in range(W):
            fp[x, y] = (235, 232, 238, a)
    c.alpha_composite(fog)

    # 3) 배경 나무 (backdrop parallax 0.5 — 맵 데이터의 나무 6그루 위치 계승)
    for wx, im in [(x, tree_l) for x in TREES_L] + [(x, tree_s) for x in TREES_S]:
        px_ = round((wx - x0 * 0.5) * PPU) - im.width // 2
        if -im.width < px_ < W:
            c.alpha_composite(im, (px_, sy(0.5) - im.height + 8))

    # 4) 지상: 청크 겹쳐 배치 (★생성기가 좌우 flush를 못 지킴 -> 승인 합성과 동일한 겹침+백필)
    dirt = load(GROUND[0]).getpixel((160, 140))[:3] + (255,)
    d.rectangle([0, sy(0.5) + 24, W, H], fill=dirt)
    PITCH = 210
    n0 = int((x0 * PPU) // PITCH) - 1
    for i in range(n0, n0 + W // PITCH + 3):
        name = GROUND[i % len(GROUND)]
        im = load(name)
        if i % 2:
            im = im.transpose(Image.FLIP_LEFT_RIGHT)
        surf = MAN[name]["surface_median"]           # 청크별 실측 표면선으로 y 정렬
        im = pad_down(im, 240)                       # 흙을 화면 하단까지 연장 -> 이음새 제거
        c.alpha_composite(im, (i * PITCH - round(x0 * PPU), sy(0.5) - surf))
    # 연장부의 세로 줄무늬를 단색 흙으로 덮어 마감 (맵 하단 y=-0.5 아래는 어차피 화면 밖 영역)
    d.rectangle([0, sy(-0.55), W, H], fill=dirt)

    # 5) 단차 블록 2개: x22..25, x32..35 (칸 좌하단 기준 -> world 21.5..25.5), 상면 y=2.5
    for wx0, im in [(21.5, mound_a), (31.5, mound_b)]:
        c.alpha_composite(im, (X(wx0), sy(2.5) - 4))

    # 6) 원웨이 발판: world 42.5..51.5 (9u), 상면 y=4.5
    py = sy(4.5) - 40
    c.alpha_composite(plat_cap, (X(42.5), py))
    for k in range(3):
        c.alpha_composite(plat_mid, (X(44.0) + k * 160, py))
    c.alpha_composite(plat_cap.transpose(Image.FLIP_LEFT_RIGHT), (X(50.0), py))

    # 7) 사다리 x=47, y 0.5..4.5 (4u=256px)
    #    ★캔버스가 아니라 content 높이로 적층해야 한다 (캔버스 적층 시 여백만큼 틈이 생김 - 실측)
    lad = tight(ladder)
    lcap = tight(ladder_cap)
    lx = X(47.0) - lad.width // 2
    y = sy(0.5) - lad.height
    while y > sy(4.5) - lad.height:
        c.alpha_composite(lad, (lx, y))
        y -= lad.height
    c.alpha_composite(lad, (lx, sy(4.5)))
    c.alpha_composite(lcap, (X(47.0) - lcap.width // 2, sy(4.5) - lcap.height + 6))

    # 8) 경계 벽 x=0 / x=59
    bsh = tight(bush)
    for wx in (0.0, 59.0):
        bx = X(wx) - bsh.width // 2
        if -bsh.width < bx < W:
            y = sy(0.5) - bsh.height
            while y > sy(8.5) - bsh.height:
                c.alpha_composite(bsh, (bx, y))
                y -= bsh.height - 6

    # 9) 데코
    for wx, im in DECOS:
        px_ = X(wx) - im.width // 2
        if -im.width < px_ < W:
            c.alpha_composite(im, (px_, sy(0.5) - im.height + 3))

    # 10) 스팟 팻말 8개 (발 칸 기준 바닥 앵커)
    for wx, wy, lbl in SPOTS:
        px_ = X(wx) - sign.width // 2
        if not (-sign.width < px_ < W):
            continue
        base = sy(wy - 0.5)
        c.alpha_composite(sign, (px_, base - sign.height))
        d.text((px_ + sign.width // 2 - 4, base - sign.height + 30), lbl, fill=(40, 30, 25, 255))

    # 11) 출구 문
    gx = X(EXIT_X) - gate.width // 2
    if -gate.width < gx < W:
        c.alpha_composite(gate, (gx, sy(0.5) - gate.height))

    # 12) 캐릭터: 현대복 = 스폰(x2, 세종 만나기 전 평지 구간) / 한복 = x14 보행 + x47 사다리
    for wx, im, wy in [(2.0, modern_walk, 0.5), (14.0, hanbok_walk, 0.5)]:
        t = tight(im)
        px_ = X(wx) - t.width // 2
        if -t.width < px_ < W:
            c.alpha_composite(t, (px_, sy(wy) - t.height))
    t = tight(hanbok_climb)
    px_ = X(47.0) - t.width // 2
    if -t.width < px_ < W:
        c.alpha_composite(t, (px_, sy(3.2) - t.height))

    out = os.path.join(OUT, f"composite_{idx}_x{int(x0)}-{int(x0+20)}.png")
    c.convert("RGB").save(out)
    print("saved", out)


if __name__ == "__main__":
    for i, x0 in enumerate([0, 20, 40]):
        section(x0, i + 1)
