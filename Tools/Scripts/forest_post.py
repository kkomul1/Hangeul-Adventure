# 시작의 숲 후처리 일괄: (필요시)키잉 -> (지형만)팔레트 락 -> 실측 -> chunk_manifest.json
#
# ★ 실측으로 확정된 규칙 (어기면 에셋이 망가진다)
#  1) 베이크 여부는 캔버스 크기로 예측 불가 (192x256 나무=베이크, 96x256 덤불=투명).
#     -> 코너 alpha로 자동 판정한다.
#  2) 팔레트 락은 "지형 청크"에만 건다. 인페인팅 프롭·나무는 이미 톤이 맞아 있고,
#     락을 걸면 한지 종이(크림)->풀색, 솔잎->돌 회색으로 파괴된다 (실측).
#  3) 락의 참조 팔레트는 지형 청크만으로 뽑으면 안 된다 (나무색이 없어 목재가 초록이 된다).
#     -> 승인 룩 b2 전체(지형+목재+석물+능선)에서 뽑는다.
import json, os
from collections import deque
from PIL import Image
from palette_lock import ref_palette, lock

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
B2 = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b2"

# 승인 룩 전체 = 이 세계의 팔레트 (지형/목재/석물/원경)
PALETTE_REFS = [os.path.join(B2, f) for f in [
    "terrain_flat_b_320x160_keyed.png", "ladder_wood_48x128.png",
    "prop_jangseung_b2_64x130.png", "prop_seokdeung_b2_48x78.png",
    "bg_ridge_200x120_v2_final.png",
]]

# 팔레트 락 대상 = basic 모드 지형 (호출마다 팔레트가 흔들림)
LOCK = ["ground_flat_02_320x160", "ground_flat_03_320x160", "ground_flat_04_320x160",
        "step_mound_a_256x128", "step_mound_b_256x128",
        "platform_earth_mid_192x96", "platform_earth_cap_L_96x96",
        "ladder_body_seg_96x128", "ladder_top_cap_96x64", "boundary_bush_96x256"]
# 락 금지 = 인페인팅으로 이미 톤 매칭된 프롭 + 원본이 좋은 배경 나무
NO_LOCK = ["tree_pine_large_320x384", "tree_pine_small_192x256",
           "prop_mossy_boulder", "prop_tree_stump", "prop_bush_cluster", "prop_fern_tuft",
           "prop_fallen_log", "spot_sign_hanji", "exit_gate_jangseung"]


def has_baked_bg(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    a = im.load()
    corners = [a[0, 0], a[w - 1, 0], a[0, h - 1], a[w - 1, h - 1]]
    return sum(1 for c in corners if c[3] > 128) >= 3


def key_corner(src, dst, tol=30):
    """배경 키잉. ★실측 버그: 구 key_bg.py는 4개 코너 전부를 시드로 썼는데,
    가장자리까지 꽉 찬 지형 청크는 아래쪽 코너가 '흙 그 자체'라 몸체를 통째로 먹었다
    (ground_flat_03 -> 픽셀 1.4%만 생존). 배경색은 항상 상단에 있으므로 상단 행을 시드로 쓰고,
    다른 변은 '상단 배경색과 같은 색일 때만' 시드로 추가한다."""
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    px = im.load()
    bg = px[0, 0][:3]

    def near(c):
        return all(abs(p - r) <= tol for p, r in zip(c[:3], bg))

    seen = [[False] * w for _ in range(h)]
    q = deque()
    seeds = [(x, 0) for x in range(w)]                       # 상단 행 = 배경 확정
    seeds += [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]  # 좌우 변
    seeds += [(x, h - 1) for x in range(w)]                  # 하단 변
    for (sx, sy) in seeds:
        c = px[sx, sy]
        if c[3] > 0 and near(c) and not seen[sy][sx]:
            seen[sy][sx] = True
            q.append((sx, sy))
    while q:
        x, y = q.popleft()
        c = px[x, y]
        px[x, y] = (0, 0, 0, 0)
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx]:
                nc = px[nx, ny]
                if nc[3] > 0 and all(abs(p - r) <= tol for p, r in zip(nc[:3], c[:3])):
                    seen[ny][nx] = True
                    q.append((nx, ny))
    # ★ 플러드필은 닫힌 영역에 못 들어간다 (나무 캐노피 안쪽에 크림색 후광 잔존 - 실측).
    #   남은 픽셀 중 배경색과 동일한 것을 전역 제거한다. 오브젝트 고유색과 배경색이
    #   충분히 다를 때만 안전하므로 tol을 좁게 쓴다.
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            if c[3] > 0 and all(abs(p_ - r) <= tol - 8 for p_, r in zip(c[:3], bg)):
                px[x, y] = (0, 0, 0, 0)
    im.save(dst)


def profile(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    a = im.load()
    bbox = im.split()[3].getbbox()
    if bbox is None:
        return dict(canvas=[w, h], content_bbox=None, EMPTY=True)
    x0, y0, x1, y1 = bbox
    tops = []
    for x in range(x0, x1):
        col = [y for y in range(h) if a[x, y][3] > 128]
        tops.append(col[0] if col else None)
    solid = [t for t in tops if t is not None]
    mid = sorted(solid)[len(solid) // 2] if solid else None
    return dict(canvas=[w, h], content_bbox=list(bbox), content_w=x1 - x0, content_h=y1 - y0,
                fill_ratio=round(sum(1 for y in range(h) for x in range(w) if a[x, y][3] > 128) / (w * h), 3),
                surface_left=tops[0], surface_right=tops[-1], surface_median=mid,
                edge_delta=(abs(tops[0] - tops[-1]) if tops[0] is not None and tops[-1] is not None else None))


if __name__ == "__main__":
    pal = ref_palette(PALETTE_REFS, 40)
    print(f"reference palette: {len(pal)} colors from {len(PALETTE_REFS)} approved assets")
    man = {}
    for name in LOCK + NO_LOCK:
        src = os.path.join(OUT, name + ".png")
        if not os.path.exists(src):
            print("MISSING", name)
            continue
        cur = src
        baked = has_baked_bg(src)
        if baked:
            cur = os.path.join(OUT, name + "_keyed.png")
            key_corner(src, cur)
        final = os.path.join(OUT, name + "_final.png")
        if name in LOCK:
            lock(cur, final, pal)
        else:
            Image.open(cur).convert("RGBA").save(final)
        man[name] = dict(baked_bg=baked, palette_locked=name in LOCK, **profile(final))
        m = man[name]
        print(f"  {name}: baked={baked} lock={name in LOCK} fill={m.get('fill_ratio')} "
              f"bbox={m.get('content_bbox')} surfMed={m.get('surface_median')}")
    json.dump(man, open(os.path.join(OUT, "chunk_manifest.json"), "w"), indent=1)
    print("-> chunk_manifest.json")
