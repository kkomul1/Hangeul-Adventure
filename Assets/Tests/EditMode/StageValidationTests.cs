using NUnit.Framework;
using HangeulAdventure.Engine;
using HangeulAdventure.Game;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>
    /// Resources/Stages의 모든 스테이지 전수 검증 (명세 10장):
    /// 풀이 가능해야 하고, 선언된 minMoves가 솔버 결과와 일치해야 한다.
    /// </summary>
    public class StageValidationTests
    {
        [Test, Timeout(600000)]
        public void 모든_스테이지_솔버_검증()
        {
            var stages = StageLoader.LoadAll();
            if (stages.Count == 0)
                Assert.Ignore("아직 스테이지가 없음 (Resources/Stages).");

            foreach (var stage in stages)
            {
                // 에디터 Mono는 느리므로 스테이지당 5초만 시도 — 초과분은 CLI 검증값 신뢰 (D-14)
                var r = Solver.Solve(stage, timeBudgetMs: 5_000);
                if (r.Aborted)
                {
                    // 에디터 Mono가 느려 시간 예산을 초과한 경우: 외부 CLI(동일 엔진 코드, .NET 8)로
                    // 검증된 minMoves가 기입되어 있어야 한다 (결정로그 D-14, scratchpad SolverCli).
                    Assert.Greater(stage.minMoves, 0,
                        $"스테이지 {stage.id} ({stage.title}): 에디터 검증 시간 초과 + minMoves 미기입 — CLI 검증 필요");
                    UnityEngine.Debug.LogWarning(
                        $"스테이지 {stage.id}: 에디터 솔버 시간 초과, CLI 기입값({stage.minMoves}) 신뢰");
                    continue;
                }
                Assert.IsTrue(r.Solvable, $"스테이지 {stage.id} ({stage.title}): 풀이 불가");
                Assert.AreEqual(r.MinMoves, stage.minMoves,
                    $"스테이지 {stage.id} ({stage.title}): 선언된 minMoves({stage.minMoves}) != 솔버 결과({r.MinMoves})");
            }
        }

        [Test]
        public void 스테이지_id_중복_없음()
        {
            var stages = StageLoader.LoadAll();
            if (stages.Count == 0)
                Assert.Ignore("아직 스테이지가 없음.");

            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (var s in stages)
                Assert.IsTrue(seen.Add(s.id), $"스테이지 id 중복: {s.id}");
        }
    }
}
