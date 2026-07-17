using NUnit.Framework;
using HangeulAdventure.Engine;
using HangeulAdventure.Game;
using UnityEngine;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>
    /// Resources/Battles의 모든 보스 시련 전수 검증 (명세 10장).
    /// 스테이지(Resources/Stages)와 달리 보스 시련은 그동안 자동 검증 사각지대였다 —
    /// SolverCli는 stage_*.json만 훑고 StageValidationTests도 Stages 한정이다.
    ///
    /// 시련은 제한 수 안에 풀 수 있어야 한다. 못 풀면 플레이어는 반격당할 뿐 진행이 막히진 않지만,
    /// 수학적으로 불가능한 시련은 '제한 초과 = 실력 부족'이라는 전투 규칙의 전제를 깨뜨린다.
    /// </summary>
    public class BattleTrialSolvableTests
    {
        [Test, Timeout(600000)]
        public void 모든_보스_시련_솔버_검증()
        {
            var assets = Resources.LoadAll<TextAsset>("Battles");
            if (assets.Length == 0)
                Assert.Ignore("아직 전투 설정이 없음 (Resources/Battles).");

            foreach (var asset in assets)
            {
                var config = JsonUtility.FromJson<BattleConfig>(asset.text);
                Assert.IsNotNull(config?.trials, $"{asset.name}: trials 파싱 실패");
                Assert.Greater(config.trials.Length, 0, $"{asset.name}: 시련이 비어 있음");

                for (int i = 0; i < config.trials.Length; i++)
                {
                    var trial = config.trials[i];
                    var stage = StageBuilder.FromRows(trial.rows, BuildGoals(trial.goals));

                    // 상태 상한은 기본값(500k) 유지 — 탐색 공간의 실제 경계이므로 건드리지 않는다.
                    // 시간 예산만 넉넉히: 에디터 Mono가 .NET보다 느려 기본 30초로는 억울한 중단이 날 수 있다.
                    // (중단이 나면 상한을 올리지 말고 시련을 축소할 것 — 아래 Aborted 단언이 그 신호다)
                    var r = Solver.Solve(stage, timeBudgetMs: 60_000);

                    string tag = $"{config.id}#{i} '{trial.goals}'";
                    // Aborted는 '풀이 불가 확정'이 아니라 탐색 중단이다. 통과로 흘려보내면
                    // 클리어 불가능한 시련이 그대로 출시된다.
                    Assert.IsFalse(r.Aborted, $"{tag}: 솔버 중단 (탐색 {r.ExploredStates}) — 시련이 너무 크다");
                    Assert.IsTrue(r.Solvable, $"{tag}: 풀이 불가");
                    Assert.LessOrEqual(r.MinMoves, trial.moveLimit,
                        $"{tag}: 최소 {r.MinMoves}수 > 제한 {trial.moveLimit}수 — 제한 내 클리어 불가");
                }
            }
        }

        /// <summary>BattleScreen.StartTrialPuzzle의 goals 파싱과 동일해야 런타임과 일치한다.</summary>
        private static GoalGroup[] BuildGoals(string goals)
        {
            var groups = new System.Collections.Generic.List<GoalGroup>();
            foreach (string part in goals.Split(','))
            {
                string g = part.Trim();
                if (g.Length > 0) groups.Add(new GoalGroup { display = g, slots = g.ToCharArray() });
            }
            return groups.ToArray();
        }
    }
}
