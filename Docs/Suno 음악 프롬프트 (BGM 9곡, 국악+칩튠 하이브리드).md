# Suno 배경음악 프롬프트 (v4.5 기준)

## 사용법
1. Suno **만들기 → 고급(Custom) 모드**, 모델 v4.5
2. **가사 칸은 비워두고** 인스트루멘탈(보이스 없음) 토글 ON
3. 아래 각 곡의 "스타일 프롬프트"를 스타일 칸에 붙여넣기
4. **제외 스타일**(모든 곡 공통): `vocals, singing, lyrics, rap, choir, spoken word, k-pop, EDM drops`
5. 곡당 2번 생성해서 마음에 드는 쪽을 mp3로 다운로드
6. 파일명을 아래 표기대로 바꿔 `ArtDrop\Audio\`에 저장 → Claude에게 신호

공통 사운드 정체성: **국악기(가야금·대금·해금·장구) + 따뜻한 8비트 칩튠** 하이브리드. 모든 곡이 이 조합을 공유해야 게임 전체가 한 앨범처럼 들린다.

**단, 이 조합의 비율은 곡마다 다르다.** 그 비율을 게임 진행도에 따라 움직이는 것이 아래 "국악 그라데이션" 축이다.

---

# 사운드 축 — 국악 그라데이션

2026-07-17 사용자 확정. **현 단계는 기획 반영만이며, 축에 맞춘 신규 곡 생성은 아직 착수하지 않는다.** 기존 11곡은 재생성하지 않고 축 위에 위치만 매핑한다.

> 근거 문서: `스토리기획 (…)` **v2 — 묵음 대왕 세계관** (1.2 인과 구조, 2장 오프닝 4컷, 3장 복장 전환, 10장 엔딩).
> 이 축은 v2 서사에 종속된다. v2가 바뀌면 이 축도 같이 고친다.

## A-1. 개념

현대인 주인공이 타임머신을 타고 조선으로 건너가, 세종의 한글 창제를 도우며 그 세계에 점점 물들어 간다. 음악은 이 변화를 그대로 따라간다.

- **초반 = 칩튠 우세** — 주인공의 소리. 이방인의 귀에 들리는 세계.
- **후반 = 국악 우세** — 조선의 소리. 주인공이 이 세계의 일부가 된 상태.

한 곡 안에서 바뀌는 게 아니라 **곡과 곡 사이에서, 지역을 옮겨 다니는 플레이 시간 전체에 걸쳐** 바뀐다. 플레이어가 진행 중에는 알아차리지 못하다가 엔딩에서 "언제부터 음악이 이렇게 됐지?"라고 느끼면 성공이다.

### 축의 시각적 쌍둥이 = 복장 전환

스토리기획 v2 3장에 **현대복 → 한복** 전환이 이미 확정돼 있다. **이 축은 그 복장 전환의 청각판이다.**

| | 시각 (확정) | 청각 (이 축) |
|---|---|---|
| 오프닝~숲 초입 | 현대복 | 순칩튠 |
| 세종 조우 (x≈4) | **한복으로 전환** | **첫 국악기가 들어오는 지점** |
| 중반~후반 | 한복 | 국악이 리드를 가져감 |
| 엔딩 | 현대로 귀환 | 칩튠으로 회귀 (단, 변형됨 — A-4) |

복장과 음악이 같은 지점에서 같은 방향으로 움직이면 연출이 서로를 보강한다. **구현 시 동기화 지점은 "세종 조우 = 한복 전환" 한 곳으로 충분하다.**

## A-2. 축 정의

**`국악 비중 %`** 하나로 관리한다 (나머지가 칩튠 비중). 이 수치는 측정값이 아니라 프롬프트 작성·판정을 위한 **작업용 눈금**이다.

```
칩튠 우세 ◄──────────────────────────────────────────────────► 국악 우세
국악 비중  0%       30%       45%       60%       80%      100%
         ├─────────┼─────────┼─────────┼─────────┼─────────┤       ┌─────┐
         │ 0 현대  │1 이방인 │ 2 적응  │ 3 동화  │ 4 각성  │5 대치 │ 코다│
         │ 오프닝  │  도착   │사람들과 │ 조선의  │훈민정음 │최종장 │ 귀환│
         │ 현대복  │  직후   │  섞임   │걸음걸이 │ 의 세계 │       │  ↩  │
         └─────────┴─────────┴─────────┴─────────┴─────────┘       └─────┘
                                                          코다는 칩튠으로 회귀
```

**장소 색 편차 규칙**: 구간값은 지역의 **기본선(baseline)**이다. 장소 고유 성격이 축보다 강한 경우 **±15%p까지 편차를 허용**한다. (예: 사찰은 어느 구간에 놓이든 태생적으로 국악이 짙다.) 축을 지키려고 장소를 어색하게 만들지 않는다.

**축은 편도가 아니라 왕복이다**: 스토리기획 v2 10장에서 주인공은 **현대로 돌아온다.** 따라서 음악도 끝에서 칩튠으로 돌아온다 — 단, 같은 칩튠이 아니다 (A-4).

## A-3. 구간별 배분과 기존 11곡 매핑

현재 `Assets/Resources/Audio/`에 실제 임포트되어 연결된 곡은 **11곡**이다. 국악 비중은 각 곡의 프롬프트 문안을 기준으로 추정했다.

| 구간 | 지역 / 상황 | baseline | 기존 곡 | 추정 국악% | 축 정합 |
|---|---|---|---|---|---|
| **0 현대** | 오프닝 컷1~2 (현대·타임머신) | 0~10 | **없음** | — | **곡 부재 — 최우선 공백** |
| **0 현대** | 숲 초입 현대복 구간 (60~90초) | 0~10 | **없음** | — | **곡 부재** |
| 액자 | 타이틀 | — | `bgm_title` | ~55 | 축 밖 |
| 액자? | 오프닝 (대마왕의 습격) | — | `bgm_intro` | ~70 | **✗ v2와 전제 불일치 (주의 4)** |
| **1 이방인** | map_01 시작의 숲 | 30 | `bgm_forest` | ~50 | △ 국악이 20%p 과함 |
| **2 적응** | map_02 시작의 마을 | 45 | `bgm_village` | ~55 | ○ 편차 내 |
| **2 적응** | map_02 보스 · 십자말 훈장 | 45 | `bgm_battle4` | ~65 | △ 번호·강도 역전 (주의 2) |
| **3 동화** | map_03 받침 고개 | 60 | `bgm_pass` | ~65 | ◎ 정합 |
| **3 동화** | map_03 보스 · 받침 사범 | 60 | `bgm_battle1` | ~60 | ◎ 정합 |
| **4 각성** | map_04 자모 사찰 | 80 | `bgm_temple` | ~85 | ◎ 정합 (장소 색과도 일치) |
| **4 각성** | map_04 보스 · 자모 노승 | 80 | `bgm_battle2` | ~65 | △ 칩튠이 과함 |
| **4 각성** | map_05 낱말 저잣거리 | 80 | `bgm_market` | ~75 | ○ 편차 내 **(미임포트 — 주의 1)** |
| **4 각성** | map_05 보스 · 낱말 장사꾼 | 80 | `bgm_battle3` | ~65 | △ 칩튠이 과함 |
| **5 대치** | 최종 결전 (무자의 땅) | 100 | `bgm_final` | ~70* | A-4 참고 |
| **코다** | 엔딩 · 현대 귀환 | 회귀 | **없음** | — | 곡 부재 |

\* `bgm_final`의 칩튠 비중은 **주인공의 칩튠이 아니라 글리치 노이즈**다 — A-4 참고.

**한 줄 요약**: 축의 **중간(구간 2~4)은 11곡으로 이미 꽉 차 있고, 양 끝(구간 0 / 코다)이 통째로 비어 있다.** 그라데이션은 양 끝이 있어야 성립한다.

**주의 1 — `bgm_market` 미임포트**: `map_05.json`이 `"bgm": "bgm_market"`을 지정하고 있으나, 파일이 `ArtDrop/Audio/`에만 있고 `Assets/Resources/Audio/`에는 없다. `BgmPlayer.Play()`는 클립이 없으면 **경고만 남기고 이전 곡을 그대로 유지**하므로, 현재 저잣거리에 들어가도 사찰 곡이 계속 흐른다. 임포트 필요 (이 문서 작업 범위 밖 — 기록만).

**주의 2 — 곡 번호가 진행 순서와 어긋남**: `bgm_battle4`(가장 후반 번호)가 진행상 가장 이른 map_02에 붙어 있고, `bgm_battle1`이 map_03에 붙어 있다. 파일명 번호는 생성 순서일 뿐 진행 순서가 아니다. 축 관점에서는 재배정 후보지만 **이 문서 작업 범위 밖**이므로 기록만 해 둔다.

**주의 3 — 최종 보스 이름**: 스토리기획 **v2에서 `묵음 대왕`(默音大王)으로 확정**됐다. 이 문서의 8번 곡 제목도 그에 맞춰 갱신했다. `bgm_final.mp3`는 이름 중립이라 **파일명 변경 불필요**.

**주의 4 — `bgm_intro`가 v2 오프닝과 어긋남 (재검토 필요)**: 이 곡은 "**대마왕이 조선에 강림**"하는 오프닝을 전제로 쓰였다(웅장한 궁중악·국악 ~70%). 그런데 v2의 오프닝 4컷은 **컷1 현대의 도시 → 컷2 타임머신 → 컷3 조선 도착 → 컷4 세종 만남**으로, **조선 강림 장면이 없고 현대에서 시작한다.**
- 즉 게임의 **첫 60초가 현대**인데, 거기에 깔릴 곡이 국악 70%다 — 축의 출발점과 정반대.
- 선택지: (a) `bgm_intro`를 **컷3~4(조선 도착 이후) 전용**으로 좁히고 컷1~2용 현대 곡을 새로 만든다 → **기존 곡을 버리지 않으므로 권장**. (b) 오프닝 전체를 새 곡으로 교체. (c) 유지하고 축을 포기.
- 확정 필요 — A-6 우선순위 1과 직결된다.

## A-4. 축의 양 끝 — 칩튠의 의미가 두 번 바뀐다

칩튠을 단순히 "초반 음색"으로 두지 않고 **의미를 두 번 뒤집으면**, 그라데이션이 페이드아웃 이상의 서사 장치가 된다. 셋 다 이미 확정된 v2 서사에서 곧바로 따라 나온다.

| 시점 | 칩튠의 의미 | 근거 |
|---|---|---|
| 구간 0~4 | **주인공의 소리** — 이방인의 색. 점점 옅어진다 | v2 3장 복장 전환 |
| **구간 5** | **적의 소리로 전환** — 주인공의 칩튠은 거의 사라졌는데, 묵음 대왕 쪽에서 칩튠이 **글리치로 되돌아온다**. 같은 음색이 "주인공의 색" → "글자를 지우는 오염음"으로 뒤집힌다 | `bgm_final` 프롬프트에 이미 `glitchy 8-bit noise bursts like splattering black ink` 존재. **재생성 없이 성립** |
| **코다** | **집의 소리** — 현대로 돌아오면 칩튠이 되돌아온다. 하지만 이제 그 안에 **국악 잔향이 남아 있다**. 주인공이 조선을 겪고 왔다는 증거 | v2 10장 "현대 귀환", "처음으로 소리 내어 말한다" |

즉 칩튠은 **사라지는 게 아니라 적에게 넘어갔다가, 마지막에 주인공에게 되돌아온다.** 구간 5의 해석은 이미 생성된 `bgm_final`과 맞아떨어지므로 추가 비용이 0이다.

(구간 5·코다 해석은 **제안 단계** — 확정 시 이 괄호를 제거)

## A-5. 구간별 악기 편성 가이드

| 구간 | 국악% | 리드 | 베이스 | 퍼커션 | 칩튠의 역할 |
|---|---|---|---|---|---|
| **0 현대** | 0~10 | 8-bit lead 단독 | pulse / triangle bass | chip noise / drum machine | **전부**. 국악기 0 — 여기서 국악이 들리면 축이 시작을 못 한다 |
| **1 이방인** | 30 | 8-bit square lead | pulse / triangle bass | chip noise hat 중심 | **주역** — 멜로디·화성 전부 |
| **2 적응** | 45 | 칩튠 ↔ 가야금 **교대** (프레이즈 주고받기) | 8-bit bass 유지 | 장구 + chip clap 병존 | **대등** — 해금이 카운터라인으로 답함 |
| **3 동화** | 60 | 해금 / 대금이 **주선율** | 8-bit bass 유지 (칩튠의 마지막 거점) | 장구·북이 리듬 주도 | **반주로 후퇴** — 저음과 아르페지오만 |
| **4 각성** | 80 | 가야금 / 대금 / 피리 | 거문고 저음 · 북 | 장구·모탁·꽹과리 | **텍스처로만 잔존** — 패드·잔향·희미한 아르페지오 |
| **5 대치** | 100 | 태평소 · 피리 · 대금 (궁중악 계열) | 대북 · 저음 드론 | 대북 · 겹장구 | **적의 소리로 전환** — glitch burst, detuned noise (A-4) |
| **코다** | 회귀 | 8-bit lead 복귀 | 8-bit bass | 부드러운 chip perc | **주인공에게 귀환** — 단 가야금/대금이 잔향으로 남음 (A-4) |

구간별 보충:

- **0 현대**: 국악기를 **한 개도 넣지 않는다.** 이 구간의 결백함이 뒤의 전부를 만든다. 단, 음색은 공통 정체성대로 **따뜻한 8비트**(차가운 EDM 아님).
- **1 이방인**: 국악은 **대금 그레이스노트 1~2음**과 약한 뒷박 장구만 — "멀리서 들려오는 낯선 소리" 수준. 국악이 선율을 잡으면 안 된다.
- **2 적응**: `bgm_village` 프롬프트의 `trade phrases`(주고받기) 구조가 이 구간의 표준형.
- **3 동화**: 칩튠 베이스는 남기되 리드 자리는 완전히 내준다. 여기서 칩튠 리드가 남아 있으면 축이 멈춘 것처럼 들린다.
- **4 각성**: 칩튠이 "악기"가 아니라 "공기"가 되는 구간 — 있는지 없는지 애매해야 정상.
- **코다**: 구간 0의 곡을 **같은 선율로 다시 편곡**하면 수미상관이 가장 세게 걸린다. 신규 작곡보다 **구간 0 곡의 변주**로 접근할 것.

프롬프트 작성 시 이 표의 어휘를 그대로 영문으로 옮겨 쓰면 구간 색이 유지된다.

## A-6. 앞으로 메워야 할 구간

축을 그어 보면 **어디가 비어 있는지가 명확해진다. 비어 있는 곳은 중간이 아니라 양 끝이다.**

| 우선순위 | 빈 구간 | 필요한 곡 | 비고 |
|---|---|---|---|
| **1** | **구간 0 (순칩튠)** | 오프닝 컷1~2 (현대·타임머신) + 숲 초입 현대복 구간 | **축의 출발점.** 현재 최저가 `bgm_forest` ~50%로 **칩튠 우세 곡이 한 곡도 없다.** 게다가 v2 오프닝이 현대에서 시작하므로 **서사상으로도 필수** (주의 4). 한 곡으로 두 장면을 겸할 수 있음 |
| **2** | 코다 (칩튠 회귀) | 엔딩 · 현대 귀환 | 구간 0 곡의 **변주**로 만들면 저비용 + 수미상관 (A-5) |
| **3** | 구간 5 (국악 100%) | 무자의 땅 등 최종장 **지역곡** | `bgm_final`은 보스전이라 지역곡으로 못 씀 |
| **4** | 구간 2~3 | 신규 지역 (경복궁, 활자 공방, 서고, 산길, 왜곡된 문자 미궁 등) | 기획초안 13장 장소 목록 + v2 5장(경복궁 허브)·7.1(미궁) 기준. 지역 확정 후 |
| — | 축 밖 | `jingle_clear` | 미생성 (아래 9번) |
| — | 축 밖(가변) | `bgm_select` — 스테이지 선택 | 아래 11번. 변형 3종을 쓰면 축의 눈금으로도 활용 가능 |

**구간 0이 최우선인 이유**: 그라데이션은 "끝점"이 아니라 **"시작점"이 있어야 보인다.** 지금 상태로 곡을 계속 추가하면 전부 중간~후반 구간에 몰려 축이 평평해진다. **다음 생성 배치에는 반드시 순칩튠 곡이 들어가야 한다.**

---

# 곡별 프롬프트

## 1. bgm_title.mp3 — 타이틀 화면
```
Instrumental video game title theme. Korean folk-chiptune hybrid: solo gayageum plucks a slow, wistful pentatonic melody over warm 8-bit square-wave pads and soft tape hiss. Sparse janggu heartbeat, occasional low daegeum breath. Mysterious, nostalgic, slightly melancholic — a broken world waiting to be restored. 72 BPM, minor pentatonic, gentle throughout, seamless loop, consistent dynamics, clean warm mix, no vocals.
```

## 2. bgm_forest.mp3 — 시작의 숲
```
Instrumental cozy puzzle game area theme. Korean folk-chiptune hybrid: breathy daegeum flute lead over bouncy 8-bit arpeggios, soft gayageum plucks, light janggu rhythm with woodblock ticks. Fresh morning forest — birdsong-like grace notes, curious, gentle, encouraging tutorial-area warmth. 92 BPM, major pentatonic, light dynamics, seamless loop, lo-fi warmth, no vocals.
```

## 3. bgm_village.mp3 — 시작의 마을
```
Instrumental cheerful village theme for a cozy Korean folk puzzle game. Playful gayageum and plucky 8-bit chip melody trade phrases, haegeum answers with a warm counter-line, steady janggu with clappy chiptune percussion. A relaxed hanok village at midday — friendly merchants, warm sunlight, smoke from kitchen fires. 104 BPM, major pentatonic, medium-light energy, seamless loop, clean bright mix, no vocals.
```

## 4. bgm_pass.mp3 — 받침 고개
```
Instrumental mountain-trail theme, determined and misty. Korean folk-chiptune hybrid: haegeum long bowed notes over a walking buk drum pulse and deep 8-bit bass, daegeum echoing across a foggy mountain pass, sparse gayageum accents. A steady uphill trek with mild tension and quiet resolve. 96 BPM, minor pentatonic gyemyeonjo feeling, mid energy, seamless loop, spacious reverb, no vocals.
```

## 5. bgm_temple.mp3 — 자모 사찰
```
Instrumental serene Korean Buddhist temple theme. Meditative moktak woodblock pulse, distant temple bell and wind chimes, long breathy daegeum tones, very sparse gayageum notes, faint warm synth pad underneath. Zen stillness — incense, morning mist in a mountain temple courtyard. 60 BPM, minimal and spacious with long silences, soft dynamics throughout, no percussion buildup, seamless loop, no vocals.
```

## 6. bgm_market.mp3 — 낱말 저잣거리
```
Instrumental lively Korean marketplace theme. Bright taepyeongso-style reed horn lead melody over fast samulnori-inspired janggu and kkwaenggwari percussion, funky 8-bit bassline, quick gayageum runs. Joseon market day — haggling energy, playful chaos, street food sizzling. 120 BPM, major pentatonic, high energy but friendly not aggressive, seamless loop, punchy mix, no vocals.
```
※ 생성 완료. 단 **`Assets/Resources/Audio/`에 아직 임포트되지 않음** — `ArtDrop/Audio/`에만 존재. map_05가 이 곡을 지정하고 있으므로 임포트 전까지 저잣거리에서 이전 곡이 유지된다. (A-3 주의 1)

## 7. bgm_battle.mp3 — 사천왕 보스전
```
Instrumental boss battle theme. Korean percussion-driven chiptune: pounding buk and fast janggu patterns, aggressive haegeum sawing riff, dark 8-bit square-wave bass ostinato, tense gayageum tremolo stabs. A duel against a stern guardian master — urgent, driving, disciplined menace. 140 BPM, minor pentatonic, high tension, seamless loop, tight punchy mix, no vocals, no cinematic orchestral drums.
```

### 7-1. 실제 파생 — 사천왕별 4곡 (M4에서 생성·연결 완료)

위 프롬프트를 바탕으로 사천왕 각각의 곡을 생성해 아래처럼 연결했다. **파일명 번호는 생성 순서이며 진행 순서가 아니다** (A-3 주의 2).

| 파일 | 보스 | 배틀 JSON | 소속 맵 | 축 구간 |
|---|---|---|---|---|
| `bgm_battle1.mp3` | 받침 사범 | `boss_batchim.json` | map_03 받침 고개 | 3 동화 |
| `bgm_battle2.mp3` | 자모 노승 | `boss_jamo.json` | map_04 자모 사찰 | 4 각성 |
| `bgm_battle3.mp3` | 낱말 장사꾼 | `boss_word.json` | map_05 낱말 저잣거리 | 4 각성 |
| `bgm_battle4.mp3` | 십자말 훈장 | `boss_crossword.json` | map_02 시작의 마을 | 2 적응 |

- 배틀 JSON에 `bgm` 필드가 없으면 `bgm_boss`로 폴백하지만, **`bgm_boss.mp3`는 존재하지 않는다** — 현재는 4개 JSON 모두 명시 지정이라 문제없음.
- **4번째 사천왕(미궁의 넷째, 스토리기획 v2 7.1)의 지역이 미구현**이라 곡 배정처가 없다. 현재 `bgm_battle4`는 십자말 훈장이 쓰고 있다.
- v2에서 사천왕 격파는 **처치가 아니라 정화**(→ 세종의 조력자로 전향)로 재해석됐다. 곡을 다시 만든다면 `menace`보다 **"물든 자를 되돌리는 대련"**의 톤이 맞는다 — 단, 기존 4곡이 이미 연결돼 있으므로 **재생성은 권장하지 않는다.**

## 8. bgm_final.mp3 — 묵음 대왕 최종 결전
```
Instrumental final boss theme. Corrupted Korean royal court music: slow ceremonial piri and daegeum motif twisted with dissonant detuned synths, glitchy 8-bit noise bursts like splattering black ink, massive buk drum hits, ominous low drone, taepyeongso wailing over double-time janggu in intense sections. Majestic but wrong, overwhelming — a demon king wearing a stolen crown. Mostly 150 BPM driving, short dread intro, minor key, seamless loop, dark wide mix, no vocals, no choir.
```
- ※ 곡 제목의 보스명을 v1 `가나다 대마왕` → **v2 확정명 `묵음 대왕`**으로 갱신 (A-3 주의 3). **프롬프트 원문과 파일명은 변경 없음** — 이미 생성·연결된 곡이고, 프롬프트의 `a demon king wearing a stolen crown`은 v2에서도 그대로 유효하다.
- ※ 이 곡의 8-bit 글리치는 그라데이션 축에서 **"적에게 넘어간 칩튠"**으로 재해석된다 (A-4). v2 9장의 보스 기믹(**보드의 타일을 깨뜨리거나 지운다**)과 음색이 정확히 대응한다.

## 9. jingle_clear.mp3 — 스테이지 클리어 팡파레
```
Short instrumental victory jingle: a bright gayageum glissando sweep into a triumphant four-note chiptune fanfare, one janggu roll and a single kkwaenggwari hit at the end. Cheerful, rewarding, clean stop ending. No loop, no vocals.
```
- ※ Suno는 짧은 징글도 긴 곡으로 뽑는 경우가 많음 — 그대로 다운로드해서 주면 앞부분 5~8초만 잘라서 사용함.
- ※ **미생성 상태** (2026-07-17 기준).

## 10. bgm_intro.mp3 — 오프닝 스토리 (대마왕의 습격)
```
Instrumental opening story theme for a Korean folk puzzle adventure. Majestic and ominous royal court gugak-chiptune hybrid: ceremonial taepyeongso and piri fanfare motif over massive buk war drums, deep 8-bit bass drone and a slow janggu march, haegeum tremolo swelling like gathering storm clouds, sparse gayageum notes flickering out like breaking letters. A demon king descends on Joseon and the world's letters begin to crack — grand, solemn, foreboding storytelling. 80 BPM, minor pentatonic, builds in waves but stays loopable, wide solemn mix, seamless loop, no vocals, no choir.
```
- ※ 타이틀(1번)과 확실히 구분되도록 "웅장·불길" 방향. 인트로 3페이지 동안 루프되므로 기승전결보다 파도형 고조가 맞음. 너무 밝게 나오면 맨 앞에 `dark, ominous,` 추가.
- ※ **⚠ v2 오프닝과 전제가 어긋남 — 재검토 필요.** 이 곡은 "대마왕이 조선에 강림"하는 오프닝용인데, v2의 오프닝 4컷은 **현대 도시에서 시작**한다(컷1 현대의 혼란 → 컷2 타임머신 → 컷3 조선 도착 → 컷4 세종 만남). 상세와 선택지는 **A-3 주의 4** 참고. **권장안: 이 곡을 컷3~4(조선 도착 이후) 전용으로 좁히고, 컷1~2용 순칩튠 곡을 새로 만든다** (A-6 우선순위 1).

---

## 11. bgm_select.mp3 — 스테이지 선택 화면 (아리랑 계열, 신규)

2026-07-17 사용자 확정: **아리랑 계열, 잔잔하게.**

### 설계 조건

스테이지 선택은 다른 화면과 요구가 다르다.

- **최장 체류 화면**: 스테이지를 고르고 별을 확인하는 동안 계속 흐른다. 지역곡보다 **훨씬 오래, 훨씬 자주** 듣는다.
- **주의를 뺏으면 안 됨**: 플레이어는 이 화면에서 "생각 중"이다. 드라마·전개·기승전결 금지.
- **그런데 지루하면 안 됨**: 반복 노출이 많으므로 완전 정적이면 금세 질린다. → **선율은 단순하게, 음색과 장식만 조금씩 변주**하는 방식으로 해결.
- **루프 이음매가 티나면 안 됨**: 최장 체류 화면이라 이음매를 가장 많이 듣는 곡이다. `seamless loop`를 반드시 넣고, 생성 결과에서 이음매를 직접 확인할 것.

아리랑은 이 조건에 잘 맞는다 — 누구나 아는 선율이라 **주의를 끌지 않으면서** 정서적으로 채워 준다.

> **라이선스 주의**: 아리랑 선율 자체는 전통 민요(퍼블릭 도메인)지만, **Suno 생성물의 이용 조건은 Suno 요금제 약관을 따른다.** 상업 배포 전 현재 구독 등급의 상업적 이용 권한을 확인할 것 — 이 문서의 **모든 곡에 동일하게 적용**된다.

### 변형 3종 — 골라서 생성

셋 다 "잔잔한 아리랑"이지만 **국악 그라데이션 축 위의 위치가 다르다.** 하나만 골라 써도 되고, 셋 다 채택해 진행도에 따라 교체할 수도 있다 (아래 "확장 선택지").

#### 변형 A — 칩튠 우세 (국악 ~30%, 구간 1~2)
```
Instrumental menu theme for a cozy Korean puzzle game. Gentle chiptune arrangement of the Arirang folk melody: soft 8-bit triangle-wave lead carries the tune slowly and simply, warm square-wave pads underneath, quiet pulse bass, faint tape hiss. A single gayageum pluck and one distant daegeum breath drift in occasionally, like a memory of somewhere far away. Calm, wistful, unobtrusive background music for browsing a menu. 68 BPM, minor pentatonic, very soft dynamics with no builds or drops, no percussion, seamless loop, warm lo-fi mix, no vocals.
```

#### 변형 B — 균형 (국악 ~55%, 구간 2~3) — **기본 추천**
```
Instrumental menu theme for a cozy Korean puzzle game. Gentle folk-chiptune arrangement of the Arirang melody: gayageum plucks the tune softly while a warm 8-bit square-wave pad and quiet chip bass hold the harmony, breathy daegeum takes over the phrase now and then, a very light janggu heartbeat far in the background. Calm, nostalgic, contemplative — quiet afternoon light, letters slowly coming home. 66 BPM, minor pentatonic, soft steady dynamics, no builds or drops, seamless loop, clean warm mix, no vocals.
```

#### 변형 C — 국악 우세 (국악 ~85%, 구간 4~5)
```
Instrumental menu theme, traditional Korean and meditative. Sparse gugak arrangement of the Arirang melody: solo gayageum plays the tune slowly with sanjo-style ornamentation and long pauses, haegeum answers with a soft bowed counter-line, breathy low daegeum tones underneath, a faint 8-bit pad barely audible like distant air. Still, spacious, deeply nostalgic — a quiet hanok courtyard at dusk. 60 BPM, minor pentatonic gyemyeonjo, very soft dynamics, long silences, no percussion, no builds, seamless loop, spacious natural reverb, no vocals.
```

### 생성 팁 (이 곡 한정)
- 아리랑이 안 나오고 그냥 국악풍 즉흥으로 나오면 맨 앞에 `based on the Korean folk song Arirang,`를 한 번 더 붙인다.
- 너무 슬프거나 비장해지면 `wistful`을 `warm, gentle`로 바꾸고 `melancholic` 계열 단어를 뺀다.
- **드럼이 들어오면 실패다** — 메뉴 곡이 리듬을 밀면 스테이지 고르는 동안 조급해진다. 제외 스타일에 `drums, percussion buildup`을 추가해 재생성.
- 후렴이 반복되며 커지는 편곡이 나오면 `no builds or drops, consistent dynamics`를 앞으로 옮긴다.

### 파일명
- **한 곡만 채택**: `bgm_select.mp3`
- **변형 3종 다 채택**: `bgm_select_a.mp3` / `bgm_select_b.mp3` / `bgm_select_c.mp3`

### 확장 선택지 — 스테이지 선택 = 그라데이션 눈금 (제안, 미확정)

스테이지 선택 화면은 **모든 화면 중 가장 자주 재방문**한다. 같은 아리랑 선율이 진행도에 따라 A → B → C로 편곡만 바뀌면, 플레이어는 **비교 대상이 명확한 상태로** 그라데이션을 체감한다. 지역곡은 지역마다 곡이 달라 변화가 "곡이 달라서"인지 "축이 움직여서"인지 구분되지 않는데, 이 화면은 **선율이 고정이라 편곡 변화만 남는다.** 축을 가장 싸게, 가장 확실하게 들려주는 자리다.

- 구현 비용: `GameApp.StageSelect.cs`의 BGM 호출부에서 진행도에 따라 트랙명을 고르는 분기 한 줄 수준. (※ 현재 `ShowStageSelect()`에는 **BGM 호출 자체가 없다** — 이전 화면의 곡이 그대로 이어진다. 곡 채택 시 `BgmPlayer.Instance?.Play(...)` 추가가 선행돼야 함)
- 전환 지점(안): 변형 A = 자음 0~4개 회수 / B = 5~9개 / C = 10개~ (기획초안 15장의 14자음 구조 기준)
- **지금 결정할 필요는 없다** — 일단 3종을 생성해 듣고 고른 뒤, 나머지 2종이 아까우면 그때 이 선택지를 꺼내면 된다.

---

## 팁
- 결과가 국악 없이 일반 칩튠으로만 나오면 스타일 맨 앞에 `Korean traditional gugak instruments,`를 한 번 더 붙여서 재생성
- 반대로 너무 국악 다큐멘터리처럼 나오면 `8-bit chiptune video game soundtrack,`을 맨 앞으로
- 같은 곡을 2~3번 생성해 비교하는 게 프롬프트 수정보다 빠를 때가 많음
- **그라데이션 축을 의식할 것**: 위 두 팁은 결과물의 국악/칩튠 비중을 조정하는 손잡이 그 자체다. 곡이 속한 구간(A-2)의 목표 비중에 맞춰 어느 쪽 손잡이를 당길지 정한다.
