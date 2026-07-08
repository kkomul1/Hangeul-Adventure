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
        [Test]
        public void 모든_스테이지_솔버_검증()
        {
            var stages = StageLoader.LoadAll();
            if (stages.Count == 0)
                Assert.Ignore("아직 스테이지가 없음 (Resources/Stages).");

            foreach (var stage in stages)
            {
                var r = Solver.Solve(stage);
                Assert.IsFalse(r.Aborted, $"스테이지 {stage.id} ({stage.title}): 상태 폭발로 검증 불가 — 단순화 필요");
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
