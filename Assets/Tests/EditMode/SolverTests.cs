using NUnit.Framework;
using HangeulAdventure.Engine;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>BFS 솔버 (명세 10장).</summary>
    public class SolverTests
    {
        private static StageData Stage(string[] rows, params string[] goals)
        {
            var groups = new GoalGroup[goals.Length];
            for (int i = 0; i < goals.Length; i++) groups[i] = StageBuilder.Goal(goals[i]);
            return StageBuilder.FromRows(rows, groups);
        }

        [Test]
        public void 최소수_합성1_수집은_무료()
        {
            var r = Solver.Solve(Stage(new[] { "ㄱㅏ" }, "가"));
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(1, r.MinMoves); // 합성1 (수집 0)
        }

        [Test]
        public void 최소수_이동_포함()
        {
            var r = Solver.Solve(Stage(new[] { "ㄱ.ㅏ" }, "가"));
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(2, r.MinMoves); // 이동1 + 합성1 (수집 0)
        }

        [Test]
        public void 방향_불일치는_풀이불가()
        {
            // 세로 1열에 ㅏ(위) ㄱ(아래): 세로모음은 세로 결합 불가, 빈칸도 없음
            var r = Solver.Solve(Stage(new[] { "ㅏ", "ㄱ" }, "가"));
            Assert.IsFalse(r.Solvable);
        }

        [Test]
        public void 자모_부족은_풀이불가()
        {
            var r = Solver.Solve(Stage(new[] { "ㄱㅁ" }, "가"));
            Assert.IsFalse(r.Solvable);
        }

        [Test]
        public void 연쇄_경로_탐색()
        {
            // 사ㅣ → (사→: ㅅ|ㅐ) → (ㅐ←: 새) → 수집(무료) = 2수
            var r = Solver.Solve(Stage(new[] { "사ㅣ" }, "새"));
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(2, r.MinMoves);
        }

        [Test]
        public void 받침_왕복_경로()
        {
            // 세로 1열: 간(위) ㅗ(아래). 간↓: 가 남고 ㄴ이 ㅗ와 결합해 노 → 수집(무료) = 1수
            var r = Solver.Solve(Stage(new[] { "간", "ㅗ" }, "노"));
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(1, r.MinMoves);
        }

        [Test]
        public void 비정형_보드()
        {
            // L자: 오른쪽 위가 없는 칸
            var r = Solver.Solve(Stage(new[] { "ㄱ#", ".ㅏ" }, "가"));
            // ㄱ(0,1)↓ 이동 → (0,0), ㄱ→ㅏ 합성 → 가, 수집(무료) = 2수
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(2, r.MinMoves);
        }

        [Test]
        public void 목표_두개()
        {
            var r = Solver.Solve(Stage(new[] { "ㄱㅏ", "ㄴㅏ" }, "가", "나"));
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(2, r.MinMoves); // 합성2 (수집 0)
        }

        // ── 아래 3개는 솔버 내부 최적화(M6)의 전제를 지킨다.
        //    깨지면 조용한 오답이 아니라 여기서 먼저 터지게 하는 것이 목적이다.

        [Test]
        public void 바위는_길을_막는다()
        {
            // 솔버는 바위 칸을 '없는 칸'과 동등하게 보고 상태 인코딩에서 뺀다.
            // 그 동등성이 성립하려면 바위가 밀기를 막아야 한다 — 한 줄에 ㄱ@ㅏ면 영영 못 만난다.
            var blocked = Solver.Solve(Stage(new[] { "ㄱ@ㅏ" }, "가"));
            Assert.IsFalse(blocked.Solvable, "바위 너머로 합성되면 안 된다");

            // 우회로가 있으면 풀린다 (아랫줄로 돌아가 ㅏ의 왼쪽에서 합성)
            var around = Solver.Solve(Stage(new[] { "ㄱ@ㅏ", "..." }, "가"));
            Assert.IsTrue(around.Solvable, "바위를 우회하는 경로는 살아 있어야 한다");
            Assert.AreEqual(4, around.MinMoves);
        }

        [Test]
        public void 회전으로만_되는_모음도_찾는다()
        {
            // 솔버는 알파벳을 '원자 예산'으로 추린다. 회전 순환(ㅏ→ㅜ→ㅓ→ㅗ)을 한 부류로 세지
            // 않으면 ㅓ가 예산 밖으로 밀려나 '거'를 영영 못 만든다고 오판한다.
            var r = Solver.Solve(Stage(new[] { "ㄱㅏ" }, "거"));
            Assert.IsTrue(r.Solvable, "ㅏ를 두 번 회전해 ㅓ를 만들 수 있어야 한다");
            Assert.AreEqual(3, r.MinMoves); // 회전2 + 가로합성1 (수집 0)

            // 가로모음(ㅜ)은 세로로만 붙으므로 세로 여유가 있어야 한다
            var vertical = Solver.Solve(Stage(new[] { "ㄱㅏ", ".." }, "구"));
            Assert.IsTrue(vertical.Solvable);
            Assert.AreEqual(4, vertical.MinMoves);
        }

        [Test]
        public void 분해로_생기는_자모도_알파벳에_있다()
        {
            // ㄲ을 분해하면 ㄱ 두 개가 나온다. 원자 예산이 ㄱ을 2개로 세지 못하면
            // 초성과 받침에 쓸 ㄱ이 모자란다며 '각'을 풀이불가로 오판한다.
            var r = Solver.Solve(Stage(new[] { "ㄲㅏ", ".." }, "각"));
            Assert.IsTrue(r.Solvable, "ㄲ 분해로 ㄱ 두 개를 얻어 '각'을 만들 수 있어야 한다");
            Assert.AreEqual(5, r.MinMoves);
        }
    }
}
