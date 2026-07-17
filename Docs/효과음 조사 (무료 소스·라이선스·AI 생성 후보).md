# 효과음 조사 — 무료 소스 · 라이선스 · AI 생성 후보

조사일: 2026-07-17 / 대상: Steam 상업 출시 게임(HangeulAdventure)
**이 문서는 조사·보고 전용이다. 파일 다운로드·코드 변경 없음.** 라이선스는 바뀌므로 실제 다운로드 시점에 각 사운드 페이지를 재확인할 것.

---

## 0. 현재 상태 요약 (왜 이 조사가 필요한가)

`Assets/Scripts/Game/SfxPlayer.cs`는 효과음 **6개를 전부 `Tone()`으로 실시간 사인파 합성**한다. 오디오 파일을 하나도 쓰지 않는다.
프로젝트 전체에서 `Resources.Load<AudioClip>`은 `BgmPlayer.cs:51` 한 곳뿐이며 BGM 전용(`Resources/Audio/{track}.mp3`)이다.

`Tone()`은 `(주파수, 시작시각, 길이)` 노트를 겹치는 구조라 **노트당 주파수가 고정**이다. 즉 포켓몬 도망 특유의 **연속 피치 스윕("휘리릭")이 구조적으로 불가능**하다. `_flee`가 `null`로 남아 있고(`SfxPlayer.cs:23`) `Play()`가 조용히 무시하는 이유가 이것이다. → **도망 효과음은 파일(또는 스윕 생성 도구)이 필요하다.**

---

## 1. 무료 효과음 소스 라이선스 비교표

| 소스 | 라이선스 | 상업 이용 | 출처 표기 | 게임 embed | 재배포 조건 / 함정 | 비용 |
|---|---|---|---|---|---|---|
| **Kenney.nl** | CC0 | ✅ 가능 | ❌ 불필요 (권장만) | ✅ | 사실상 없음 (퍼블릭 도메인 헌정) | 무료(기부 선택) |
| **Freesound (CC0 필터)** | CC0 | ✅ 가능 | ❌ 불필요 | ✅ | 없음 | 무료 |
| **Freesound (CC-BY 4.0)** | CC BY 4.0 | ✅ 가능 | ⚠️ **필수** (사운드명+업로더+URL+라이선스) | ✅ | 표기만 하면 자유 | 무료 |
| **Freesound (CC-BY-NC)** | CC BY-NC 4.0 | ❌ **불가** | 필수 | ❌ | **상업 게임에 쓰면 안 됨** | 무료 |
| **Sonniss GDC Bundle** | 자체 로열티프리 | ✅ 가능 | ❌ 불필요 ("appreciated, never required") | ✅ 게임 명시 허용 | 개별 사운드 재판매 금지, **AI/ML 학습 금지** | 무료 |
| **Pixabay** | Pixabay Content License | ✅ 가능 | ❌ 불필요 | ✅ | **standalone 재배포·판매 금지** (게임 embed는 해당 없음) | 무료 |
| **Mixkit** | Sound Effects Free License | ✅ 가능 (video games 명시) | ❌ 불필요 | ✅ | 원본 형태 재판매·재배포 금지 | 무료 |
| **ZapSplat (무료)** | Standard License | ✅ 가능 (Games and apps 명시) | ⚠️ **필수** — "Sound effects obtained from https://www.zapsplat.com" | ✅ ("embedded or synchronized with your production") | **"must not be shared with any person" / "must not be distributed in any form"** — embed는 허용되나 조항이 공격적 | 무료 / Gold 기부 시 표기 면제 |
| **OpenGameArt** | 업로드마다 다름 (CC0/CC-BY/CC-BY-SA/GPL) | 라이선스별 | 라이선스별 | 라이선스별 | **CC-BY-SA는 파생물도 SA 전염** — 상업 게임에 주의. 복수 라이선스면 하나만 골라 준수 가능 | 무료 |
| **국립국악원 — 단음** | **KOGL 제1유형(출처표시)** | ✅ 가능 | ⚠️ 필수 (발행연도·기관명·URL·작성자) | ✅ | 변경 자유 | 무료 |
| **국립국악원 — 확장/악구** | **KOGL 제4유형** | ❌ **불가** (상업금지+변경금지) | — | ❌ | **함정: 절대 쓰지 말 것** | 무료 |
| **공유마당** | 저작물마다 다름 (CC/KOGL 혼재) | 개별 확인 | 개별 확인 | 개별 확인 | 항목별로 라이선스가 다름 — 일괄 신뢰 금지 | 무료 |

### 표에 안 들어가는 위험 요소 (중요)
- **Freesound / Pixabay / OpenGameArt는 사용자 업로드 플랫폼이다.** 업로더가 상용 라이브러리 음원을 훔쳐 올린 뒤 CC0로 표시하는 사례가 실재한다. 플랫폼이 CC0라고 표시해도 **업로더에게 권리가 없었으면 그 라이선스는 무효**다. Steam 상업 출시라면 → 다운로드 시점의 사운드 페이지 스크린샷·업로더·URL·라이선스를 기록해 두는 것이 최소 방어다.
- **Kenney / Sonniss는 단일 저작자·큐레이션 배포**라 이 위험이 사실상 없다. 그래서 아래 1순위가 Kenney다.
- ZapSplat 라이선스 전문은 2021-06-30자 PDF 미러로 확인했다. 현행 웹 페이지가 403이라 직접 대조하지 못했으므로 실제 사용 전 재확인 필요.

---

## 2. 추천 1순위와 이유

### 사이트 1순위: **Kenney.nl (CC0)**
- **이유 1 — 라이선스가 가장 깨끗하다.** CC0 + 단일 저작자 배포 = 출처 표기 의무 없음, 재배포 조항 없음, 업로더 권리 사기 위험 없음. Steam 출시 게임에서 라이선스 리스크를 0으로 만드는 유일한 조합.
- **이유 2 — 톤이 맞는다.** 이 게임은 국악+칩튠 하이브리드다. Kenney의 `Interface Sounds`(100개), `UI Audio`(50개), `Digital Audio`(60개)는 전부 레트로/디지털 UI 톤이라 현재의 절차 합성 사인파와 이질감이 적다.
- **이유 3 — 팩 단위라 일관성이 유지된다.** 한 팩에서 여러 개를 가져오면 음색·음압이 자동으로 통일된다. Freesound에서 개별 수집하면 톤이 제각각이라 후처리가 필요하다.
- **이유 4 — 크레딧 문서를 안 만들어도 된다.** CC-BY(ZapSplat, Freesound CC-BY)를 쓰면 게임 내 크레딧 화면을 유지·관리해야 하고, 사운드가 늘 때마다 갱신해야 한다.

### 사이트 2순위: **Freesound (CC0 필터 고정)** — Kenney에 없는 특정 음(도망 스윕, 국악 타악 등)을 보충할 때.
검색 URL에 `&f=license%3A%22Creative+Commons+0%22`를 붙이면 CC0만 나온다. **필터 없이 검색하면 CC-BY-NC가 섞여 들어오므로 반드시 필터를 건다.**

### 그런데 — 도망 효과음에 한해서는 **jfxr / Bfxr로 직접 만드는 게 더 빠르고 정확하다.**
사용자가 "효과음 만들어주는 AI가 있으면 내가 만들어볼게"라고 했는데, **이 게임 톤에서는 AI보다 jfxr이 우월하다:**
- 포켓몬 도망음의 정체는 **급격한 피치 스윕**이다. jfxr/Bfxr의 `Slide` / `Delta slide` 파라미터가 정확히 그것 하나를 위한 슬라이더다. 프롬프트 재굴림 없이 슬라이더 두 개로 5분 안에 나온다.
- **생성물은 100% 사용자 소유다.** jfxr 공식: "any sound you create is entirely yours, and you are free to use it in any way you like, including commercial projects. Attribution is not required." Bfxr 공식: "you have full rights to all sounds made with bfxr, and are free to use them for any purposes, commercial or otherwise." → 라이선스 리스크 0, 비용 0.
- 8-bit/칩튠 원샷은 AI 오디오 모델이 오히려 약한 영역이다(AI는 foley·앰비언스에 강함).
- 게임의 나머지 6개 음이 이미 절차 합성 사인파다. jfxr 출력이 톤 매칭이 가장 잘 된다.

> **결론:** 칩튠 계열 원샷(도망·회전·버튼·코인) = **jfxr 직접 생성**. 실사·국악 계열(먹물, 종이, 징·장구) = **Kenney CC0 → Freesound CC0 → 국립국악원 단음(KOGL 1유형)**.

---

## 3. 도망 효과음 구체적 후보

### 후보 A (최우선) — jfxr / Bfxr 직접 생성
- jfxr: https://jfxr.frozenfractal.com/ (브라우저, 설치 불필요, WAV 내보내기)
- Bfxr: https://www.bfxr.net/ (브라우저/데스크톱, Apache 2.0, sfxr 기반)
- **레시피(포켓몬풍 "휘리릭"):** `Powerup` 또는 `Blip` 프리셋 → Waveform = Square 또는 Sine → **Frequency 시작 낮게 + Slide(+) 크게** → Repeat speed 살짝 → Duration 0.3~0.5s → Sustain 짧게, Decay 길게. 상승 스윕이 "도망 성공", 하강 스윕이 "도망 실패" 대비로 쓰기 좋다.
- 라이선스: 생성물 전부 사용자 소유. 표기 불필요. 무료.

### 후보 B — Freesound CC0 기성품 (실물 확인 완료)
- **"Escape - Rpg" / colorsCrimsonTears / CC0 / 2.26초 / WAV 96kHz 16bit 스테레오**
  https://freesound.org/people/colorsCrimsonTears/sounds/562293/
  업로더 설명: 사인 징글을 변형해 "RPG 던전 탈출/도망 사운드를 흉내" — 용도가 정확히 일치. 태그: escape, rpg, game-sound, warp. 다운로드 531회.
  ⚠️ 96kHz 스테레오라 Unity 임포트에서 Force To Mono + 44.1kHz 리샘플 권장(현재 `Tone()` 출력이 mono 44100).

### 후보 C — Freesound CC0 검색어 (필터 포함 URL)
아래 URL의 `q=` 부분만 바꾸면 된다. 뒤의 `f=` 필터가 CC0 고정이다.
```
https://freesound.org/search/?q=8bit+slide+up&f=license%3A%22Creative+Commons+0%22
```
추천 검색어: `8bit slide up` / `chiptune warp` / `retro teleport` / `arcade whoosh` / `slide whistle` / `powerup sweep` / `game escape` / `retro jingle`
(참고: `retro warp jingle`, `retro whistle sweep`은 CC0 필터에서 결과 0건이었다. `8bit slide up`은 35건.)

### 후보 D — Kenney CC0 팩
- Digital Audio: https://kenney.nl/assets/digital-audio (60개)
- Interface Sounds: https://kenney.nl/assets/interface-sounds (100개, CC0 확인)
- UI Audio: https://kenney.nl/assets/ui-audio (50개)
→ 스윕/워프 계열이 포함돼 있으나 "도망" 전용음은 아니라 후보 A/B보다 적합도가 낮다.

---

## 4. AI 효과음 생성 도구 비교

| 도구 | 무료 한도 | 무료 티어 상업 이용 | 상업 이용 최소 비용 | 이 게임 적합도 |
|---|---|---|---|---|
| **ElevenLabs Sound Effects** | 10,000 크레딧/월, SFX = **200크레딧/생성** → **월 50개** | ❌ **불가** (Free는 비상업 전용 + ElevenLabs 표기 의무) | **Starter $6/월** = 30,000크레딧 → 월 150개 + 상업 라이선스 | △ — 프롬프트 품질은 최상급이나 8-bit 원샷은 약함. 국악·먹·종이 질감 foley엔 유용 |
| **Adobe Firefly SFX** | 월 무료 크레딧 있음 | 플랜별 상이 — **확인 필요** | 유료 플랜 | △ — 라이선스 콘텐츠 학습이라 저작권 리스크 낮음. 레트로 톤엔 부적합 |
| **Stable Audio** | 무료 티어 있음 | **티어별 상이, 대체로 제한** | 유료 플랜 | ✕ — 긴 텍스처·앰비언스용. 짧은 원샷 SFX엔 부적합 |
| **jfxr** (AI 아님) | **무제한 무료** | ✅ **완전 자유** | $0 | ◎ **1순위** — "any sound you create is entirely yours… including commercial projects" |
| **Bfxr** (AI 아님) | **무제한 무료** | ✅ **완전 자유** | $0 | ◎ — "full rights to all sounds made with bfxr… commercial or otherwise" |
| **ChipTone** (AI 아님) | 무제한 무료 | 상업 가능 (사용 전 재확인) | $0 | ○ — jfxr보다 파라미터가 많음 |

### 판단
- **직접 만들 거면 jfxr이 정답이다.** AI가 아니지만 사용자 요구("내가 만들어볼게")를 가장 잘 충족하고, 이 게임의 칩튠 톤에 맞고, 라이선스가 완벽하고, 공짜다.
- **ElevenLabs를 쓸 거면 반드시 Starter $6 이상**이다. Free 티어 결과물을 Steam 게임에 넣으면 **라이선스 위반**이다. 필요한 효과음이 15개 내외이므로 **한 달 $6 결제 → 몰아서 생성 → 해지**가 현실적이다(월 150개 생성 가능).

---

## 5. 필요 효과음 목록

### 현재 있는 것 (6개, 전부 절차 합성)
| 이름 | 구성 | 호출 위치 |
|---|---|---|
| `_move` | 440Hz 0.06s | `GameController.cs:176, 211` |
| `_compose` | 523→784Hz | `GameController.cs:177, 179` |
| `_split` | 660→440Hz | `GameController.cs:183` |
| `_fail` | 180Hz 0.12s | `GameController.cs:170, 219, 237` 외 |
| `_collect` | 880→1175Hz | `GameController.cs:228` |
| `_clear` | 523→659→784→1047Hz | `GameController.cs:273`, `BattleScreen.cs:209` |

### 우선순위 A — 무음이고 당장 필요
| 효과음 | 상황 | 위치 | 톤 방향 |
|---|---|---|---|
| **도망** | 보스전 도망 버튼 (배선 완료, 현재 무음) | `BattleScreen.cs:200` | **상승 피치 스윕** — jfxr Slide |
| **자음 회수** | 사천왕 격파 보상 | `BattleScreen.cs:211` (`RecoverConsonant`) | 상승 아르페지오 + 반짝 |
| **전투 패배** | `End(false)` — 도망과 같은 경로라 분리 필요 | `BattleScreen.cs:204` | 하강 스윕 |

### 우선순위 B — 다른 음을 재활용 중 (전용음 필요)
| 효과음 | 현재 | 위치 | 문제 |
|---|---|---|---|
| **회전** | `Move()` 재활용 | `GameController.cs:211` | 이동과 구분 불가 — 회전은 핵심 기믹인데 피드백이 이동과 동일 |
| **구매** | `Collect()` 재활용 | `ItemPanels.cs:138` | 퍼즐 수집음과 상점 구매음이 같음 |
| **장착/해제** | `Move()` 재활용 | `ItemPanels.cs:168, 204` | 장착/해제가 같은 소리 |

### 우선순위 C — 무음 (있으면 좋음)
| 효과음 | 상황 | 위치 |
|---|---|---|
| 전투 공격 / 피격 / 데미지 | `ResolveTrial` 후 HP 감소 | `BattleScreen.cs:180` |
| 글자 도감 최초 등록 | `AnimatePop()`만 있고 소리 없음 (Compose 음에 묻힘) | `GameController.cs:189-192` |
| 반절표 칸 열림 | 글자 도감 패널 | `GlyphCodexPanel.cs` (SFX 호출 0건) |
| 맵 이동 발소리 | 현재 `Fail()`만 존재 | `MapWorld.Input.cs:147`, `SideWorld.Input.cs:406` |
| UI 버튼 클릭 | 전역 | `UiFactory.CreateButton` |
| 팝업 열기 / 닫기 | 전역 | `GameApp.Popups.cs` |
| 대화 텍스트 타이핑 | 스토리 씬 | — |
| 코인 획득 | 보상 | `ProgressStore` |
| 스테이지 별 획득 | 클리어 시 | `GameController.cs:273` (Clear에 묻힘) |

**합계: 신규 필요 ~15개.** ElevenLabs Starter($6/월, 150개) 한 달이면 충분하고, jfxr이면 무료다.

---

## 6. 파일 방식 도입 시 SfxPlayer에 필요한 변경 (설계 관점 — 구현 안 함)

**최소 변경 원칙: `BgmPlayer`가 이미 검증한 Resources 규약을 그대로 재사용한다.**
`Assets/Resources/Audio/Sfx/{name}.wav` 경로를 신설하고, `SfxPlayer.Awake()`에서 각 효과음마다 `Resources.Load<AudioClip>("Audio/Sfx/{name}")`을 먼저 시도한 뒤 **null이면 기존 `Tone()` 결과로 폴백**하는 형태가 적절하다. 이 구조의 장점은 세 가지다. (1) 기존 6개 절차음을 하나도 건드리지 않고 파일이 존재하는 것만 점진적으로 덮어쓸 수 있어 롤백이 자유롭다. (2) `BgmPlayer.Play()`의 "클립 없으면 경고 후 현재 유지" 패턴과 규약이 같아 학습 비용이 없다. (3) `Play(AudioClip)`가 이미 `if (clip != null)` 가드를 가지고 있고 `_flee`가 null 전제로 설계돼 있어(`SfxPlayer.cs:23, 66`) 파일 누락 시 안전망이 그대로 작동한다.

**주의할 점 세 가지.** 첫째, **음압 정규화**다. 현재 `Play()`는 `PlayOneShot(clip, 0.5f)` 고정 배율이고 `Tone()`은 진폭 0.5로 만든 사인파라 음압이 낮다. 상용 라이브러리 WAV는 대개 0dBFS 근처로 노멀라이즈돼 있어 **파일음만 2~3배 크게 튄다.** 효과음별 배율 테이블(또는 `Fail()`처럼 개별 볼륨 인자)이 필요하다. 둘째, **임포트 설정 통일**이다. 후보 B("Escape - Rpg")는 96kHz 스테레오인데 `Tone()` 출력은 44.1kHz 모노다. SFX용 프리셋(Force To Mono ✔, Load Type = Decompress On Load, Preload Audio Data ✔, Sample Rate Setting = Override 44100, Compression = PCM 또는 ADPCM)을 정해 폴더 단위로 적용해야 재생 지연과 음색 불일치를 막는다. 셋째, **Resources의 한계**다. Resources 폴더는 빌드에 무조건 전량 포함되고 동기 로드라 SFX가 수십 개로 늘면 초기화 비용이 커진다. 다만 현재 예상 규모(~15개 × 100KB 미만 = 1.5MB 이하)에서는 Addressables 도입 비용이 이득보다 크고, BgmPlayer와 규약이 갈라지는 손해가 더 크다. **지금은 Resources, SFX가 50개를 넘으면 그때 재검토**가 합리적이다.

---

## 7. 출처 기록 의무 (라이선스 준수용)

CC-BY / ZapSplat / KOGL 1유형을 쓰기로 결정하면 게임 내 크레딧 화면이 필요하다. 형식 예:
- Freesound CC-BY: `"사운드명" by 업로더 (https://freesound.org/s/{id}/) — CC BY 4.0`
- ZapSplat: `Sound effects obtained from https://www.zapsplat.com` (⚠️ 소셜미디어 크레딧은 금지, 게임 크레딧 페이지에 표기)
- 국립국악원 KOGL 1유형: `발행연도, 국립국악원(https://www.gugak.go.kr), 저작물 작성자명`

**CC0(Kenney/Freesound CC0)와 jfxr 생성물만 쓰면 크레딧 화면 자체가 불필요하다** — 이것이 1순위 추천의 실질적 이득이다.

---

## Sources

- [Freesound - Help - FAQ](https://freesound.org/help/faq/)
- [Freesound - "Escape - Rpg" by colorsCrimsonTears (CC0)](https://freesound.org/people/colorsCrimsonTears/sounds/562293/)
- [Kenney - Interface Sounds (CC0)](https://kenney.nl/assets/interface-sounds)
- [Kenney - Digital Audio](https://kenney.nl/assets/digital-audio)
- [Kenney - UI Audio](https://kenney.nl/assets/ui-audio)
- [Pixabay - Content License](https://pixabay.com/service/license-summary/)
- [Pixabay - License: What is allowed and what is not](https://pixabay.com/blog/posts/pixabay-license-what-is-allowed-and-what-is-not-4/)
- [Mixkit - License](https://mixkit.co/license/)
- [Mixkit - Official Information About Mixkit (LLM info)](https://mixkit.co/llm-info/)
- [ZapSplat - Standard License Agreement](https://www.zapsplat.com/license-type/standard-license/)
- [ZapSplat - Standard License 전문 PDF (2021-06-30자 미러)](https://style.rc3.world/Official%20interval%20music%20/zapsplat-standard-license.pdf)
- [ZapSplat - How to credit us](https://www.zapsplat.com/how-to-credit-us/)
- [OpenGameArt - FAQ](https://opengameart.org/content/faq)
- [Sonniss - GDC Game Audio Bundle License](https://sonniss.com/gdc-bundle-license/)
- [Sonniss - GDC Game Audio Bundle (무료 다운로드)](https://gdc.sonniss.com/)
- [국립국악원 - 저작권정책](http://www.gugak.go.kr/site/homepage/menu/viewMenu?menuid=001007002)
- [국립국악원 - 국악기 디지털 음원 단음 다운로드 (KOGL 제1유형)](https://www.gugak.go.kr/digitaleum/front/monotone/list.do)
- [공유마당](https://gongu.copyright.or.kr/)
- [jfxr (GitHub - ttencate/jfxr)](https://github.com/ttencate/jfxr)
- [jfxr (웹 앱)](https://jfxr.frozenfractal.com/)
- [Bfxr](https://www.bfxr.net/)
- [Bfxr (GitHub - increpare/bfxr)](https://github.com/increpare/bfxr)
- [ElevenLabs - Pricing](https://elevenlabs.io/pricing)
- [ElevenLabs - Sound Effects](https://elevenlabs.io/sound-effects)
- [Adobe Firefly - AI sound effects generator](https://www.adobe.com/products/firefly/features/sound-effect-generator.html)
