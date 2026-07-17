# 지형 수정 생성: 단차 바위(3종) + 사다리 몸통/캡 재생성 + 발판 중간/마구리 재생성.
# create_map_object 사용. forest_gen.py의 queue/poll 재사용.
#
# ★ 실측 근거:
#   - 단차 step_mound_a/b = 지상청크 크롭 파생물 → 흙 텍스처가 공중에 네모나게 떠 보임. 바위로 교체.
#   - 사다리 body_seg = 밑동에 초록 풀 + 상/하단 비flush(y0 오른쪽레일만, 하단 풀로 확장) → 적층 시 깨짐.
#     cap = 몸통과 다른 울타리 디자인 → 재생성.
#   - 발판 mid = 좌우 12px 투명 여백(콘텐츠 12~179) → cap 오른쪽과 12px 틈. 여백 없이 재생성.
#
# usage: forest_terrain_fix.py gen <group>   group = rock | ladder | platform | all
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from forest_gen import run
import forest_prompts as P

ANCHOR = P.ANCHOR
SIDE = P.SIDE
GUARD = P.GUARD
COMMON = P.COMMON

# ── 단차 바위: 4x2칸(256x128). 흙과 재질이 달라야 "튀어나온 것"이 자연스럽다 ──
_ROCK = ("one single SOLID MASS of weathered grey granite bedrock that completely FILLS THE ENTIRE "
         "CANVAS edge to edge, left to right and top to bottom, with no empty space anywhere. Its top "
         "is a FLAT walkable rock shelf running straight across the whole canvas width at the very top "
         "edge, and the stone body below is massive and solid all the way down to the bottom canvas "
         "edge. Angular cracked facets, patches of soft moss clinging to the top and in the crevices. ")
_ROCK_SEAM = ("The left, right and bottom edges are cut off flush at the canvas edges so the rock reads "
              "as a natural outcrop half-buried in the ground, its base sinking into the earth rather "
              "than a clean-cut floating block. Nothing protrudes above the flat top. No gaps, no "
              "floating pieces, no background. ")
_ROCK_MAT = ("The rock is cool grey mossy granite - clearly stone, NOT brown packed earth, NOT soil; "
             "its grey stone material must read as different from a dirt mound.")

ROCK = [
    ("rock_step_a_256x128", dict(description=ANCHOR +
        "A low rectangular raised rock outcrop for a side-scrolling platformer: " + _ROCK + SIDE +
        _ROCK_SEAM + _ROCK_MAT, width=256, height=128, **COMMON)),
    ("rock_step_b_256x128", dict(description=ANCHOR +
        "A low rectangular raised rock ledge for a side-scrolling platformer: " + _ROCK +
        "A few thin grass tufts sprout from the mossy top surface only. " + SIDE +
        _ROCK_SEAM + _ROCK_MAT, width=256, height=128, **COMMON)),
    ("rock_step_c_256x128", dict(description=ANCHOR +
        "A low rectangular boulder shelf for a side-scrolling platformer: " + _ROCK +
        "One larger crack splits the stone face and a small fern grows from a crevice near the top. " +
        SIDE + _ROCK_SEAM + _ROCK_MAT, width=256, height=128, **COMMON)),
]

# ── 사다리: 몸통(96x128, 상하 flush 적층) + 캡(96x64, 몸통과 동일 디자인) ──
_LADDER_MAT = ("The rails and rungs are plain WARM GREYISH BROWN weathered timber - absolutely NOT "
               "green, NOT bamboo, NOT mossy; no grass, no moss, no plants, no ground anywhere. "
               "Transparent background.")
LADDER = [
    ("ladder_body_v2_96x128", dict(description=ANCHOR +
        "The repeating middle body of a rustic wooden ladder standing vertically for a side-scrolling "
        "platformer: two straight vertical wooden side rails with three evenly spaced round rungs "
        "lashed on with braided straw rope. Seen strictly from the side as a flat side elevation. Both "
        "side rails run perfectly straight and are CUT OFF FLUSH at BOTH the top and bottom canvas "
        "edges, passing straight through both edges, so copies stacked vertically connect seamlessly "
        "with an even rung rhythm. The rungs are evenly spaced top to bottom so stacking keeps the "
        "same spacing. " + _LADDER_MAT, width=96, height=128, **COMMON)),
    ("ladder_cap_v2_96x64", dict(description=ANCHOR +
        "The top cap of the SAME rustic wooden ladder for a side-scrolling platformer: the same two "
        "straight vertical wooden side rails at the same width and spacing, ending in gently rounded "
        "worn tops, with one final round rung lashed with braided straw rope between them near the top. "
        "Seen strictly from the side as a flat side elevation. The two rails are CUT OFF FLUSH at the "
        "BOTTOM canvas edge so this cap connects seamlessly onto the ladder body below. This is a "
        "LADDER top with round lashed rungs, NOT a fence, NOT horizontal planks. " + _LADDER_MAT,
        width=96, height=64, **COMMON)),
]

# ── 발판: 중간(192x96, 좌우 flush) + 좌마구리(96x96) ──
PLATFORM = [
    ("platform_mid_v2_192x96", dict(description=ANCHOR +
        "The middle section of a long floating earth platform drifting in the air for a side-scrolling "
        "platformer: a THICK deep body of packed brown earth with a few embedded weathered grey stones, "
        "topped by a flat walkable layer of short mossy grass whose surface runs perfectly straight "
        "across the entire canvas width and sits exactly at the top edge of the canvas. The earth body "
        "FILLS THE WHOLE CANVAS from the very left edge to the very right edge and down to the bottom "
        "edge, and is CUT OFF FLUSH at both the left and right canvas edges - no transparent margin, "
        "the earth touches both side edges - so copies placed side by side connect seamlessly. The "
        "underside carries two short hanging roots. " + SIDE +
        "Nothing protrudes above the grass surface. Transparent background below the platform only. " +
        GUARD, width=192, height=96, **COMMON)),
    ("platform_cap_L_v2_96x96", dict(description=ANCHOR +
        "The left end cap of a floating earth platform for a side-scrolling platformer: a THICK deep "
        "body of packed brown earth with one embedded grey stone, topped by a flat walkable layer of "
        "short mossy grass running perfectly straight at the very top edge of the canvas; the left end "
        "tapers into a gently rounded nose with one short hanging root underneath. The earth fills the "
        "canvas from top to bottom, and the RIGHT edge is CUT OFF FLUSH at the canvas edge - the earth "
        "touches the right edge with no transparent margin - so it connects seamlessly to the middle "
        "section. " + SIDE + "Transparent background below and left of the nose only. " + GUARD,
        width=96, height=96, **COMMON)),
]

GROUPS = {"rock": ROCK, "ladder": LADDER, "platform": PLATFORM,
          "all": ROCK + LADDER + PLATFORM}

if __name__ == "__main__":
    run(GROUPS[sys.argv[2]] if sys.argv[1] == "gen" else [])
