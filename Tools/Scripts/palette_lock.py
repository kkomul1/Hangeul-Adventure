# 승인 룩(lookcheck_b2)의 팔레트를 기준으로 신규 생성물의 색을 강제 정렬한다. 생성 0회.
# 근거: create_map_object에 seed가 없어 팔레트가 호출마다 흔들린다 -> 프롬프트로 못 잡는다.
#       프로젝트 선례: manifest.md "물 색 보정 = 0회 (로컬 팔레트 치환)".
# usage: palette_lock.py <ref.png[,ref2.png...]> <in.png> <out.png> [ncolors]
import sys
from PIL import Image


def srgb_to_lab(c):
    r, g, b = [v / 255.0 for v in c]

    def f(u):
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4
    r, g, b = f(r), f(g), f(b)
    x = r * 0.4124 + g * 0.3576 + b * 0.1805
    y = r * 0.2126 + g * 0.7152 + b * 0.0722
    z = r * 0.0193 + g * 0.1192 + b * 0.9505
    xn, yn, zn = 0.95047, 1.0, 1.08883

    def g_(t):
        return t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    fx, fy, fz = g_(x / xn), g_(y / yn), g_(z / zn)
    return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz))


def ref_palette(paths, n):
    """참조 이미지들의 불투명 픽셀을 모아 median-cut으로 대표색 n개 추출."""
    px = []
    for p in paths:
        im = Image.open(p).convert("RGBA")
        w, h = im.size
        a = im.load()
        for y in range(h):
            for x in range(w):
                c = a[x, y]
                if c[3] > 128:
                    px.append(c[:3])
    tmp = Image.new("RGB", (len(px), 1))
    tmp.putdata(px)
    q = tmp.quantize(colors=n, method=Image.MEDIANCUT)
    pal = q.getpalette()[: n * 3]
    return [tuple(pal[i * 3:i * 3 + 3]) for i in range(n)]


def lock(src, dst, pal):
    lab_pal = [srgb_to_lab(c) for c in pal]
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    a = im.load()
    cache = {}
    changed = 0
    for y in range(h):
        for x in range(w):
            c = a[x, y]
            if c[3] < 128:
                continue
            key = c[:3]
            if key not in cache:
                l0 = srgb_to_lab(key)
                best, bd = pal[0], 1e18
                for pc, pl in zip(pal, lab_pal):
                    # 명도차에 가중 -> 셰이딩 단계 보존, 색상만 승인 팔레트로 끌어옴
                    d = 2.2 * (l0[0] - pl[0]) ** 2 + (l0[1] - pl[1]) ** 2 + (l0[2] - pl[2]) ** 2
                    if d < bd:
                        bd, best = d, pc
                cache[key] = best
            nc = cache[key]
            if nc != key:
                changed += 1
            a[x, y] = nc + (c[3],)
    im.save(dst)
    print(f"palette-locked {changed} px -> {dst}")


if __name__ == "__main__":
    refs = sys.argv[1].split(",")
    n = int(sys.argv[4]) if len(sys.argv) > 4 else 28
    pal = ref_palette(refs, n)
    lock(sys.argv[2], sys.argv[3], pal)
