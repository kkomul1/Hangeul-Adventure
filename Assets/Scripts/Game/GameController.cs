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
                case PushResultType.Compose:
                case PushResultType.SplitCompose: SfxPlayer.Instance?.Compose(); break;
                case PushResultType.SplitMove: SfxPlayer.Instance?.Split(); break;
            }

            // 선택 추적: 이동/합성이면 도착 칸, 분해면 제자리(남은 성분)
            if (report.Type == PushResultType.Move || report.Type == PushResultType.Compose)
                Select(report.ToX, report.ToY);
            else
                Select(report.FromX, report.FromY);

            AfterAction();
        }

        private void DoCollect(int slotIndex)
        {
            if (!HasSelection()) return;
            if (_session.TryCollect(_selX, _selY, slotIndex))
            {
                SfxPlayer.Instance?.Collect();
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

            if (_session.IsCleared)
            {
                _finished = true;
                SfxPlayer.Instance?.Clear();
                int earned = ProgressStore.Record(_session.Stage.id, _session.Stars(), _session.IsRuby,
                    _session.MoveCount, _session.Stage.difficulty);
                _hud.ShowClearPopup(earned);
            }
        }
    }
}
