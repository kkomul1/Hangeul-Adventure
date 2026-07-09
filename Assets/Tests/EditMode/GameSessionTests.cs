using NUnit.Framework;
using HangeulAdventure.Engine;

namespace HangeulAdventure.Engine.Tests
{
    /// <summary>보드 적용, 이동 수, 수집, undo (명세 5, 7, 8장).</summary>
    public class GameSessionTests
    {
        private static GameSession Session(string[] rows, params string[] goals)
        {
            var groups = new GoalGroup[goals.Length];
            for (int i = 0; i < goals.Length; i++) groups[i] = StageBuilder.Goal(goals[i]);
            var stage = StageBuilder.FromRows(rows, groups);
            return new GameSession(stage);
        }

        [Test]
        public void 성공한_밀기만_이동수_증가()
        {
            var s = Session(new[] { "ㄱ.ㅏ" }, "가");
            // (0,0)의 ㄱ을 →로: 빈칸 이동 성공
            Assert.IsTrue(s.TryPush(0, 0, Direction.Right).Success);
            Assert.AreEqual(1, s.MoveCount);
            // 보드 왼쪽 끝에서 ←로: 실패, 카운트 불변
            Assert.IsFalse(s.TryPush(1, 0, Direction.Up).Success); // 1x3 보드, 위는 밖
            Assert.AreEqual(1, s.MoveCount);
            // ㄱ→ㅏ 합성
            Assert.IsTrue(s.TryPush(1, 0, Direction.Right).Success);
            Assert.AreEqual(2, s.MoveCount);
            Assert.AreEqual('가', s.GetCell(2, 0));
            Assert.AreEqual('\0', s.GetCell(1, 0));
        }

        [Test]
        public void 없는칸으로_밀기_실패()
        {
            var s = Session(new[] { "ㄱ#" }, "가");
            Assert.IsFalse(s.TryPush(0, 0, Direction.Right).Success);
            Assert.AreEqual(0, s.MoveCount);
        }

        [Test]
        public void 수집_정확일치만_이동수_미포함()
        {
            var s = Session(new[] { "가나" }, "가");
            // '나'는 목표가 아님 → 수집 실패
            Assert.IsFalse(s.TryCollect(1, 0));
            Assert.AreEqual(0, s.MoveCount);
            // '가' 수집 성공 → 클리어. 수집은 이동 수 미포함 (D-15)
            Assert.IsTrue(s.TryCollect(0, 0));
            Assert.AreEqual(0, s.MoveCount);
            Assert.AreEqual('\0', s.GetCell(0, 0));
            Assert.IsTrue(s.IsCleared);
        }

        [Test]
        public void 자모_단독_목표_수집()
        {
            // 명세 8장: 목표는 자모 단독일 수도 있음 (커리큘럼 초반 "자모 찾기")
            var s = Session(new[] { "ㄱㅏ" }, "ㄱ");
            Assert.IsFalse(s.TryCollect(1, 0)); // ㅏ는 목표 아님
            Assert.IsTrue(s.TryCollect(0, 0));  // ㄱ 수집
            Assert.IsTrue(s.IsCleared);
        }

        [Test]
        public void 단어_슬롯_수집_순서무관_중복글자()
        {
            var s = Session(new[] { "수수" }, "수수");
            Assert.IsFalse(s.IsCleared);
            // 오른쪽 '수'를 먼저 수집해도 됨
            Assert.IsTrue(s.TryCollect(1, 0));
            Assert.IsFalse(s.IsCleared);
            Assert.IsTrue(s.TryCollect(0, 0));
            Assert.IsTrue(s.IsCleared);
            Assert.AreEqual(0, s.MoveCount); // 수집은 미카운트
        }

        [Test]
        public void 슬롯_지정_수집()
        {
            var s = Session(new[] { "사과" }, "사과");
            // 슬롯 1('과')을 지정해 '과' 수집
            Assert.IsTrue(s.TryCollect(1, 0, slotIndex: 1));
            // 슬롯 1은 이미 참 → '사'를 슬롯 1에 넣으려 하면 실패 (문자 불일치이기도 함)
            Assert.IsFalse(s.TryCollect(0, 0, slotIndex: 1));
            Assert.IsTrue(s.TryCollect(0, 0, slotIndex: 0));
            Assert.IsTrue(s.IsCleared);
        }

        [Test]
        public void undo_이동수와_상태_복원()
        {
            var s = Session(new[] { "ㄱ.ㅏ" }, "가");
            s.TryPush(0, 0, Direction.Right);
            s.TryPush(1, 0, Direction.Right);
            Assert.AreEqual('가', s.GetCell(2, 0));
            Assert.AreEqual(2, s.MoveCount);

            Assert.IsTrue(s.Undo());
            Assert.AreEqual(1, s.MoveCount);
            Assert.AreEqual('ㄱ', s.GetCell(1, 0));
            Assert.AreEqual('ㅏ', s.GetCell(2, 0));

            Assert.IsTrue(s.Undo());
            Assert.AreEqual(0, s.MoveCount);
            Assert.AreEqual('ㄱ', s.GetCell(0, 0));
            Assert.IsFalse(s.Undo()); // 더 되돌릴 것 없음
        }

        [Test]
        public void 수집_undo_복원()
        {
            var s = Session(new[] { "가" }, "가");
            Assert.IsTrue(s.TryCollect(0, 0));
            Assert.IsTrue(s.IsCleared);
            Assert.IsTrue(s.Undo());
            Assert.IsFalse(s.IsCleared);
            Assert.AreEqual('가', s.GetCell(0, 0));
            Assert.AreEqual(0, s.MoveCount);
        }

        [Test]
        public void 별_판정()
        {
            var stage = StageBuilder.FromRows(new[] { "ㄱㅏ" }, StageBuilder.Goal("가"));
            stage.minMoves = 1; // 합성 1 (수집은 미카운트)
            stage.starThresholds = StageData.DefaultStarThresholds(1); // 3별<=2

            var s = new GameSession(stage);
            Assert.IsTrue(s.TryPush(0, 0, Direction.Right).Success);
            Assert.IsTrue(s.TryCollect(1, 0));
            Assert.IsTrue(s.IsCleared);
            Assert.AreEqual(1, s.MoveCount);
            Assert.AreEqual(3, s.Stars());
            Assert.IsTrue(s.IsRuby); // 정확히 최소 수
        }

        [Test]
        public void 루비아닌_별()
        {
            var stage = StageBuilder.FromRows(new[] { "ㄱ.ㅏ" }, StageBuilder.Goal("가"));
            stage.minMoves = 2; // 이동 1 + 합성 1
            stage.starThresholds = StageData.DefaultStarThresholds(2); // 3별<=3, 2별<=4

            var s = new GameSession(stage);
            s.TryPush(0, 0, Direction.Right);  // 1
            s.TryPush(1, 0, Direction.Left);   // 2 (되돌아감 — 낭비)
            s.TryPush(0, 0, Direction.Right);  // 3
            s.TryPush(1, 0, Direction.Right);  // 4 합성
            Assert.IsTrue(s.TryCollect(2, 0)); // 수집 미카운트
            Assert.IsTrue(s.IsCleared);
            Assert.AreEqual(4, s.MoveCount);
            Assert.AreEqual(2, s.Stars()); // 3별 기준(3) 초과, 2별 기준(4) 이내
            Assert.IsFalse(s.IsRuby);
        }

        [Test]
        public void Q필터_합성가능_탐지()
        {
            var s = Session(new[] { "ㄱㅏ" }, "가");
            var list = new System.Collections.Generic.List<(int, int, Direction)>();
            s.FindComposablePushes(list);
            // ㄱ→ (합성), ㅏ← (합성) 두 개
            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void E필터_분해가능_탐지()
        {
            var s = Session(new[] { "가." }, "가");
            var list = new System.Collections.Generic.List<(int, int, Direction)>();
            s.FindSplittablePushes(list);
            // 가→ 분해 1개 (←는 보드 밖, ↑↓는 보드 밖)
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual((0, 0, Direction.Right), list[0]);
        }
    }
}
