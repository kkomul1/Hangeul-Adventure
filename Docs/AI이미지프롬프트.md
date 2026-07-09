# AI 이미지 생성 프롬프트 (복사-붙여넣기 전용)

ChatGPT(GPT-4o 이미지 생성)에서 사용. **하나의 새 대화**를 열고, 위에서 아래 순서대로 코드블록 내용을 통째로 복사해 붙여넣기만 하면 된다.
생성된 이미지는 프로젝트 루트 `ArtDrop/` 폴더에 각 프롬프트에 표기된 파일명으로 저장한다.

스타일은 두 갈래다 (2026-07-09 사용자 피드백 반영):
- **키아트 (프롬프트 1~6)**: 고퀄리티 디지털 페인팅 일러스트 — 타이틀, 대마왕, 사천왕 4종
- **인게임 요소 (프롬프트 7~16)**: 16비트 도트풍 — 아이템 아이콘 6종, 맵 오브젝트 3종, 파편

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
