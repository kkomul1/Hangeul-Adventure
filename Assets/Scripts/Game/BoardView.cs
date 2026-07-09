using System.Collections.Generic;
using HangeulAdventure.Engine;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 보드의 시각 표현: 바닥 칸(빈칸) + 타일. 없는 칸은 그리지 않는다 (명세 1장).
    /// 매 행동 후 세션 상태로부터 재동기화하는 단순한 방식.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private GameSession _session;
        private readonly Dictionary<(int x, int y), TileView> _tiles = new Dictionary<(int, int), TileView>();
        private readonly List<GameObject> _floors = new List<GameObject>();
        private static readonly Color FloorColor = new Color(0.88f, 0.85f, 0.79f);

        public GameSession Session => _session;

        public void Bind(GameSession session)
        {
            _session = session;
            Clear();
            BuildFloors();
            SyncTiles(animate: false);
        }

        public Vector3 CellToLocal(int x, int y)
        {
            var st = _session.Stage;
            return new Vector3(x - (st.width - 1) * 0.5f, y - (st.height - 1) * 0.5f, 0);
        }

        public TileView GetTile(int x, int y) => _tiles.TryGetValue((x, y), out var t) ? t : null;

        public IEnumerable<TileView> AllTiles() => _tiles.Values;

        private void Clear()
        {
            foreach (var t in _tiles.Values) if (t != null) Destroy(t.gameObject);
            _tiles.Clear();
            foreach (var f in _floors) if (f != null) Destroy(f);
            _floors.Clear();
        }

        private void BuildFloors()
        {
            var st = _session.Stage;
            for (int y = 0; y < st.height; y++)
            {
                for (int x = 0; x < st.width; x++)
                {
                    if (!st.CellExists(x, y)) continue; // 없는 칸은 미표시
                    var go = new GameObject($"Floor_{x}_{y}", typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = CellToLocal(x, y);
                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sprite = UiFactory.RoundedSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(0.96f, 0.96f);
                    sr.color = FloorColor;
                    sr.sortingOrder = 0;
                    _floors.Add(go);
                }
            }
        }

        /// <summary>세션 상태에 맞게 타일 뷰 재구성.</summary>
        public void SyncTiles(bool animate)
        {
            var st = _session.Stage;
            // 제거/갱신
            var stale = new List<(int, int)>();
            foreach (var kv in _tiles)
            {
                char now = _session.GetCell(kv.Key.x, kv.Key.y);
                if (now == Hangul.Empty) stale.Add(kv.Key);
                else if (now != kv.Value.Tile)
                {
                    kv.Value.SetChar(now);
                    if (animate) kv.Value.AnimatePop();
                }
            }
            foreach (var key in stale)
            {
                Destroy(_tiles[key].gameObject);
                _tiles.Remove(key);
            }
            // 신규
            for (int y = 0; y < st.height; y++)
            {
                for (int x = 0; x < st.width; x++)
                {
                    char c = _session.GetCell(x, y);
                    if (c == Hangul.Empty || _tiles.ContainsKey((x, y))) continue;
                    var view = TileView.Create(transform, x, y, c, CellToLocal(x, y));
                    if (animate) view.AnimatePop();
                    _tiles[(x, y)] = view;
                }
            }
        }

        /// <summary>밀기 결과를 반영: 간단한 연출 후 재동기화.</summary>
        public void ApplyPush(PushReport report)
        {
            if (!report.Success)
            {
                GetTile(report.FromX, report.FromY)?.AnimateShake();
                return;
            }

            var mover = GetTile(report.FromX, report.FromY);
            if (report.Type == PushResultType.Move && mover != null)
            {
                // 통째 이동: 뷰를 실제로 옮겨서 딕셔너리 갱신
                _tiles.Remove((report.FromX, report.FromY));
                _tiles[(report.ToX, report.ToY)] = mover;
                mover.SetBoardPosition(report.ToX, report.ToY);
                mover.AnimateMove(CellToLocal(report.ToX, report.ToY));
                return;
            }

            // 합성/분해: 상태 기준 재동기화 (팝 연출 포함)
            SyncTiles(animate: true);
        }

        public void ClearHighlights()
        {
            foreach (var t in _tiles.Values) t.SetFilterHighlight(false, false);
        }

        // 엔진의 FindComposablePushes/FindSplittablePushes가 내부에서 Clear 후 채우는 계약이므로 재사용 안전
        private readonly List<(int x, int y, Direction d)> _filterBuffer = new List<(int, int, Direction)>();

        /// <summary>Q/E 필터 하이라이트 (명세 9장). 행동 이벤트 시에만 호출 (매 프레임 아님).</summary>
        public void ShowFilter(bool compose, bool split)
        {
            ClearHighlights();
            if (compose)
            {
                _session.FindComposablePushes(_filterBuffer);
                foreach (var (x, y, _) in _filterBuffer)
                    GetTile(x, y)?.SetFilterHighlight(true, false);
            }
            if (split)
            {
                _session.FindSplittablePushes(_filterBuffer);
                foreach (var (x, y, _) in _filterBuffer)
                    GetTile(x, y)?.SetFilterHighlight(false, true);
            }
        }
    }
}
