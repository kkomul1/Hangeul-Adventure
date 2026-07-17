using System.Collections.Generic;
using System.Threading.Tasks;
using HangeulAdventure.Engine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 플레이 중 입력 처리 (명세 9장): 드래그/클릭+방향키 밀기, Space/슬롯 클릭 수집,
    /// Q/E 필터, Z 되돌리기, R 재시작. 행동 후 뷰·HUD 갱신과 클리어 판정.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        private GameSession _session;
        private BoardView _board;
        private GameHud _hud;
        private Camera _cam;

        private int _selX = -1, _selY = -1;
        private bool _dragging;
        private Vector2 _dragStartWorld;
        private int _dragTileX, _dragTileY;
        private const float DragThreshold = 0.35f;

        private bool _finished;

        private Task<HintOutcome> _hintTask;
        private int _boardGen;      // 보드가 바뀔 때마다 증가 (수집은 MoveCount가 안 오르므로 별도 세대)
        private int _hintGen = -1;  // 힌트 요청 시점의 _boardGen

        /// <summary>분해 직후 결과 글자로 포커스를 옮긴다 (A-⑩). 설정 토글 키, 기본 켬.</summary>
        private static bool FocusAfterSplit => PlayerPrefs.GetInt("focus_after_split", 1) == 1;

        /// <summary>전투 모드: 클리어 시 기록/팝업 대신 콜백만 호출 (BattleScreen이 처리).</summary>
        public bool BattleMode;
        public event System.Action<GameSession> PuzzleCleared;
        public event System.Action ActionTaken; // 성공한 행동마다 (전투의 행동 수 감시용)

        public void Bind(GameSession session, BoardView board, GameHud hud, Camera cam)
        {
            _session = session;
            _board = board;
            _hud = hud;
            _cam = cam;
            _selX = _selY = -1;
            _dragging = false;
            _finished = false;
            _hintTask = null;   // 이전 세션의 힌트 결과는 버린다
            _boardGen = 0;
            _hintGen = -1;

            _hud.SlotClicked -= OnSlotClicked;
            _hud.SlotClicked += OnSlotClicked;
            _hud.UndoClicked -= OnUndo;
            _hud.UndoClicked += OnUndo;
            _hud.ResetClicked -= OnReset;
            _hud.ResetClicked += OnReset;
            _hud.HintClicked -= OnHintRequested;
            _hud.HintClicked += OnHintRequested;
        }

        private void Update()
        {
            PollHint(); // 클리어 후 도착한 결과도 정리해야 하므로 아래 가드보다 앞

            // ESC = 퍼즐 나가기 (나가기 버튼과 같은 경로). 클리어 후에도 동작해야 하므로 _finished 가드 앞에 둔다
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _hud.RaiseExit();
                return;
            }

            if (_session == null || _finished) return;
            // 힌트 팝업이 뜬 동안 보드를 조작하면 팝업의 "이동 전" 보드가 실제와 어긋난다
            if (_hud.HintPopupOpen) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null)
            {
                // Q/E 필터 (홀드) — 눌림/뗌 엣지와 행동 시에만 재계산
                if (kb.qKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame
                    || kb.qKey.wasReleasedThisFrame || kb.eKey.wasReleasedThisFrame)
                    RefreshHighlights();

                if (kb.zKey.wasPressedThisFrame) OnUndo();
                if (kb.rKey.wasPressedThisFrame) OnReset();
                if (kb.hKey.wasPressedThisFrame) OnHintRequested();

                // 방향키/WASD: 선택 타일 밀기
                if (HasSelection())
                {
                    if (Pressed(kb.upArrowKey, kb.wKey)) DoPush(Direction.Up);
                    else if (Pressed(kb.downArrowKey, kb.sKey)) DoPush(Direction.Down);
                    else if (Pressed(kb.leftArrowKey, kb.aKey)) DoPush(Direction.Left);
                    else if (Pressed(kb.rightArrowKey, kb.dKey)) DoPush(Direction.Right);
                    else if (kb.spaceKey.wasPressedThisFrame) DoCollect(-1);
                    else if (kb.xKey.wasPressedThisFrame) DoRotate();
                }
            }

            // 마우스: 클릭 선택 + 드래그 밀기 (키보드 없어도 동작)
            if (mouse != null)
            {
                Vector2 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
                if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
                {
                    var tile = TileAt(world);
                    if (tile != null)
                    {
                        Select(tile.X, tile.Y);
                        _dragging = true;
                        _dragStartWorld = world;
                        _dragTileX = tile.X;
                        _dragTileY = tile.Y;
                    }
                }
                else if (mouse.rightButton.wasPressedThisFrame && !IsPointerOverUi())
                {
                    var tile = TileAt(world);
                    if (tile != null) { Select(tile.X, tile.Y); DoRotate(); }
                }
                else if (_dragging && mouse.leftButton.wasReleasedThisFrame)
                {
                    _dragging = false;
                    Vector2 delta = world - _dragStartWorld;
                    if (delta.magnitude >= DragThreshold)
                    {
                        var d = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                            ? (delta.x > 0 ? Direction.Right : Direction.Left)
                            : (delta.y > 0 ? Direction.Up : Direction.Down);
                        Select(_dragTileX, _dragTileY);
                        DoPush(d);
                    }
                }
            }
        }

        /// <summary>UI 위 클릭이 보드까지 관통하지 않도록 (버튼/슬롯 클릭 보호).</summary>
        private static bool IsPointerOverUi()
            => UnityEngine.EventSystems.EventSystem.current != null
               && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        /// <summary>Q/E 홀드 상태를 반영해 하이라이트 갱신. 필터 미사용 시 선택 외곽선.</summary>
        private void RefreshHighlights()
        {
            var kb = Keyboard.current;
            bool q = kb != null && kb.qKey.isPressed;
            bool e = kb != null && kb.eKey.isPressed;
            if (q || e)
            {
                _board.ShowFilter(q, e);
            }
            else
            {
                _board.ClearHighlights();
                ApplySelectionOutline();
            }
        }

        private static bool Pressed(KeyControl a, KeyControl b)
            => a.wasPressedThisFrame || b.wasPressedThisFrame;

        private TileView TileAt(Vector2 world)
        {
            var hit = Physics2D.OverlapPoint(world);
            return hit != null ? hit.GetComponentInParent<TileView>() : null;
        }

        private bool HasSelection() => _selX >= 0 && _session.GetCell(_selX, _selY) != Hangul.Empty;

        private void Select(int x, int y)
        {
            _selX = x; _selY = y;
            ApplySelectionOutline();
        }

        private void ApplySelectionOutline()
        {
            foreach (var t in _board.AllTiles()) t.SetSelected(false);
            if (HasSelection()) _board.GetTile(_selX, _selY)?.SetSelected(true);
        }

        private void DoPush(Direction d)
        {
            var report = _session.TryPush(_selX, _selY, d);
            _board.ApplyPush(report);

            if (!report.Success)
            {
                SfxPlayer.Instance?.Fail();
                return;
            }

            switch (report.Type)
            {
                case PushResultType.Move: SfxPlayer.Instance?.Move(); break;
                case PushResultType.Compose: SfxPlayer.Instance?.Compose(); GameHud.Learn(GameHud.ActCompose); break;
                case PushResultType.SplitCompose:
                    SfxPlayer.Instance?.Compose();
                    GameHud.Learn(GameHud.ActSplit);   // 연쇄 = 분해 + 합성
                    GameHud.Learn(GameHud.ActCompose);
                    break;
                case PushResultType.SplitMove: SfxPlayer.Instance?.Split(); GameHud.Learn(GameHud.ActSplit); break;
            }

            // 글자 도감: 합성으로 만든 완성 글자를 최초 등록 (모험 요소 1)
            if ((report.Type == PushResultType.Compose || report.Type == PushResultType.SplitCompose)
                && Hangul.IsSyllable(report.TargetAfter)
                && ProgressStore.RegisterGlyph(report.TargetAfter))
            {
                // 최초 등록 연출: 해당 타일 팝
                _board.GetTile(report.ToX, report.ToY)?.AnimatePop();
            }

            // 선택 추적: 이동/합성은 도착 칸. 분해는 설정에 따라 결과 글자(도착 칸) 또는 남은 성분(제자리)
            bool isSplit = report.Type == PushResultType.SplitMove || report.Type == PushResultType.SplitCompose;
            if (!isSplit || FocusAfterSplit)
                Select(report.ToX, report.ToY);
            else
                Select(report.FromX, report.FromY);

            AfterAction();
        }

        /// <summary>선택 타일 회전 (X키/우클릭, D-21). 유효한 자모가 되는 회전만 성공.</summary>
        private void DoRotate()
        {
            if (!HasSelection()) return;
            if (_session.TryRotate(_selX, _selY))
            {
                SfxPlayer.Instance?.Move();
                GameHud.Learn(GameHud.ActRotate);
                _board.SyncTiles(animate: true);
                AfterAction();
            }
            else
            {
                _board.GetTile(_selX, _selY)?.AnimateShake();
                SfxPlayer.Instance?.Fail();
            }
        }

        private void DoCollect(int slotIndex)
        {
            if (!HasSelection()) return;
            if (_session.TryCollect(_selX, _selY, slotIndex))
            {
                SfxPlayer.Instance?.Collect();
                GameHud.Learn(GameHud.ActCollect);
                _board.SyncTiles(animate: true);
                _selX = _selY = -1;
                AfterAction();
            }
            else
            {
                _board.GetTile(_selX, _selY)?.AnimateShake();
                SfxPlayer.Instance?.Fail();
            }
        }

        private void OnSlotClicked(int slotIndex) => DoCollect(slotIndex);

        private void OnUndo()
        {
            if (_finished) return;
            if (_session.Undo())
            {
                _board.SyncTiles(animate: false);
                _selX = _selY = -1;
                RefreshHighlights();
                _hud.Refresh();
                InvalidateHint();
            }
        }

        private void OnReset()
        {
            if (_finished) return;
            _session.Reset();
            _board.Bind(_session);
            _selX = _selY = -1;
            _hud.Refresh();
            InvalidateHint();
        }

        private void AfterAction()
        {
            InvalidateHint();
            _hud.Refresh();
            RefreshHighlights();
            ActionTaken?.Invoke();

            if (_session.IsCleared)
            {
                _finished = true;
                SfxPlayer.Instance?.Clear();
                if (BattleMode)
                {
                    PuzzleCleared?.Invoke(_session); // 전투가 결과 판정 (기록/팝업 없음)
                    return;
                }
                int earned = ProgressStore.Record(_session.Stage.id, _session.Stars(), _session.IsRuby,
                    _session.MoveCount, _session.Stage.difficulty);
                _hud.ShowClearPopup(earned);
            }
        }

        // ---- 힌트 (비동기 재솔브) ----

        private const int HintMaxStates = 500_000;

        /// <summary>힌트 1회 전체 예산 (기준 솔브 + 후보 재솔브 합계). 초과 시 "찾지 못했습니다".</summary>
        private const int HintBudgetMs = 6_000;

        private enum HintStatus { Move, Unsolvable, Exhausted }

        private readonly struct HintOutcome
        {
            public readonly HintStatus Status;
            public readonly GameHud.HintView View; // Status == Move일 때만 유효

            public HintOutcome(HintStatus status, GameHud.HintView view) { Status = status; View = view; }
        }

        private void OnHintRequested()
        {
            if (_session == null || _finished) return;
            if (_hintTask != null && !_hintTask.IsCompleted) return; // 계산 중 중복 요청 무시

            var snap = Snapshot(_session);
            int used = _session.MoveCount, declared = _session.Stage.minMoves;
            _hintGen = _boardGen;
            _hud.ShowHintPending();
            // 워커 스레드 실행: Engine 어셈블리는 UnityEngine을 참조하지 않는 순수 C#이고
            // (asmdef noEngineReferences) snap은 깊은 복사본이라 메인스레드와 공유 상태가 없다.
            _hintTask = Task.Run(() => ComputeHint(snap, used, declared));
        }

        /// <summary>보드가 바뀌면 대기 중인 힌트는 낡은 보드의 답이 되므로 버린다.</summary>
        private void InvalidateHint()
        {
            _boardGen++;
            _hud.HideHintPending();
        }

        private void PollHint()
        {
            if (_hintTask == null || !_hintTask.IsCompleted) return;
            var t = _hintTask;
            _hintTask = null;

            _hud.HideHintPending();
            if (_hintGen != _boardGen) return; // 대기 중 보드가 바뀌었다
            if (t.IsFaulted) { _hud.ShowHintMessage("힌트 계산에 실패했습니다."); return; }

            var r = t.Result;
            switch (r.Status)
            {
                case HintStatus.Move: _hud.ShowHintPopup(r.View); break;
                case HintStatus.Unsolvable:
                    _hud.ShowHintMessage("지금 상태로는 풀 수 없습니다 — 되돌리기(Z)나 재시작(R)을 해보세요.");
                    break;
                default:
                    _hud.ShowHintMessage("힌트를 찾지 못했습니다 — 보드가 너무 복잡합니다.");
                    break;
            }
        }

        /// <summary>
        /// 워커 스레드에서 실행. Solver.Solve는 최소 수만 알려주고 첫 수는 알려주지 않으므로,
        /// 후보 행동마다 재솔브해 "비용 + 남은 최소 수 == 현재 최소 수"인 첫 행동을 최적 행동으로 고른다.
        /// 후보 수만큼 솔브가 반복되므로 전체를 HintBudgetMs 하나로 묶어 제한한다.
        /// </summary>
        private static HintOutcome ComputeHint(StageData snap, int usedMoves, int declaredMin)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var baseline = Solver.Solve(snap, HintMaxStates, HintBudgetMs);
            if (baseline.Aborted) return new HintOutcome(HintStatus.Exhausted, default);
            if (!baseline.Solvable) return new HintOutcome(HintStatus.Unsolvable, default);

            int remain = baseline.MinMoves;
            // 초기 최적 수(minMoves)는 외부 CLI가 기입한 값 — 미기입(0 이하)이면 판정을 건너뛴다
            bool notOptimal = declaredMin > 0 && usedMoves + remain > declaredMin;

            foreach (var c in Candidates(snap))
            {
                int budget = HintBudgetMs - (int)sw.ElapsedMilliseconds;
                if (budget <= 0) return new HintOutcome(HintStatus.Exhausted, default);

                var after = Snapshot(c.After);
                var r = Solver.Solve(after, HintMaxStates, budget);
                if (r.Aborted) return new HintOutcome(HintStatus.Exhausted, default);
                if (!r.Solvable || c.Cost + r.MinMoves != remain) continue;

                return new HintOutcome(HintStatus.Move, new GameHud.HintView(
                    snap.cells, after.cells, c.Fx, c.Fy, c.Tx, c.Ty, c.Desc, notOptimal));
            }
            return new HintOutcome(HintStatus.Exhausted, default);
        }

        /// <summary>후보 행동 열거. 비용은 엔진이 정한다 (밀기·회전 = 1, 수집 = 0, D-15).</summary>
        private static IEnumerable<(GameSession After, int Cost, int Fx, int Fy, int Tx, int Ty, string Desc)>
            Candidates(StageData snap)
        {
            for (int y = 0; y < snap.height; y++)
            {
                for (int x = 0; x < snap.width; x++)
                {
                    int i = y * snap.width + x;
                    if (!snap.mask[i]) continue;
                    char tile = snap.cells[i];
                    if (tile == Hangul.Empty) continue;

                    foreach (Direction d in GameSession.DirectionsAll) // 순서는 Solver의 후보 생성과 동일
                    {
                        var s = new GameSession(snap);
                        var rep = s.TryPush(x, y, d);
                        if (rep.Success)
                            yield return (s, s.MoveCount, x, y, rep.ToX, rep.ToY, PushDesc(tile, d, rep.Type));
                    }

                    var rot = new GameSession(snap);
                    if (rot.TryRotate(x, y))
                        yield return (rot, rot.MoveCount, x, y, x, y, $"'{tile}' 타일 회전");

                    var col = new GameSession(snap);
                    if (col.TryCollect(x, y))
                        yield return (col, col.MoveCount, x, y, x, y, $"'{tile}' 타일 수집");
                }
            }
        }

        /// <summary>
        /// 진행 중인 상태를 솔버 입력용 StageData로 (미수집 슬롯만 목표로).
        /// 워커가 읽는 동안 메인스레드가 보드를 바꿔도 안전하도록 전부 새 배열로 복제한다.
        /// </summary>
        private static StageData Snapshot(GameSession s)
        {
            var st = s.Stage;
            var cells = new char[st.width * st.height];
            for (int y = 0; y < st.height; y++)
                for (int x = 0; x < st.width; x++)
                    cells[y * st.width + x] = s.GetCell(x, y);

            var left = new List<char>();
            for (int i = 0; i < s.SlotCount; i++)
                if (!s.IsSlotFilled(i)) left.Add(s.SlotChar(i));

            return new StageData
            {
                width = st.width,
                height = st.height,
                mask = (bool[])st.mask.Clone(),
                pinnedMask = st.pinnedMask == null ? null : (bool[])st.pinnedMask.Clone(),
                cells = cells,
                // 같은 문자 슬롯은 대칭이므로 그룹 분해 없이 하나로 (Solver의 수집 루프)
                goals = new[] { new GoalGroup { slots = left.ToArray() } },
            };
        }

        private static string PushDesc(char tile, Direction d, PushResultType type)
        {
            string dir = d switch
            {
                Direction.Up => "위로",
                Direction.Down => "아래로",
                Direction.Left => "왼쪽으로",
                _ => "오른쪽으로",
            };
            string suffix = type switch
            {
                PushResultType.Compose => " (합성)",
                PushResultType.SplitCompose => " (분해 + 연쇄 합성)",
                PushResultType.SplitMove => " (분해)",
                _ => "",
            };
            return $"'{tile}' 타일을 {dir} 밀기{suffix}";
        }
    }
}
