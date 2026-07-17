# -*- coding: utf-8 -*-
"""지상 청크를 좌우 flush로 보정한다.

PixelLab은 캔버스 좌우 끝까지 채우지 않아 청크가 "둥근 섬"으로 나온다(실측: 02는 80열,
04는 40열이 빔. 03만 정상). 겹침 배치만으로는 이 빈 구간을 다 못 가려 화면에 세로 틈이 보인다.

주의: 청크 좌우 끝은 아웃라인 색이다. 그대로 복제하면 검은 세로줄이 생기므로,
아웃라인을 건너뛴 안쪽 흙 색을 캔버스 가장자리까지 복제한다.

원본은 ArtDrop/Generated/forest_main/preflush/ 에 보존한다.
"""
import os
import shutil
import sys

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

TERRAIN = r"C:\Users\minjae\UnityProjects\HangeulAdventure\Assets\Resources\Art\Forest\Terrain"
BACKUP = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main\preflush"
ALPHA = 200
OUTLINE_SCAN = 6  # 아웃라인 두께 상한 (px)


def inner_color(px, y, start, end, step):
    """start에서 step 방향으로 스캔해 아웃라인 다음의 본체 색을 찾는다."""
    first = px[start, y][:3]
    x = start + step
    for _ in range(OUTLINE_SCAN):
        if (step > 0 and x > end) or (step < 0 and x < end):
            break
        c = px[x, y]
        if c[3] > ALPHA and c[:3] != first:
            return c
        x += step
    return px[start, y]


def flush_row_extend(im):
    w, h = im.size
    px = im.load()
    changed = 0
    for y in range(h):
        xs = [x for x in range(w) if px[x, y][3] > ALPHA]
        if not xs:
            continue
        lo, hi = min(xs), max(xs)
        left = inner_color(px, y, lo, hi, +1) if lo > 0 else px[lo, y]
        right = inner_color(px, y, hi, lo, -1) if hi < w - 1 else px[hi, y]
        for x in range(0, lo):
            px[x, y] = left
            changed += 1
        for x in range(hi + 1, w):
            px[x, y] = right
            changed += 1
    return changed


def main():
    os.makedirs(BACKUP, exist_ok=True)
    for name in ("ground_flat_02.png", "ground_flat_03.png", "ground_flat_04.png"):
        path = os.path.join(TERRAIN, name)
        backup = os.path.join(BACKUP, name.replace(".png", "_preflush.png"))

        # 이전 실행분이 있으면 원본에서 다시 시작
        if os.path.exists(backup):
            shutil.copy2(backup, path)

        im = Image.open(path).convert("RGBA")
        w, h = im.size
        before = sum(1 for x in range(w) if im.getpixel((x, h - 1))[3] <= ALPHA)
        if before == 0:
            print(f"{name:20s} 이미 flush (빈 열 0) — 건너뜀")
            continue

        if not os.path.exists(backup):
            shutil.copy2(path, backup)
        n = flush_row_extend(im)
        im.save(path)
        print(f"{name:20s} 빈 열 {before} -> 0  ({n}px 확장)")

    print("\n=== 검증 (하단행 좌우 끝 색이 흙이어야 한다) ===")
    for name in ("ground_flat_02.png", "ground_flat_03.png", "ground_flat_04.png"):
        im = Image.open(os.path.join(TERRAIN, name)).convert("RGBA")
        w, h = im.size
        empty = sum(1 for x in range(w) if im.getpixel((x, h - 1))[3] <= ALPHA)
        print(f"{name:20s} bbox={im.getbbox()} 빈열={empty} "
              f"하단좌={im.getpixel((0, h - 1))[:3]} 하단우={im.getpixel((w - 1, h - 1))[:3]}")


if __name__ == "__main__":
    main()
