# -*- coding: utf-8 -*-
"""지상 청크 보정 — preflush 백업에서 원본 복원.

★결론: 청크 02·04는 타일로 쓸 수 없다. 매니페스트 실측이 이유를 말해준다.

    청크   좌측표면선  우측표면선  중앙(median)  편차
    03        62         63          63         1px  ← 평탄. 진짜 타일
    02        89         86          69        20px  ← 가장자리가 푹 꺼진 "둥근 섬"
    04        94         93          83        11px  ← 같은 문제

피벗이 중앙 표면선(surface_median)이라 청크를 y=0.5에 놓으면 중앙은 맞지만 가장자리가
20px 내려앉는다. 청크를 1.72u 겹쳐 깔기 때문에 겹침 경계마다 이 낙차가 계단으로 드러난다.
= 화면에서 보이던 "틈"의 정체.

시도했다가 폐기한 것 (반복 금지):
- 행 단위 edge extend로 좌우를 캔버스 끝까지 늘리기 → 각 행의 색이 가로로 늘어나
  **단색 가로 줄무늬**가 생기고, 겹침 구간에서 인접 청크 위에 그려져 더 나빠졌다.
- 하단 깊이 통일 → 하단은 맞았지만 표면선 낙차는 그대로라 계단이 남았다.

채택: SideWorld의 GroundChunks를 03 하나로 좁힌다. 반복감은 데코(바위·그루터기·나무)와
좌우 미러로 완화한다. 02·04는 에셋으로 남겨두되 지면 타일로는 쓰지 않는다
(표면선이 평탄한 청크를 새로 뽑으면 그때 합류시킬 것).
"""
import os
import shutil
import sys

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

TERRAIN = r"C:\Users\minjae\UnityProjects\HangeulAdventure\Assets\Resources\Art\Forest\Terrain"
BACKUP = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main\preflush"
CHUNKS = ("ground_flat_02.png", "ground_flat_03.png", "ground_flat_04.png")


def main():
    for name in CHUNKS:
        path = os.path.join(TERRAIN, name)
        backup = os.path.join(BACKUP, name.replace(".png", "_preflush.png"))
        if not os.path.exists(backup):
            print(f"{name:20s} 백업 없음 — 원본 그대로")
            continue
        shutil.copy2(backup, path)
        im = Image.open(path).convert("RGBA")
        print(f"{name:20s} 원본 복원 (bbox={im.getbbox()})")

    print("\n지면 타일은 SideWorld.Build.cs의 GroundChunks에서 03만 쓴다.")


if __name__ == "__main__":
    main()
