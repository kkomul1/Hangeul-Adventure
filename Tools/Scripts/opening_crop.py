# -*- coding: utf-8 -*-
"""오프닝 컷 크롭 — 나노바나나 출력의 하단 검은 띠를 실측해 제거하고 16:9로 맞춘다.

나노바나나(Gemini)가 16:9를 요청해도 캔버스 하단에 검은 띠를 남기는 경우가 있다.
띠 높이를 하드코딩하지 않고 행 밝기로 실측해 자른 뒤, 16:9가 되도록 위아래를 균형 있게 다듬는다.
"""
import os
import sys

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

SRC_DIR = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\opening"
DARK = 18          # 이 밝기 이하 = 검은 띠로 본다
DARK_RATIO = 0.97  # 행의 이 비율 이상이 어두우면 띠 행


def row_is_black(px, w, y):
    dark = sum(1 for x in range(0, w, 4) if sum(px[x, y][:3]) / 3 <= DARK)
    return dark / len(range(0, w, 4)) >= DARK_RATIO


def crop_one(path):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    px = im.load()

    top = 0
    while top < h and row_is_black(px, w, top):
        top += 1
    bottom = h - 1
    while bottom > top and row_is_black(px, w, bottom):
        bottom -= 1
    content_h = bottom - top + 1
    print(f"  검은 띠: 위 {top}px / 아래 {h - 1 - bottom}px  → 실내용 {w}x{content_h}")

    # 16:9로 맞춘다. 내용이 16:9보다 높으면 위아래를 균등하게 깎는다.
    target_h = round(w * 9 / 16)
    if content_h > target_h:
        extra = content_h - target_h
        t = top + extra // 2
        b = t + target_h
        print(f"  16:9 조정: 세로 {content_h} → {target_h} (위 {extra//2}, 아래 {extra - extra//2} 추가 크롭)")
    else:
        t, b = top, bottom + 1
        if content_h < target_h:
            print(f"  ※ 내용이 16:9보다 낮다({content_h} < {target_h}) — 자르지 않고 그대로 둔다")

    out = im.crop((0, t, w, b))
    stem = os.path.splitext(path)[0]
    dst = stem + "_16x9.png"
    out.save(dst)
    print(f"  저장: {os.path.basename(dst)}  {out.size[0]}x{out.size[1]}  (비율 {out.size[0]/out.size[1]:.4f})")
    return dst


def main():
    files = [f for f in os.listdir(SRC_DIR)
             if f.lower().endswith((".png", ".jpg", ".jpeg")) and "_16x9" not in f]
    if not files:
        print("크롭할 이미지가 없다:", SRC_DIR)
        return
    for f in sorted(files):
        print(f)
        crop_one(os.path.join(SRC_DIR, f))


if __name__ == "__main__":
    main()
