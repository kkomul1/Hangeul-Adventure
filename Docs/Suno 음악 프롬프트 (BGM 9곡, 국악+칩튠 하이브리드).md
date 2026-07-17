# Suno 배경음악 프롬프트 (v4.5 기준)

## 사용법
1. Suno **만들기 → 고급(Custom) 모드**, 모델 v4.5
2. **가사 칸은 비워두고** 인스트루멘탈(보이스 없음) 토글 ON
3. 아래 각 곡의 "스타일 프롬프트"를 스타일 칸에 붙여넣기
4. **제외 스타일**(모든 곡 공통): `vocals, singing, lyrics, rap, choir, spoken word, k-pop, EDM drops`
5. 곡당 2번 생성해서 마음에 드는 쪽을 mp3로 다운로드
6. 파일명을 아래 표기대로 바꿔 `ArtDrop\Audio\`에 저장 → Claude에게 신호

공통 사운드 정체성: **국악기(가야금·대금·해금·장구) + 따뜻한 8비트 칩튠** 하이브리드. 모든 곡이 이 조합을 공유해야 게임 전체가 한 앨범처럼 들린다.

---

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

## 7. bgm_battle.mp3 — 사천왕 보스전
```
Instrumental boss battle theme. Korean percussion-driven chiptune: pounding buk and fast janggu patterns, aggressive haegeum sawing riff, dark 8-bit square-wave bass ostinato, tense gayageum tremolo stabs. A duel against a stern guardian master — urgent, driving, disciplined menace. 140 BPM, minor pentatonic, high tension, seamless loop, tight punchy mix, no vocals, no cinematic orchestral drums.
```

## 8. bgm_final.mp3 — 가나다 대마왕 최종 결전
```
Instrumental final boss theme. Corrupted Korean royal court music: slow ceremonial piri and daegeum motif twisted with dissonant detuned synths, glitchy 8-bit noise bursts like splattering black ink, massive buk drum hits, ominous low drone, taepyeongso wailing over double-time janggu in intense sections. Majestic but wrong, overwhelming — a demon king wearing a stolen crown. Mostly 150 BPM driving, short dread intro, minor key, seamless loop, dark wide mix, no vocals, no choir.
```

## 9. jingle_clear.mp3 — 스테이지 클리어 팡파레
```
Short instrumental victory jingle: a bright gayageum glissando sweep into a triumphant four-note chiptune fanfare, one janggu roll and a single kkwaenggwari hit at the end. Cheerful, rewarding, clean stop ending. No loop, no vocals.
```
※ Suno는 짧은 징글도 긴 곡으로 뽑는 경우가 많음 — 그대로 다운로드해서 주면 앞부분 5~8초만 잘라서 사용함.

## 10. bgm_intro.mp3 — 오프닝 스토리 (대마왕의 습격)
```
Instrumental opening story theme for a Korean folk puzzle adventure. Majestic and ominous royal court gugak-chiptune hybrid: ceremonial taepyeongso and piri fanfare motif over massive buk war drums, deep 8-bit bass drone and a slow janggu march, haegeum tremolo swelling like gathering storm clouds, sparse gayageum notes flickering out like breaking letters. A demon king descends on Joseon and the world's letters begin to crack — grand, solemn, foreboding storytelling. 80 BPM, minor pentatonic, builds in waves but stays loopable, wide solemn mix, seamless loop, no vocals, no choir.
```
※ 타이틀(1번)과 확실히 구분되도록 "웅장·불길" 방향. 인트로 3페이지 동안 루프되므로 기승전결보다 파도형 고조가 맞음. 너무 밝게 나오면 맨 앞에 `dark, ominous,` 추가.

---

## 팁
- 결과가 국악 없이 일반 칩튠으로만 나오면 스타일 맨 앞에 `Korean traditional gugak instruments,`를 한 번 더 붙여서 재생성
- 반대로 너무 국악 다큐멘터리처럼 나오면 `8-bit chiptune video game soundtrack,`을 맨 앞으로
- 같은 곡을 2~3번 생성해 비교하는 게 프롬프트 수정보다 빠를 때가 많음
