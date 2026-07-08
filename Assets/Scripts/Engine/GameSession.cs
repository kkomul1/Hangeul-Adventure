using System;
using System.Collections.Generic;

namespace HangeulAdventure.Engine
{
    /// <summary>한 번의 밀기 결과 (연출용 정보 포함). Fail이면 SourceAfter/TargetAfter는 변경 전 값 그대로.</summary>
    public readonly struct PushReport
    {
        public readonly PushResultType Type;
        public readonly int FromX, FromY, ToX, ToY;
        public readonly char SourceAfter, TargetAfter;

        public PushReport(PushResultType type, int fx, int fy, int tx, int ty, char sourceAfter, char targetAfter)
        {
            Type = type; FromX = fx; FromY = fy; ToX = tx; ToY = ty;
            SourceAfter = sourceAfter; TargetAfter = targetAfter;
        }

        public bool Success => Type != PushResultType.Fail;
    }

    /// <summary>
    /// 진행 중인 스테이지의 상태: 보드, 수집 슬롯, 이동 수, undo (명세 5, 7, 8장).
    /// </summary>
    public class GameSession
    {
        public StageData Stage { get; }

        private char[] _cells;
        private bool[] _slotFilled;
        private int _moveCount;
        private readonly char[] _slots; // 평탄화된 목표 슬롯
        private readonly Stack<Snapshot> _undo = new Stack<Snapshot>();

        private readonly struct Snapshot
        {
            public readonly char[] Cells;
            public readonly bool[] SlotFilled;
            public readonly int MoveCount;
            public Snapshot(char[] c, bool[] s, int m) { Cells = c; SlotFilled = s; MoveCount = m; }
        }

        public GameSession(StageData stage)
        {
            Stage = stage;
            var slotList = stage.AllSlots();
            _slots = new char[slotList.Count];
            for (int i = 0; i < slotList.Count; i++) _slots[i] = slotList[i];
            Reset();
        }

        public int MoveCount => _moveCount;
        public int SlotCount => _slots.Length;
        public char SlotChar(int i) => _slots[i];
        public bool IsSlotFilled(int i) => _slotFilled[i];
        public char GetCell(int x, int y) => Stage.CellExists(x, y) ? _cells[Stage.Index(x, y)] : Hangul.Empty;
        public bool CanUndo => _undo.Count > 0;

        public bool IsCleared
        {
            get
            {
                foreach (bool f in _slotFilled) if (!f) return false;
                return true;
            }
        }

        public void Reset()
        {
            _cells = (char[])Stage.cells.Clone();
            _slotFilled = new bool[_slots.Length];
            _moveCount = 0;
            _undo.Clear();
        }

        /// <summary>(x,y)의 타일을 방향 d로 민다. 실패 시 상태·이동 수 불변 (명세 5장 4항, 7장).</summary>
        public PushReport TryPush(int x, int y, Direction d)
        {
            var (dx, dy) = d.Delta();
            int tx = x + dx, ty = y + dy;

            char mover = GetCell(x, y);
            bool targetExists = Stage.CellExists(tx, ty);
            char target = targetExists ? _cells[Stage.Index(tx, ty)] : Hangul.Empty;

            var type = PushLogic.Resolve(mover, target, targetExists, d, out char newSource, out char newTarget);
            if (type == PushResultType.Fail)
                return new PushReport(type, x, y, tx, ty, mover, target);

            PushUndo();
            _cells[Stage.Index(x, y)] = newSource;
            _cells[Stage.Index(tx, ty)] = newTarget;
            _moveCount++;
            return new PushReport(type, x, y, tx, ty, newSource, newTarget);
        }

        /// <summary>
        /// (x,y)의 타일을 수집한다 (명세 8장). slotIndex=-1이면 첫 번째 일치 빈 슬롯.
        /// 정확 일치하는 미수집 슬롯이 있어야 성공. 성공 시 이동 수 +1.
        /// </summary>
        public bool TryCollect(int x, int y, int slotIndex = -1)
        {
            char tile = GetCell(x, y);
            if (tile == Hangul.Empty) return false;

            int idx = -1;
            if (slotIndex >= 0)
            {
                if (slotIndex < _slots.Length && !_slotFilled[slotIndex] && _slots[slotIndex] == tile)
                    idx = slotIndex;
            }
            else
            {
                for (int i = 0; i < _slots.Length; i++)
                    if (!_slotFilled[i] && _slots[i] == tile) { idx = i; break; }
            }
            if (idx < 0) return false;

            PushUndo();
            _cells[Stage.Index(x, y)] = Hangul.Empty;
            _slotFilled[idx] = true;
            _moveCount++;
            return true;
        }

        public bool Undo()
        {
            if (_undo.Count == 0) return false;
            var s = _undo.Pop();
            _cells = s.Cells;
            _slotFilled = s.SlotFilled;
            _moveCount = s.MoveCount;
            return true;
        }

        private void PushUndo()
            => _undo.Push(new Snapshot((char[])_cells.Clone(), (bool[])_slotFilled.Clone(), _moveCount));

        /// <summary>클리어 시 별 개수 (0~3). 1별은 클리어 자체 (t[0]은 사용하지 않음 — 결정로그 D-05).</summary>
        public int Stars()
        {
            if (!IsCleared) return 0;
            int[] t = Stage.starThresholds ?? StageData.DefaultStarThresholds(Stage.minMoves);
            if (_moveCount <= t[2]) return 3;
            if (_moveCount <= t[1]) return 2;
            return 1;
        }

        /// <summary>정확히 솔버 최소 수로 클리어했는가 (명세 7장 루비 별).</summary>
        public bool IsRuby => IsCleared && _moveCount == Stage.minMoves;

        /// <summary>선언된 minMoves보다 적게 클리어 — 스테이지 데이터 오류 신호 (검증용).</summary>
        public bool BeatDeclaredMin => IsCleared && _moveCount < Stage.minMoves;

        // ---- Q/E 필터 지원 (명세 9장) ----
        // non-alloc: 호출자가 버퍼를 재사용한다. 보드는 행동 시에만 변하므로
        // UI는 행동 이벤트 때만 재계산하면 된다 (매 프레임 호출 불필요).

        /// <summary>지금 밀어서 합성(연쇄 포함)이 일어나는 (위치, 방향)을 buffer에 채운다. Q 필터.</summary>
        public void FindComposablePushes(List<(int x, int y, Direction d)> buffer)
        {
            FindPushes(buffer, compose: true);
        }

        /// <summary>지금 분해가 가능한 (위치, 방향)을 buffer에 채운다. E 필터.</summary>
        public void FindSplittablePushes(List<(int x, int y, Direction d)> buffer)
        {
            FindPushes(buffer, compose: false);
        }

        private void FindPushes(List<(int x, int y, Direction d)> buffer, bool compose)
        {
            buffer.Clear();
            for (int y = 0; y < Stage.height; y++)
            {
                for (int x = 0; x < Stage.width; x++)
                {
                    if (!Stage.CellExists(x, y)) continue;
                    char tile = _cells[Stage.Index(x, y)];
                    if (tile == Hangul.Empty) continue;

                    foreach (Direction d in DirectionsAll)
                    {
                        var (dx, dy) = d.Delta();
                        int tx = x + dx, ty = y + dy;
                        bool exists = Stage.CellExists(tx, ty);
                        char target = exists ? _cells[Stage.Index(tx, ty)] : Hangul.Empty;
                        var t = PushLogic.Resolve(tile, target, exists, d, out _, out _);
                        bool hit = compose
                            ? (t == PushResultType.Compose || t == PushResultType.SplitCompose)
                            : (t == PushResultType.SplitMove || t == PushResultType.SplitCompose);
                        if (hit) buffer.Add((x, y, d));
                    }
                }
            }
        }

        public static readonly Direction[] DirectionsAll =
            { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
    }
}
