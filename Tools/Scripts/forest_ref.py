# ref_style_192.png 재작성. 생성 0회.
# ★ 실측 버그: 지면선을 캔버스 85% 지점에 두었더니 인페인팅 중앙 마스크가 통째로 하늘에 걸려
#   수풀·고사리·통나무가 "내 하늘 그라데이션"만 그대로 뱉었다. 지면선을 마스크 중심(62%)으로 올린다.
import os
from PIL import Image, ImageDraw

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
B2 = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b2"
W = H = 192
GROUND_Y = 120  # 지면선 = 캔버스 62% -> rect/oval 마스크(중앙 ~53..139)가 지면을 가로지른다

stops = [(0.00, (116, 120, 146)), (0.45, (163, 164, 184)), (0.72, (206, 192, 182)), (1.00, (228, 209, 184))]
sky = Image.new("RGBA", (W, H))
px = sky.load()
for y in range(H):
    t = y / (H - 1)
    for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0)
            col = tuple(round(a + (b - a) * f) for a, b in zip(c0, c1))
            break
    for x in range(W):
        px[x, y] = col + (255,)

ter = Image.open(os.path.join(B2, "terrain_flat_b_320x160_keyed.png")).convert("RGBA")
# 승인 청크의 표면선은 y~=60. 그 선이 GROUND_Y에 오도록 배치.
crop = ter.crop((64, 0, 256, 160))
sky.alpha_composite(crop, (0, GROUND_Y - 60))
tpx = crop.load()
dirt = tpx[96, 120][:3] + (255,)
d = ImageDraw.Draw(sky)
if GROUND_Y + 96 < H:
    d.rectangle([0, GROUND_Y + 96, W, H], fill=dirt)  # 청크가 하단에 못 닿을 때만 흙 백필

sky.convert("RGB").save(os.path.join(OUT, "ref_style_192.png"))
print(f"ref_style_192.png 재작성: 지면선 y={GROUND_Y} (마스크가 지면을 가로지름)")
