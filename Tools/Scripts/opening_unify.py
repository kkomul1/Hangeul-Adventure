# -*- coding: utf-8 -*-
"""오프닝 4컷의 화면비를 통일한다.

실측: 컷1은 나노바나나가 하단에 검은 레터박스 69px을 그려 넣어, 제거하면 1024x503(2.04:1).
      컷2~4는 레터박스 없이 1024x572(1.79:1)로 나왔다.
4컷이 비율이 다르면 컷신 재생 중 화면이 흔들린다.

통일 방향: 가장 작은 높이(503)에 맞추되 **위에서 잘라낸다**.
  - 아래를 자르면 컷3·4의 주인공 발이 잘린다(컷4는 발이 y=540 근처).
  - 위는 천장(컷2·4)·하늘(컷3)이라 손실이 적다.
  - 프롬프트가 "핵심 액션을 상단 2/3에"를 요구했으므로 얼굴·액션은 안전 범위 안에 있다.
    잘라낸 뒤 반드시 눈으로 확인할 것.
"""
import os
import sys

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

SRC = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\opening"
TARGET_H = 503   # 컷1 실내용 높이 (2.04:1)

FILES = [
    "opening_01_chaos_16x9.png",
    "opening_02_16x9.png",
    "opening_03_16x9.png",
    "opening_04_16x9.png",
]


def main():
    for i, name in enumerate(FILES, 1):
        path = os.path.join(SRC, name)
        im = Image.open(path).convert("RGB")
        w, h = im.size
        if h == TARGET_H:
            print(f"컷{i} {name:26s} {w}x{h} — 이미 기준 높이")
            continue
        cut = h - TARGET_H
        out = im.crop((0, cut, w, h))   # 위에서 cut px 제거
        dst = os.path.join(SRC, f"opening_{i:02d}_final.png")
        out.save(dst)
        print(f"컷{i} {name:26s} {w}x{h} → 위 {cut}px 제거 → {out.size[0]}x{out.size[1]}")

    # 컷1도 final 이름으로 통일
    src1 = os.path.join(SRC, FILES[0])
    dst1 = os.path.join(SRC, "opening_01_final.png")
    Image.open(src1).convert("RGB").save(dst1)

    print("\n=== 최종 확인 ===")
    for i in range(1, 5):
        p = os.path.join(SRC, f"opening_{i:02d}_final.png")
        im = Image.open(p)
        print(f"컷{i}  {im.size[0]}x{im.size[1]}  비율 {im.size[0]/im.size[1]:.4f}")


if __name__ == "__main__":
    main()
