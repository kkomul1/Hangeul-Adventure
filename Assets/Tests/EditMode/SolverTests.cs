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
    }
}
