# 지형 파생 (생성 0회). 생성기가 캔버스 flush를 못 지키므로, 잘 나온 생성물에서
# 심리스/규격 조각을 오프라인으로 깎아 만든다 (원본 forest_derive.py와 동일 철학).
#
# 산출:
#   1) 사다리 body_seg (심리스 적층) + top_cap (몸통과 동일 아트) <- ladder_body_v2 하나에서 파생
#   2) 단차 rock a/b/c (256x128, 상면 flush, 하단 채움) <- rock_step_b/d/f 에서 파생
#   3) 발판 mid (평평한 흙 슬래브, 상하 flush) + cap_L (둥근 좌측 마구리) <- platform_mid_v2 에서 파생
#
# usage: forest_terrain_derive.py
import os
from PIL import Image

G = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"


def L(n):
    return Image.open(os.path.join(G, n + ".png")).convert("RGBA")


def save(im, n):
    im.save(os.path.join(G, n + ".png"))
    print(f"  {n}: {im.size}")


def alpha_bbox_x(im):
    px = im.load(); W, H = im.size
    l = next(x for x in range(W) if any(px[x, y][3] > 128 for y in range(H)))
    r = next(x for x in range(W - 1, -1, -1) if any(px[x, y][3] > 128 for y in range(H)))
    return l, r


def pad_down(im, target_h):
    """마지막 불투명 행을 아래로 반복 (몸체 연장). forest_derive.pad_down 동일."""
    w, h = im.size
    out = Image.new("RGBA", (w, target_h), (0, 0, 0, 0))
    out.alpha_composite(im, (0, 0))
    px = out.load()
    for x in range(w):
        last = None
        for y in range(h):
            if px[x, y][3] > 128:
                last = px[x, y]
        if last:
            for y in range(h, target_h):
                px[x, y] = last
    return out


# ── 1) 사다리 ─────────────────────────────────────────────────────────
# ladder_body_v2: 레일 y=10~118 연속, 가로대 중심 y=21.5/43.5/65.5/88.5 (주기 22px).
#   가로대 '중간'에서 잘라 심리스 세그먼트를, '상단 둥근 끝'을 잘라 캡을 만든다.
def ladder():
    src = L("ladder_body_v2_96x128")
    W = 96
    seg = src.crop((0, 33, W, 55))    # 96x22, 한 주기, 레일 상/하단 flush, 가로대 1개 중앙
    save(seg, "ladder_body_seg")
    cap = src.crop((0, 8, W, 33))     # 96x25, 둥근 레일 끝 + 첫 가로대 ~ 가로대 중간(세그와 이어짐)
    save(cap, "ladder_top_cap")
    segH, capH, n = 22, 25, 4
    H = capH + segH * n + 8
    sim = Image.new("RGBA", (W, H), (245, 244, 240, 255))
    y = H - 4 - segH
    for _ in range(n):
        sim.alpha_composite(seg, (0, y)); y -= segH
    sim.alpha_composite(cap, (0, y + segH - capH))
    save(sim.resize((W * 4, H * 4), Image.NEAREST), "_sim_ladder")


# ── 2) 단차 바위 ──────────────────────────────────────────────────────
# rock_b/d/f: 회색 화강암 돌담, 상면 이끼. 상단 여백 제거→상면을 top으로, 폭 256 채움, 하단 128 pad.
def _rock_fill(core, target_h=128):
    """콘텐츠 아래를 '돌 몸체 색'으로 채운다. pad_down(마지막 행=그림자 반복)이 검은 띠를
    만드는 문제를 피해, 각 열의 하위 70% 지점 픽셀(돌 몸체)을 아래로 늘린다."""
    w, h = core.size
    out = Image.new("RGBA", (w, target_h), (0, 0, 0, 0))
    out.alpha_composite(core, (0, 0))
    px = out.load(); cpx = core.load()
    for x in range(w):
        ys = [y for y in range(h) if cpx[x, y][3] > 128]
        if not ys:
            continue
        bot = max(ys)
        sample = cpx[x, ys[0] + int((bot - ys[0]) * 0.7)]  # 돌 몸체 샘플
        if sample[3] <= 128:
            sample = cpx[x, bot]
        for y in range(bot + 1, target_h):
            px[x, y] = sample
    return out


def rock(src_name, dst_name):
    """생성된 바위에서 256x128 단차 블록 파생. bbox 크롭 후 256x128로 통째 리사이즈 —
    불규칙 바위라 균일 스트레치는 무해하고, pad로 인한 세로 결/검은 띠가 생기지 않는다.
    상면(이끼)=top, 몸체=bottom 까지 꽉 참."""
    im = L(src_name)
    core = im.crop(im.split()[3].getbbox()).resize((256, 128), Image.NEAREST)
    save(core, dst_name)
    return core


def rocks():
    # a=rock_b(각진 이끼 바위), b=rock_f(조약돌 담) — 둘 다 평평 측면도, 흙과 구분되는 회색 화강암.
    #   생성 리롤 d/e/f/g/h/i 는 원근·하늘배경·나무 등으로 이 세계의 측면도 룩과 어긋나 탈락.
    a = rock("rock_step_b_256x128", "step_mound_a")
    b = rock("rock_step_f_256x128", "step_mound_b")
    GROUND = (150, 140, 110, 255)
    outs = [a, b]
    sim = Image.new("RGBA", (256 * 2 + 16, 128 + 40), (245, 244, 240, 255))
    for x in range(sim.width):
        for y in range(128 + 8, sim.height):
            sim.putpixel((x, y), GROUND)
    for i, im in enumerate(outs):
        sim.alpha_composite(im, (i * (256 + 16), 8))
    save(sim, "_sim_rocks")


# ── 3) 발판 ──────────────────────────────────────────────────────────
# platform_mid_v2 는 '뭉툭한 섬'(가운데 불룩+양끝 얇음)이라 태생적 tileable이 아니다.
#   밑면을 공통 깊이로 pad 해 '평평한 흙 슬래브'(풀 상면 유지)로 만들면 완벽히 이어진다.
#   슬래브 두께 THK(px). cap_L 은 슬래브 좌측 96px에 둥근 코를 깎아 만든다.
THK = 34   # 슬래브 두께 0.53u (풀 ~8 + 흙 ~26)


def _slab(midc):
    """크롭 mid → 표면 top부터 THK 두께의 꽉 찬 흙 슬래브 (모든 열을 THK까지 채워 평평한 밑면)."""
    px = midc.load(); W, H = midc.size
    top = min(next((y for y in range(H) if px[x, y][3] > 128), H) for x in range(W))
    out = Image.new("RGBA", (W, THK), (0, 0, 0, 0))
    op = out.load()
    for x in range(W):
        last = None
        for y in range(THK):
            sy = top + y
            c = midc.getpixel((x, sy)) if sy < H else (0, 0, 0, 0)
            if c[3] > 128:
                op[x, y] = c; last = c
            elif last is not None:
                op[x, y] = last          # 밑면을 마지막 흙색으로 연장
    return out


def _round_nose(cap):
    """좌측 하단 모서리를 둥글게 깎아 '끝'처럼. 왼쪽 8열의 하단을 계단식으로 올림."""
    px = cap.load(); W, H = cap.size
    cut = [6, 4, 3, 2, 1, 1, 0, 0]  # 좌→우 각 열에서 아래에서 지울 픽셀 수
    for i, c in enumerate(cut):
        for y in range(H - c, H):
            px[i, y] = (0, 0, 0, 0)
    return cap


def platform():
    mid = L("platform_mid_v2_192x96")
    l, r = alpha_bbox_x(mid)
    midc = mid.crop((l, 0, r + 1, 96))
    slab = _slab(midc)
    save(slab, "platform_earth_mid")
    cap = _round_nose(slab.crop((0, 0, min(96, slab.width), THK)).copy())
    save(cap, "platform_earth_cap_L")
    W = slab.width
    sim = Image.new("RGBA", (96 + W * 2 + 96 + 8, THK + 16), (245, 244, 240, 255))
    x, y = 0, 4
    sim.alpha_composite(cap, (x, y)); x += cap.width
    sim.alpha_composite(slab, (x, y)); x += W
    sim.alpha_composite(slab, (x, y)); x += W
    sim.alpha_composite(cap.transpose(Image.FLIP_LEFT_RIGHT), (x, y))
    save(sim.resize((sim.width * 3, sim.height * 3), Image.NEAREST), "_sim_platform")
    print(f"  [보고] 슬래브 mid 폭 = {W}px = {W/64:.3f}u  (PlatformMidW 갱신 필요), 두께 {THK}px")


if __name__ == "__main__":
    print("ladder:"); ladder()
    print("rocks:"); rocks()
    print("platform:"); platform()
    print("done (0 generations)")
