# AI 이미지 생성 프롬프트 (복사-붙여넣기 전용)

ChatGPT(GPT-4o 이미지 생성)에서 사용. **하나의 새 대화**를 열고, 위에서 아래 순서대로 코드블록 내용을 통째로 복사해 붙여넣기만 하면 된다.
생성된 이미지는 프로젝트 루트 `ArtDrop/` 폴더에 각 프롬프트에 표기된 파일명으로 저장한다.

---

## 프롬프트 0 — 최초 1회, 대화 시작 시 붙여넣기

```
지금부터 내 게임의 아트 에셋 이미지를 여러 장 만들 거야. 아래 게임 소개와 스타일
가이드를 기억하고, 앞으로 이 대화에서 내가 요청하는 모든 이미지에 이 스타일을
일관되게 유지해줘. 이미지 사이의 화풍, 외곽선 두께, 팔레트, 라이팅이 서로 같아야 해.

[게임 소개]
- 제목: 한글 어드벤처 (Hangeul Adventure)
- 장르: 조선시대를 배경으로 한 전연령 한글 퍼즐 어드벤처 게임
- 스토리: 악당 "가나다 대마왕"(살아있는 먹물로 이루어진 도깨비 왕)이 훈민정음을
  훔쳐 산산조각 내고 파편을 세상에 흩뿌렸다. 세상의 글자들은 깨진 채 먹물처럼
  번져 사라지고 있다. 어린 선비 주인공이 조선 팔도를 여행하며 자음을 되찾고
  훈민정음을 복원한다.
- 만들 이미지: 타이틀 화면, 보스 초상, 상점 아이템 아이콘 6종, 맵 장식 오브젝트
  3종, 수집품 아이콘

[스타일 가이드 — 모든 이미지에 적용]
Cozy storybook illustration of Joseon-dynasty Korea. Soft Korean ink-wash
(sumukhwa) brush textures on warm hanji paper. Muted earthy palette: ivory
paper, ink black, faded indigo, persimmon orange, celadon green. Gentle
hand-drawn brush outlines, flat soft shading, casual all-ages game art.
No photorealism, no 3D render look, no harsh gradients, NOT Japanese anime,
NOT western cartoon. Never include any text, letters, or letter-like shapes
in any image — replace inscriptions with abstract brush marks.

이해했으면 "준비됐어"라고만 답해줘. 이미지는 다음 메시지부터 요청할게.
```

---

## 프롬프트 1 — [파일명: title_art.png / 타이틀 화면 키비주얼]

```
다음 이미지를 생성해줘. 앞서 정한 스타일 가이드를 그대로 유지해.

Wide landscape title screen illustration, 1536x1024 (landscape). A young
Joseon scholar boy in a simple white-and-indigo hanbok and a black horsehair
hat (gat), seen from behind on a grassy hill, looking down at a twilight
Korean village with tiled hanok roofs and a distant palace silhouette. In the
dusk sky, mysterious cracked and shattering geometric fragments drift like
broken pieces of calligraphy dissolving into black ink smoke — ominous but
wondrous, not scary. Warm lantern lights in the village, soft ink-wash clouds.
Keep the upper third of the sky relatively empty and calm for a game logo,
and keep all important elements away from the top and bottom edges (the image
will be cropped to 16:9). No text anywhere in the image.
```

## 프롬프트 2 — [파일명: boss_ganada.png / 가나다 대마왕 초상 (전투·컷씬)]

```
다음 이미지를 생성해줘. 앞서 정한 스타일 가이드를 그대로 유지해.

Character portrait, 1024x1536 (portrait orientation). A villain king made of
living black ink: a large rotund goblin-like Korean folklore monster
(dokkaebi vibe) wearing an exaggerated royal Korean crown and a tattered dark
royal robe that dissolves into dripping ink smoke at the edges. Big glowing
pale eyes, smug grin with small fangs — menacing yet slightly comical and
charming, suitable for an all-ages puzzle game. One clawed ink hand crushing
small glowing geometric fragments that scatter like torn paper. Dark
indigo-and-ink palette with a single warm accent color. Three-quarter view,
chest-up portrait, plain hanji-paper background with ink splashes. No text.
```

## 프롬프트 3 — [파일명: item_w_mokgeom.png / 아이템 아이콘: 목검 (무기)]

```
다음 이미지를 생성해줘. 앞서 정한 스타일 가이드를 그대로 유지하고, 지금부터 만드는
아이템 아이콘들은 전부 같은 각도·라이팅·외곽선 두께로 통일해줘.

Game inventory item icon, 1024x1024, single object only, centered, tilted 45
degrees diagonally. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow blob, no frame). Clean bold
silhouette readable at small size, soft flat shading, hand-drawn brush outline.

Object: a simple Korean wooden practice sword (mokgeom), carved pale wood with
visible grain, humble training weapon.
```

## 프롬프트 4 — [파일명: item_w_musoe.png / 아이템 아이콘: 무쇠검 (무기)]

```
같은 스타일, 같은 각도, 같은 아이콘 규격으로 다음 이미지를 생성해줘.

Game inventory item icon, 1024x1024, single object only, centered, tilted 45
degrees diagonally. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow blob, no frame). Clean bold
silhouette readable at small size, soft flat shading, hand-drawn brush outline.

Object: a heavy Joseon iron sword with a dark gray blade and a simple
cord-wrapped hilt, sturdy and plain.
```

## 프롬프트 5 — [파일명: item_w_eunjangdo.png / 아이템 아이콘: 은장도 (무기)]

```
같은 스타일, 같은 각도, 같은 아이콘 규격으로 다음 이미지를 생성해줘.

Game inventory item icon, 1024x1024, single object only, centered, tilted 45
degrees diagonally. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow blob, no frame). Clean bold
silhouette readable at small size, soft flat shading, hand-drawn brush outline.

Object: an ornate Korean silver dagger (eunjangdo) with an elegant engraved
silver sheath and a small tassel, refined and precious.
```

## 프롬프트 6 — [파일명: item_a_mumyeong.png / 아이템 아이콘: 무명 도포 (방어구)]

```
같은 스타일, 같은 각도, 같은 아이콘 규격으로 다음 이미지를 생성해줘.

Game inventory item icon, 1024x1024, single object only, centered, tilted 45
degrees diagonally. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow blob, no frame). Clean bold
silhouette readable at small size, soft flat shading, hand-drawn brush outline.

Object: a humble white cotton Korean overcoat (dopo), neatly displayed, simple
undyed fabric with indigo trim.
```

## 프롬프트 7 — [파일명: item_a_gajuk.png / 아이템 아이콘: 가죽 배자 (방어구)]

```
같은 스타일, 같은 각도, 같은 아이콘 규격으로 다음 이미지를 생성해줘.

Game inventory item icon, 1024x1024, single object only, centered, tilted 45
degrees diagonally. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow blob, no frame). Clean bold
silhouette readable at small size, soft flat shading, hand-drawn brush outline.

Object: a Korean leather-padded sleeveless vest (baeja), warm brown leather
panels stitched over fabric.
```

## 프롬프트 8 — [파일명: item_a_dujeonggap.png / 아이템 아이콘: 두정갑 (방어구)]

```
같은 스타일, 같은 각도, 같은 아이콘 규격으로 다음 이미지를 생성해줘.

Game inventory item icon, 1024x1024, single object only, centered, tilted 45
degrees diagonally. Fully transparent background (real PNG alpha channel — no
backdrop, no drawn checkerboard pattern, no shadow blob, no frame). Clean bold
silhouette readable at small size, soft flat shading, hand-drawn brush outline.

Object: a Joseon brass-studded armor coat (dujeonggap), dark fabric with rows
of round metal studs and simple plate trim, imposing but not ornate.
```

## 프롬프트 9 — [파일명: prop_jangseung.png / 맵 오브젝트: 장승]

```
다음 이미지를 생성해줘. 앞서 정한 스타일 가이드를 그대로 유지하고, 지금부터 만드는
맵 오브젝트들은 전부 같은 시점·라이팅·외곽선 두께로 통일해줘.

Game map decoration object, 1024x1024, single object only, centered, viewed
from a 3/4 top-down angle (slightly above, like a classic 2D RPG). Fully
transparent background (real PNG alpha channel — no ground plane, no backdrop,
no drawn checkerboard pattern). Clean silhouette, soft flat shading,
hand-drawn brush outline.

Object: a Korean jangseung — a tall carved wooden village guardian pole with a
comically stern grimacing face, weathered wood texture. No readable letters on
it; replace any inscription with abstract worn marks.
```

## 프롬프트 10 — [파일명: prop_doltap.png / 맵 오브젝트: 돌탑]

```
같은 스타일, 같은 시점, 같은 오브젝트 규격으로 다음 이미지를 생성해줘.

Game map decoration object, 1024x1024, single object only, centered, viewed
from a 3/4 top-down angle (slightly above, like a classic 2D RPG). Fully
transparent background (real PNG alpha channel — no ground plane, no backdrop,
no drawn checkerboard pattern). Clean silhouette, soft flat shading,
hand-drawn brush outline.

Object: a small Korean stacked stone cairn (doltap) — rounded gray stones
piled by travelers for good luck, a little moss on the lower stones.
```

## 프롬프트 11 — [파일명: prop_giwajip.png / 맵 오브젝트: 기와집 외관]

```
같은 스타일, 같은 시점, 같은 오브젝트 규격으로 다음 이미지를 생성해줘.

Game map decoration object, 1024x1024, single object only, centered, viewed
from a 3/4 top-down angle (slightly above, like a classic 2D RPG). Fully
transparent background (real PNG alpha channel — no ground plane, no backdrop,
no drawn checkerboard pattern). Clean silhouette, soft flat shading,
hand-drawn brush outline.

Object: a traditional Korean tiled-roof house (giwajip) exterior — curved dark
gray roof tiles, wooden pillars, white-clay walls, a small stone step,
paper-screen doors. No text on any signboard.
```

## 프롬프트 12 — [파일명: item_fragment_hunmin.png / 수집품: 훈민정음 파편]

```
다음 이미지를 생성해줘. 앞서 정한 스타일 가이드를 그대로 유지해.

Game collectible item icon, 1024x1024, single object only, centered. Fully
transparent background (real PNG alpha channel — no backdrop, no drawn
checkerboard pattern, no shadow blob, no frame). Clean silhouette readable at
small size, soft flat shading, hand-drawn brush outline.

Object: a torn fragment of ancient Korean hanji paper, aged ivory with ragged
edges, faintly glowing with warm golden light and tiny drifting light motes —
a precious sacred-relic feeling. The surface shows only soft blurred abstract
vertical brush-stroke marks suggesting old calligraphy. Absolutely no readable
letters or characters of any language.
```

---

## 부록 — 최소 실무 팁

- **저장 위치·파일명**: 프로젝트 루트 `ArtDrop/`에 각 프롬프트 표기 파일명(소문자 스네이크케이스)으로 저장. 아이템 파일명의 `w_`/`a_` 식별자는 `Assets/Resources/Items/items.json`의 id와 일치한다. 검수 후 필요한 것만 `Assets/Art/`로 이동.
- **왜 이미지에 텍스트 금지인가**: GPT-4o는 이미지 안의 한글을 거의 반드시 깨뜨린다. 타이틀 로고 등 모든 텍스트는 Unity에서 폰트로 얹는다.
- **투명 배경 실패 시**: 결과에 체커보드 무늬가 *그려져* 있으면 실패 → "The background must be actually transparent alpha, not a drawn checkerboard pattern." 으로 재요청.
- **비율**: GPT-4o의 안정 해상도는 1024x1024 / 1536x1024 / 1024x1536뿐. 타이틀(프롬프트 1)은 1536x1024로 받은 뒤 위아래를 크롭해 16:9(1536x864)로 사용.
- **일관성**: 반드시 한 대화에서 순서대로 진행. 새 대화를 열어야 하면 잘 나온 기존 이미지를 첨부하고 프롬프트 0을 다시 붙여넣은 뒤 "Match the art style of the attached image"를 추가.
- **리트라이**: 글자가 그려지면 "Remove all text and letter-like shapes", 너무 사실적이면 "Flatter shading, more like a hand-painted storybook illustration", 화풍이 튀면 프롬프트 0의 스타일 가이드 문단을 해당 블록에 다시 붙여 재요청. 구도만 아쉬우면 "Keep everything the same, but ..." 식 부분 수정이 일관성에 유리. 2~3회로 안 잡히는 요소는 빼는 쪽이 성공률이 높다.
- **출처 기록**: 생성 후 `ArtDrop/출처.md`에 "파일명 / GPT-4o 생성 / 날짜 / 프롬프트 번호"를 한 줄씩 기록 (스토어 등록 시 AI 생성물 고지 대비).
