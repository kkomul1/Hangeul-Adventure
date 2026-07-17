# 시작의 숲 본생성 프롬프트 정의 (map_101). 스타일 앵커/사이드뷰 문구는 여기서만 관리.
#
# ★ 프롬프트 구조 규칙 (실측으로 확정 — 어기면 실패한다)
#   description = ANCHOR + SUBJECT + SIDE + details + SEAM + GUARD
#   - ANCHOR는 반드시 접두 (프로젝트 방침).
#   - ANCHOR 바로 뒤에 SUBJECT가 와야 한다. 승인된 batch1이 이 구조였다.
#   - 재롤1: ANCHOR + SIDE(장문) + SUBJECT -> 구조는 맞았으나 팔레트 이탈(형광 초록/붉은 흙).
#   - 재롤2: ANCHOR + SIDE(장문) + GUARD(장문) + SUBJECT -> 주제가 밀려나 인물·구름 풍경화가 나옴. 완전 실패.
#   => SIDE/GUARD는 짧게, 주제 뒤로.

ANCHOR = ("Korean Joseon folk pixel art, muted dawn palette of desaturated mossy green, "
          "wet earth brown and misty blue-gray, soft dawn light from the left, "
          "dark warm brown single color outline (no pure black), gentle mid value contrast, "
          "tranquil ink-wash mood. ")

# 사이드뷰 필수 문구 (m6 실측: 빼먹으면 탑다운처럼 나온다). 장문 금지 — 주제를 밀어낸다.
SIDE = ("Seen strictly from the side as a flat side elevation: no top plane is visible, the grass "
        "reads edge-on as a thin fringe along the top silhouette, NOT a visible top surface. ")

SEAM = ("The soil body is filled completely solid down to the bottom edge of the canvas and is cut "
        "off flush at both the left and right canvas edges so copies placed side by side connect "
        "seamlessly. No gaps, no floating pieces, no background. ")

# 팔레트 강제 (재롤1 대응). 짧게 유지할 것.
GUARD = ("Moss and grass are desaturated dull olive, never vivid green; earth is soft muted greyish "
         "brown, never red or rust; no harsh cracks or high contrast striping.")

COMMON = dict(view="side", outline="single color outline", shading="medium shading", detail="high detail")

# 지상 청크 공통 주제 (표면선 45% = 승인본 terrain_flat_b 실측 71/160에서 역산)
_GROUND = ("Continuous ground terrain chunk for a side-scrolling platformer: a solid mass of packed "
           "brown earth topped with a walkable layer of short mossy grass. The grass surface line runs "
           "nearly flat with only soft subtle undulation, sitting about 45% down from the top of the "
           "canvas at both the left and right canvas edges at exactly the same height. Below that line "
           "the earth body is deep and massive and completely fills the entire lower half of the canvas "
           "all the way down to the bottom edge - a deep cross-section of soil, never a thin strip. ")

TERRAIN = [
    ("ground_flat_02_320x160", dict(description=ANCHOR + _GROUND + SIDE +
        "On top sit one small weathered tree stump and a low cluster of ferns. A few weathered gray "
        "stones and thin roots are embedded in the earth. " + SEAM + GUARD,
        width=320, height=160, **COMMON)),
    ("ground_flat_03_320x160", dict(description=ANCHOR + _GROUND + SIDE +
        "On top two low gnarled tree roots arch out of the soil close to the ground and a scatter of "
        "small gray pebbles rests beside them. No tree, no trunk, no branches and no leaves rise above "
        "the ground - only the low roots. A few weathered gray stones are embedded in the earth. " + SEAM + GUARD,
        width=320, height=160, **COMMON)),
    ("ground_flat_04_320x160", dict(description=ANCHOR + _GROUND + SIDE +
        "The top carries only three sparse tufts of short grass - the plainest, most featureless "
        "variant, meant to repeat often without drawing attention. " + SEAM + GUARD,
        width=320, height=160, **COMMON)),
    ("step_mound_a_256x128", dict(description=ANCHOR +
        "A low rectangular raised earth ledge for a side-scrolling platformer: one single SOLID BLOCK of "
        "packed brown earth and half-buried mossy boulders that completely FILLS THE ENTIRE CANVAS "
        "edge to edge, left to right and top to bottom, with no empty space anywhere. Its flat walkable "
        "top runs perfectly straight across the entire canvas width at the very top edge, and the earth "
        "body below is massive and solid all the way down to the bottom canvas edge. " + SIDE +
        "The left, right and bottom edges are cut off flush at the canvas edges so the block reads as a "
        "solid raised ledge sitting flush on the ground. Nothing protrudes above the top line. No gaps, "
        "no floating pieces, no background. " + GUARD, width=256, height=128, **COMMON)),
    ("step_mound_b_256x128", dict(description=ANCHOR +
        "A low rectangular raised stone ledge for a side-scrolling platformer: one single SOLID BLOCK of "
        "stacked weathered mossy granite slabs with packed brown earth between them that completely "
        "FILLS THE ENTIRE CANVAS edge to edge, left to right and top to bottom, with no empty space "
        "anywhere. Its flat walkable top runs perfectly straight across the entire canvas width at the "
        "very top edge, and the stone body below is massive and solid all the way down to the bottom "
        "canvas edge. " + SIDE +
        "The left, right and bottom edges are cut off flush at the canvas edges so the block reads as a "
        "solid raised ledge sitting flush on the ground. Nothing protrudes above the top line. No gaps, "
        "no floating pieces, no background. " + GUARD, width=256, height=128, **COMMON)),
    ("platform_earth_mid_192x96", dict(description=ANCHOR +
        "The middle section of a long floating earth platform drifting in the air for a side-scrolling "
        "platformer: a THICK deep body of packed brown earth with a few embedded weathered gray stones "
        "that FILLS THE WHOLE CANVAS from the left edge to the right edge and down to the bottom edge, "
        "topped by a flat walkable layer of short mossy grass whose surface runs perfectly straight "
        "across the entire canvas width and sits exactly at the top edge of the canvas. The underside "
        "carries two short hanging roots and a small moss patch. " + SIDE +
        "The earth body is cut off flush at both the left and right canvas edges so copies placed side "
        "by side connect seamlessly. Same material and style as the grassy ground terrain of the same "
        "world. Nothing protrudes above the grass surface. Transparent background. " + GUARD,
        width=192, height=96, **COMMON)),
    ("platform_earth_cap_L_96x96", dict(description=ANCHOR +
        "The left end cap of a floating earth platform for a side-scrolling platformer: a THICK deep body "
        "of packed brown earth with one embedded gray stone filling the canvas from top to bottom, "
        "topped by a flat walkable layer of short mossy grass running perfectly straight at the very "
        "top edge of the canvas; only the left end tapers into a gently rounded nose with one short "
        "hanging root and a moss patch underneath. " + SIDE +
        "The right edge is cut off flush at the canvas edge so it connects seamlessly to the middle "
        "section of the same platform. Same material and style as the grassy ground terrain of the same "
        "world. Transparent background. " + GUARD, width=96, height=96, **COMMON)),
    ("ladder_body_seg_96x128", dict(description=ANCHOR +
        "The repeating middle body of a rustic wooden ladder standing vertically for a side-scrolling "
        "platformer: two weathered wooden side rails with three evenly spaced round rungs lashed on "
        "with braided straw rope. Seen strictly from the side as a flat side elevation. The two side "
        "rails are cut off flush at both the top and bottom canvas edges so copies stacked vertically "
        "connect seamlessly, and the rung spacing is even so stacked copies keep the same rhythm. "
        "Transparent background. The rails and rungs are WARM GREYISH BROWN weathered timber - "
        "absolutely NOT green, NOT bamboo, NOT mossy; plain old brown wood only.",
        width=96, height=128, **COMMON)),
    ("ladder_top_cap_96x64", dict(description=ANCHOR +
        "The top cap of a rustic wooden ladder for a side-scrolling platformer: two weathered wooden "
        "side rails ending in rounded worn tops with one final rung between them and a short straw rope "
        "binding. Seen strictly from the side as a flat side elevation. The rails are cut off flush at "
        "the bottom canvas edge so it connects to the repeating ladder body below. Transparent "
        "background. The rails and rungs are WARM GREYISH BROWN weathered timber - absolutely NOT "
        "green, NOT bamboo, NOT mossy; plain old brown wood only.",
        width=96, height=64, **COMMON)),
    ("boundary_bush_96x256", dict(description=ANCHOR +
        "A dense impassable wall of forest undergrowth for a side-scrolling platformer: tightly packed "
        "dark mossy bushes, tall ferns and a few slender bamboo culms forming a solid vertical thicket "
        "that COMPLETELY FILLS THE ENTIRE CANVAS from the very top edge down to the very bottom edge and "
        "from the left edge to the right edge, leaving no empty space. Seen strictly from the side as "
        "a flat side elevation. The thicket "
        "is cut off flush at the top and bottom canvas edges so copies stacked vertically connect "
        "seamlessly, with the bamboo culms running straight through both edges. The left and right "
        "silhouette is a ragged leafy edge. Transparent background. " + GUARD,
        width=96, height=256, **COMMON)),
]

BACKGROUND = [
    ("tree_pine_large_320x384", dict(description=ANCHOR +
        "A large old Korean red pine tree standing alone, drawn as a background parallax layer for a "
        "side-scrolling platformer: a gnarled leaning trunk with flaking bark rising from the bottom "
        "centre of the canvas, spreading into three or four broad horizontal layered cushions of dark "
        "blue-green needles across the upper two thirds, ink-wash style. Seen strictly from the side at "
        "eye level. Slightly hazier and lower in contrast than foreground objects because it sits "
        "behind the play field. The trunk base is cut off flush at the bottom canvas edge. Transparent "
        "background, no ground, no other trees, no sky, no clouds, no mountains. Desaturated muted "
        "colours, low contrast.", width=320, height=384, **COMMON)),
    ("tree_pine_small_192x256", dict(description=ANCHOR +
        "A slender young Korean red pine tree standing alone, drawn as a background parallax layer for "
        "a side-scrolling platformer: a thin straight trunk with flaking bark rising from the bottom "
        "centre of the canvas, spreading into two broad horizontal layered cushions of dark blue-green "
        "needles in the upper half, ink-wash style. Seen strictly from the side at eye level. Slightly "
        "hazier and lower in contrast than foreground objects because it sits behind the play field. "
        "The trunk base is cut off flush at the bottom canvas edge. Transparent background, no ground, "
        "no other trees, no sky, no clouds, no mountains. Desaturated muted colours, low contrast.",
        width=192, height=256, **COMMON)),
]

# 인페인팅(지형 참조 톤 통일) 대상 — background_image=ref_style_192.png 는 호출측에서 주입.
# 스타일이 참조 이미지에서 오므로 GUARD 불필요, 설명은 짧게.
PROPS = [
    # ★ 실측: 인페인팅 마스크 모양이 곧 오브젝트 실루엣이 된다. oval 마스크 -> 바위가 완전한 구(球)로
    #   나왔다(초콜릿 공). 전부 rectangle 마스크로 바꾸고 실루엣을 문장으로 명시한다.
    #   반환은 192x192 캔버스 + 투명 배경(코너 alpha=0) 확정 -> key_bg 불필요, alpha bbox로 크롭.
    ("prop_mossy_boulder", ANCHOR +
     "An irregular angular weathered granite boulder resting on the forest floor, clearly wider than "
     "it is tall, with flat chipped facets and a broken uneven silhouette, soft moss growing only on "
     "its top surface. Not a sphere, not round, not a ball. " + SIDE + "Transparent background.",
     {"type": "rectangle", "fraction": 0.45}),
    ("prop_tree_stump", ANCHOR +
     "An old cut pine tree stump standing upright on the forest floor, taller than it is wide, bark "
     "peeling, moss on its side and one small mushroom at its base. " + SIDE +
     "Transparent background.", {"type": "rectangle", "fraction": 0.4}),
    ("prop_bush_cluster", ANCHOR +
     "A low wide cluster of mossy forest bushes sitting on the ground, much wider than tall, with a "
     "soft ragged leafy silhouette and a few thin twigs poking out of the top. " + SIDE +
     "Transparent background.", {"type": "rectangle", "fraction": 0.45}),
    ("prop_fern_tuft", ANCHOR +
     "A small tuft of forest ferns and tall grass blades sprouting from the ground, with a spiky "
     "ragged silhouette of separate leaves. " + SIDE + "Transparent background.",
     {"type": "rectangle", "fraction": 0.35}),
    ("prop_fallen_log", ANCHOR +
     "A fallen mossy pine log lying flat and horizontal on the forest floor, a long low cylinder much "
     "wider than tall, bark peeling, moss along its top, one end showing its growth rings. " + SIDE +
     "Transparent background.", {"type": "rectangle", "fraction": 0.5}),
    ("spot_sign_hanji", ANCHOR +
     "A small wooden roadside signpost for a side-scrolling platformer, tall and narrow: a single "
     "weathered wooden post planted in the ground holding up a blank rectangular panel of pale cream "
     "hanji paper framed in dark wood, a tiny tiled roof cap above it and a braided straw rope tied "
     "around the post. The paper panel is completely EMPTY - no letters, no writing, no symbols, just "
     "flat pale cream paper, because game text is drawn on top of it later. " + SIDE +
     "The post base is cut off flush at the bottom canvas edge. Transparent background.",
     {"type": "rectangle", "fraction": 0.55}),
    ("exit_gate_jangseung", ANCHOR +
     "A small forest gateway for a side-scrolling platformer: two weathered wooden posts carved as "
     "Korean jangseung guardian totem faces standing apart on either side of a path, joined at the top "
     "by a single horizontal beam hung with a braided straw rope and two small paper streamers, "
     "forming an open doorway to walk through. The middle of the doorway is completely empty and open "
     "so the player can be seen walking between the posts. " + SIDE +
     "The post bases are cut off flush at the bottom canvas edge. Transparent background.",
     {"type": "rectangle", "fraction": 0.75}),
]

# 캐릭터 (한복 = 승인 룩 계승 / 현대복 = 조선 팔레트에 섞지 않는 차가운 회색 — 서사 장치)
CHAR_HANBOK = ANCHOR + (
    "A young Joseon dynasty scholar (seonbi): a slender man in a pale blue-gray hanbok robe with a "
    "wide dark sash and white inner collar, a black horsehair gat hat with a wide brim, black cloth "
    "shoes, a small book satchel on his hip, calm gentle face. Standing in a relaxed neutral pose.")

CHAR_MODERN = (
    "Korean Joseon folk pixel art rendering style, dark warm brown single color outline (no pure "
    "black), gentle mid value contrast, soft dawn light from the left. "
    "A young modern-day Korean man who has just arrived from the present day, deliberately out of "
    "place in this historical world: he wears a cold neutral gray hooded zip-up jacket over a plain "
    "white t-shirt, slim dark charcoal jeans and white sneakers, with a slate gray canvas backpack on "
    "his back. Short modern black hair, no hat. His entire outfit stays cold desaturated gray and "
    "white - it must NOT pick up any mossy green, earth brown or dawn warmth from the world around "
    "him; the gray clothing is intentionally alien to the palette. Standing in a relaxed neutral pose.")
