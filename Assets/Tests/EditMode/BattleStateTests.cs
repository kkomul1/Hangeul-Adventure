using NUnit.Framework;
using HangeulAdventure.Game;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>전투 상태 머신 (스토리기획 4장: 공격/수비/기술, 전조, 카운터).</summary>
    public class BattleStateTests
    {
        private static BattleConfig Config() => new BattleConfig
        {
            id = "test", name = "시험 사범", maxHp = 10, atk = 5, def = 1,
            trials = new[] { new TrialConfig { rows = new[] { "ㄱㅏ" }, goals = "가", moveLimit = 3 } },
        };

        /// <summary>시련이 여러 개인 설정 (셔플백 검증용). goals가 곧 시련 식별자.</summary>
        private static BattleConfig MultiConfig(int trialCount)
        {
            var trials = new TrialConfig[trialCount];
            for (int i = 0; i < trialCount; i++)
                trials[i] = new TrialConfig { rows = new[] { "ㄱㅏ" }, goals = $"t{i}", moveLimit = 3 };
            return new BattleConfig
            {
                id = "multi", name = "시험 사범", maxHp = 999, atk = 5, def = 1, trials = trials,
            };
        }

        [Test]
        public void 셔플백_한_주기_내_중복_없음()
        {
            const int n = 8;
            var b = new BattleState(MultiConfig(n), 0, 0, seed: 12345);

            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < n; i++)
            {
                var (trial, _) = b.NextTrial();
                Assert.IsTrue(seen.Add(trial.goals), $"한 주기 안에서 시련 중복 출제: {trial.goals}");
            }
            Assert.AreEqual(n, seen.Count); // 주기 = 풀 전체를 한 번씩
        }

        [Test]
        public void 셔플백_경계에서_직전_시련이_연속되지_않음()
        {
            const int n = 4;
            // 시드를 바꿔가며 백 경계(주기 이음매)에서 같은 시련이 연달아 나오지 않는지 확인
            for (int seed = 0; seed < 50; seed++)
            {
                var b = new BattleState(MultiConfig(n), 0, 0, seed);
                string prev = null;
                for (int i = 0; i < n * 4; i++)
                {
                    var (trial, _) = b.NextTrial();
                    Assert.AreNotEqual(prev, trial.goals,
                        $"시드 {seed}, {i}번째: 직전 시련({prev})이 연속 재출제됨");
                    prev = trial.goals;
                }
            }
        }

        [Test]
        public void 시련이_하나뿐이면_계속_같은_시련()
        {
            // trials 길이 1에서 셔플백이 깨지지 않아야 한다 (예외/무한루프 없이 항상 그 시련)
            var b = new BattleState(Config(), 0, 0, seed: 7);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual("가", b.NextTrial().trial.goals);
        }

        [Test]
        public void 같은_시드는_같은_출제_순서()
        {
            var a = new BattleState(MultiConfig(6), 0, 0, seed: 99);
            var b = new BattleState(MultiConfig(6), 0, 0, seed: 99);
            for (int i = 0; i < 12; i++)
                Assert.AreEqual(a.NextTrial().trial.goals, b.NextTrial().trial.goals);
        }

        [Test]
        public void 공격_최소피해_보정()
        {
            var b = new BattleState(Config(), weaponAtk: 0, armorDef: 0); // atk 3 - def 1 = 2
            b.Act(BattleAction.Attack);
            Assert.AreEqual(8, b.EnemyHp);
            Assert.AreEqual(BattlePhase.Trial, b.Phase);
        }

        [Test]
        public void 카운터_성공()
        {
            var b = new BattleState(Config(), 0, 0);
            b.Act(BattleAction.Attack);              // 적 8
            var (_, limit) = b.NextTrial();          // 제한 3
            int dmg = b.ResolveTrial(movesUsed: 2, limit, cleared: true);
            Assert.AreEqual(0, dmg);                 // 피해 없음
            Assert.AreEqual(6, b.EnemyHp);           // 카운터 2
            Assert.AreEqual(BattlePhase.PlayerTurn, b.Phase);
        }

        [Test]
        public void 초과시_초과분만큼_강한_피격()
        {
            var b = new BattleState(Config(), 0, armorDef: 1); // def 1, hp 22
            b.Act(BattleAction.Attack);
            var (_, limit) = b.NextTrial();
            int dmg = b.ResolveTrial(movesUsed: limit + 2, limit, cleared: true);
            Assert.AreEqual(6, dmg);                 // 5 + 초과2 - 방1 = 6
            Assert.AreEqual(16, b.PlayerHp);
        }

        [Test]
        public void 수비는_피해절반_그리고_제한수_보너스()
        {
            var b = new BattleState(Config(), 0, 0); // hp 20
            b.Act(BattleAction.Guard);
            var (_, limit) = b.NextTrial();
            Assert.AreEqual(5, limit);               // 3 + 2
            int dmg = b.ResolveTrial(limit + 1, limit, cleared: true); // 초과1: raw 5+1=6 → 절반 3
            Assert.AreEqual(3, dmg);
        }

        [Test]
        public void 기술은_카운터_2배_제한수_패널티()
        {
            var b = new BattleState(Config(), weaponAtk: 1, armorDef: 0); // atk 4
            b.Act(BattleAction.Skill);               // 1.5배 = 6 - 1 = 5 → 적 5
            Assert.AreEqual(5, b.EnemyHp);
            var (_, limit) = b.NextTrial();
            Assert.AreEqual(1, limit);               // 3 - 2
            b.ResolveTrial(1, limit, cleared: true); // 카운터 (4-1)*2 = 6 → 적 0
            Assert.AreEqual(BattlePhase.Victory, b.Phase);
        }

        [Test]
        public void 패배_판정()
        {
            var b = new BattleState(Config(), 0, 0); // hp 20
            for (int i = 0; i < 5 && b.Phase != BattlePhase.Defeat; i++)
            {
                b.Act(BattleAction.Attack);
                if (b.Phase != BattlePhase.Trial) break;
                var (_, limit) = b.NextTrial();
                b.ResolveTrial(0, limit, cleared: false); // 시련 실패 = 초과4 취급: 5+4-0=9
            }
            Assert.AreEqual(BattlePhase.Defeat, b.Phase); // 9x3 > 20
        }
    }
}
