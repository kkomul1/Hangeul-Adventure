# ArtDrop/Generated/forest_main -> Assets/Resources/Art/Forest 임포트 준비.
#
# 하는 일 (Unity가 못 하는 것만 오프라인 처리):
#   1) 채택본 선별 복사 (_final / derived_ 선택은 forest_compose.py 승인 합성과 동일)
#   2) 프롭류 알파 bbox 크롭  -> Unity Single 스프라이트 + BottomCenter 피벗이 곧 승인 합성의 tight() 앵커가 된다
#   3) 캐릭터 프레임 -> 가로 스트립 시트 (셀 136px). 한복 jump는 프레임 0~5만 채택 (7~9번 갓 소실 — 검수 지시)
#   4) 원경 능선 x3 NEAREST 확대 (승인 합성과 동일 기법 — Unity에서 PPU 64로 1:1 사용)
#   5) 하늘 그라데이션 / 안개 띠 생성 (승인 합성의 sky_layer/fog 스톱을 그대로 굽는다)
#
# 재실행 안전 (덮어쓰기). 좌표·수치 근거는 forest_compose.py 참조.
import json, os, shutil, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ridge_mirror import mirror_bake  # 능선 심리스화 — ridge_x3() 주석 참조

ROOT = r"C:\Users\minjae\UnityProjects\HangeulAdventure"
SRC = os.path.join(ROOT, r"ArtDrop\Generated\forest_main")
B2 = os.path.join(ROOT, r"ArtDrop\Generated\lookcheck_b2")
DST = os.path.join(ROOT, r"Assets\Resources\Art\Forest")
PPU = 64
CELL = 136  # 캐릭터 캔버스

# (원본, 대상, 크롭 여부) — 크롭 = 승인 합성의 tight()
TERRAIN = [
    (SRC, "ground_flat_02_320x160_final", "Terrain/ground_flat_02", False),
    (SRC, "ground_flat_03_320x160_final", "Terrain/ground_flat_03", False),
    (SRC, "ground_flat_04_320x160_final", "Terrain/ground_flat_04", False),
    (SRC, "derived_step_mound_a_256x128_final", "Terrain/step_mound_a", False),
    (SRC, "derived_step_mound_b_256x128_final", "Terrain/step_mound_b", False),
    (SRC, "platform_earth_mid_192x96_final", "Terrain/platform_earth_mid", False),
    (SRC, "derived_platform_cap_L_96x96_final", "Terrain/platform_earth_cap_L", False),
    (SRC, "ladder_body_seg_96x128_final", "Terrain/ladder_body_seg", True),
    (SRC, "ladder_top_cap_96x64_final", "Terrain/ladder_top_cap", True),
    (SRC, "boundary_bush_96x256_final", "Terrain/boundary_bush", True),
]

PROPS = [
    (SRC, "prop_mossy_boulder_final", "Props/prop_mossy_boulder", True),
    (SRC, "prop_tree_stump_final", "Props/prop_tree_stump", True),
    (SRC, "prop_bush_cluster_final", "Props/prop_bush_cluster", True),
    (SRC, "prop_fern_tuft_final", "Props/prop_fern_tuft", True),
    (SRC, "prop_fallen_log_final", "Props/prop_fallen_log", True),
    (SRC, "spot_sign_hanji_final", "Props/spot_sign_hanji", True),
    (SRC, "exit_gate_jangseung_final", "Props/exit_gate_jangseung", True),
    (B2, "prop_jangseung_b2_64x130", "Props/prop_jangseung", True),
    (B2, "prop_seokdeung_b2_48x78", "Props/prop_seokdeung", True),
]

# ★ 배경 나무는 반드시 크롭할 것 (실측 — 사용자 지적 "나무가 공중에 떠 있다"의 원인).
#   과거엔 여기만 crop=False였다. 프롭은 전부 True인데 나무만 예외였던 게 버그다.
#   미크롭이면 캔버스 하단 = 밑동이 아니다: large는 밑동 아래 투명 여백 31px, small은 14px.
#   SideWorld.Build.cs는 BottomCenter 피벗을 TreeBaseY(= 표면선 8px 아래)에 놓으므로
#   large는 31-8 = 23px(0.36u), small은 14-8 = 6px 공중에 뜬다.
#   여백이 나무마다 달라서(31 vs 14) TreeBaseY 상수 하나로는 둘 다 못 고친다 — 크롭이 유일한 해법.
BACKDROP = [
    (SRC, "tree_pine_large_320x384_final", "Backdrop/tree_pine_large", True),
    (SRC, "tree_pine_small_192x256_final", "Backdrop/tree_pine_small", True),
]

# 한복 jump: 프레임 7~9(idx 6~8)에서 갓이 사라진다 -> 0~5만 채택 (검수 지시)
CHARS = [
    ("seonbi_hanbok/Seonbi_Hanbok_Forest", "SeonbiHanbok", [
        ("idle/east", "Idle", None),
        ("walk/east", "Walk", None),
        ("run/east", "Run", None),
        ("jump/east", "Jump", 6),
        ("climb/north", "Climb", None),
    ]),
    ("player_modern/Player_Modern_Clothes", "PlayerModern", [
        ("idle/east", "Idle", None),
        ("walk/east", "Walk", None),
    ]),
]

# 승인 합성 sky_layer()의 스톱 (forest_compose.py:88)
SKY_STOPS = [(0.00, (116, 120, 146)), (0.45, (163, 164, 184)),
             (0.72, (206, 192, 182)), (1.00, (228, 209, 184))]
SKY_H = 720   # 화면 세로 = 11.25u (orthoSize 5.625) — PPU 64에서 1:1
FOG_RGB, FOG_A = (235, 232, 238), 110
FOG_TOP_WY, FOG_BOT_WY, FOG_TAIL = 4.2, 0.5, 40  # 승인 합성 fog (forest_compose.py:117)


def out(rel):
    p = os.path.join(DST, rel.replace("/", os.sep) + ".png")
    os.makedirs(os.path.dirname(p), exist_ok=True)
    return p


def copy(src_dir, name, rel, crop):
    im = Image.open(os.path.join(src_dir, name + ".png")).convert("RGBA")
    if im.format is not None and Image.open(os.path.join(src_dir, name + ".png")).format != "PNG":
        raise SystemExit(f"[치명] {name}: PNG가 아님 (WebP 위장?) — Unity 임포트가 조용히 실패한다")
    if crop:
        b = im.split()[3].getbbox()
        if b:
            im = im.crop(b)
    im.save(out(rel))
    return rel, im.size


def strips():
    rows = []
    for src_rel, dst_name, anims in CHARS:
        base = os.path.join(SRC, "char", *src_rel.split("/"), "animations")
        for anim_rel, anim_name, limit in anims:
            d = os.path.join(base, *anim_rel.split("/"))
            fs = sorted(f for f in os.listdir(d) if f.endswith(".png"))
            if limit:
                fs = fs[:limit]
            sheet = Image.new("RGBA", (CELL * len(fs), CELL), (0, 0, 0, 0))
            foot = 0
            for i, f in enumerate(fs):
                im = Image.open(os.path.join(d, f)).convert("RGBA")
                if im.size != (CELL, CELL):
                    raise SystemExit(f"[치명] {d}/{f}: 캔버스 {im.size} != {CELL}")
                sheet.alpha_composite(im, (i * CELL, 0))
            rel = f"Char/{dst_name}/{anim_name}"
            sheet.save(out(rel))
            rows.append((rel, len(fs)))
    return rows


def foot_pivot(src_rel):
    """idle 프레임들의 알파 최하단 = 발 바닥선. 캐릭터별로 하나의 피벗을 공유해야
    애니메이션 중 발이 떨리지 않는다 (점프의 뜬 발은 그대로 떠 보여야 정상)."""
    d = os.path.join(SRC, "char", *src_rel.split("/"), "animations", "idle", "east")
    bottom = max(Image.open(os.path.join(d, f)).convert("RGBA").getbbox()[3]
                 for f in sorted(os.listdir(d)) if f.endswith(".png"))
    return (CELL - bottom) / CELL  # 하단 기준 정규화 피벗 y


def ridge_x3():
    """원경 능선: 미러 베이크(심리스화) 후 x3 NEAREST 확대.

    ★ 미러 베이크를 반드시 확대 '전'에 할 것 (실측):
      원본은 심리스 타일이 아니다 — 좌우 끝 실루엣이 안 맞아(200x120 기준 좌우 열 평균 채널차
      12.7, 120행 중 32행이 20 초과) SideWorld.Build.cs가 가로로 반복하면 경계에서 뚝 끊긴다.
      확대 후에 베이크하면 T=[A|mirror(A)[1:-1]]의 1px 크롭이 x3 픽셀 블록 정렬을 깨서
      2px짜리 어중간한 블록이 생긴다. 원본 스케일에서 베이크하면 3px 블록이 온전하다
      (실측: 정렬 위반 0). 남는 이음매 오차 4.6은 A[0]-A[1] 자연 계단이지 불연속이 아니다.
    """
    im = Image.open(os.path.join(B2, "bg_ridge_200x120_v2_final.png")).convert("RGBA")
    im = mirror_bake(im)                                          # 200x120 -> 398x120, 이음매 소멸
    im = im.resize((im.width * 3, im.height * 3), Image.NEAREST)  # 승인 합성과 동일
    im.save(out("Backdrop/bg_ridge"))
    return im.size


def sky():
    im = Image.new("RGBA", (8, SKY_H))
    px = im.load()
    for y in range(SKY_H):
        t = y / (SKY_H - 1)
        col = SKY_STOPS[-1][1]
        for (t0, c0), (t1, c1) in zip(SKY_STOPS, SKY_STOPS[1:]):
            if t0 <= t <= t1:
                f = (t - t0) / (t1 - t0)
                col = tuple(round(a + (b - a) * f) for a, b in zip(c0, c1))
                break
        for x in range(8):
            px[x, y] = col + (255,)
    im.save(out("Backdrop/sky_gradient"))
    return im.size


def fog():
    ramp = round((FOG_TOP_WY - FOG_BOT_WY) * PPU)  # 알파 0 -> 110 구간
    h = ramp + FOG_TAIL
    im = Image.new("RGBA", (8, h), (0, 0, 0, 0))
    px = im.load()
    for y in range(h):
        a = round(FOG_A * min(1.0, y / ramp))
        for x in range(8):
            px[x, y] = FOG_RGB + (a,)
    im.save(out("Backdrop/fog_band"))
    return im.size


if __name__ == "__main__":
    man = json.load(open(os.path.join(SRC, "chunk_manifest.json")))
    if os.path.isdir(DST):
        shutil.rmtree(DST)   # .meta도 함께 지워 재생성 (임포트 설정은 ForestImportTools가 다시 건다)
    report = {}
    for group in (TERRAIN, PROPS, BACKDROP):
        for src_dir, name, rel, crop in group:
            r, size = copy(src_dir, name, rel, crop)
            report[r] = size
    for rel, n in strips():
        report[rel] = f"{n}프레임 스트립"
    report["Backdrop/bg_ridge"] = ridge_x3()
    report["Backdrop/sky_gradient"] = sky()
    report["Backdrop/fog_band"] = fog()

    for k in sorted(report):
        print(f"  {k}: {report[k]}")
    print("\n지면 청크 표면선 (surface_median, 피벗 y = (160-surf)/160):")
    for n in ("ground_flat_02_320x160", "ground_flat_03_320x160", "ground_flat_04_320x160"):
        s = man[n]["surface_median"]
        print(f"  {n}: surf={s} -> pivot y={(160 - s) / 160:.4f}")
    print("\n캐릭터 발바닥 피벗 y:")
    for src_rel, dst_name, _ in CHARS:
        print(f"  {dst_name}: {foot_pivot(src_rel):.4f}")
