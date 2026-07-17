# AI 이미지 생성 프롬프트 (복사-붙여넣기 전용)

ChatGPT(GPT-4o 이미지 생성)에서 사용. **하나의 새 대화**를 열고, 위에서 아래 순서대로 코드블록 내용을 통째로 복사해 붙여넣기만 하면 된다.
생성된 이미지는 프로젝트 루트 `ArtDrop/` 폴더에 각 프롬프트에 표기된 파일명으로 저장한다.

스타일은 두 갈래다 (2026-07-09 사용자 피드백 반영):
- **키아트 (프롬프트 1~6)**: 고퀄리티 디지털 페인팅 일러스트 — 타이틀, 대마왕, 사천왕 4종
- **인게임 요소 (프롬프트 7~16)**: 16비트 도트풍 — 아이템 아이콘 6종, 맵 오브젝트 3종, 파편
- **오프닝 컷신 (프롬프트 22~25)**: 페인팅 — 오프닝 4컷 **[채택]** (2026-07-17)
- ~~오프닝 컷신 (프롬프트 17~21): 도트 시네마틱~~ — **미채택**, 기록 보존

---

## 프롬프트 0 — 최초 1회, 대화 시작 시 붙여넣기

```
지금부터 내 게임의 아트 에셋 이미지를 여러 장 만들 거야. 아래 게임 소개와 스타일
가이드를 기억해줘. 스타일은 두 가지 모드가 있고, 각 요청마다 어느 모드인지 지정할게.
같은 모드의 이미지끼리는 화풍·팔레트·라이팅이 서로 같아야 해.

[게임 소개]
- 제목: 한글 어드벤처 (Hangeul Adventure)
- 장르: 조선시대를 배경으로 한 전연령 한글 퍼즐 어드벤처 게임
- 스토리: 악당 "가나다 대마왕"(살아있는 먹물로 이루어진 도깨비 왕)이 훈민정음을
  훔쳐 산산조각 내고 파편을 세상에 흩뿌렸다. 세상의 글자들은 깨진 채 먹물처럼
  번져 사라지고 있다. 어린 선비 주인공이 조선 팔도를 여행하며 자음을 되찾고
  훈민정음을 복원한다. 대마왕의 부하 사천왕 4명이 각 지역을 지배한다.
- 만들 이미지: (키아트) 타이틀 화면, 대마왕 초상, 사천왕 초상 4종 /
  (도트) 아이템 아이콘 6종, 맵 오브젝트 3종, 수집품 아이콘

[스타일 모드 A — 키아트: 고퀄리티 일러스트]
Highly detailed digital painting, rich painterly brushwork, intricate textures
(fabric folds of hanbok, weathered wood, aged paper grain), dramatic cinematic
lighting with atmospheric depth, layered composition. Muted Joseon-Korea
inspired color palette only: ivory, ink black, faded indigo, persimmon orange,
celadon green. Fantasy storybook mood, all-ages friendly. NOT flat vector art,
NOT minimal, NOT simple shapes, NOT Japanese anime, NOT western cartoon.

[스타일 모드 B — 인게임 요소: 16비트 도트]
16-bit pixel art style, chunky clearly visible pixels, limited color palette,
crisp clean pixel clusters, classic JRPG sprite/icon look (SNES era). Same
muted Joseon palette as mode A. No anti-aliased smooth gradients, no painterly
rendering.

[공통 규칙]
Never include any text, letters, or letter-like shapes in any image — replace
inscriptions with abstract marks.

이해했으면 "준비됐어"라고만 답해줘. 이미지는 다음 메시지부터 요청할게.
```

---

## 프롬프트 1 — [파일명: title_art.png / 타이틀 화면 키비주얼 (모드 A)]

```
스타일 모드 A(키아트: 고퀄리티 일러스트)로 다음 이미지를 생성해줘.

Wide landscape title screen key art, 1536x1024 (landscape). Highly detailed
digital painting with rich painterly brushwork and dramatic cinematic
lighting. A young Joseon scholar boy in a white-and-indigo hanbok with
intricately painted fabric folds and a black horsehair hat (gat), seen from
behind on a grassy hill at twilight, wind stirring his clothes and the tall
grass. Below him, a detailed Korean village: layered tiled hanok roofs with
individually rendered curved tiles, glowing warm lantern light spilling from
paper windows, a grand palace silhouette on the horizon in atmospheric haze.
The dusk sky is a dramatic gradient of deep indigo and persimmon; mysterious
cracked geometric fragments drift and shatter in the air, dissolving into
tendrils of black ink smoke that catch the last light — ominous but wondrous.
Cinematic composition with strong foreground-midground-background depth.
Keep the upper third of the sky relatively calm for a game logo, and keep
important elements away from the top and bottom edges (will be cropped to
16:9). NOT flat vector art, no text anywhere.
```

## 프롬프트 2 — [파일명: boss_ganada.png / 가나다 대마왕 초상 (모드 A)]

```
스타일 모드 A(키아트: 고퀄리티 일러스트)로 다음 이미지를 생성해줘.

Villain boss portrait, 1024x1536 (portrait orientation). Highly detailed
digital painting, dramatic low-angle lighting with rim light. A king made of
living black ink: a large rotund goblin-like Korean folklore monster
(dokkaebi vibe) wearing an exaggerated ornate royal Korean crown with
intricate metalwork, and a tattered dark royal robe with richly painted
embroidery that dissolves into dripping, glossy wet-ink smoke at the edges.
Big glowing pale eyes, smug grin with small fangs — menacing yet slightly
comical and charming, suitable for an all-ages puzzle game. One clawed ink
hand crushing small glowing geometric fragments that scatter like torn
burning paper, casting warm light onto his face (the single warm accent in a
dark indigo-and-ink palette). Three-quarter view, chest-up, textured aged
hanji-paper background with expressive ink splashes. Painterly texture on
every surface. NOT flat vector art, no text.
```

## 프롬프트 3 — [파일명: boss_sa1_batchim.png / 사천왕 1: 받침 사범 (모드 A)]

```
스타일 모드 A(키아트: 고퀄리티 일러스트)로 다음 이미지를 생성해줘. 지금부터
사천왕 4명의 초상을 연달아 만들 거야. 4장의 구도(가슴 위 3/4 초상), 라이팅,
질감 밀도를 서로 통일해줘.

Elite villain portrait 1 of 4, 1024x1536 (portrait). Highly detailed digital
painting, dramatic lighting. A stern old Korean martial-arts master who guards
a foggy mountain pass: wiry elderly man in a weathered gray-indigo training
hanbok with detailed fabric folds, topknot with iron hairpin, long thin beard,
sharp disciplined eyes. He holds a wooden training staff planted like a
gatekeeper. Streaks of living black ink crawl up his sleeves and staff,
marking him as a servant of the ink king. Background: misty mountain-pass
cliffs and stone steps in atmospheric haze. Menacing yet dignified, with a
hint of dry humor — all-ages friendly. Muted Joseon palette, painterly
brushwork. NOT flat vector art, no text.
```

## 프롬프트 4 — [파일명: boss_sa2_gyeopjamo.png / 사천왕 2: 겹자모 노승 (모드 A)]

```
같은 사천왕 시리즈로, 같은 구도·라이팅·질감으로 다음 초상을 생성해줘.

Elite villain portrait 2 of 4, 1024x1536 (portrait). Highly detailed digital
painting. An ancient Buddhist monk of a corrupted mountain temple: bald
elderly monk in layered gray-and-persimmon robes with rich fabric detail,
serene half-closed eyes that glow faintly pale, an enigmatic gentle smile
that feels slightly unsettling. Everything about him comes in PAIRS: he holds
two identical strings of prayer beads, and twin wisps of black ink incense
smoke rise symmetrically behind his shoulders, forming mirrored patterns.
Background: dim temple hall with paired stone lanterns and faded dancheong
(traditional painted woodwork) in atmospheric shadow. Menacing yet calm and
charming — all-ages friendly. Muted Joseon palette, painterly brushwork.
NOT flat vector art, no text.
```

## 프롬프트 5 — [파일명: boss_sa3_natmal.png / 사천왕 3: 낱말 장사꾼 (모드 A)]

```
같은 사천왕 시리즈로, 같은 구도·라이팅·질감으로 다음 초상을 생성해줘.

Elite villain portrait 3 of 4, 1024x1536 (portrait). Highly detailed digital
painting. A sly traveling merchant who rules a bustling marketplace: a
round-bellied middle-aged man in a patched but flashy silk vest over hanbok,
wide-brimmed traveling hat, gold tooth in a wide crooked grin, calculating
eyes. He carries a huge wooden A-frame backpack (jige) overflowing with
richly painted goods — bundles, gourds, strings of coins — and among them
small glowing geometric fragments in cages, as if words themselves are his
merchandise. One hand flips a coin trailing black ink smoke. Background:
warm lantern-lit market stalls in atmospheric blur. Greedy and menacing yet
comical — all-ages friendly. Muted Joseon palette with warm lantern accents,
painterly brushwork. NOT flat vector art, no text.
```

## 프롬프트 6 — [파일명: boss_sa4_migung.png / 사천왕 4: 미궁의 넷째 (모드 A)]

```
같은 사천왕 시리즈로, 같은 구도·라이팅·질감으로 다음 초상을 생성해줘.
이 넷째가 사천왕 중 가장 강하고 미스터리해야 해.

Elite villain portrait 4 of 4, 1024x1536 (portrait). Highly detailed digital
painting, the most mysterious and powerful of the four. A slender figure in a
flowing midnight-indigo scholar robe whose lower half dissolves into drifting
black ink, wearing a traditional Korean mask (tal) — pale carved wood with a
faint unreadable smile, one eyehole leaking a thin wisp of ink. Long sleeves
hide the hands; torn paper talismans with abstract brush marks float around
the figure. Background: an impossible labyrinth of shifting hanok walls and
doorways receding in fog, subtly distorted geometry. Cold moonlit lighting
with a single pale glow from the mask. Eerie and menacing yet elegant —
all-ages friendly, not horror. Muted Joseon palette, painterly brushwork.
NOT flat vector art, no text.
```

---

## 프롬프트 7 — [파일명: item_w_mokgeom.png / 아이템 아이콘: 목검 (모드 B)]

```
지금부터는 스타일 모드 B(16비트 도트)로 전환할게. 인게임 아이템 아이콘 시리즈야.
전부 같은 픽셀 밀도, 같은 팔레트, 같은 각도(45도 대각선)로 통일해줘.

16-bit pixel art JRPG inventory item icon, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, tilted 45 degrees diagonally. Fully transparent background (real
PNG alpha channel — no backdrop, no drawn checkerboard pattern, no shadow, no
frame, no oval plate behind the object). Bold readable silhouette.

Object: a simple Korean wooden practice sword (mokgeom), pale wood with a
hint of grain, humble training weapon.
```

## 프롬프트 8 — [파일명: item_w_musoe.png / 아이템 아이콘: 무쇠검 (모드 B)]

```
같은 도트 아이콘 시리즈로, 같은 픽셀 밀도·팔레트·각도로 생성해줘.

16-bit pixel art JRPG inventory item icon, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, tilted 45 degrees diagonally. Fully transparent background (real
PNG alpha channel — no backdrop, no drawn checkerboard pattern, no shadow, no
frame, no oval plate behind the object). Bold readable silhouette.

Object: a heavy Joseon iron sword, dark gray blade with a simple cord-wrapped
hilt, sturdy and plain.
```

## 프롬프트 9 — [파일명: item_w_eunjangdo.png / 아이템 아이콘: 은장도 (모드 B)]

```
같은 도트 아이콘 시리즈로, 같은 픽셀 밀도·팔레트·각도로 생성해줘.

16-bit pixel art JRPG inventory item icon, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, tilted 45 degrees diagonally. Fully transparent background (real
PNG alpha channel — no backdrop, no drawn checkerboard pattern, no shadow, no
frame, no oval plate behind the object). Bold readable silhouette.

Object: an ornate Korean silver dagger (eunjangdo) with an engraved silver
sheath and a small tassel, refined and precious.
```

## 프롬프트 10 — [파일명: item_a_mumyeong.png / 아이템 아이콘: 무명 도포 (모드 B)]

```
같은 도트 아이콘 시리즈로, 같은 픽셀 밀도·팔레트·각도로 생성해줘.

16-bit pixel art JRPG inventory item icon, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, tilted 45 degrees diagonally. Fully transparent background (real
PNG alpha channel — no backdrop, no drawn checkerboard pattern, no shadow, no
frame, no oval plate behind the object). Bold readable silhouette.

Object: a humble white cotton Korean overcoat (dopo), neatly folded, undyed
fabric with indigo trim.
```

## 프롬프트 11 — [파일명: item_a_gajuk.png / 아이템 아이콘: 가죽 배자 (모드 B)]

```
같은 도트 아이콘 시리즈로, 같은 픽셀 밀도·팔레트·각도로 생성해줘.

16-bit pixel art JRPG inventory item icon, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, tilted 45 degrees diagonally. Fully transparent background (real
PNG alpha channel — no backdrop, no drawn checkerboard pattern, no shadow, no
frame, no oval plate behind the object). Bold readable silhouette.

Object: a Korean leather-padded sleeveless vest (baeja), warm brown leather
panels stitched over fabric.
```

## 프롬프트 12 — [파일명: item_a_dujeonggap.png / 아이템 아이콘: 두정갑 (모드 B)]

```
같은 도트 아이콘 시리즈로, 같은 픽셀 밀도·팔레트·각도로 생성해줘.

16-bit pixel art JRPG inventory item icon, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, tilted 45 degrees diagonally. Fully transparent background (real
PNG alpha channel — no backdrop, no drawn checkerboard pattern, no shadow, no
frame, no oval plate behind the object). Bold readable silhouette.

Object: a Joseon brass-studded armor coat (dujeonggap), dark fabric with rows
of round metal studs and simple plate trim, imposing but not ornate.
```

## 프롬프트 13 — [파일명: prop_jangseung.png / 맵 오브젝트: 장승 (모드 B)]

```
계속 스타일 모드 B(16비트 도트)로, 이번엔 맵 장식 오브젝트 시리즈야.
전부 같은 픽셀 밀도·팔레트, 같은 3/4 탑다운 시점으로 통일해줘.

16-bit pixel art game map object sprite, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, viewed from a 3/4 top-down angle (slightly above, classic 2D JRPG
overworld view). Fully transparent background (real PNG alpha channel — no
ground plane, no backdrop, no drawn checkerboard pattern, no oval plate).
Bold readable silhouette.

Object: a Korean jangseung — a tall carved wooden village guardian pole with
a comically stern grimacing face, weathered wood. No readable letters on it;
use abstract worn marks instead.
```

## 프롬프트 14 — [파일명: prop_doltap.png / 맵 오브젝트: 돌탑 (모드 B)]

```
같은 도트 맵 오브젝트 시리즈로, 같은 픽셀 밀도·팔레트·시점으로 생성해줘.

16-bit pixel art game map object sprite, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, viewed from a 3/4 top-down angle (slightly above, classic 2D JRPG
overworld view). Fully transparent background (real PNG alpha channel — no
ground plane, no backdrop, no drawn checkerboard pattern, no oval plate).
Bold readable silhouette.

Object: a small Korean stacked stone cairn (doltap) — rounded gray stones
piled by travelers for good luck, a little moss on the lower stones.
```

## 프롬프트 15 — [파일명: prop_giwajip.png / 맵 오브젝트: 기와집 외관 (모드 B)]

```
같은 도트 맵 오브젝트 시리즈로, 같은 픽셀 밀도·팔레트·시점으로 생성해줘.

16-bit pixel art game map object sprite, 1024x1024, chunky clearly visible
pixels, limited color palette, crisp pixel clusters, single object only,
centered, viewed from a 3/4 top-down angle (slightly above, classic 2D JRPG
overworld view). Fully transparent background (real PNG alpha channel — no
ground plane, no backdrop, no drawn checkerboard pattern, no oval plate).
Bold readable silhouette.

Object: a traditional Korean tiled-roof house (giwajip) exterior — curved
dark gray roof tiles, wooden pillars, white-clay walls, a small stone step,
paper-screen doors. No text on any signboard.
```

## 프롬프트 16 — [파일명: item_fragment_hunmin.png / 수집품: 훈민정음 파편 (모드 B)]

```
스타일 모드 B(16비트 도트)로 다음 수집품 아이콘을 생성해줘.

16-bit pixel art JRPG collectible item icon, 1024x1024, chunky clearly
visible pixels, limited color palette, crisp pixel clusters, single object
only, centered. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow, no frame, no oval plate).
Bold readable silhouette.

Object: a torn fragment of ancient Korean hanji paper, aged ivory with ragged
pixel edges, glowing with warm golden light and a few pixel sparkles — a
precious sacred-relic feeling. The surface shows only a few abstract dark
pixel marks suggesting old calligraphy. Absolutely no readable letters or
characters of any language.
```

---

## 오프닝 4컷 (프롬프트 22~25) — 모드 A **[채택]**

**결정 (2026-07-17)**: 오프닝 컷신은 **모드 A(페인팅)로 확정**. 픽셀(모드 C, 프롬프트 17~21)과
컷1을 실제로 각각 뽑아 비교한 뒤 사용자가 페인팅을 선택했다. 채택 근거는 컷1 실물에서
title_art의 붓질·먹 연기 넝쿨·균열 파편이 그대로 계승됐고, "생기 빠진 차가운 현대"라는 컷1의
핵심 요구를 페인팅의 대기 표현이 도트보다 확실히 잘 살렸다는 점. 모드 C 절은 기록으로 남긴다.

**컷1은 생성 완료·채택됨** → `ArtDrop/Generated/opening/opening_01_chaos_16x9.png`

### 화면비 — 컷1 실측에서 역산한 규칙

| 항목 | 실측값 |
|---|---|
| 나노바나나 웹 원본 | 1024×572 (**정확히 16:9**) |
| 하단에 모델이 그려 넣은 검은 레터박스 | 69px |
| 제거 후 실제 내용 영역 | **1024×503 = 2.04:1** (시네마스코프) |

- **웹 UI 화면비는 컷1과 동일하게 16:9로 설정**하고, 결과에서 검은 띠를 잘라 **1024×503**으로 맞춘다.
  띠가 안 생기면 하단을 크롭해 2.04:1을 만든다. 4컷이 비율이 다르면 컷신이 흔들리므로 이 수치를 고정한다.
- **"하단 1/5를 비워두라"는 지시는 컷1에서 먹지 않았다.** 생성 모델은 빈 공간을 채우려 한다.
  → 22~25번 프롬프트는 **"비워두라"를 버리고 "얼굴과 핵심 액션을 상단 2/3에 두라"**는 구도 지시로 바꿨다.
  대사창을 반투명 오버레이로 얹기로 한 이상 실제 요구는 여백이 아니라 "하단 1/5에 얼굴이 없을 것"이며,
  컷1은 이미 이 조건을 만족한다.

### 참조 이미지 — 컷1을 참조로 쓴다 (title_art 아님)

| 컷 | 첨부할 참조 이미지 | 이유 |
|---|---|---|
| 22 (컷1) | `ArtDrop/title_art.jpg` | 생성 완료. 기록용 |
| 23 (컷2) | **컷1** | 아직 현대. 주인공·파편·먹 연기·화면비를 컷1에서 그대로 물려받는다 |
| 24 (컷3) | **컷1 + `title_art.jpg`** | 주인공·파편은 컷1에서, 조선 팔레트·건축은 title_art에서 |
| 25 (컷4) | **컷3 + `title_art.jpg`** | 조선 팔레트가 확정된 직전 컷을 물려받는 게 가장 가깝다 |

**근거**: title_art가 아니라 **컷1을 기준 참조로 삼는다.** ① title_art의 주인공은 조선 선비 소년이라
현대인 주인공과 다른 인물이다 — 회색 후디·머리 모양 등 **캐릭터 연속성은 컷1만 공급할 수 있다.**
② 컷1은 이미 "title_art 화풍을 나노바나나가 해석한 결과물"이라, 컷1을 참조하면 **실제로 달성된 룩**이
전파돼 4컷 일관성이 더 조인다. ③ 화면비·지평선 높이도 컷1이 기준이다.
다만 조선 세계(팔레트·건축)는 컷1에 없으므로 컷3·4에서 title_art를 **함께** 첨부한다.

**생성 순서: 23 → 24 → 25 (반드시 순차).** 25번이 24번 결과를 참조로 쓰는 체인이라 건너뛸 수 없다.

---

### 프롬프트 22 — [파일명: opening_01_chaos.png / 오프닝 컷1: 현대의 혼란 (모드 A)] **생성 완료**

참조 이미지: `ArtDrop/title_art.jpg`

```
Panel 1 of 4 in a game's opening cutscene, 16:9 landscape.

Use the EXACT same art style as the attached reference illustration: highly
detailed painterly digital painting, rich visible brushwork, intricate textures,
dramatic cinematic lighting, strong atmospheric depth, layered
foreground-midground-background composition. Match the reference's brushwork,
lighting drama and level of detail identically.

This is the ONE panel of the opening that is NOT set in Joseon -- it is the modern
world. So shift the palette away from the reference: make it cold and drained --
dead concrete gray, sickly pale cyan-white fluorescent light, wet asphalt
blue-black, and living black ink. The reference's persimmon-orange warmth is almost
entirely absent here; the world has had its life bleached out. Keep the mood
tranquil and ominous -- quiet dread, not loud action.

Scene: a modern city street at dawn, eye level. Shop signboards, a bookshop window
display, a road sign and a bus stop board are all COMING APART -- the marks on them
crack into small angular geometric fragments that peel off, tumble through the air
and dissolve into curling tendrils of glossy black ink smoke, exactly like the
drifting cracked fragments and black ink tendrils in the reference image. Loose book
pages and leaflets blow down the street, their surfaces already blank. In the middle
ground, ordinary modern people stand frozen and helpless, mouths open, gesturing at
one another -- speech has stopped working. A young modern man in a gray hoodie and
dark jeans stands left of center with his back to us, small against the street,
staring up at a disintegrating signboard -- the only one still trying to read,
echoing the lone figure seen from behind in the reference.

Composition: strong left-to-right depth with the street receding to the right, soft
directional light from the LEFT. Keep the bottom fifth calm for a dialogue text box.

Absolutely no text, letters, numbers or letter-like shapes anywhere -- every sign,
book and page carries only abstract angular marks or blank surfaces.
```

### 프롬프트 23 — [파일명: opening_02_timemachine.png / 오프닝 컷2: 타임머신 (모드 A)]

참조 이미지: **컷1** (`opening_01_chaos_16x9.png`)

```
Panel 2 of 4 in a game's opening cutscene. The attached image is panel 1 of the same
cutscene.

Match the attached panel EXACTLY in art style, rendering and framing: highly detailed
painterly digital painting, rich visible brushwork, intricate textures, dramatic
cinematic lighting, strong atmospheric depth, layered composition, and the same very
wide cinematic aspect ratio and horizon height. Same hand, same film.

Character continuity: the same young modern man from the attached panel -- gray hoodie,
dark jeans, same build and hair -- again seen from behind.

Palette: the cold modern palette of the attached panel still dominates (dead concrete
gray, sickly pale cyan-white light, wet asphalt blue-black, living black ink), BUT a
warm amber-brown glow now bleeds out from the machine's core and a first hint of misty
blue-gray appears in the vortex. Roughly a quarter of the frame has turned warm -- this
is the first real warmth in the whole opening.

Scene: the interior of a hard science-fiction time-travel laboratory, eye level. A
precision-engineered machine, unmistakably modern high technology: a machined alloy
containment ring lined with superconducting coils and cryogenic lines venting cold
vapour, heavy power conduits, instrument racks with glowing indicator arrays, blast
shielding, and a raised platform at its centre. Inside the ring, spacetime is opening:
a swirling vortex of amber and misty blue-gray light. The young man in the gray hoodie
stands left of centre, seen from behind, stepping up onto the platform toward the
vortex, one hand steadying himself on the ring frame. Broken angular geometric fragments
and curling tendrils of glossy black ink smoke -- identical in look to those in the
attached panel -- drift in from the LEFT of the frame and are being SUCKED INTO the
vortex, spiralling as they go: the disaster is following him in. On the racks behind
him, pinned printouts and documents are already blank, their marks eaten away.

Composition: the machine on the right, the man reading left-to-right into it; the vortex
is the brightest point and the only real warmth in the frame. Keep the man, the machine
and all key action in the upper two-thirds of the frame -- the bottom fifth should hold
only floor, cabling and incidental foreground, because a dialogue box will be laid over
it.

Absolutely no text, letters, numbers or letter-like shapes anywhere -- every screen,
label, printout and control panel carries only abstract marks or blank surfaces.
```

- **담아야 할 것**: 명백한 하드SF 장치(정밀 가공 합금 링·초전도 코일·극저온 배관), 소용돌이로 빨려드는 파편(컷1의 파편이 어디로 가는지), 첫 호박색 온기, 뒷모습 유지.
- **담지 말아야 할 것**: 수제 조악한 기계(하드SF 확정으로 폐기됨), 조선 요소, 주인공 얼굴, 화면에 읽히는 글자·숫자.

### 프롬프트 24 — [파일명: opening_03_arrival.png / 오프닝 컷3: 조선 도착 (모드 A)]

참조 이미지: **컷1** + **`title_art.jpg`** (2장 첨부)

```
Panel 3 of 4 in a game's opening cutscene. Two images are attached: the FIRST is panel 1
of the same cutscene (for the character, the fragments and the ink smoke), the SECOND is
a Joseon-era key art illustration (for the period world, its palette and its
architecture).

Match the FIRST attached panel exactly in art style, rendering and framing: highly
detailed painterly digital painting, rich visible brushwork, intricate textures,
dramatic cinematic lighting, strong atmospheric depth, and the same very wide cinematic
aspect ratio and horizon height.

Character continuity: the same young modern man -- gray hoodie, dark jeans, white
sneakers, same build and hair.

Palette: this is where the world turns. Take the palette from the SECOND attached
reference -- a muted Joseon dawn: desaturated mossy green, wet earth brown, misty
blue-gray ridges, soft dawn light from the LEFT, with a warm amber accent. It now fills
about 85% of the frame. CRITICAL: the ONE thing that stays cold and out of palette is
the young man's modern clothing -- keep his gray hoodie and dark jeans noticeably
cooler, grayer and deader than everything around them, as if this world has not coloured
him in yet. He has NOT changed clothes and wears NO hanbok.

Scene: a quiet Joseon dynasty countryside path at dawn -- layered misty blue-gray
mountain ridges far behind, a pine forest silhouette in the middle distance, a dirt path
with mossy green verges and wet earth brown banks. The arrival has gone wrong. The time
machine has crashed: its machined alloy ring is cracked open and half-buried in the
earth, torn conduits sparking, cold vapour and smoke bleeding from its ruptured core --
clearly destroyed beyond repair, no way home. Thrown clear of the wreck lies an ancient
Korean bound manuscript, its cover split open; the crash is TEARING IT APART, and its
pages are breaking into a rising storm of small angular geometric fragments -- the same
fragments as in the first attached panel -- that scatter up and away toward the right,
out over the ridges. The young man kneels left of centre in three-quarter back view amid
the wreckage, one hand on the ground, staring after the escaping fragments. A single
thin wisp of glossy black ink smoke lingers on the far horizon, watching.

Composition: wreck and man on the left, fragments streaming right toward the ridges,
strong left-to-right depth. Keep the man, the manuscript and the fragment storm in the
upper two-thirds of the frame -- the bottom fifth should hold only path, earth and
debris, because a dialogue box will be laid over it.

Absolutely no text, letters, numbers or letter-like shapes anywhere -- the manuscript's
pages and the fragments must carry only abstract brush marks, never readable characters.
```

- **담아야 할 것**: 수리 불가로 부서진 타임머신(귀환 불가 = 동기), 찢겨 파편이 되어 오른쪽으로 날아가는 해례본, 앵커 팔레트 85%, **주인공 현대복만 차갑게 남음**, 지평선의 먹 연기 한 줄.
- **담지 말아야 할 것**: 주인공의 한복(가장 잦은 실패 — 4컷 대사의 전제가 무너진다), 읽히는 글자, 멀쩡한 타임머신, 군중.

### 프롬프트 25 — [파일명: opening_04_sejong.png / 오프닝 컷4: 세종 만남 (모드 A)]

참조 이미지: **컷3** + **`title_art.jpg`** (2장 첨부) — 컷3 생성 후에 진행

```
Panel 4 of 4 in a game's opening cutscene. Two images are attached: the FIRST is panel 3
of the same cutscene (for the character and the established Joseon palette), the SECOND
is a Joseon-era key art illustration (for the architecture and period detail).

Match the attached panels exactly in art style, rendering and framing: highly detailed
painterly digital painting, rich visible brushwork, intricate textures, dramatic
cinematic lighting, strong atmospheric depth, and the same very wide cinematic aspect
ratio and horizon height.

Palette: the muted Joseon palette, now complete -- desaturated mossy green, wet earth
brown, misty blue-gray, warm amber lamplight, with deep indigo and muted vermilion
accents from the painted woodwork. CRITICAL: exactly ONE thing in the frame is still
off-palette -- the young man's cold gray modern clothing. Make that clash obvious; it is
the entire point of this shot. He still wears the gray hoodie, dark jeans and white
sneakers and has NOT changed into a hanbok.

Scene: inside a grand Joseon palace hall at dawn, warm amber light spilling through
paper-screen doors on the LEFT. On the right sits a dignified Joseon king in his forties
-- round kind face, thin beard, calm intelligent eyes -- wearing an ornate royal robe and
a black winged official cap, behind a low writing desk with a brush, an inkstone and
blank paper scrolls. He has just looked up from his work and is gesturing lightly toward
the visitor's strange clothes with an amused, genuinely puzzled expression, one eyebrow
raised -- curious, not angry. On the left the young modern man stands in profile facing
him, stiff and awkward, one hand rubbing the back of his neck, his cold gray clothing
glaringly wrong against the warm hall. Painted wooden beams overhead, a folding screen
with abstract landscape brushwork behind the king. One small angular geometric fragment
-- the same kind as in the attached panels -- floats quietly in the air above the desk
between the two men. A single faint wisp of glossy black ink smoke slips out through the
far right doorway, unnoticed by both.

Composition: a two-shot -- the man on the left, the king on the right, facing each other
across the frame with clear space between them. Keep both faces in the upper two-thirds
of the frame -- the bottom fifth should hold only the floor and the base of the desk,
because a dialogue box will be laid over it.

Absolutely no text, letters, numbers or letter-like shapes anywhere -- the scrolls, the
folding screen and the desk must be blank or carry only abstract brush marks.
```

- **담아야 할 것**: 왕의 손짓·시선이 주인공의 옷을 정확히 가리킬 것(대사 "그 이상한 옷…"의 근거), 놀림기 섞인 호기심(분노 아님 — 전연령 톤), 두 사람 사이의 파편 한 조각, 오른쪽 문으로 빠져나가는 먹 연기(본편 출발 신호).
- **담지 말아야 할 것**: **세종의 실명**(아래 팁 참조 — 절대 프롬프트에 넣지 말 것), 신하·호위 군중, 위압적 옥좌 정면 구도(만남이 아니라 알현이 된다), 주인공의 한복, 읽히는 글자.

---

### 모드 A 오프닝 — 연속성 장치 (컷1 실물 기준으로 갱신)

| 장치 | 컷1 | 컷2 | 컷3 | 컷4 |
|---|---|---|---|---|
| **① 균열 파편** | 간판에서 뜯겨 폭풍 (**결과**) | 소용돌이로 빨려듦 | **해례본에서 터져 나옴 (원인)** | 책상 위 한 조각 정지 |
| **② 먹 연기** | 화면 가득 | 파편과 함께 빨려듦 | 지평선에 한 줄 | 오른쪽 문으로 빠져나감 |
| **③ 팔레트** | 차가운 현대 100% | 현대 75% + 호박 25% | 앵커 85% | 앵커 100% |
| **④ 주인공 현대복** | 회색 후디 (배경과 동색) | 동색 | **혼자 차갑게 남음** | **왕이 지적 → 해소** |
| **⑤ 왼쪽 광원** | 형광 청백색 | 왼쪽에서 파편 유입 | 새벽빛 | 문살 사이 아침빛 |
| **⑥ 좌→우 시선** | 거리가 우측 후퇴 | 인물 → 기계 | 인물 → 우측으로 날아가는 파편 | 인물 → 왕 (동선 종결) |

- **④가 이 오프닝의 논리 축**이다. 색이 안 맞는 옷 → 왕이 지적 → 환복. 팔레트 불일치가 곧 플롯이므로,
  컷3·4에서 주인공 옷이 조선색으로 물들면 오프닝이 무너진다. **재생성 판정 1순위.**
- **①의 의미가 컷3 확정 설정으로 바뀌었다**: 컷1의 파편이 '결과', 컷3의 해례본 찢김이 '원인'이 되어
  오프닝이 인과 루프로 닫힌다(주인공의 도착이 곧 글자 소멸의 발단). 의도된 것이면 강한 한 수이고,
  아니라면 컷3에서 파괴의 주체를 먹 연기 쪽으로 옮기는 수정이 필요하다 — **기획 확인 필요 항목**.

### 모드 A 오프닝 — 실패 시 재요청 문구

- **주인공이 한복을 입고 나올 때** (컷3·4에서 가장 잦을 실패 — 모델이 "조선 배경 = 한복"으로 자동 보정한다):
  → `The young man must stay in his modern gray hoodie, dark jeans and white sneakers. He has NOT changed clothes and wears no hanbok. Keep his clothing cold gray and desaturated while everything around him stays warm.`
- **하단에 얼굴·핵심 액션이 걸릴 때** (컷1에서 "비워두라"가 안 먹힌 실패의 재발):
  → `Raise the camera slightly and re-frame so that all faces and the key action sit in the upper two-thirds. The bottom fifth must contain only ground and incidental foreground.`
  ※ "leave the bottom empty"는 쓰지 말 것 — 컷1에서 무시당했다. 반드시 **구도(re-frame)** 로 요구한다.
- **화면비가 컷1과 다르게 나올 때**:
  → `Match the exact aspect ratio, framing and horizon height of the attached panel. Same very wide cinematic frame.`
  그래도 어긋나면 16:9로 받아 하단을 잘라 **1024×503**으로 맞춘다.
- **팔레트가 안 물들 때 (컷3)**: → `The environment must fully adopt the muted Joseon dawn palette of the second reference -- mossy green, wet earth brown, misty blue-gray. Only the man's clothing stays cold gray.`
- **세종은 절대 이름으로 부르지 말 것.** `King Sejong`을 쓰면 ① 생성 거부 ② 표준영정 모사 ③ 만원권 초상
  흉내 중 하나가 나온다. 25번 프롬프트의 묘사 방식(`a dignified Joseon king in his forties, round kind
  face, thin beard, ornate royal robe, black winged official cap`)을 그대로 유지하고 이름을 넣지 말 것.
- **글자가 읽히게 나올 때**: → `Remove all readable characters. Every sign, page and scroll must carry only abstract brush marks or be blank.` (컷1도 간판에 글자 비슷한 추상 기호가 남았으나 판독 불가라 허용된 상태.)

### 모드 A 오프닝 — 저장·후처리

- `ArtDrop/Generated/opening/`에 `opening_02_timemachine.png` … 로 저장 → 검은 띠 제거·크롭본은 `_16x9.png` 접미(컷1 관례 유지. 실제 비율은 2.04:1이지만 파일명 관례는 컷1과 통일).
- 4컷 전부 **1024×503**으로 통일한 뒤 Unity로 임포트.
- `ArtDrop/출처.md`에 "파일명 / 나노바나나(Gemini) 웹 생성 / 날짜 / 프롬프트 번호" 기록.

---

## 오프닝 4컷 (프롬프트 17~21) — 모드 C **[미채택 · 기록 보존]**

> **미채택 (2026-07-17)**: 컷1을 모드 A(페인팅)와 모드 C(픽셀)로 각각 실제 생성해 비교한 결과
> **사용자가 모드 A를 선택**했다. 아래 모드 C 프롬프트는 실행하지 말 것 — 판단 근거와 설계
> (문법/팔레트 분리, 연속성 장치)는 모드 A 절에 계승되었으므로 기록으로만 남긴다.
> 채택된 프롬프트는 위 "오프닝 4컷 (프롬프트 22~25) — 모드 A" 절을 볼 것.

게임 시작 시 재생되는 오프닝 컷신 4장. **키아트(모드 A)가 아니라 도트 시네마틱(모드 C)이다** —
오프닝은 곧바로 인게임으로 이어지므로 타이틀 일러스트보다 **플레이 화면(사이드뷰 도트)과의
연속성**이 우선이다. 룩 기준은 `ArtDrop/Generated/lookcheck_b2/scene_composite_1280.png`.

**화면비**: 1536x1024로 받아 위아래를 크롭해 **16:9 (1536x864)** 로 사용 — 타이틀(프롬프트 1)과
동일한 워크플로. 컷신은 전체화면으로 재생하므로 16:9가 맞고, 하단 1/5은 대사창이 덮으므로
중요 요소를 두지 않는다.

**핵심 설계**: 스타일 앵커를 **[문법]과 [팔레트]로 분리**한다. 문법(아웃라인·셰이딩·광원 방향·
픽셀 밀도)은 4컷 전부 고정하고, 팔레트만 컷1(차가운 현대) → 컷4(조선)로 이행시킨다. "같은 손이
그린 다른 세계"가 되어 스타일은 안 깨지면서 이야기의 '물들어감'이 그림으로 드러난다. BGM의
칩튠→국악 그라데이션과 같은 타이밍으로 간다.

---

### 프롬프트 17 — [모드 C 선언 / 최초 1회, 컷1 전에 붙여넣기]

```
지금부터 오프닝 컷신 4컷을 순서대로 만들 거야. 새로운 스타일 모드 C야.
4장은 하나의 연속된 장면으로 읽혀야 하니까, 아래 [문법]은 4장 모두 100% 동일하게
유지하고, [팔레트]만 컷마다 내 지시대로 바꿔줘.

[스타일 모드 C — 오프닝 컷신: 도트 시네마틱]
16-bit pixel art cinematic cutscene panel, chunky clearly visible pixels, crisp
pixel clusters, limited palette, no anti-aliased smooth gradients. Korean Joseon
folk pixel art sensibility.

[문법 — 4컷 전부 고정, 절대 바꾸지 말 것]
- Dark warm brown single-color outline on everything. Never pure black.
- Cel shading: base color + one shadow step + one highlight step. No airbrush.
- Gentle mid-value contrast. No blown-out highlights, no crushed blacks.
- Soft directional light always coming from the LEFT of the frame.
- Eye-level camera, cinematic left-to-right depth staging.
- Tranquil ink-wash mood — even the chaotic panel stays quiet, not loud.
- 1536x1024 landscape. Keep important elements away from the top and bottom
  edges (I will crop to 16:9), and keep the bottom fifth calm for a text box.

[팔레트 — 컷마다 다름. 컷1의 차가운 현대색에서 컷4의 조선색으로 서서히 물들어간다]
- 컷1: 차가운 현대색 100% (콘크리트 회색, 형광 청백색, 아스팔트 청흑색, 먹색)
- 컷2: 현대색 75% + 따뜻한 호박색이 25% 침투
- 컷3: 조선 앵커색 85% (탁한 이끼 초록, 젖은 흙 갈색, 안개 청회색, 왼쪽 새벽빛)
- 컷4: 조선 앵커색 100%
- 단, 컷3·컷4에서도 주인공의 현대복(회색 후디·청바지·흰 운동화)만은 차갑고 칙칙한
  색으로 남겨줘. 이게 마지막 컷의 대사로 이어지는 장치라 절대 조선색으로 물들이면 안 돼.

[공통 규칙]
이미지 안에 글자·문자·숫자·글자처럼 보이는 형태를 절대 넣지 마. 간판·책·문서·족자는
전부 추상적인 흔적이나 빈 면으로 처리해. (글자는 내가 나중에 폰트로 얹을 거야.)

이해했으면 "준비됐어"라고만 답해줘. 컷1부터 다음 메시지에서 요청할게.
```

---

### 프롬프트 18 — [파일명: opening_01_chaos.png / 오프닝 컷1: 현대의 혼란 (모드 C)]

```
스타일 모드 C(오프닝 컷신: 도트 시네마틱)로 4컷 중 첫 번째를 생성해줘.

Panel 1 of 4 in a pixel art cutscene series. 1536x1024 (landscape, will be
cropped to 16:9). 16-bit pixel art, chunky clearly visible pixels, crisp pixel
clusters, limited palette, no anti-aliased smooth gradients.

Style grammar (identical across all four panels): dark warm brown single-color
outline, never pure black; cel shading with one shadow step and one highlight
step; gentle mid-value contrast; soft directional light from the LEFT;
eye-level camera; tranquil ink-wash mood underneath the chaos — quiet dread,
not loud action.

Palette for THIS panel only — deliberately cold and wrong: dead concrete gray,
sickly pale cyan-white fluorescent light, wet asphalt blue-black, and living
black ink. NO mossy green, NO warm earth brown, almost no warm accent — a world
drained of life.

Scene: a modern city street at dawn, eye level. Shop signboards, a bookshop
window display, a road sign and a bus stop board are all COMING APART — the
marks on them crack into small angular geometric fragments that peel off,
tumble through the air, and dissolve into curling tendrils of glossy black ink
smoke. Loose book pages and leaflets blow down the street, their surfaces
already blank. In the middle ground, ordinary modern people stand frozen and
helpless, mouths open, gesturing at one another — speech has stopped working;
from one or two of them, small broken fragments drift out of their mouths and
crumble. A young modern man in a gray hoodie and dark jeans stands left of
center with his back to us, small against the street, staring up at a
disintegrating signboard — the only one still trying to read.

Composition: strong left-to-right depth, the street receding toward the right.
Keep the top and bottom edges clear of important elements (cropped to 16:9),
bottom fifth calm for a text box.

Absolutely no text, letters, numbers, or letter-like shapes anywhere — every
sign, book and page carries only abstract angular marks or blank surfaces.
```

- **담아야 할 것**: 깨진 글자 파편이 먹 연기로 흩어지는 게임의 핵심 비주얼(간판·책·표지판 3종에서 동시에), 소통 실패한 사람들, 뒷모습의 주인공(작게), 차가운 색만.
- **담지 말아야 할 것**: 이끼 초록·흙 갈색(3컷을 위해 아껴둔다), 따뜻한 액센트, 폭발·비명 같은 액션 톤(앵커의 "tranquil" 위반), 실제 글자, 대마왕·먹물귀의 실체(오프닝에서 악당을 보여주지 않는다 — 먹 연기만이 그의 지문).
- 한국어 설명: 세상에서 글자가 뜯겨 나가는 장면. 이 게임이 무엇에 관한 게임인지를 첫 3초에 알려주는 컷이며, 유일하게 조선이 아니므로 색이 노골적으로 이질적이어야 한다.

### 프롬프트 19 — [파일명: opening_02_timemachine.png / 오프닝 컷2: 타임머신 (모드 C)]

```
같은 모드 C 컷신 시리즈로, 같은 문법(아웃라인·셰이딩·왼쪽 광원·픽셀 밀도)으로
두 번째 컷을 생성해줘.

Panel 2 of 4 in the same pixel art cutscene series. 1536x1024 (landscape, will
be cropped to 16:9). Same style grammar as panel 1: 16-bit pixel art, chunky
clearly visible pixels, crisp pixel clusters, limited palette, dark warm brown
single-color outline (never pure black), cel shading with one shadow step and
one highlight step, gentle mid-value contrast, soft directional light from the
LEFT, eye-level camera, tranquil mood.

Palette for THIS panel — the cold modern palette is starting to break: still
dominated by dead concrete gray, pale cyan-white fluorescent and wet asphalt
blue-black, but a warm amber-brown glow now bleeds out from the machine's core
and a first hint of misty blue-gray appears in its vortex. Roughly a quarter of
the panel has turned warm.

Scene: a cluttered basement laboratory, eye level. A hand-built time machine: a
rounded capsule of riveted metal plates, exposed cabling, a round glass panel,
and a ring of coils spinning up a swirling vortex of amber and misty blue-gray
light inside the open hatch. The same young modern man in a gray hoodie and
dark jeans stands left of center, seen from behind, one hand on the hatch frame,
one foot already inside, looking into the vortex. Broken angular geometric
fragments and tendrils of black ink smoke drift in from the LEFT edge of the
frame and are being SUCKED INTO the vortex, spiraling as they go — the machine
is pulling the disaster in along with him. On the wall behind him, taped-up
papers and a corkboard are blank, their marks already eaten away.

Composition: the machine on the right, the man reading left-to-right into it;
the vortex is the brightest point and the only real warmth in the frame. Keep
the top and bottom edges clear of important elements (cropped to 16:9), bottom
fifth calm for a text box.

Absolutely no text, letters, numbers, or letter-like shapes anywhere — papers,
dials and control panels carry only abstract marks.
```

- **담아야 할 것**: 소용돌이로 빨려드는 파편(1컷의 파편이 어디로 가는지 = 시선 유도), 왼쪽에서 들어와 오른쪽 기계로 향하는 동선, 첫 따뜻한 빛(호박색 = 앞으로 올 조선색의 예고), 손수 만든 티가 나는 조악한 기계.
- **담지 말아야 할 것**: 매끈한 SF 하이테크(장르가 어긋난다 — 리벳·노출 배선의 수제 느낌이어야 한다), 조선 요소, 주인공 얼굴(아직 익명), 번쩍이는 형광 라이트닝.
- 한국어 설명: 주인공이 과거로 떠나는 컷. 소용돌이의 호박색이 이 시리즈에서 처음 등장하는 따뜻한 색이며, 여기서부터 팔레트가 조선 쪽으로 넘어가기 시작한다.

### 프롬프트 20 — [파일명: opening_03_arrival.png / 오프닝 컷3: 조선 도착 (모드 C)]

```
같은 모드 C 컷신 시리즈로, 같은 문법으로 세 번째 컷을 생성해줘. 이 컷에서
팔레트가 조선 쪽으로 넘어간다.

Panel 3 of 4 in the same pixel art cutscene series. 1536x1024 (landscape, will
be cropped to 16:9). Same style grammar: 16-bit pixel art, chunky clearly
visible pixels, crisp pixel clusters, limited palette, dark warm brown
single-color outline (never pure black), cel shading with one shadow step and
one highlight step, gentle mid-value contrast, soft dawn light from the LEFT,
eye-level camera, tranquil ink-wash mood.

Palette for THIS panel — the Joseon anchor palette now takes over: desaturated
mossy green, wet earth brown, misty blue-gray, soft dawn light from the left,
with a gentle warm amber accent. The ONLY cold, out-of-palette thing left in
the frame is the young man's modern clothing (gray hoodie, dark jeans, white
sneakers) — keep it noticeably cooler, grayer and flatter than everything
around it, as if this world has not colored him in yet.

Scene: a quiet Joseon dynasty countryside path at dawn. Layered misty blue-gray
mountain ridges far in the background, a pine forest silhouette in the middle
distance, and a dirt path with mossy green grass verges and wet earth brown
banks in the foreground. Beside the path stand a weathered carved wooden
village guardian pole (jangseung) with a comically stern face and a small
stacked stone cairn. The young modern man stands left of center in three-quarter
back view, dropped in the middle of the path, one hand shading his eyes,
looking toward the right where a distant palace roofline floats in the dawn
haze — completely out of place. A faint circular scorch of settling amber light
on the ground around his feet marks where he arrived. A few broken angular
geometric fragments drift slowly in the still air — calmer and fewer than
before, now catching the warm dawn light; a single thin wisp of black ink smoke
lingers on the far horizon.

Composition: left-to-right depth, the man on the left, the palace on the right
horizon. Keep the top and bottom edges clear of important elements (cropped to
16:9), bottom fifth calm for a text box.

Absolutely no text, letters, numbers, or letter-like shapes anywhere — the
jangseung and any marker carry only abstract worn marks.
```

- **담아야 할 것**: 앵커 팔레트 전면 등장(인게임 룩과 처음으로 일치 — 여기서 플레이어는 "이제 이 세계다"를 안다), 현대복만 차갑게 남는 대비, 오른쪽 지평선의 궁궐 예고(4컷으로의 시선 유도), 잦아든 파편 + 지평선의 먹 연기 한 줄(위협은 여기까지 따라왔다).
- **담지 말아야 할 것**: 주인공의 한복(아직 갈아입지 않았다 — 4컷 대사의 전제), 인물·군중(도착의 고립감), 차가운 현대색(주인공 옷 제외), 폭발·충격파 같은 도착 이펙트.
- 한국어 설명: 팔레트가 뒤집히는 지점. 배경은 완전히 조선인데 주인공만 색이 안 맞아서, 대사 없이도 "쟤 여기 사람 아니다"가 읽힌다.

### 프롬프트 21 — [파일명: opening_04_sejong.png / 오프닝 컷4: 세종 만남 (모드 C)]

```
같은 모드 C 컷신 시리즈로, 같은 문법으로 마지막 네 번째 컷을 생성해줘.

Panel 4 of 4 in the same pixel art cutscene series. 1536x1024 (landscape, will
be cropped to 16:9). Same style grammar: 16-bit pixel art, chunky clearly
visible pixels, crisp pixel clusters, limited palette, dark warm brown
single-color outline (never pure black), cel shading with one shadow step and
one highlight step, gentle mid-value contrast, soft dawn light from the LEFT,
eye-level camera, tranquil ink-wash mood.

Palette for THIS panel — the Joseon anchor palette, complete: desaturated mossy
green, wet earth brown, misty blue-gray, warm amber lamplight, plus deep indigo
and muted vermilion accents from the painted woodwork. Exactly ONE thing in the
frame is still off-palette: the young man's cold gray modern clothing. Make that
clash obvious — it is the point of the shot.

Scene: inside a grand Joseon palace hall at dawn, warm light spilling through
paper-screen doors on the LEFT. On the right, a dignified Joseon king in his
forties — round kind face, thin beard, calm intelligent eyes — wearing an ornate
royal robe and a black winged official cap, seated behind a low writing desk
with a brush, an inkstone and blank paper scrolls. He has just looked up and is
gesturing lightly toward the visitor's clothes with an amused, curious
expression, one eyebrow raised — not angry, genuinely puzzled. On the left, the
young modern man stands in profile facing him, in his gray hoodie, dark jeans
and white sneakers, stiff and awkward, one hand rubbing the back of his neck —
his cold gray clothing glaringly wrong against the warm hall. Painted wooden
beams overhead, a folding screen with abstract landscape brushwork behind the
king, one small broken angular geometric fragment floating quietly above the
desk between the two men, and a single faint wisp of black ink smoke slipping
out through the far right doorway.

Composition: a two-shot — man on the left, king on the right, facing each other
across the frame with clear space between them. Keep the top and bottom edges
clear of important elements (cropped to 16:9), bottom fifth calm for a text box.

Absolutely no text, letters, numbers, or letter-like shapes anywhere — the
scrolls, folding screen and desk must be blank or carry only abstract brush
marks.
```

- **담아야 할 것**: 왕의 시선/손짓이 주인공의 옷을 정확히 가리킬 것(대사 "그 이상한 옷…"의 근거), 놀림기 섞인 호기심(분노 아님 — 전연령 톤), 두 사람 사이의 파편 한 조각(둘을 잇는 이유), 오른쪽 문으로 빠져나가는 먹 연기 한 줄(1컷의 위협이 조선에도 있다 → 본편 시작).
- **담지 말아야 할 것**: 세종 실명·어진 모사(아래 팁 참조), 신하·호위 군중(2인 대화의 밀도가 깨진다), 위압적 옥좌 정면 구도(만남이 아니라 알현이 되어버린다), 주인공의 한복, 실제 글자.
- 한국어 설명: 오프닝의 마침표이자 본편의 시작. 3컷부터 유지해온 "주인공 옷만 색이 안 맞는다"는 장치를 왕이 말로 짚어주면서, 다음 장면의 한복 환복으로 자연스럽게 넘어간다.

---

### 4컷 연속성 장치 (Continuity devices)

한 장씩 보면 딴 장면이지만 붙여 보면 한 흐름이 되도록, 아래 6개를 프롬프트에 심어 두었다.

| 장치 | 컷1 | 컷2 | 컷3 | 컷4 |
|---|---|---|---|---|
| **① 깨진 글자 파편** (핵심 모티프) | 간판에서 뜯겨 폭풍처럼 흩날림 | 소용돌이로 빨려듦 | 잦아들어 몇 조각 표류 | 책상 위 한 조각 정지 |
| **② 먹 연기** (악당의 지문) | 화면 가득 | 파편과 함께 빨려듦 | 지평선에 한 줄 | 오른쪽 문으로 한 줄 빠져나감 |
| **③ 팔레트 이행** | 차가운 현대 100% | 현대 75% + 호박 25% | 앵커 85% | 앵커 100% |
| **④ 주인공 현대복** (미수렴 색) | 회색 후디 (배경과 동색) | 회색 후디 | **혼자 차갑게 남음** | **왕이 지적함 → 해소** |
| **⑤ 왼쪽 광원** | 형광 청백색 | 왼쪽에서 파편 유입 | 새벽빛 | 문살 사이 아침빛 |
| **⑥ 좌→우 시선 유도** | 거리가 오른쪽으로 후퇴 | 왼쪽 인물 → 오른쪽 기계 | 왼쪽 인물 → 오른쪽 지평선의 궁 | 왼쪽 인물 → 오른쪽 왕 (동선 종결) |

- **①②는 게임의 비주얼 아이덴티티**다. 4컷 내내 같은 모양(각진 기하 파편 + 광택 있는 먹 연기 넝쿨)을 유지해야 타이틀 키아트 및 인게임의 먹 얼룩 연출과 연결된다.
- **④가 이 오프닝의 논리 축**이다. 색이 안 맞는 옷 → 왕이 지적 → 환복. 팔레트 불일치가 곧 플롯이므로, 컷3·4에서 주인공 옷이 조선색으로 물들면 오프닝이 무너진다. 재생성 판정 시 이 항목을 1순위로 볼 것.
- **⑥ 왼쪽 인물 → 오른쪽 목표**가 4컷 고정. 컷4에서 목표가 사람(왕)이 되면서 전진 운동이 멈추고 대화가 시작된다.

### 오프닝 컷신 전용 팁

- **세종은 이름으로 부르지 말 것.** 프롬프트에 "King Sejong"을 쓰면 ① 생성 거부 ② 표준영정 모사 ③ 만원권 초상 흉내 중 하나가 나오기 쉽다. "a dignified Joseon king in his forties, round kind face, thin beard, ornate royal robe, black winged official cap"처럼 **묘사로만** 지정하면 원하는 얼굴이 나온다. 위 프롬프트는 그렇게 되어 있다 — 임의로 이름을 넣지 말 것.
- **컷1 팔레트에 조선색이 섞여 나올 때**: "Panel 1 must contain NO green and NO warm brown at all. Only concrete gray, cyan-white, asphalt blue-black and ink. Repaint." 컷1이 이질적이지 않으면 4컷 이행 설계 전체가 무의미해진다.
- **컷3·4에서 주인공이 한복을 입고 나올 때** (가장 잦은 실패): 모델이 "조선 배경 = 한복"으로 자동 보정한다. → "The young man must stay in a modern gray hoodie, dark jeans and white sneakers. He has NOT changed clothes. Keep his clothing cold gray while everything else stays warm." 로 재요청.
- **도트가 뭉개져 그냥 일러스트로 나올 때**: 모드 B 팁과 동일 — "Make the pixels much larger and clearly visible, like a real SNES-era cutscene. No smooth anti-aliased edges." 컷신은 축소하지 않고 전체화면으로 띄우므로 **모드 B(아이콘)보다 픽셀 뭉개짐에 더 엄격해야 한다**.
- **4컷 일관성**: 반드시 한 대화에서 17 → 18 → 19 → 20 → 21 순서로. 중간에 다른 모드(A/B)를 끼워 넣지 말 것. 특정 컷만 재생성할 때는 앞뒤 컷을 첨부하고 "Match the palette and pixel density of the attached panels"를 덧붙인다.
- **크롭**: 1536x1024 → 위아래를 잘라 1536x864(16:9). 하단 1/5은 대사창이 덮으므로, 크롭 시 인물의 눈높이가 상단 1/3 근처에 오도록 잡으면 안정적이다.
- **저장·출처**: `ArtDrop/`에 위 파일명으로 저장하고 `ArtDrop/출처.md`에 "파일명 / GPT-4o 생성 / 날짜 / 프롬프트 번호" 기록.

---

## 부록 — 최소 실무 팁

- **저장 위치·파일명**: 프로젝트 루트 `ArtDrop/`에 각 프롬프트 표기 파일명(소문자 스네이크케이스)으로 저장. 아이템 파일명의 `w_`/`a_` 식별자는 `Assets/Resources/Items/items.json`의 id와 일치한다. 검수 후 필요한 것만 `Assets/Art/`로 이동. 사천왕 이름·컨셉은 기획상 미확정(스토리기획.md 4.5절)이므로 파일명은 임시 식별자다.
- **왜 이미지에 텍스트 금지인가**: GPT-4o는 이미지 안의 한글을 거의 반드시 깨뜨린다. 타이틀 로고 등 모든 텍스트는 Unity에서 폰트로 얹는다.
- **플랫 벡터로 나올 때 (키아트 실패)**: "This looks like flat vector art. Repaint the same scene as a highly detailed painterly digital illustration — rich textures, visible brushwork, dramatic lighting, atmospheric depth." 로 재요청. 반복되면 질감 형용사(weathered, intricate, layered)를 대상마다 붙여 준다.
- **타원형 공백 아티팩트 (투명 배경 요청 시)**: 오브젝트 뒤에 타원형 접시/비네트 모양의 빈 영역이 생기면 → "Do not place the object on an oval plate, disc, or vignette. The surroundings must be fully transparent with nothing behind the object." 로 재요청.
- **투명 배경 실패 시**: 결과에 체커보드 무늬가 *그려져* 있으면 실패 → "The background must be actually transparent alpha, not a drawn checkerboard pattern." 으로 재요청.
- **도트가 뭉개질 때 (모드 B 실패)**: 픽셀이 안 보이고 매끈하게 나오면 → "Make the pixels much larger and clearly visible, like a real SNES-era sprite. No smooth anti-aliased edges." 실사용 시에는 어차피 축소하므로 픽셀 격자가 완벽히 균일하지 않아도 된다.
- **비율**: GPT-4o의 안정 해상도는 1024x1024 / 1536x1024 / 1024x1536뿐. 타이틀(프롬프트 1)은 1536x1024로 받은 뒤 위아래를 크롭해 16:9(1536x864)로 사용.
- **일관성**: 반드시 한 대화에서 순서대로 진행 — 키아트군(1~6)을 먼저 끝내고 도트군(7~16)으로 넘어간다(스타일 모드를 오가면 섞임 위험). 새 대화를 열어야 하면 잘 나온 기존 이미지를 첨부하고 프롬프트 0을 다시 붙여넣은 뒤 "Match the art style of the attached image"를 추가.
- **리트라이 일반**: 구도만 아쉬우면 "Keep everything the same, but ..." 식 부분 수정이 일관성에 유리. 2~3회로 안 잡히는 요소는 빼는 쪽이 성공률이 높다.
- **출처 기록**: 생성 후 `ArtDrop/출처.md`에 "파일명 / GPT-4o 생성 / 날짜 / 프롬프트 번호"를 한 줄씩 기록 (스토어 등록 시 AI 생성물 고지 대비).
