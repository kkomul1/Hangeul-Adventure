using NUnit.Framework;
using HangeulAdventure.Engine;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>밀기 4단계 판정 (명세 5장) — PushLogic 순수 함수 단위 테스트.</summary>
    public class PushLogicTests
    {
        private static PushResultType Push(char mover, char target, Direction d, out char ns, out char nt)
            => PushLogic.Resolve(mover, target, targetExists: true, d, out ns, out nt);

        // ---- 1. 합성 ----

        [Test]
        public void 자음_세로모음_가로합성()
        {
            // ㄱ을 →로 밀어 ㅏ 위에: ㄱ왼쪽+ㅏ오른쪽 = 가
            Assert.AreEqual(PushResultType.Compose, Push('ㄱ', 'ㅏ', Direction.Right, out var ns, out var nt));
            Assert.AreEqual('\0', ns);
            Assert.AreEqual('가', nt);

            // ㅏ를 ←로 밀어 ㄱ 위에: 역시 ㄱ왼쪽+ㅏ오른쪽 = 가 (누굴 밀든 무관)
            Assert.AreEqual(PushResultType.Compose, Push('ㅏ', 'ㄱ', Direction.Left, out _, out nt));
            Assert.AreEqual('가', nt);
        }

        [Test]
        public void 역순_배치는_실패()
        {
            // ㄱ이 오른쪽, ㅏ가 왼쪽: ㅏ를 →로 밀거나 ㄱ을 ←로 밀면 모음이 왼쪽 = 불가
            Assert.AreEqual(PushResultType.Fail, Push('ㅏ', 'ㄱ', Direction.Right, out _, out _));
            Assert.AreEqual(PushResultType.Fail, Push('ㄱ', 'ㅏ', Direction.Left, out _, out _));
        }

        [Test]
        public void 자음_가로모음_세로합성()
        {
            // ㄱ을 ↓로 밀어 ㅗ 위에: ㄱ위+ㅗ아래 = 고
            Assert.AreEqual(PushResultType.Compose, Push('ㄱ', 'ㅗ', Direction.Down, out _, out var nt));
            Assert.AreEqual('고', nt);
            // ㅗ를 ↑로 밀어 ㄱ 위에: 같은 배치 = 고
            Assert.AreEqual(PushResultType.Compose, Push('ㅗ', 'ㄱ', Direction.Up, out _, out nt));
            Assert.AreEqual('고', nt);
            // ㄱ 오른쪽에 ㅗ: 가로 결합 불가 (초안의 대표 예시)
            Assert.AreEqual(PushResultType.Fail, Push('ㄱ', 'ㅗ', Direction.Right, out _, out _));
        }

        [Test]
        public void 복합모음은_양축_합성()
        {
            // 위에서: ㄱ↓ + ㅘ = 과
            Assert.AreEqual(PushResultType.Compose, Push('ㄱ', 'ㅘ', Direction.Down, out _, out var nt));
            Assert.AreEqual('과', nt);
            // 왼쪽에서: ㄱ→ + ㅘ = 과
            Assert.AreEqual(PushResultType.Compose, Push('ㄱ', 'ㅘ', Direction.Right, out _, out nt));
            Assert.AreEqual('과', nt);
            // 모음을 밀어도: ㅘ↑ ㄱ, ㅘ← ㄱ = 과
            Assert.AreEqual(PushResultType.Compose, Push('ㅘ', 'ㄱ', Direction.Up, out _, out nt));
            Assert.AreEqual('과', nt);
            Assert.AreEqual(PushResultType.Compose, Push('ㅘ', 'ㄱ', Direction.Left, out _, out nt));
            Assert.AreEqual('과', nt);
        }

        [Test]
        public void 받침_결합()
        {
            // 가를 ↓로 밀어 ㄴ 위에 = 간
            Assert.AreEqual(PushResultType.Compose, Push('가', 'ㄴ', Direction.Down, out _, out var nt));
            Assert.AreEqual('간', nt);
            // ㄴ을 ↑로 밀어 가 밑으로 = 간
            Assert.AreEqual(PushResultType.Compose, Push('ㄴ', '가', Direction.Up, out _, out nt));
            Assert.AreEqual('간', nt);
            // 겹받침은 조립된 것만: 가+ㄳ = 갃
            Assert.AreEqual(PushResultType.Compose, Push('가', 'ㄳ', Direction.Down, out _, out nt));
            Assert.AreEqual('갃', nt);
            // 이미 받침 있는 글자에 자음 추가 불가: 각+ㅅ → 갃 금지 (명세 3장)
            Assert.AreEqual(PushResultType.Fail, Push('각', 'ㅅ', Direction.Down, out _, out _));
            Assert.AreEqual(PushResultType.Fail, Push('ㅅ', '각', Direction.Up, out _, out _));
            // 종성 불가 자음: 가+ㄸ 실패
            Assert.AreEqual(PushResultType.Fail, Push('가', 'ㄸ', Direction.Down, out _, out _));
        }

        [Test]
        public void 겹받침_초성_사용_불가()
        {
            // ㄻ+ㅏ 직접 합성은 불가 (ㄻ은 초성이 될 수 없음)
            Assert.AreEqual('\0', PushLogic.ComposeHorizontal('ㄻ', 'ㅏ'));
            // 다만 밀기는 분해 규칙이 적용됨: ㅁ이 나가며 ㅁ+ㅏ=마 연쇄 (규칙상 유효)
            Assert.AreEqual(PushResultType.SplitCompose, Push('ㄻ', 'ㅏ', Direction.Right, out var s1, out var t1));
            Assert.AreEqual(('ㄹ', '마'), (s1, t1));
            // 하지만 쌍자음 ㄲ은 초성 가능: ㄲ+ㅏ=까
            Assert.AreEqual(PushResultType.Compose, Push('ㄲ', 'ㅏ', Direction.Right, out _, out var nt));
            Assert.AreEqual('까', nt);
            // ㄸ 초성 가능: ㄸ+ㅏ=따
            Assert.AreEqual(PushResultType.Compose, Push('ㄸ', 'ㅏ', Direction.Right, out _, out nt));
            Assert.AreEqual('따', nt);
        }

        [Test]
        public void 자모끼리_표준쌍_합성()
        {
            // ㅗ→ㅏ = ㅘ
            Assert.AreEqual(PushResultType.Compose, Push('ㅗ', 'ㅏ', Direction.Right, out _, out var nt));
            Assert.AreEqual('ㅘ', nt);
            // ㄹ→ㅁ = ㄻ
            Assert.AreEqual(PushResultType.Compose, Push('ㄹ', 'ㅁ', Direction.Right, out _, out nt));
            Assert.AreEqual('ㄻ', nt);
            // 세로로 밀면 자모끼리 결합 없음
            Assert.AreEqual(PushResultType.Fail, Push('ㅗ', 'ㅏ', Direction.Down, out _, out _));
            Assert.AreEqual(PushResultType.Fail, Push('ㄱ', 'ㄱ', Direction.Down, out _, out _));
        }

        [Test]
        public void 글자끼리_글자모음_승급_없음()
        {
            // 단어 타일 폐기: 글자+글자 합성 없음
            Assert.AreEqual(PushResultType.Fail, Push('사', '과', Direction.Right, out _, out _));
            // 글자+모음 승급 없음... 단, 사→ㅣ는 분해+연쇄로 ㅅ|ㅐ가 됨 (별도 테스트)
            // 모음을 글자로 미는 것은 실패
            Assert.AreEqual(PushResultType.Fail, Push('ㅣ', '사', Direction.Left, out _, out _));
        }

        [Test]
        public void 합성이_분해보다_우선한다()
        {
            // 명세 5장 판정 순서(1.합성 → 2.분해)를 고정하는 테스트.
            // (요약 표의 "겉조합 축=분해" 서술과 충돌하는 케이스 — 구현결정로그 D-09 참조)

            // ㄲ을 →로 ㅏ 위에: 전체 합성 우선 → 까 (분해 우선이었다면 ㄱ|가)
            Assert.AreEqual(PushResultType.Compose, Push('ㄲ', 'ㅏ', Direction.Right, out _, out var nt));
            Assert.AreEqual('까', nt);

            // ㅔ를 ←로 ㅜ 위에: 표준쌍 ㅜ+ㅔ 합성 우선 → ㅞ
            Assert.AreEqual(PushResultType.Compose, Push('ㅔ', 'ㅜ', Direction.Left, out _, out nt));
            Assert.AreEqual('ㅞ', nt);

            // 고를 ↓로 ㄴ 위에: 받침 합성 우선 → 곤 (분해 우선이었다면 실패)
            Assert.AreEqual(PushResultType.Compose, Push('고', 'ㄴ', Direction.Down, out _, out nt));
            Assert.AreEqual('곤', nt);

            // 과를 ↓로 ㄱ 위에: 받침 합성 우선 → 곽 (분해 우선이었다면 실패)
            Assert.AreEqual(PushResultType.Compose, Push('과', 'ㄱ', Direction.Down, out _, out nt));
            Assert.AreEqual('곽', nt);
        }

        // ---- 2. 분해 ----

        [Test]
        public void 민글자_가로분해_방향별_성분()
        {
            // 가→: ㅏ가 나가고 ㄱ이 남음
            Assert.AreEqual(PushResultType.SplitMove, Push('가', '\0', Direction.Right, out var ns, out var nt));
            Assert.AreEqual('ㄱ', ns);
            Assert.AreEqual('ㅏ', nt);
            // 가←: ㄱ이 나가고 ㅏ가 남음
            Assert.AreEqual(PushResultType.SplitMove, Push('가', '\0', Direction.Left, out ns, out nt));
            Assert.AreEqual('ㅏ', ns);
            Assert.AreEqual('ㄱ', nt);
        }

        [Test]
        public void 민글자_세로분해()
        {
            // 고↑: ㄱ이 위로
            Assert.AreEqual(PushResultType.SplitMove, Push('고', '\0', Direction.Up, out var ns, out var nt));
            Assert.AreEqual('ㅗ', ns);
            Assert.AreEqual('ㄱ', nt);
            // 고↓: ㅗ가 아래로
            Assert.AreEqual(PushResultType.SplitMove, Push('고', '\0', Direction.Down, out ns, out nt));
            Assert.AreEqual('ㄱ', ns);
            Assert.AreEqual('ㅗ', nt);
        }

        [Test]
        public void 받침글자_세로분해_단계식()
        {
            // 간↑: 가가 위로, ㄴ 남음
            Assert.AreEqual(PushResultType.SplitMove, Push('간', '\0', Direction.Up, out var ns, out var nt));
            Assert.AreEqual('ㄴ', ns);
            Assert.AreEqual('가', nt);
            // 간↓: ㄴ이 아래로, 가 남음
            Assert.AreEqual(PushResultType.SplitMove, Push('간', '\0', Direction.Down, out ns, out nt));
            Assert.AreEqual('가', ns);
            Assert.AreEqual('ㄴ', nt);
            // 갃↓: 겹받침이 한 단위로 나감
            Assert.AreEqual(PushResultType.SplitMove, Push('갃', '\0', Direction.Down, out ns, out nt));
            Assert.AreEqual('가', ns);
            Assert.AreEqual('ㄳ', nt);
        }

        [Test]
        public void 복합모음_글자는_4방향_분해()
        {
            // 과↑: ㄱ / 과←: ㄱ / 과↓: ㅘ / 과→: ㅘ
            Assert.AreEqual(PushResultType.SplitMove, Push('과', '\0', Direction.Up, out var ns, out var nt));
            Assert.AreEqual(('ㅘ', 'ㄱ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('과', '\0', Direction.Left, out ns, out nt));
            Assert.AreEqual(('ㅘ', 'ㄱ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('과', '\0', Direction.Down, out ns, out nt));
            Assert.AreEqual(('ㄱ', 'ㅘ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('과', '\0', Direction.Right, out ns, out nt));
            Assert.AreEqual(('ㄱ', 'ㅘ'), (ns, nt));
        }

        [Test]
        public void 복합자모_가로분해_단계식()
        {
            // ㅞ→: ㅔ가 나감 → ㅔ는 다시 ㅓ+ㅣ로 (2번 합치고 2번 분해)
            Assert.AreEqual(PushResultType.SplitMove, Push('ㅞ', '\0', Direction.Right, out var ns, out var nt));
            Assert.AreEqual(('ㅜ', 'ㅔ'), (ns, nt));
            Assert.AreEqual(PushResultType.SplitMove, Push('ㅔ', '\0', Direction.Right, out ns, out nt));
            Assert.AreEqual(('ㅓ', 'ㅣ'), (ns, nt));
            // 복합 자모는 세로 분해 없음 (ㅘ는 세로로 이동)
            Assert.AreEqual(PushResultType.Move, Push('ㅘ', '\0', Direction.Down, out _, out _));
        }

        // ---- 연쇄 (분해+합성, 1단계) ----

        [Test]
        public void 분해_연쇄합성()
        {
            // 사→ (도착지에 ㅣ): ㅏ가 나가며 ㅏ+ㅣ=ㅐ → ㅅ|ㅐ
            Assert.AreEqual(PushResultType.SplitCompose, Push('사', 'ㅣ', Direction.Right, out var ns, out var nt));
            Assert.AreEqual('ㅅ', ns);
            Assert.AreEqual('ㅐ', nt);
            // 간↓ (도착지에 ㅗ): ㄴ이 나가며 ㄴ위+ㅗ아래=노 → 가/노
            Assert.AreEqual(PushResultType.SplitCompose, Push('간', 'ㅗ', Direction.Down, out ns, out nt));
            Assert.AreEqual('가', ns);
            Assert.AreEqual('노', nt);
        }

        [Test]
        public void 분해_불발이면_전체_실패()
        {
            // 가→ (도착지에 ㄴ): ㅏ가 ㄴ과 결합 불가 → 실패, 아무 변화 없음
            Assert.AreEqual(PushResultType.Fail, Push('가', 'ㄴ', Direction.Right, out var ns, out var nt));
            Assert.AreEqual('가', ns);
            Assert.AreEqual('ㄴ', nt);
        }

        // ---- 3. 이동 / 잠김 ----

        [Test]
        public void 낱자모는_자유_이동()
        {
            foreach (var d in GameSession.DirectionsAll)
            {
                Assert.AreEqual(PushResultType.Move, Push('ㄱ', '\0', d, out _, out var nt));
                Assert.AreEqual('ㄱ', nt);
            }
        }

        [Test]
        public void 조합없는_축으로만_통째_이동()
        {
            // 가: 세로 이동 가능
            Assert.AreEqual(PushResultType.Move, Push('가', '\0', Direction.Up, out _, out _));
            Assert.AreEqual(PushResultType.Move, Push('가', '\0', Direction.Down, out _, out _));
            // 고: 가로 이동 가능
            Assert.AreEqual(PushResultType.Move, Push('고', '\0', Direction.Left, out _, out _));
            // 곤: 받침 있어도 속이 전부 세로라 가로 이동 가능
            Assert.AreEqual(PushResultType.Move, Push('곤', '\0', Direction.Right, out _, out _));
            // 개: 세로 이동 가능 (ㅐ 내부는 가로)
            Assert.AreEqual(PushResultType.Move, Push('개', '\0', Direction.Up, out _, out _));
        }

        [Test]
        public void 간_좌우_잠김()
        {
            // 간: 겉=세로(분해), 속에 가로 조합(ㄱ+ㅏ) → 좌우는 빈칸이어도 아무 일도 없음
            Assert.AreEqual(PushResultType.Fail, Push('간', '\0', Direction.Left, out _, out _));
            Assert.AreEqual(PushResultType.Fail, Push('간', '\0', Direction.Right, out _, out _));
        }

        [Test]
        public void 꼬_가로_잠김()
        {
            // 꼬: ㄲ 내부가 가로 조합 → 가로 잠김, 세로는 분해
            Assert.AreEqual(PushResultType.Fail, Push('꼬', '\0', Direction.Left, out _, out _));
            Assert.AreEqual(PushResultType.SplitMove, Push('꼬', '\0', Direction.Up, out var ns, out var nt));
            Assert.AreEqual(('ㅗ', 'ㄲ'), (ns, nt));
        }

        // ---- 4. 실패 ----

        [Test]
        public void 보드밖_없는칸_실패()
        {
            Assert.AreEqual(PushResultType.Fail,
                PushLogic.Resolve('ㄱ', '\0', targetExists: false, Direction.Right, out _, out _));
        }

        [Test]
        public void 줄밀기_없음()
        {
            // ㄱ을 ㅁ 쪽으로: 합성 불가, ㅁ은 밀려나지 않음 → 실패
            Assert.AreEqual(PushResultType.Fail, Push('ㄱ', 'ㅁ', Direction.Right, out _, out _));
        }
    }
}
