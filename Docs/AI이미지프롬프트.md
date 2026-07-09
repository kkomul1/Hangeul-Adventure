# AI 이미지 생성 프롬프트 세트

2026-07-09 작성. ChatGPT(GPT-4o 이미지 생성)로 직접 생성해 프로젝트 루트의 `ArtDrop/` 폴더에 저장하는 용도.
스토리 근거: `Docs/스토리기획.md` (가나다 대마왕이 훈민정음을 조각냄), 아이템 근거: `Assets/Resources/Items/items.json`.

## 0. 공통 스타일 가이드 (모든 프롬프트 앞에 붙일 문단)

인게임은 Ninja Adventure 16px 픽셀아트이므로, 일러스트는 **타이틀·컷씬·UI 아이콘 전용**으로만 쓴다.
픽셀아트와 일러스트가 공존해도 어색하지 않으려면 (1) 채도를 낮춘 흙빛·먹빛 팔레트를 공유하고,
(2) 일러스트 쪽을 "수묵 담채 + 동화책" 톤으로 눌러서 사실적 렌더링을 피하는 것이 핵심이다.
디테일이 과하면 픽셀아트 옆에서 붕 뜬다.

모든 프롬프트 맨 앞에 아래 문단을 그대로 붙인다.

```
Style guide: Cozy storybook illustration of Joseon-dynasty Korea. Soft Korean
ink-wash (sumukhwa) brush textures on warm hanji paper, muted earthy palette
(ivory paper, ink black, faded indigo, persimmon orange, celadon green),
gentle hand-drawn brush outlines, flat soft shading, casual all-ages game art.
No photorealism, no 3D render look, no harsh gradients. Do NOT include any
text, letters, or logos in the image unless explicitly requested.
```

> 참고: 한글/한자를 이미지 안에 그리게 하면 거의 반드시 깨진 글자가 나온다.
> 타이틀 로고 텍스트는 이미지에 넣지 말고 Unity에서 폰트로 얹는다.

---

## 1. 타이틀 화면 일러스트

- **파일명**: `title_art.png`
- **권장 해상도**: 1536x1024 생성(GPT-4o 가로 최대) 후 위아래 크롭해 16:9(1536x864)로 사용. 필요 시 업스케일.
- **한국어 설명**: 저녁 어스름의 조선 마을(기와집·경복궁 실루엣)을 배경으로, 어린 선비 주인공이 언덕에서 마을을 내려다본다. 하늘과 공중에는 균열이 가고 먹물처럼 번져 부서지는 기하학적 파편(깨진 글자의 은유)이 떠다닌다. 로고 자리를 위해 상단 1/3은 비교적 비어 있게.

```
[스타일 가이드 문단] Wide landscape title screen illustration. A young Joseon
scholar boy in a simple white-and-indigo hanbok and black hat (gat), seen from
behind on a grassy hill, looking down at a twilight Korean village with tiled
hanok roofs and a distant palace silhouette. In the dusk sky, mysterious
cracked and shattering geometric fragments drift like broken pieces of
calligraphy dissolving into black ink smoke — ominous but not scary, wondrous
mood. Keep the upper third of the sky relatively empty and calm for a game
logo. Warm lantern lights in the village, soft ink-wash clouds.
```

## 2. 가나다 대마왕 초상 (전투/컷씬용)

- **파일명**: `boss_ganada.png`
- **권장 해상도**: 1024x1536 (세로) — 전투 화면 옆 초상/컷씬 겸용. 정사각이 필요하면 1024x1024로 재생성.
- **한국어 설명**: 먹물로 이루어진 거대한 왕. 도깨비+먹구름 느낌의 설화풍 악당이되, 전연령 게임답게 위협적이지만 귀여운 구석(과장된 눈, 뭉툭한 실루엣)이 있어야 한다. 왕관과 곤룡포를 먹물로 흉내 낸 모습, 손에서 글자 파편이 부서져 흩어진다.

```
[스타일 가이드 문단] Character portrait of a villain king made of living black
ink: a large rotund goblin-like folklore monster (dokkaebi vibe) wearing an
exaggerated royal Korean crown and a tattered dark royal robe that dissolves
into dripping ink smoke at the edges. Big glowing pale eyes, smug grin with
small fangs — menacing yet slightly comical and charming, suitable for an
all-ages puzzle game. One clawed ink hand crushing small glowing geometric
fragments that scatter like torn paper. Dark indigo-and-ink palette with a
single warm accent. Three-quarter view, chest-up portrait, plain hanji-paper
background with ink splashes.
```

## 3. 아이템 아이콘 6종 (상점/인벤토리)

- **공통 스펙**: 투명 배경, 1024x1024 생성 → 게임에서는 128~256px로 축소 사용. 전부 **같은 각도(45도 기울인 대각선 배치), 같은 라이팅, 같은 두께의 붓 외곽선**으로 통일. 한 대화에서 연속 생성할 것(4절 참고).
- **아이콘 공통 접두 문단** (스타일 가이드 뒤에 이어 붙임):

```
Game inventory item icon, single object only, centered, tilted 45 degrees
diagonally, isolated on a fully transparent background (PNG with alpha, no
backdrop, no shadow blob, no frame). Clean bold silhouette readable at small
size, soft flat shading, consistent hand-drawn brush outline.
```

| # | 아이템 | 파일명 | 개별 프롬프트 (공통 접두 뒤에 추가) |
|---|--------|--------|--------------------------------------|
| 1 | 목검 (무기) | `item_w_mokgeom.png` | `A simple Korean wooden practice sword (mokgeom), carved pale wood with visible grain, humble training weapon.` |
| 2 | 무쇠검 (무기) | `item_w_musoe.png` | `A heavy Joseon iron sword with a dark gray blade and simple cord-wrapped hilt, sturdy and plain.` |
| 3 | 은장도 (무기) | `item_w_eunjangdo.png` | `An ornate Korean silver dagger (eunjangdo) with an elegant engraved silver sheath and tassel, refined and precious.` |
| 4 | 무명 도포 (방어구) | `item_a_mumyeong.png` | `A humble white cotton Korean overcoat (dopo) neatly folded or displayed, simple undyed fabric with indigo trim.` |
| 5 | 가죽 배자 (방어구) | `item_a_gajuk.png` | `A Korean leather-padded sleeveless vest (baeja), warm brown leather panels stitched over fabric.` |
| 6 | 두정갑 (방어구) | `item_a_dujeonggap.png` | `A Joseon brass-studded armor coat (dujeonggap), dark fabric with rows of round metal studs and simple plate trim, imposing but not ornate.` |

## 4. 맵 장식 오브젝트 3종

- **공통 스펙**: 투명 배경, 1024x1024. 시점은 인게임(탑다운 3/4 뷰)과 맞춰 **비스듬히 내려다본 3/4 시점**으로 통일. 픽셀 타일맵 위에 놓기보다는 우선 맵 화면 장식/컷씬·지도 UI 용도로 검토 — 픽셀 타일과 나란히 놓을 때 이질감이 크면 다운스케일+감색(포스터라이즈) 후 사용.
- **오브젝트 공통 접두 문단**:

```
Game map decoration object, single object only, centered, viewed from a 3/4
top-down angle (slightly above, like a classic 2D RPG), isolated on a fully
transparent background (PNG with alpha, no ground plane, no backdrop). Clean
silhouette, soft flat shading, consistent brush outline.
```

| # | 오브젝트 | 파일명 | 개별 프롬프트 |
|---|----------|--------|----------------|
| 1 | 장승 | `prop_jangseung.png` | `A Korean jangseung: a tall carved wooden village guardian pole with a comically stern grimacing face, weathered wood texture. No readable letters on it — replace any inscription with abstract worn marks.` |
| 2 | 돌탑 | `prop_doltap.png` | `A small Korean stacked stone cairn (doltap): rounded gray stones piled by travelers for good luck, moss on lower stones.` |
| 3 | 기와집 외관 | `prop_giwajip.png` | `A traditional Korean tiled-roof house (giwajip) exterior: curved dark gray roof tiles, wooden pillars, white-clay walls, small stone step, paper-screen doors.` |

## 5. 훈민정음 파편 아이콘 (수집품)

- **파일명**: `item_fragment_hunmin.png`
- **권장 해상도**: 1024x1024, 투명 배경.
- **한국어 설명**: 찢어진 옛 한지 조각이 은은한 금빛으로 빛나는 수집품 아이콘. 실제 옛한글을 그리게 하면 반드시 깨지므로, 글자는 "흐릿한 붓글씨 자국" 수준의 추상 표현으로 지시한다. 실제 서문 텍스트는 엔딩에서 옛한글 폰트로 렌더링한다.

```
[스타일 가이드 문단] Game collectible item icon, single object, centered,
isolated on a fully transparent background (PNG with alpha). A torn fragment
of ancient Korean hanji paper, aged ivory with ragged burnt-looking edges,
faintly glowing with warm golden light and tiny drifting light motes,
precious sacred-relic feeling. The surface shows only soft blurred abstract
vertical brush-stroke marks suggesting old calligraphy — absolutely no
readable letters or characters. Clean silhouette readable at small size.
```

---

## 6. 실무 팁

### 6.1 투명 배경 / 비율 얻기
- 투명 배경: 프롬프트에 넣는 것에 더해 채팅 지시로도 명시한다 —
  **"Generate this as a PNG with a fully transparent background (alpha channel). No backdrop, no checkerboard pattern, no drop shadow."**
  결과에 체커보드 무늬가 *그려져* 나오면 실패한 것 → "The background must be actually transparent alpha, not a drawn checkerboard pattern."으로 재요청.
- 비율: GPT-4o 이미지는 1024x1024 / 1536x1024 / 1024x1536 세 가지가 안정적이다. "16:9"라고 써도 1536x1024(3:2)로 나오는 경우가 많으므로, **가로형은 1536x1024로 받고 위아래를 크롭해 16:9를 만든다**. 크롭을 전제로 "keep important elements away from the top and bottom edges"를 덧붙이면 안전하다.

### 6.2 시리즈 일관성 유지
- **같은 대화에서 연속 생성**: 아이템 6종, 오브젝트 3종은 각각 한 대화 안에서 이어서 생성한다. 첫 장이 마음에 들면 "Keep exactly the same art style, outline thickness, palette, lighting and angle as the previous image. Now draw: ..."로 이어간다.
- **스타일 재참조**: 새 대화를 열어야 하면, 잘 나온 기존 이미지를 첨부하고 "Match the art style of this attached image"로 시작한다.
- 공통 스타일 가이드 문단은 매 프롬프트마다 다시 붙인다(대화가 길어지면 앞의 지시를 잊는다).

### 6.3 실패 시 리트라이 가이드
- **글자가 그려져 나옴**: "Remove all text and letter-like shapes; replace with abstract brush marks." 한글 자모를 그리게 하는 시도는 하지 않는다(항상 깨짐).
- **너무 사실적/3D틱**: "Flatter shading, more like a hand-painted storybook illustration, less rendering detail."
- **스타일이 튐(애니메/서양풍)**: 스타일 가이드 문단을 다시 붙이고 "Korean ink-wash, NOT Japanese anime, NOT western cartoon"을 추가.
- **구도만 아쉬움**: 전체 재생성보다 "Keep everything the same, but ..." 식 부분 수정 요청이 일관성 유지에 유리하다.
- 2~3회 리트라이로 안 잡히는 요소는 프롬프트를 늘리기보다 **요소를 빼는 쪽**이 성공률이 높다(예: 주인공 포즈 고집 → 뒷모습으로 단순화).

### 6.4 파일명 규칙과 저장 위치
- 저장 위치: 프로젝트 루트 `ArtDrop/` (Unity `Assets/` 밖. 검수 후 필요한 것만 `Assets/Art/`로 이동).
- 규칙: 소문자 + 스네이크케이스, `용도접두어_식별자.png`
  - `title_` 타이틀/키비주얼, `boss_` 보스, `item_` 아이템·수집품(무기 `_w_`, 방어구 `_a_` — items.json의 id와 일치), `prop_` 맵 오브젝트
- 전체 목록: `title_art.png`, `boss_ganada.png`, `item_w_mokgeom.png`, `item_w_musoe.png`, `item_w_eunjangdo.png`, `item_a_mumyeong.png`, `item_a_gajuk.png`, `item_a_dujeonggap.png`, `prop_jangseung.png`, `prop_doltap.png`, `prop_giwajip.png`, `item_fragment_hunmin.png` — 총 12개.
- 라이선스 메모: 생성 후 `ArtDrop/출처.md`에 "GPT-4o 생성, 생성일, 사용 프롬프트 번호"를 한 줄씩 기록해 둔다(추후 스토어 등록 시 AI 생성물 고지 대비).
