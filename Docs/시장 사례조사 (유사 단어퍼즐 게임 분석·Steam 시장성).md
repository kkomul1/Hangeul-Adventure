# 유사 게임 사례 조사 보고서

> HangeulAdventure(한글 자모를 그리드에서 밀어 합성/분해하는 퍼즐 어드벤처, Steam 출시 목표)를 위한 시장·사례 조사.
> 작성일: 2026-07-09. 조사 방법: 웹 검색(Steam, Wikipedia, 개발자 블로그, 언론 기사, 학술 DB).
> 수치 표기 원칙: 공식 발표가 아닌 것은 모두 **(추정)** 으로 표기하고 출처를 명시함.

---

## 1. 한글 자모 조합을 게임 메커닉으로 쓴 사례

### 결론 먼저
**"자모를 그리드 위에서 물리적으로 밀어 합성/분해하는 퍼즐"은 상용 게임으로 확인된 사례가 없다.**
한글 자모를 쓴 게임 자체는 존재하지만, 전부 ① 단어 맞히기(워들류), ② 단어 찾기/조합(스크래블류), ③ 학습용 미니게임의 세 갈래이며, 자모의 **공간 이동·합성·분해 자체를 퍼즐 규칙으로 삼은 게임은 발견되지 않았다.** 우리 콘셉트는 현재 확인 가능한 범위에서 공백 지대다.

### 발견된 사례 목록

| 게임/사례 | 형태 | 플랫폼 | 내용 및 성과 |
|---|---|---|---|
| **꼬들(Kordle)** | 워들류 단어 맞히기 | 웹/모바일 | 자음·모음 6개 조합 단어를 6회 안에 맞히는 한글판 Wordle. 2022년 인플루언서 인증샷 중심으로 MZ 세대에 바이럴. 꼬오오오오들(12자모), 꼬맨틀(단어 유사도) 등 파생작 다수 ([kordle.kr](https://kordle.kr/), [국립한글박물관 웹진](https://www.hangeul.go.kr/webzine/202205/sub3_3.html)) |
| **한글 퍼즐: 단어 찾기** | 자모 연결 단어 조합 | iOS | 주어진 자모를 연결해 2분 내 단어를 최대한 만드는 앱. 표제어 40만 단어 내장 ([App Store](https://apps.apple.com/kr/app/%ED%95%9C%EA%B8%80-%ED%8D%BC%EC%A6%90-%EB%8B%A8%EC%96%B4-%EC%B0%BE%EA%B8%B0/id721510215)) |
| **Let's Learn Korean! Hangul** (2020) | 학습용 미니게임 모음 | Steam | 한글 학습 게임. "Jamo slicer" 등 자모 단위 미니게임 포함. 리뷰 96% 긍정(76개)으로 평가는 좋으나 동시접속 1명 수준의 극소 시장 ([Steam](https://store.steampowered.com/app/1255040/Lets_Learn_Korean_Hangul/), [Steambase](https://steambase.io/games/lets-learn-korean-hangul/steam-charts)) |
| **Hangul Shooter** | 학습용 슈팅 | Steam | 적을 쏘며 한글/일본어 문자를 익히는 2D 게임. 소규모 ([Steam](https://store.steampowered.com/app/2866550/Hangul_Shooter/)) |
| **한컴타자연습** | 타자 게임 | PC | 90년대부터 이어진 타자 연습 게임. 자모 입력 속도가 본질이지 조합 퍼즐은 아님 ([국립한글박물관 웹진](https://www.hangeul.go.kr/webzine/202205/sub3_3.html)) |
| **학술 프로토타입** (2024) | 페이퍼 프로토타입 | 연구 | IEEE 학회에 "Mobile Puzzle Game Paper Prototype to Learn Hangul Writing" 발표 — 한글 쓰기 학습용 모바일 퍼즐의 페이퍼 프로토타입 연구. 상용화 단계 아님 ([IEEE Xplore](https://ieeexplore.ieee.org/document/10762080/)) |
| **특허** (2011) | 특허 출원 | - | "한글 자모를 이용한 낱말 조합 전자게임 방법 및 시스템"(KR20110114084A) — 초성·중성·종성 칸에 낱자를 배치해 조합 합법성을 판정하는 방식. 게임 출시로 이어진 흔적은 확인 안 됨 ([Google Patents](https://patents.google.com/patent/KR20110114084A/ko)) |

### 판단
- 한글 조합성(초·중·종성)을 판정 로직으로 쓰는 아이디어는 특허·학술·워들류에서 반복 등장 → **아이디어 자체는 자연스럽고 검증됨**.
- 그러나 전부 "머리로 단어를 떠올려 입력/선택"하는 형태였고, **소코반처럼 자모를 밀어서 물리적으로 붙이고 떼는 게임은 없다.** 이 차이가 우리 게임의 정체성이다 (언어 지식 게임이 아니라 공간 퍼즐).
- 리스크: 선행 사례가 없다는 것은 "아무도 재미를 증명하지 못했다"는 뜻이기도 하다. CLAUDE.md의 검증 질문("ㄱ과 ㅏ를 움직여 가를 만드는 게 재미있는가")이 그래서 중요하다.

---

## 2. 문자 조작 퍼즐 게임의 성공 사례 분석

### 2-1. Baba Is You (Hempuli, 2019) — 최상위 벤치마크

- **핵심 메커닉**: 규칙 문장("BABA IS YOU") 자체가 보드 위에서 **밀 수 있는 블록**. 규칙을 밀어 재조립하면 세계의 법칙이 바뀜.
- **성과**:
  - IGF 2018 Best Student Game + Excellence in Design, GDC Awards 2020 Best Design + Innovation Award, D.I.C.E. Awards Outstanding Achievement in Game Design ([Wikipedia](https://en.wikipedia.org/wiki/Baba_Is_You))
  - Steam 보유자 100만~200만 명 **(SteamSpy 추정)** ([SteamSpy](https://steamspy.com/app/736260)), Steam 매출 약 590만 달러 **(games-stats/VG Insights 계열 추정, 공식 발표 아님)** ([games-stats](https://games-stats.com/steam/game/baba-is-you/), [VG Insights](https://vginsights.com/game/baba-is-you))
- **핵심 재미**: "게임의 문법 요소를 물리 오브젝트로 만들어 민다"는 단 하나의 발상에서 조합적 깊이가 폭발. 1인 개발, 소박한 그래픽으로도 디자인만으로 글로벌 히트.
- **우리와의 관계**: 구조적으로 가장 가까운 참조점. Baba가 "규칙 단어"를 밀듯 우리는 "자모"를 민다. 언어를 몰라도(Baba의 영어 단어는 기능 아이콘에 가까움) 퍼즐이 성립한다는 점을 증명한 사례.

### 2-2. Bookworm Adventures (PopCap, 2006) — 워드+RPG 전투의 원형

- **핵심 메커닉**: 4x4 타일에서 단어를 만들면 그 길이·희귀도가 공격 데미지가 되는 턴제 전투. 보물(장비)·물약 등 RPG 메타 진행.
- **성과·비용**: 개발 2년 6개월, 개발비 70만 달러 이상(당시 캐주얼 게임 통념 "3명·6개월·10만 달러"의 7배) — GDC 2007 포스트모템에서 공개. Metacritic 82점, D.I.C.E. 2007 "Downloadable Game of the Year" ([Wikipedia](https://en.wikipedia.org/wiki/Bookworm_Adventures), [Game Developer](https://www.gamedeveloper.com/business/video-the-making-of-popcap-s-i-bookworm-adventures-i-)). 구체적 판매량은 미공개.
- **운명**: EA 인수 후 2016년 Steam/Origin에서 판매 중단 — 현재 정식 구매 불가. 프랜차이즈 사실상 사멸.
- **교훈**: ① 단어 만들기→데미지 변환 루프는 검증된 재미. ② 단, RPG 결합은 콘텐츠 물량(15만 줄 코드, 4,500장 이미지, 1만 줄 대사)을 폭증시켜 개발비가 통제 불능이 되기 쉽다. MVP 단계에서 전투는 최소 형태로.

### 2-3. Typoman (Brainseed Factory, 2015) — 절반의 성공, 반면교사

- **핵심 메커닉**: 글자로 이뤄진 캐릭터가 글자를 밀고 옮겨 단어(RAIN, LIE 등)를 만들면 환경이 반응하는 퍼즐 플랫포머.
- **성과**: 플랫폼별 Metacritic 57(Wii U)~75(PS4)로 갈림. Steam 유저 평가는 86% 긍정(769개) ([Metacritic](https://www.metacritic.com/game/typoman/), [Steam](https://store.steampowered.com/app/336240/Typoman/)).
- **교훈**: "글자를 민다"는 콘셉트가 우리와 가장 표면적으로 유사한 상용 게임. 평론가 비판의 요지는 **콘셉트 대비 퍼즐 깊이 부족과 조작감 문제** — 문자 기믹은 시선을 끌지만, 기믹이 조합적 깊이로 이어지지 않으면 "예쁜 원 트릭"으로 끝난다. 우리가 솔버 기반 스테이지 검증을 하는 이유가 여기 있다.

### 2-4. Letter Quest: Grimm's Journey (Bacon Bandit Games, 2014)

- **핵심 메커닉**: 스크래블식 타일로 단어를 만들어 몬스터에게 데미지. 젬으로 무기·스킬 업그레이드.
- **성과**: 소규모지만 Steam 95% 긍정(218개) ([Steam](https://store.steampowered.com/app/328730/Letter_Quest_Grimms_Journey/)). 상업 규모는 인디 니치 수준.
- **교훈**: "실패→업그레이드→재도전" 메타 루프가 워드 전투와 잘 붙는다는 것을 소규모로 증명. 다만 니치를 벗어나지는 못함.

### 2-5. SpellTower (Zach Gage, 2011) / Alphabear (Spry Fox, 2015) — 모바일 워드 퍼즐

- **SpellTower**: 격자에서 단어를 이어 지우는 모바일 워드 퍼즐의 고전. "베스트셀링 모바일 워드 게임"으로 자리매김, 2020년 리메이크 ([App Store](https://apps.apple.com/us/app/spelltower/id1490605957)).
- **Alphabear**: 단어를 만들면 곰이 자라는 워드 퍼즐. **100만 다운로드(개발사 발표)**, Google Play Awards 2016 "Standout Indie" 수상. 성공 요인으로 개발사가 지목한 것은 게임 종료 시 곰이 플레이어의 단어로 만드는 엉뚱한 문장("bear speech")의 **공유 바이럴** ([Game Developer](https://www.gamedeveloper.com/business/spry-fox-s-i-alphabear-i-cracks-the-code-of-mobile-puzzle-popularity), [Spry Fox](https://spryfox.com/our-games/alphabear/)).
- **교훈**: 문자 게임은 "내가 만든 결과물"이 스크린샷 한 장으로 전달되는 장르 — 공유 가능한 순간을 의도적으로 설계하면 마케팅 비용을 대체한다. 꼬들의 국내 바이럴도 같은 패턴.

---

## 3. 워드 게임 + 전투/RPG 결합 — 2020년대 동향

### 3-1. Cryptmaster (Paul Hart & Lee Williams / Akupara Games, 2024)

- 타이핑으로 모든 것(전투·대화·퍼즐)을 조작하는 흑백 던전 크롤러. "SAY ANYTHING"이 셀링 포인트.
- 성과: Metacritic 77, OpenCritic 평론가 85% 추천, Steam 94% 긍정(약 1,600개 리뷰). IGF·BIG Festival·IndieCade 수상 ([Wikipedia](https://en.wikipedia.org/wiki/Cryptmaster), [Steam](https://store.steampowered.com/app/1885110/Cryptmaster/)).
- 단, 출시 6주간 리뷰 1,000개 미만으로 **평단 호평 대비 판매는 니치**였다는 보도 ([GamesRadar+](https://www.gamesradar.com/games/indie-dev-behind-acclaimed-dungeon-crawler-corrects-steam-users-who-think-their-reviews-dont-matter-one-review-certainly-can-make-a-difference/)).
- 교훈: 2020년대에도 "언어 조작 + RPG"는 평단·수상에는 강하다. 그러나 언어 의존이 클수록 로컬라이징이 어려워 시장이 좁아진다(Cryptmaster는 영어 전용에 가까움).

### 3-2. Word Play (Game Maker's Toolkit / Mark Brown, 2025)

- Balatro 구조를 스크래블에 이식한 워드 로그라이크. 유튜버(구독자 160만+) Mark Brown이 **7개월 만에** 개발, 2025년 7월 출시.
- 데모가 Steam Next Fest 주간에 **1주일 20,000명 플레이**(개발자 공개) ([GMTK Substack](https://gmtk.substack.com/p/how-i-made-word-play)). 전작 Mind Over Magnet은 첫 주 1만 장 판매(개발자 공개) ([GMTK Substack](https://gmtk.substack.com/p/what-its-like-to-launch-a-game-on)).
- 교훈(개발자 자술): 모든 결정을 "어느 쪽이 더 빠른가"로 판단하는 스코프 관리, 코어 루프→로그라이크 루프→콘텐츠 순의 계층적 개발. 우리 MVP 전략과 동일한 철학.

### 3-3. 장르 배경: Balatro 이후 워드 로그라이크 붐

- Balatro(2024) 이후 스크래블×로그라이크 조합 게임이 Steam에 다수 등장(Words Can Kill 등). PC Gamer가 2025년 이 흐름을 전수 조사하는 기사를 낼 정도로 포화 시작 ([PC Gamer](https://www.pcgamer.com/games/roguelike/i-spent-2025-digging-through-all-the-word-game-roguelikes-flooding-steam-to-see-if-any-could-capture-balatros-magic-here-are-the-highly-scientific-results/), [Words Can Kill](https://store.steampowered.com/app/1732090/Words_Can_Kill/)).
- 2024년 4월 기준 Steam의 roguelike deckbuilder 태그 게임은 850개 이상 ([Wikipedia](https://en.wikipedia.org/wiki/Roguelike_deck-building_game)).
- 시사점: "영어 단어 만들기 + 전투/로그라이크"는 이미 레드오션 진입. 반면 **비영어 문자 체계의 공간 퍼즐**은 이 붐과 겹치지 않는다.

---

## 4. 한글 학습·한국 문화 소재 게임의 해외 반응

- **한글 학습 게임의 시장 규모**: Let's Learn Korean! Hangul이 대표 사례 — 리뷰 96% 긍정이지만 동시접속 1명 수준 ([Steambase](https://steambase.io/games/lets-learn-korean-hangul/steam-charts)). "한글을 배우고 싶다"는 수요는 실재하고 만족도도 높지만, **'학습 게임'으로 포지셔닝된 순간 시장이 극도로 좁아진다.**
- **한국 문화 소재의 해외 제작 사례 증가**: 프랑스 스튜디오의 조선 배경 미스터리 비주얼 노벨 '수호신', 인도네시아의 K팝 육성 '케이팝 아이돌 스토리즈' 등 한류 확산과 함께 한국 소재 해외 게임이 늘고 있다는 2026년 보도 ([게임동아/다음뉴스](https://v.daum.net/v/20260618161209927)).
- **역방향 사례 '오덕'**: 독일 2인 개발 오리 게임이 한국어 이름 '오덕'을 채택하자 한국 유저가 몰려 국내 무료 순위 상위권 진입 — 한국 유저 커뮤니티는 자국 문화를 존중하는 게임에 화력을 몰아주는 경향 (같은 기사).
- **꼬들의 바이럴**: 한글 자모 게임이 국내에서 인증샷 문화로 퍼질 수 있음을 증명 ([국립한글박물관 웹진](https://www.hangeul.go.kr/webzine/202205/sub3_3.html)).
- 시사점: 해외 시장에서 "한글"은 K-컬처 프리미엄을 얻기 시작한 소재이지만, 검증된 성공 사례는 아직 없다. 국내 시장은 꼬들·오덕 패턴(바이럴 화력)이 초기 부스트로 기대 가능.

---

## 5. 시사점 정리

### 5-1. 포지셔닝

**차별점**
1. **공백 지대**: 한글 자모의 물리적 밀기/합성/분해 퍼즐은 확인된 선행작이 없다. "Baba Is You가 규칙을 밀듯, 우리는 글자를 민다"는 한 문장 피치가 성립한다.
2. **언어 지식 불요 설계가 가능**: 한글 조합은 시각적·기하학적 규칙(ㄱ+ㅏ=가)이므로, 어휘력 게임(스크래블류)과 달리 한글을 몰라도 풀 수 있는 퍼즐로 설계할 수 있다. 이것이 Cryptmaster류의 언어 장벽 문제를 피하는 길.
3. **영어 워드 로그라이크 레드오션과 비껴감**: Balatro 아류 포화 속에서 오히려 신선.

**리스크**
1. **미검증 재미**: 선행작 부재 = 재미 미증명. "ㄱ과 ㅏ를 움직여 가를 만드는 게 재미있는가"를 프로토타입 단계에서 냉정하게 검증해야 함.
2. **해외 유저의 목표 인지 문제**: 한글을 모르는 플레이어에게 "무엇을 만들어야 하는지"를 어떻게 보여줄 것인가(목표 글자 실루엣, 조합 미리보기 등)가 UX의 최대 난제.
3. **니치 × 니치**: 퍼즐(니치) × 한글(니치)의 곱. Cryptmaster처럼 "호평받는 소규모작"에 머물 가능성을 전제로 예산·기간을 잡아야 함.

### 5-2. 참고할 디자인 3가지

1. **Baba Is You — 단일 동사, 조합적 깊이**: 조작은 "민다" 하나뿐인데 규칙 조합으로 깊이가 나온다. 우리도 밀기(이동/합성/분해 통합) 하나에 깊이를 몰아주고, 새 자모·새 조합 규칙을 '새 메커닉'처럼 단계적으로 공개하는 커리큘럼형 스테이지 설계를 따를 것.
2. **Bookworm Adventures / Letter Quest — 만든 글자의 가치 = 전투력**: 조합 결과물(글자·단어의 복잡도)이 데미지·보상으로 환산되는 루프는 20년간 반복 검증됨. 단, Letter Quest처럼 가벼운 메타 업그레이드 수준에서 시작할 것.
3. **Alphabear / 꼬들 — 공유 가능한 순간**: 내가 만든 단어/문장이 스크린샷 한 장으로 웃기거나 자랑스럽게 전달되는 장치(예: 클리어 시 만든 단어들로 생성되는 문장, 오늘의 조합 챌린지)를 초기부터 설계에 포함.

### 5-3. 피할 함정 3가지

1. **'학습 게임' 포지셔닝**: Let's Learn Korean 사례처럼 에듀테인먼트로 보이는 순간 Steam에서 시장이 소멸한다. "한글을 배우게 되는 퍼즐"이어도 마케팅은 철저히 "기발한 퍼즐 게임"으로. 학습 효과는 부가 가치로만 언급.
2. **기믹이 깊이로 이어지지 않는 것(Typoman 함정)**: 문자 기믹의 첫인상은 강하지만, 퍼즐이 얕으면 평가가 갈린다. 솔버 기반 전수 검증으로 스테이지마다 "아하 모먼트"가 있는지 확인하고, 없는 스테이지는 버릴 것.
3. **RPG 결합의 스코프 폭발(Bookworm 함정)**: 전투·스토리·연출은 콘텐츠 물량을 기하급수로 늘린다(PopCap도 70만 달러). Word Play의 "모든 결정을 개발 시간 기준으로" 원칙을 따라, MVP에서는 전투를 최소 규칙(조합 결과→데미지 변환)으로 한정하고 퍼즐 코어 검증을 우선할 것.

---

## 부록: 주요 수치 요약표

| 게임 | 연도 | 성과 지표 | 출처 성격 |
|---|---|---|---|
| Baba Is You | 2019 | Steam 보유 100만~200만, 매출 ~$5.9M | 추정 (SteamSpy / games-stats) |
| Bookworm Adventures | 2006 | 개발비 $700k+, Metacritic 82, DICE 수상 | 공식 (GDC 포스트모템) |
| Typoman | 2015 | Metacritic 57~75, Steam 86% 긍정(769) | 공식 집계 |
| Letter Quest | 2014 | Steam 95% 긍정(218) | 공식 집계 |
| Alphabear | 2015 | 100만 다운로드, Google Play 수상 | 개발사 발표 |
| Cryptmaster | 2024 | Metacritic 77, Steam 94% 긍정(~1,600), 판매는 니치 | 공식 집계 + 언론 보도 |
| Word Play | 2025 | 데모 주간 2만 명, 개발 7개월 | 개발자 발표 |
| Let's Learn Korean! Hangul | 2020 | 96% 긍정(76), 동접 ~1명 | 공식 집계 (Steambase) |
