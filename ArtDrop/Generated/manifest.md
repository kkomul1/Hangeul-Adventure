# ArtDrop/Generated — PixelLab 생성 에셋 매니페스트

생성일: 2026-07-16 | 도구: PixelLab MCP (api.pixellab.ai) | 세션 생성 소모: **10회** (트라이얼 잔여 29회)
용도: Ninja Adventure 임시 에셋 교체용 조선풍 도트 에셋 (Unity 6 URP 2D, PPU 16, Point 필터)

## 파일 목록

| 파일 | 크기 | 내용 |
|---|---|---|
| `tileset_grass_dirt.png` | 64×64 | Wang 타일셋 A: 풀밭(lower) ↔ 흙길(upper), 16px 타일 4×4 그리드 |
| `tileset_grass_dirt_metadata.json` | - | A 타일 배치 메타데이터 (corner/bounding_box 원본) |
| `tileset_grass_water.png` | 64×64 | Wang 타일셋 B: 풀밭(lower) ↔ 연못물(upper). **물 색을 로컬 후처리로 파랑 치환한 버전** (원본은 민트색이라 물로 안 읽힘) |
| `tileset_grass_water_original_mint.png` | 64×64 | B의 PixelLab 원본 (민트그린 물). 참고용 |
| `tileset_grass_water_metadata.json` | - | B 타일 배치 메타데이터 |
| `tiles_grass_dirt/` (16장) | 각 16×16 | A 개별 타일 (아래 배치표의 파일명) |
| `tiles_grass_water/` (16장) | 각 16×16 | B 개별 타일 (A와 동일 배치) |
| `bush_wall_32.png` | 32×32 | 수풀 벽 (통행 불가). 캔버스 꽉 채운 블록형, 배경 투명 |
| `player_idle_south/east/north/west.png` | 각 24×24 | 선비 4방향 idle (정지 1프레임 = 회전 스프라이트) |
| `player_idle_sheet_96x24.png` | 96×24 | idle 시트: 열 순서 남·동·북·서 |
| `player_walk_frames/` (16장) | 각 24×24 | 걷기 개별 프레임. `walk_{방향}_{0~3}.png`. 서쪽은 `walk_west_{n}_mirrored.png` (동쪽 좌우반전, 로컬 생성) |
| `player_walk_sheet_96x96.png` | 96×96 | 걷기 시트 4×4 (아래 행·열 구성) |

## 타일 배치 (두 타일셋 공통, 4×4 그리드)

코너 코드 = NW·NE·SW·SE 순서. **L(lower)=풀밭**, **U(upper)=A는 흙길 / B는 물**.
`base_LLLL` = 순수 풀밭, `base_UUUU` = 순수 흙길(A)/순수 물(B). 나머지 14장은 전이 타일.

| 그리드(열,행) | 파일명 | 코너 (NW NE SW SE) |
|---|---|---|
| 0,0 | wang13_UULU | U U L U |
| 1,0 | wang10_ULUL | U L U L |
| 2,0 | wang04_LULL | L U L L |
| 3,0 | wang12_UULL | U U L L |
| 0,1 | wang06_LUUL | L U U L |
| 1,1 | wang08_ULLL | U L L L |
| **2,1** | **base_LLLL** | **순수 풀밭** |
| 3,1 | wang01_LLLU | L L L U |
| 0,2 | wang11_ULUU | U L U U |
| 1,2 | wang03_LLUU | L L U U |
| 2,2 | wang02_LLUL | L L U L |
| 3,2 | wang05_LULU | L U L U |
| **0,3** | **base_UUUU** | **순수 흙길/물** |
| 1,3 | wang14_UUUL | U U U L |
| 2,3 | wang09_ULLU | U L L U |
| 3,3 | wang07_LUUU | L U U U |

- 코너 기반(corner Wang) 오토타일이므로 Unity 기본 Rule Tile(변 인접 기준)과는 규칙 정의가 다름. Rule Tile로 쓰려면 코너 매칭 규칙을 수동 설정하거나, 코드에서 4코너 지형값으로 타일을 선택할 것.
- 두 타일셋의 풀밭은 동일 base tile ID로 체인 생성되어 **픽셀 단위로 동일** — 한 맵에서 혼용 가능. 순수 풀밭 타일은 아무 쪽 `base_LLLL` 하나만 쓰면 됨.

## 플레이어 시트 구성

`player_walk_sheet_96x96.png` — 24×24 셀, 4열(프레임 0→3) × 4행:

| 행 | 방향 | 비고 |
|---|---|---|
| 0 (상단) | 남 (정면) | |
| 1 | 동 | |
| 2 | 북 (후면) | |
| 3 (하단) | 서 | 동쪽 프레임 좌우반전 (로컬 미러링, 생성 횟수 0회) |

`player_idle_sheet_96x24.png` — 24×24 셀 1행 4열: 남·동·북·서 (서는 PixelLab 회전 원본).

캐릭터 본체는 16px, 캔버스는 애니메이션 여백 포함 24×24 (PixelLab 자동 +40%). 발이 셀 하단에서 약간 위에 있으므로 피벗은 Bottom 또는 Custom(0.5, 0.1) 권장.

## PixelLab 오브젝트 ID

| 항목 | ID |
|---|---|
| 타일셋 A (풀밭↔흙길) | `de4267ab-77cd-495c-85c0-8c51a5e28512` |
| 타일셋 B (풀밭↔물) | `9808e996-de10-4cf4-8bc3-25776f39b305` |
| base tile: 풀밭 (체인용, A·B 공유) | `acd9617e-c028-4d65-b085-d8fba777447e` |
| base tile: 흙길 | `df83c7fd-c557-4e28-b915-adb3dd2c7aaa` |
| base tile: 물 | `b92da61e-4bee-46ce-9477-59fc15256a66` |
| 캐릭터 "Seonbi Player" (4방향, low top-down) | `3f4603f6-59e1-4ee1-8296-a4c97847d866` |
| 걷기 애니메이션 (walking-4-frames) | 남 `8af6cd49-…480016` / 동 `0e3dd307-…bf79c` / 북 `1db57f17-…1ad8bd` |
| 수풀 벽 map object (서버에서 8시간 후 자동 삭제됨, PNG 확보 완료) | `26588d95-813b-4d57-91d8-52024247f58c` |

## 생성 횟수 내역 (총 10회)

| 작업 | 소모 |
|---|---|
| 타일셋 A + 타일셋 B + 캐릭터 생성 + 수풀 (4건 일괄) | 7회 (캐릭터=1 명시, 타일셋이 각 2~3회 소모된 것으로 추정 — 잔액 델타로만 확인됨) |
| 걷기 남 | 1회 |
| 걷기 동+북 | 2회 |
| 걷기 서 | 0회 (동쪽 미러링) |
| 물 색 보정 | 0회 (로컬 팔레트 치환: R<80 & B>38 픽셀만 HSV 색상 +55°, 채도 ×0.85) |

## Unity 임포트 주의점

- 공통: Filter Mode **Point**, Compression **None**, Mip Maps 끔.
- 타일셋: PPU **16**, Sprite Mode Multiple, 16×16 그리드 슬라이스 (또는 `tiles_*` 개별 타일 사용).
- 플레이어: PPU **16**, 24×24 그리드 슬라이스, 피벗 하단. 24px 캔버스라 타일(1유닛)보다 반 칸 크게 보이는 것이 정상 (머리가 타일 위로 살짝 나옴).
- 수풀: 32×32이므로 한 칸(1유닛) 벽으로 쓰려면 PPU **32**로 임포트. 가장자리에 1~2px 투명 요철이 있어 풀밭 타일 위에 겹쳐 배치하는 전제(요철 밑으로 풀이 비침).
- 물 애니메이션 없음(정지 1프레임). 필요 시 색상 치환 스크립트로 2번째 프레임 변형 생성 가능(무료).
