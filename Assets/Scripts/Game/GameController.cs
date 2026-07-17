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

            _hud.SlotClicked -= OnSlotClicked;
            _hud.SlotClicked += OnSlotClicked;
            _hud.UndoClicked -= OnUndo;
            _hud.UndoClicked += OnUndo;
            _hud.ResetClicked -= OnReset;
            _hud.ResetClicked += OnReset;
        }

        private void Update()
        {
            if (_session == null || _finished) return;
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
            }
        }

        private void OnReset()
        {
            if (_finished) return;
            _session.Reset();
            _board.Bind(_session);
            _selX = _selY = -1;
            _hud.Refresh();
        }

        private void AfterAction()
        {
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
    }
}
