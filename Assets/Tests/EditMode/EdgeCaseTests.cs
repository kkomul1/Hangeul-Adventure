using NUnit.Framework;
using HangeulAdventure.Engine;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>
    /// 엣지 케이스 보강 (퍼즐규칙명세 3~8장).
    /// 모든 기대값은 명세 5장 판정 순서(1.합성 → 2.분해 → 3.이동 → 4.실패)로 손 계산했다.
    /// 각 테스트 주석에 계산 근거를 남긴다.
    /// </summary>
    public class EdgeCaseTests
    {
        private static PushResultType Push(char mover, char target, Direction d, out char ns, out char nt)
            => PushLogic.Resolve(mover, target, targetExists: true, d, out ns, out nt);

        // ================================================================
        // 1. 복합 자모 전수 (이중모음 11종 + 쌍자음/겹받침 16종) — 명세 3장 표 그대로
        // ================================================================

        private static readonly (char l, char r, char comp)[] StandardVowelPairs =
        {
            ('ㅏ','ㅣ','ㅐ'), ('ㅑ','ㅣ','ㅒ'), ('ㅓ','ㅣ','ㅔ'), ('ㅕ','ㅣ','ㅖ'),
            ('ㅗ','ㅏ','ㅘ'), ('ㅗ','ㅐ','ㅙ'), ('ㅗ','ㅣ','ㅚ'),
            ('ㅜ','ㅓ','ㅝ'), ('ㅜ','ㅔ','ㅞ'), ('ㅜ','ㅣ','ㅟ'), ('ㅡ','ㅣ','ㅢ'),
        };

        private static readonly (char l, char r, char comp)[] StandardConsonantPairs =
        {
            ('ㄱ','ㄱ','ㄲ'), ('ㄷ','ㄷ','ㄸ'), ('ㅂ','ㅂ','ㅃ'), ('ㅅ','ㅅ','ㅆ'), ('ㅈ','ㅈ','ㅉ'),
            ('ㄱ','ㅅ','ㄳ'), ('ㄴ','ㅈ','ㄵ'), ('ㄴ','ㅎ','ㄶ'),
            ('ㄹ','ㄱ','ㄺ'), ('ㄹ','ㅁ','ㄻ'), ('ㄹ','ㅂ','ㄼ'), ('ㄹ','ㅅ','ㄽ'),
            ('ㄹ','ㅌ','ㄾ'), ('ㄹ','ㅍ','ㄿ'), ('ㄹ','ㅎ','ㅀ'), ('ㅂ','ㅅ','ㅄ'),
        };

        private static (char l, char r, char comp)[] AllStandardPairs()
        {
            var all = new (char, char, char)[StandardVowelPairs.Length + StandardConsonantPairs.Length];
            StandardVowelPairs.CopyTo(all, 0);
            StandardConsonantPairs.CopyTo(all, StandardVowelPairs.Length);
            return all;
        }

        [Test]
        public void 복합자모_27종_표준쌍_양방향_밀기_합성()
        {
            // 손계산: l을 →로 r 위에 = l왼쪽+r오른쪽 → 1단계 합성 성립.
            // r을 ←로 l 위에 = 같은 공간 배치(누가 밀든 무관, 명세 4장) → 동일 결과.
            foreach (var (l, r, comp) in AllStandardPairs())
            {
                Assert.AreEqual(PushResultType.Compose, Push(l, r, Direction.Right, out var ns, out var nt),
                    $"{l}→{r}");
                Assert.AreEqual((Hangul.Empty, comp), (ns, nt), $"{l}→{r}");

                Assert.AreEqual(PushResultType.Compose, Push(r, l, Direction.Left, out _, out nt),
                    $"{r}←{l}");
                Assert.AreEqual(comp, nt, $"{r}←{l}");
            }
        }

        [Test]
        public void 복합자모_27종_가로분해_왕복()
        {
            // 손계산: 복합 자모는 가로 겉조합(명세 5장 표) →
            // →로 밀면 오른쪽 성분(r)이 나가고 l이 남음, ←로 밀면 l이 나가고 r이 남음.
            foreach (var (l, r, comp) in AllStandardPairs())
            {
                Assert.AreEqual(PushResultType.SplitMove, Push(comp, '\0', Direction.Right, out var ns, out var nt),
                    $"{comp}→");
                Assert.AreEqual((l, r), (ns, nt), $"{comp}→: {l}가 남고 {r}가 나가야 함");

                Assert.AreEqual(PushResultType.SplitMove, Push(comp, '\0', Direction.Left, out ns, out nt),
                    $"{comp}←");
                Assert.AreEqual((r, l), (ns, nt), $"{comp}←: {r}가 남고 {l}가 나가야 함");
            }
        }

        [Test]
        public void 복합자모_27종_세로는_통째_이동()
        {
            // 손계산: 복합 자모의 조합 트리는 전부 가로 결합뿐 → 세로 축은 깨끗함 → 상하는 이동.
            foreach (var (_, _, comp) in AllStandardPairs())
            {
                Assert.AreEqual(PushResultType.Move, Push(comp, '\0', Direction.Up, out _, out var nt), $"{comp}↑");
                Assert.AreEqual(comp, nt);
                Assert.AreEqual(PushResultType.Move, Push(comp, '\0', Direction.Down, out _, out nt), $"{comp}↓");
                Assert.AreEqual(comp, nt);
            }
        }

        [Test]
        public void 복합자모_27종_역순배치_전수_실패()
        {
            // 손계산: 표준 쌍의 좌우가 뒤집힌 배치는 표에 없음 → 합성 불가.
            // 분해 연쇄로도 성립하는 역순 조합 없음(예: ㅐ→ㅗ는 ㅣ가 나가 ㅣ+ㅗ 불가) → 전체 실패.
            // 같은 자모 쌍(ㄱ+ㄱ 등)은 뒤집어도 동일하므로 제외.
            foreach (var (l, r, _) in AllStandardPairs())
            {
                if (l == r) continue;
                Assert.AreEqual(PushResultType.Fail, Push(r, l, Direction.Right, out _, out _), $"{r}→{l}");
                Assert.AreEqual(PushResultType.Fail, Push(l, r, Direction.Left, out _, out _), $"{l}←{r}");
            }
        }

        // ================================================================
        // 2. 받침 전수 (종성 27종 결합·분해 왕복, 초성 전용 3종 실패)
        // ================================================================

        [Test]
        public void 받침_27종_결합_분해_왕복()
        {
            // 손계산: 종성 인덱스의 27자는 전부 '가'와 세로 결합 가능(민글자 위 + 자음 아래).
            // 분해는 겉조합(받침)부터: ↓면 종성이 나가고, ↑면 민글자가 나감.
            // 속에 ㄱ+ㅏ 가로 조합이 있으므로 좌우는 잠김(분해 불가 + 이동 잠김) → 실패.
            foreach (char jong in Hangul.Jongseong)
            {
                char expected = Hangul.ComposeSyllable('ㄱ', 'ㅏ', jong);

                // 결합: 가↓jong, jong↑가 (같은 공간 배치)
                Assert.AreEqual(PushResultType.Compose, Push('가', jong, Direction.Down, out _, out var nt),
                    $"가↓{jong}");
                Assert.AreEqual(expected, nt);
                Assert.AreEqual(PushResultType.Compose, Push(jong, '가', Direction.Up, out _, out nt),
                    $"{jong}↑가");
                Assert.AreEqual(expected, nt);

                // 분해 왕복
                Assert.AreEqual(PushResultType.SplitMove, Push(expected, '\0', Direction.Down, out var ns, out nt));
                Assert.AreEqual(('가', jong), (ns, nt), $"{expected}↓");
                Assert.AreEqual(PushResultType.SplitMove, Push(expected, '\0', Direction.Up, out ns, out nt));
                Assert.AreEqual((jong, '가'), (ns, nt), $"{expected}↑");

                // 좌우 잠김
                Assert.AreEqual(PushResultType.Fail, Push(expected, '\0', Direction.Left, out _, out _), $"{expected}←");
                Assert.AreEqual(PushResultType.Fail, Push(expected, '\0', Direction.Right, out _, out _), $"{expected}→");
            }
        }

        [Test]
        public void 초성전용_ㄸㅃㅉ_받침결합_전수_실패()
        {
            // 손계산: ㄸㅃㅉ은 종성 불가(명세 3장 역할 제약) → 1단계 합성 불가.
            // 가는 세로 분해 없음, ㄸㅃㅉ은 가로 겉조합이라 세로 분해 없음 → 양방향 다 실패.
            foreach (char c in "ㄸㅃㅉ")
            {
                Assert.AreEqual(PushResultType.Fail, Push('가', c, Direction.Down, out _, out _), $"가↓{c}");
                Assert.AreEqual(PushResultType.Fail, Push(c, '가', Direction.Up, out _, out _), $"{c}↑가");
            }
        }

        // ================================================================
        // 3. 복합모음 글자 + 받침: 관 / ㅢ 글자: 의
        // ================================================================

        [Test]
        public void 관_받침결합_후_좌우잠김_상하분해()
        {
            // 손계산: 과(ㄱ+ㅘ, 양축 조합) + ㄴ 받침 = 관.
            // 관의 조합 트리: 받침(세로) + ㅘ(양축) → 가로·세로 모두 조합 있음.
            // 좌우: 겉조합(받침)이 세로라 가로 분해 없음 + 가로 축 잠김 → 빈칸이어도 실패.
            // 상하: 겉조합 축 → 분해 (↑ 민글자 '과' 나감 / ↓ 받침 ㄴ 나감).
            Assert.AreEqual(PushResultType.Compose, Push('과', 'ㄴ', Direction.Down, out _, out var nt));
            Assert.AreEqual('관', nt);

            Assert.AreEqual((true, true), Hangul.CompositionAxes('관'));
            Assert.AreEqual(PushResultType.Fail, Push('관', '\0', Direction.Left, out _, out _));
            Assert.AreEqual(PushResultType.Fail, Push('관', '\0', Direction.Right, out _, out _));

            Assert.AreEqual(PushResultType.SplitMove, Push('관', '\0', Direction.Up, out var ns, out nt));
            Assert.AreEqual(('ㄴ', '과'), (ns, nt), "관↑: 과가 위로 나가고 ㄴ이 남음");
            Assert.AreEqual(PushResultType.SplitMove, Push('관', '\0', Direction.Down, out ns, out nt));
            Assert.AreEqual(('과', 'ㄴ'), (ns, nt), "관↓: ㄴ이 아래로 나가고 과가 남음");
        }

        [Test]
        public void 의_양축_합성()
        {
            // 손계산: ㅢ는 복합형 모음(명세 4장) → 자음이 위에서도 왼쪽에서도 결합.
            Assert.AreEqual(PushResultType.Compose, Push('ㅇ', 'ㅢ', Direction.Right, out _, out var nt));
            Assert.AreEqual('의', nt);
            Assert.AreEqual(PushResultType.Compose, Push('ㅇ', 'ㅢ', Direction.Down, out _, out nt));
            Assert.AreEqual('의', nt);
            // 모음 쪽을 밀어도 동일 배치
            Assert.AreEqual(PushResultType.Compose, Push('ㅢ', 'ㅇ', Direction.Left, out _, out nt));
            Assert.AreEqual('의', nt);
            Assert.AreEqual(PushResultType.Compose, Push('ㅢ', 'ㅇ', Direction.Up, out _, out nt));
            Assert.AreEqual('의', nt);
        }

        [Test]
        public void 의_4방향_분해_통째이동_불가()
        {
            // 손계산: 복합모음 글자는 4방향 모두 분해. ↑/← = 자음(ㅇ) 나감, ↓/→ = 모음(ㅢ) 나감.
            // 양축에 조합을 품어 통째 이동은 어느 방향으로도 불가 (빈칸이면 항상 분해가 됨).
            Assert.AreEqual((true, true), Hangul.CompositionAxes('의'));

            Assert.AreEqual(PushResultType.SplitMove, Push('의', '\0', Direction.Up, out var ns, out var nt));
            Assert.AreEqual(('ㅢ', 'ㅇ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('의', '\0', Direction.Left, out ns, out nt));
            Assert.AreEqual(('ㅢ', 'ㅇ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('의', '\0', Direction.Down, out ns, out nt));
            Assert.AreEqual(('ㅇ', 'ㅢ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('의', '\0', Direction.Right, out ns, out nt));
            Assert.AreEqual(('ㅇ', 'ㅢ'), (ns, nt));
        }

        // ================================================================
        // 4. 겹받침 글자의 단계식 분해 전체 경로
        // ================================================================

        [Test]
        public void 닭_조립과_단계식_분해_전체경로()
        {
            // 손계산 (명세 6장): 분해는 겉조합부터 한 단계씩.
            // 닭↓ = 다 + ㄺ (달+ㄱ이 아님! 겹받침은 한 단위로 나감) → ㄺ→ = ㄹ+ㄱ, 다→ = ㄷ+ㅏ.
            Assert.AreEqual(PushResultType.Compose, Push('다', 'ㄺ', Direction.Down, out _, out var nt));
            Assert.AreEqual('닭', nt);

            // 닭 좌우 잠김 (받침=세로 겉조합 + 속 가로 조합 ㄷ+ㅏ, ㄹ+ㄱ)
            Assert.AreEqual(PushResultType.Fail, Push('닭', '\0', Direction.Left, out _, out _));
            Assert.AreEqual(PushResultType.Fail, Push('닭', '\0', Direction.Right, out _, out _));

            // 1단계: 닭↓ → 다 남고 ㄺ 나감 / 닭↑ → 다 나가고 ㄺ 남음
            Assert.AreEqual(PushResultType.SplitMove, Push('닭', '\0', Direction.Down, out var ns, out nt));
            Assert.AreEqual(('다', 'ㄺ'), (ns, nt), "닭↓: ㄱ이 아니라 ㄺ 전체가 나가야 함");
            Assert.AreEqual(PushResultType.SplitMove, Push('닭', '\0', Direction.Up, out ns, out nt));
            Assert.AreEqual(('ㄺ', '다'), (ns, nt));

            // 2단계: 나온 성분들의 분해
            Assert.AreEqual(PushResultType.SplitMove, Push('ㄺ', '\0', Direction.Right, out ns, out nt));
            Assert.AreEqual(('ㄹ', 'ㄱ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('다', '\0', Direction.Right, out ns, out nt));
            Assert.AreEqual(('ㄷ', 'ㅏ'), (ns, nt));

            // 겹받침 타일 자체는 세로가 깨끗해 통째 이동 가능
            Assert.AreEqual(PushResultType.Move, Push('ㄺ', '\0', Direction.Down, out _, out _));
        }

        // ================================================================
        // 5. 연쇄 합성 (분해 + 1단계 연쇄, 명세 5장 2항)
        // ================================================================

        [Test]
        public void 연쇄_초성이_위로_나가_받침이_된다()
        {
            // 배치: 나(위) / 고(아래). 고를 ↑로 밀면:
            // 1.합성: ComposeVertical(top=나, bottom=고) — 아래가 글자라 규칙 없음 → 불가.
            // 2.분해: 고는 세로 겉조합, ↑쪽 성분 ㄱ이 나감 → 연쇄: 나(위)+ㄱ(아래) = 받침 결합 = 낙.
            Assert.AreEqual(PushResultType.SplitCompose, Push('고', '나', Direction.Up, out var ns, out var nt));
            Assert.AreEqual(('ㅗ', '낙'), (ns, nt));

            // 복합모음 글자도 동일: 과↑나 → ㄱ이 나가 낙, ㅘ 남음
            Assert.AreEqual(PushResultType.SplitCompose, Push('과', '나', Direction.Up, out ns, out nt));
            Assert.AreEqual(('ㅘ', '낙'), (ns, nt));
        }

        [Test]
        public void 연쇄_나간_자음이_겹받침으로_결합()
        {
            // 배치: ㄹ(왼쪽) 마(오른쪽). 마를 ←로 밀면:
            // 1.합성: ComposeHorizontal(left=ㄹ, right=마) — 글자가 오른쪽이라 규칙 없음 → 불가.
            // 2.분해: 마는 가로 겉조합, ←쪽 성분 ㅁ이 나감 →
            //   연쇄: 오른쪽에서 접근한 ㅁ이 오른쪽 성분 = ㄹ+ㅁ 표준쌍 = ㄻ.
            Assert.AreEqual(PushResultType.SplitCompose, Push('마', 'ㄹ', Direction.Left, out var ns, out var nt));
            Assert.AreEqual(('ㅏ', 'ㄻ'), (ns, nt));
        }

        [Test]
        public void 연쇄_쌍자음_한칸_걷기()
        {
            // 배치: ㄲ(왼쪽) ㄱ(오른쪽). ㄲ을 →로 밀면:
            // 1.합성: ㄲ+ㄱ은 표준쌍 아님 → 불가.
            // 2.분해: ㄲ의 →쪽 성분 ㄱ이 나감 → 연쇄: ㄱ(왼쪽)+ㄱ(오른쪽) = ㄲ.
            // 결과적으로 ㄲ|ㄱ → ㄱ|ㄲ (ㄲ이 한 칸 오른쪽으로 걸어간 모양).
            Assert.AreEqual(PushResultType.SplitCompose, Push('ㄲ', 'ㄱ', Direction.Right, out var ns, out var nt));
            Assert.AreEqual(('ㄱ', 'ㄲ'), (ns, nt));
        }

        [Test]
        public void 연쇄_비표준_이중모음은_불발()
        {
            // 과를 →로 ㅣ 위에: 1.합성 불가(과는 글자) →
            // 2.분해: ㅘ가 나감 → 연쇄 ㅘ+ㅣ는 표준쌍 아님(ㅙ는 ㅗ+ㅐ만) → 분해 불발 → 전체 실패.
            Assert.AreEqual(PushResultType.Fail, Push('과', 'ㅣ', Direction.Right, out var ns, out var nt));
            Assert.AreEqual(('과', 'ㅣ'), (ns, nt), "실패 시 상태 불변");
        }

        [Test]
        public void 연쇄_아래로_나간_겹받침은_받침결합_불가()
        {
            // 갃을 ↓로 으 위에: 1.합성 불가 → 2.분해: ㄳ이 아래로 나감 →
            // 연쇄: ㄳ(위)+으(아래) — 받침 결합은 글자가 위여야 하므로 불가 → 전체 실패.
            // (분해로 나간 겹받침이 받침으로 연쇄 결합하는 시나리오는 공간 배치상 존재하지 않음)
            Assert.AreEqual(PushResultType.Fail, Push('갃', '으', Direction.Down, out _, out _));
        }

        [Test]
        public void 연쇄_위로_나간_민글자는_결합_불가()
        {
            // 간을 ↑로 ㄷ 위에: 1.합성 불가 → 2.분해: 가가 위로 나감 →
            // 연쇄: ㄷ(위)+가(아래) — 어떤 규칙에도 없음(받침 결합은 글자가 위) → 전체 실패.
            Assert.AreEqual(PushResultType.Fail, Push('간', 'ㄷ', Direction.Up, out _, out _));
        }

        [Test]
        public void 이중모음_표준쌍이면_분해연쇄가_아니라_전체합성()
        {
            // ㅐ를 ←로 ㅗ 위에: 판정 순서상 1.합성을 먼저 검사 —
            // ComposeHorizontal(left=ㅗ, right=ㅐ) = ㅙ 성립 → 합성 (ㅐ가 ㅣ를 내보내는 분해가 아님).
            Assert.AreEqual(PushResultType.Compose, Push('ㅐ', 'ㅗ', Direction.Left, out var ns, out var nt));
            Assert.AreEqual((Hangul.Empty, 'ㅙ'), (ns, nt));
        }

        // ================================================================
        // 6. RuleFlags (명세 11장 — 엔진 자리만 확보된 특수 규칙)
        // ================================================================

        [Test]
        public void disableMove_이동만_금지_합성_분해는_가능()
        {
            var f = new RuleFlags { disableMove = true };

            // 이동: 금지 → 실패
            Assert.AreEqual(PushResultType.Fail,
                PushLogic.Resolve('ㄱ', '\0', true, Direction.Right, out _, out _, f));
            // 합성: 영향 없음
            Assert.AreEqual(PushResultType.Compose,
                PushLogic.Resolve('ㄱ', 'ㅏ', true, Direction.Right, out _, out var nt, f));
            Assert.AreEqual('가', nt);
            // 분해: 영향 없음
            Assert.AreEqual(PushResultType.SplitMove,
                PushLogic.Resolve('가', '\0', true, Direction.Right, out _, out _, f));
        }

        [Test]
        public void disableSplit_분해와_연쇄만_금지_이동_합성은_가능()
        {
            var f = new RuleFlags { disableSplit = true };

            // 분해: 금지 → 실패. 가는 가로 축 잠김이라 이동으로도 넘어가지 못함.
            Assert.AreEqual(PushResultType.Fail,
                PushLogic.Resolve('가', '\0', true, Direction.Right, out var ns, out var nt, f));
            Assert.AreEqual(('가', '\0'), (ns, nt));
            // 연쇄 합성(SplitCompose)도 분해의 일부 → 금지 (사→ㅣ = ㅅ|ㅐ가 안 됨)
            Assert.AreEqual(PushResultType.Fail,
                PushLogic.Resolve('사', 'ㅣ', true, Direction.Right, out _, out _, f));
            // 깨끗한 축 이동: 영향 없음 (가는 세로 이동 가능)
            Assert.AreEqual(PushResultType.Move,
                PushLogic.Resolve('가', '\0', true, Direction.Up, out _, out _, f));
            // 합성: 영향 없음
            Assert.AreEqual(PushResultType.Compose,
                PushLogic.Resolve('ㄱ', 'ㅏ', true, Direction.Right, out _, out nt, f));
            Assert.AreEqual('가', nt);
        }

        // ================================================================
        // 7. GameSession — 비정형(십자) 보드
        // ================================================================

        private static GameSession CrossSession()
        {
            // 십자 보드 (#=없는 칸), 좌표는 (0,0)이 왼쪽 아래:
            //   # 가 #      가@(1,2)
            //   ㄱ . ㅏ      ㄱ@(0,1), 빈칸(1,1), ㅏ@(2,1)
            //   # ㄴ #      ㄴ@(1,0)
            var stage = StageBuilder.FromRows(
                new[] { "#가#", "ㄱ.ㅏ", "#ㄴ#" },
                StageBuilder.Goal("간"));
            return new GameSession(stage);
        }

        [Test]
        public void 비정형보드_없는칸_밀기실패는_undo에_안쌓임()
        {
            var s = CrossSession();
            // 없는 칸은 보드 밖과 동일 (명세 1장) → 실패, 이동 수/undo 스택 불변
            Assert.IsFalse(s.TryPush(0, 1, Direction.Up).Success);   // (0,2)는 #
            Assert.IsFalse(s.TryPush(0, 1, Direction.Down).Success); // (0,0)은 #
            Assert.IsFalse(s.TryPush(0, 1, Direction.Left).Success); // 보드 밖
            Assert.AreEqual(0, s.MoveCount);
            Assert.IsFalse(s.CanUndo, "실패한 밀기는 undo 스택에 쌓이면 안 됨");
            Assert.AreEqual('\0', s.GetCell(0, 0), "없는 칸 조회는 빈 값");
        }

        [Test]
        public void 비정형보드_풀이_수집_undo_전체복원()
        {
            var s = CrossSession();

            // 손계산 풀이: 가↓(이동) → 가↓ㄴ(합성=간) → 수집 = 3수
            Assert.AreEqual(PushResultType.Move, s.TryPush(1, 2, Direction.Down).Type);
            Assert.AreEqual('가', s.GetCell(1, 1));

            Assert.AreEqual(PushResultType.Compose, s.TryPush(1, 1, Direction.Down).Type);
            Assert.AreEqual('간', s.GetCell(1, 0));
            Assert.AreEqual(2, s.MoveCount);

            // 간의 좌우는 어차피 없는 칸 → 실패, 카운트 불변
            Assert.IsFalse(s.TryPush(1, 0, Direction.Left).Success);
            Assert.AreEqual(2, s.MoveCount);

            Assert.IsTrue(s.TryCollect(1, 0));
            Assert.AreEqual(3, s.MoveCount);
            Assert.IsTrue(s.IsCleared);

            // undo 3회로 초기 상태 완전 복원
            Assert.IsTrue(s.Undo()); // 수집 취소
            Assert.IsFalse(s.IsCleared);
            Assert.AreEqual('간', s.GetCell(1, 0));
            Assert.AreEqual(2, s.MoveCount);

            Assert.IsTrue(s.Undo()); // 합성 취소
            Assert.AreEqual('가', s.GetCell(1, 1));
            Assert.AreEqual('ㄴ', s.GetCell(1, 0));

            Assert.IsTrue(s.Undo()); // 이동 취소
            Assert.AreEqual(0, s.MoveCount);
            Assert.AreEqual('가', s.GetCell(1, 2));
            Assert.AreEqual('ㄱ', s.GetCell(0, 1));
            Assert.AreEqual('ㅏ', s.GetCell(2, 1));
            Assert.IsFalse(s.CanUndo);
        }

        // ================================================================
        // 8. Solver — 수집 타이밍이 최소 수를 결정하는 배치
        // ================================================================

        [Test]
        public void 수집으로_공간을_열어야_풀리는_배치_최소수()
        {
            // 1x3 보드 [ㄱ][가][ㅏ], 목표: 가 2개.
            // 손계산:
            //  - 첫 수 후보는 둘뿐: (a) 가운데 '가' 수집, (b) 가←ㄱ = 분해연쇄로 ㄲ 생성.
            //    (ㄱ→가: 합성 규칙 없음+줄밀기 없음=실패, ㅏ←가: 실패, 가→ㅏ: ㅏ+ㅏ 연쇄 불가=실패)
            //  - (b)는 함정: ㄱ+ㄱ이 ㄲ로 잠기고, ㄲ 옆에 ㅏ가 오면 합성 우선으로 까만 생김.
            //    까 분해는 표준쌍 ㄲ+ㅏ로만 → 이 가지에선 '가'를 두 번 다시 못 만듦.
            //  - 정해: 수집(1) → ㄱ 이동(2) → ㄱ+ㅏ 합성(3) → 수집(4). 합성엔 인접이 필요해 3수는 불가.
            var stage = StageBuilder.FromRows(new[] { "ㄱ가ㅏ" },
                StageBuilder.Goal("가"), StageBuilder.Goal("가"));

            var r = Solver.Solve(stage);
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(4, r.MinMoves, "가운데 '가'를 먼저 수집해 공간을 열어야 최단");

            // 같은 풀이를 세션으로 재현
            var s = new GameSession(stage);
            Assert.IsFalse(s.TryPush(0, 0, Direction.Right).Success, "수집 전엔 ㄱ이 못 움직임");
            Assert.IsFalse(s.TryPush(2, 0, Direction.Left).Success, "수집 전엔 ㅏ도 못 움직임");
            Assert.IsTrue(s.TryCollect(1, 0));
            Assert.IsTrue(s.TryPush(0, 0, Direction.Right).Success);
            Assert.AreEqual(PushResultType.Compose, s.TryPush(1, 0, Direction.Right).Type);
            Assert.IsTrue(s.TryCollect(2, 0));
            Assert.IsTrue(s.IsCleared);
            Assert.AreEqual(4, s.MoveCount);
        }
    }
}
