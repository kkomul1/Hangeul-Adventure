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
        public void 회전_유효한_모음만_이동수_포함()
        {
            var s = Session(new[] { "ㅏㄱ" }, "고");
            // ㅏ 회전: ㅏ→ㅜ→ㅓ→ㅗ (시계방향 3회)
            Assert.IsTrue(s.TryRotate(0, 0));
            Assert.AreEqual('ㅜ', s.GetCell(0, 0));
            Assert.IsTrue(s.TryRotate(0, 0));
            Assert.IsTrue(s.TryRotate(0, 0));
            Assert.AreEqual('ㅗ', s.GetCell(0, 0));
            Assert.AreEqual(3, s.MoveCount);
            // 자음은 회전 불가
            Assert.IsFalse(s.TryRotate(1, 0));
            Assert.AreEqual(3, s.MoveCount);
            // undo로 복원
            Assert.IsTrue(s.Undo());
            Assert.AreEqual('ㅓ', s.GetCell(0, 0));
        }

        [Test]
        public void 바위는_모든_상호작용_불가()
        {
            var s = Session(new[] { "ㄱ@ㅏ", "..." }, "가");
            Assert.IsFalse(s.TryPush(0, 0, Direction.Right).Success); // 바위로 밀기 실패
            Assert.IsFalse(s.TryPush(1, 0, Direction.Down).Success);  // 바위 자체를 밀기 실패
            Assert.IsFalse(s.TryRotate(1, 0));                        // 바위 회전 불가
            Assert.IsFalse(s.TryCollect(1, 0));                       // 바위 수집 불가
            Assert.AreEqual(0, s.MoveCount);
        }

        [Test]
        public void 사슬칸_밀기불가_회전_합성대상_수집_가능()
        {
            // [ㄱ][ㅏ!]: ㅏ가 사슬 칸 — ㅏ는 못 밀지만 ㄱ이 ㅏ로 합성해 들어갈 수 있다
            var stage = StageBuilder.FromRows(new[] { "ㄱㅏ" }, new[] { ".!" }, StageBuilder.Goal("가"));
            var s = new GameSession(stage);
            Assert.IsFalse(s.TryPush(1, 0, Direction.Left).Success);  // 사슬 타일 밀기 실패
            Assert.IsTrue(s.TryRotate(1, 0));                          // 회전은 가능 (ㅏ→ㅜ)
            Assert.IsTrue(s.Undo());
            Assert.IsTrue(s.TryPush(0, 0, Direction.Right).Success);   // 사슬 칸으로 합성은 가능
            Assert.AreEqual('가', s.GetCell(1, 0));
            Assert.IsTrue(s.TryCollect(1, 0));                         // 수집도 가능
            Assert.IsTrue(s.IsCleared);

            // 솔버도 동일 판정: 합성 1수
            var r = Solver.Solve(stage);
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(1, r.MinMoves);
        }

        [Test]
        public void 회전으로_풀리는_스테이지_솔버()
        {
            // [ㅏ][ㄱ]: 목표 고 — ㄱ은 못 움직이고(ㅏ가 옆에서 역순 배치) 회전으로 ㅏ→ㅗ 만들어
            // ㄱ 아래로... 보드 재설계: 세로 2칸 [ㄱ / ㅏ]: ㅏ를 3회전해 ㅗ → 자동으로 고? 인접 세로 배치라
            // 회전 후 합성은 밀기가 필요. [ㄱ][.] / [ㅏ][.]: ㅏ 3회전(3) → ㄱ↓ 합성(4) → 수집 = 4
            var stage = StageBuilder.FromRows(new[] { "ㄱ.", "ㅏ." }, StageBuilder.Goal("고"));
            var r = Solver.Solve(stage);
            Assert.IsTrue(r.Solvable);
            Assert.AreEqual(4, r.MinMoves);
        }

        [Test]
        public void 자음_단독_목표_수집()
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
