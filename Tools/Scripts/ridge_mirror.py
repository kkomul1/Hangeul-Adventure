"""능선 심리스 미러 베이크.

문제: bg_ridge.png는 심리스 타일이 아니다. 좌우 끝 실루엣·색이 안 맞아
      (실측: 좌우 열 평균 채널차 12.7, 360행 중 96행이 20 초과)
      SideWorld.Build.cs가 가로로 반복하면 경계에서 뚝 끊긴다.

해결: T = [A | mirror(A)[1:-1]]  (폭 2w-2)
      - 내부 이음: A[w-1] 다음 mirror(A)[1] = A[w-2]  -> 연속
      - 반복 이음: T[last] = A[1] 다음 T[0] = A[0]    -> 연속
      => 중복 열 없이 이음매 오차 0. 원본 픽셀을 하나도 손상시키지 않는다.

부수 효과: 생성기가 캔버스 좌우 flush를 안 지키는 실측 함정이 이 방식에선 무의미해진다.
          어떤 폭의 능선이든 미러 베이크만 하면 무조건 심리스가 된다.

주의: 타일 폭이 2배가 되지만 SideWorld.Build.cs가
      n = CeilToInt((_map.width + 40f) / tileW) 로 개수를 계산하므로 코드 변경 불필요.
"""
import sys
from PIL import Image


def mirror_bake(im):
    w, h = im.size
    mir = im.transpose(Image.FLIP_LEFT_RIGHT).crop((1, 0, w - 1, h))
    t = Image.new("RGBA", (w + mir.size[0], h))
    t.paste(im, (0, 0))
    t.paste(mir, (w, 0))
    return t


def seam_error(t):
    """반복 이음매 오차: 마지막 열 vs 첫 열."""
    p = t.load(); w, h = t.size
    diffs = [max(abs(p[w - 1, y][i] - p[0, y][i]) for i in range(4)) for y in range(h)]
    return sum(diffs) / h, sum(1 for d in diffs if d > 20), h


if __name__ == "__main__":
    src, dst = sys.argv[1], sys.argv[2]
    a = Image.open(src).convert("RGBA")
    m0, b0, h = seam_error(a)
    t = mirror_bake(a)
    m1, b1, _ = seam_error(t)
    t.save(dst)
    print("%s %s -> %s %s" % (src, a.size, dst, t.size))
    print("  이음매 오차: 평균 %.1f -> %.1f | 20 초과 행 %d/%d -> %d/%d" % (m0, m1, b0, h, b1, h))
