# 책보(冊褓) 실측: 위치·색·크기를 프레임 간 대조한다.
#
# ★ 지난 실수 교훈: brownPx(양)만 재면 '손에 든 갈색'과 '허리의 갈색 가방'을 구분 못한다.
#   위치(중심 x/y)와 크기(bbox)를 함께 재야 정체가 흔들리는지 잡힌다.
#
# 책보 = 갈색 (도포 청록과 대비). 갈색 판정: r>g, g>=b, 중간 명도.
import os, sys, glob
from PIL import Image

CELL = 136


def is_brown(p):
    r, g, b, a = p
    return a > 128 and r > g + 14 and g >= b - 4 and 55 < r < 200 and r - b > 22


def bundle_stats(im):
    """책보 픽셀의 개수 / 중심(x,y) / bbox(w,h). 없으면 count=0."""
    px = im.load()
    xs, ys = [], []
    for y in range(CELL):
        for x in range(CELL):
            if is_brown(px[x, y]):
                xs.append(x)
                ys.append(y)
    if not xs:
        return dict(n=0, cx=None, cy=None, w=0, h=0, x0=None, y0=None)
    return dict(n=len(xs), cx=round(sum(xs) / len(xs), 1), cy=round(sum(ys) / len(ys), 1),
                w=max(xs) - min(xs) + 1, h=max(ys) - min(ys) + 1, x0=min(xs), y0=min(ys))


def frames(src_dir, anim, direction):
    d = os.path.join(src_dir, "animations", anim, direction)
    return [Image.open(f).convert("RGBA") for f in sorted(glob.glob(os.path.join(d, "*.png")))]


if __name__ == "__main__":
    src = sys.argv[1]  # ...\Seonbi_Chaekbo
    rows = [("idle", "east"), ("walk", "east"), ("run", "east"),
            ("jump", "east"), ("climb", "north")]
    print(f"{'anim':6} {'f':>2} {'brownN':>6} {'cx':>6} {'cy':>6} {'w':>3} {'h':>3}")
    agg = {}
    for anim, dr in rows:
        cxs, cys, ns, ws, hs = [], [], [], [], []
        for i, im in enumerate(frames(src, anim, dr)):
            s = bundle_stats(im)
            print(f"{anim:6} {i:2} {s['n']:6} {str(s['cx']):>6} {str(s['cy']):>6} {s['w']:3} {s['h']:3}")
            if s["n"]:
                cxs.append(s["cx"]); cys.append(s["cy"]); ns.append(s["n"]); ws.append(s["w"]); hs.append(s["h"])
        agg[anim] = (cxs, cys, ns, ws, hs)
    print(f"\n{'anim':6} {'존재':>4} {'cy범위(허리높이)':>16} {'크기h범위':>10} {'brownN범위':>12}")
    for anim, (cxs, cys, ns, ws, hs) in agg.items():
        exist = f"{len(cys)}/{len(frames(src, anim, dict(rows)[anim]))}"
        cyr = f"{min(cys):.0f}~{max(cys):.0f}" if cys else "없음"
        hr = f"{min(hs)}~{max(hs)}" if hs else "-"
        nr = f"{min(ns)}~{max(ns)}" if ns else "-"
        print(f"{anim:6} {exist:>4} {cyr:>16} {hr:>10} {nr:>12}")
    allcy = [v for _, cys, *_ in [(a,) + agg[a] for a in agg] for v in cys]
    print(f"\ncy(허리높이) 전체 {min(allcy):.0f}~{max(allcy):.0f} (편차 {max(allcy)-min(allcy):.0f})")
