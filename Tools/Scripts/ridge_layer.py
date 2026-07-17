"""생성된 능선 원본 -> 게임에 쓸 심리스 패럴랙스 층으로 후처리.

파이프라인: silhouette(실루엣 추출+단색화) -> mirror_bake(심리스)

★ key_corner를 쓰지 않는 이유 (실측):
  key_corner의 플러드는 '이웃 픽셀과의 색차'로 번진다(그라디언트 워크). 능선 생성물은
  하늘(크림) -> 연한 세이지 -> 진한 덩어리로 값이 완만히 이어져서, tol=40이면 플러드가
  덩어리를 뚫고 들어가 전부 먹었다 (far 400열 중 14열만 생존, mid 85열). tol을 낮춰도
  하늘의 미세 노이즈 때문에 불안정하다.
  => 능선 생성물은 사실상 2톤(하늘 + 덩어리)이므로 플러드가 필요 없다.
     열마다 위에서 내려오며 '하늘색이 아닌 첫 픽셀' = 실루엣 상단. 이 방식은 원리상 덩어리를
     절대 침범하지 않는다.

왜 단색으로 눕히는가:
  원경 패럴랙스 층은 '한 장의 평면 실루엣'이어야 층끼리 깊이가 읽힌다. 생성기가 층 안에
  제 나름의 음영·밴드를 넣으므로, 이를 bg_ridge 팔레트의 밴드색 하나로 눕혀
  기존 승인 능선과 색을 정확히 일치시킨다.
  (팔레트 락 미사용: 락은 지형 전용이고 여기선 목표색이 정확히 1개라 직접 칠하는 게 확실하다.)

왜 실루엣부터 하단까지 꽉 채우는가:
  패럴랙스로 층이 세로로 어긋나는 순간 층 사이로 하늘이 새는 걸 막는다.

왜 mirror_bake 하는가:
  ridge_mirror.py 참고. 생성기가 좌우 flush를 안 지키는 함정을 무력화한다.
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from ridge_mirror import mirror_bake, seam_error

# bg_ridge.png 실측 팔레트 — 기존 승인 능선과 정확히 같은 색을 쓴다
BAND = {
    "far":  (207, 225, 236),   # #cfe1ec  lum 222
    "mid":  (158, 191, 216),   # #9ebfd8  lum 186
    "near": ( 42,  68, 108),   # #2a446c  lum  65
}

# 층별 목표 실루엣 높이(중앙값 y). 원본 bg_ridge의 성층(far 84 / mid 147 / near 189)을 따른다.
# 생성기는 세 층을 다 비슷한 높이(131~144)로 뽑기 때문에, 그대로 겹치면 깊이가 안 읽힌다.
# 각 층을 목표 높이로 세로 이동시켜 성층을 만든다 (아래는 어차피 하단까지 채우므로 손실 없음).
TARGET_TOP = {"far": 100, "mid": 150, "near": 195}
RUN = 3   # 하늘 노이즈/점 1~2px를 실루엣으로 오인하지 않도록 연속 조건


def silhouette(im, rgb, tol=45):
    """하늘색(좌상단 기준)이 아닌 첫 픽셀부터 하단까지 단색으로 채운 실루엣."""
    w, h = im.size
    p = im.load()
    bg = p[0, 0][:3]
    def is_bg(c):
        return c[3] < 128 or all(abs(a - b) <= tol for a, b in zip(c[:3], bg))

    tops = []
    for x in range(w):
        top = h
        for y in range(h - RUN):
            if all(not is_bg(p[x, y + k]) for k in range(RUN)):
                top = y
                break
        tops.append(top)
    return tops


def render(tops, size, rgb, delta=0):
    """실루엣 상단선을 delta만큼 세로 이동해 하단까지 채운 층 이미지를 만든다."""
    w, h = size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    op = out.load()
    for x in range(w):
        if tops[x] >= h:
            continue
        for y in range(max(0, tops[x] + delta), h):
            op[x, y] = rgb + (255,)
    return out


def process(raw, band, out_png, tol=45):
    im = Image.open(raw).convert("RGBA")
    w, h = im.size
    tops = silhouette(im, BAND[band], tol)
    valid = sorted(t for t in tops if t < h)
    med = valid[len(valid) // 2]
    delta = TARGET_TOP[band] - med          # 목표 성층 높이로 이동
    sil = render(tops, (w, h), BAND[band], delta)
    tile = mirror_bake(sil)
    tile.save(out_png)
    m, b, _ = seam_error(tile)
    print("%-5s %-18s -> %s | cov %3d/%d | top median %3d -> %3d (shift %+d) | seam %.1f (>20 rows %d)"
          % (band, os.path.basename(raw), tile.size, len(valid), w,
             med, med + delta, delta, m, b))
    return sil


if __name__ == "__main__":
    process(sys.argv[1], sys.argv[2], sys.argv[3])
