using NUnit.Framework;
using HangeulAdventure.Engine;
using HangeulAdventure.Game;

namespace HangeulAdventure.Engine.Tests
{
    public class StageLoaderTests
    {
        [Test]
        public void JSON_파싱_기본()
        {
            const string json = @"{
                ""id"": 7, ""title"": ""받침"",
                ""rows"": [""ㄱ.ㅏ"", ""#.ㄴ""],
                ""goals"": [{""display"": ""간"", ""slots"": ""간""}],
                ""minMoves"": 5,
                ""stars"": [0, 0, 0]
            }";
            var stage = StageLoader.FromJson(json);

            Assert.AreEqual(7, stage.id);
            Assert.AreEqual(3, stage.width);
            Assert.AreEqual(2, stage.height);
            // 첫 행이 위: ㄱ은 (0,1), ㄴ은 (2,0), (0,0)은 없는 칸
            Assert.AreEqual('ㄱ', stage.cells[stage.Index(0, 1)]);
            Assert.AreEqual('ㄴ', stage.cells[stage.Index(2, 0)]);
            Assert.IsFalse(stage.CellExists(0, 0));
            Assert.IsTrue(stage.CellExists(1, 0));
            // 목표
            Assert.AreEqual(1, stage.goals.Length);
            Assert.AreEqual("간", stage.goals[0].display);
            Assert.AreEqual(new[] { '간' }, stage.goals[0].slots);
            // 별 기본 공식 적용 (stars[2]==0이므로): 3별 = 5+1 = 6
            Assert.AreEqual(6, stage.starThresholds[2]);
        }

        [Test]
        public void JSON_단어_슬롯()
        {
            const string json = @"{
                ""id"": 22, ""rows"": [""사과""],
                ""goals"": [{""display"": ""사과"", ""slots"": ""사과""}],
                ""minMoves"": 2
            }";
            var stage = StageLoader.FromJson(json);
            Assert.AreEqual(new[] { '사', '과' }, stage.goals[0].slots);

            var session = new GameSession(stage);
            Assert.AreEqual(2, session.SlotCount);
        }

        [Test]
        public void JSON_명시적_별_기준()
        {
            const string json = @"{
                ""id"": 1, ""rows"": [""ㄱㅏ""],
                ""goals"": [{""slots"": ""가""}],
                ""minMoves"": 2, ""stars"": [0, 4, 3]
            }";
            var stage = StageLoader.FromJson(json);
            Assert.AreEqual(int.MaxValue, stage.starThresholds[0]); // 0 → 1별=클리어
            Assert.AreEqual(4, stage.starThresholds[1]);
            Assert.AreEqual(3, stage.starThresholds[2]);
            Assert.AreEqual("가", stage.goals[0].display); // display 생략 시 slots 사용
        }
    }
}
