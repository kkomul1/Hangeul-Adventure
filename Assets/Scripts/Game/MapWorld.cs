using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 탑다운 맵 월드: 지형 렌더, 캐릭터 이동, 퍼즐 지점 상호작용, 진행 게이트 (임시 비주얼).
    /// 최초 진입 시 튜토리얼 직렬 → 완료 시 전체 개방 → unlockCount 클리어 시 출구 개방.
    /// </summary>
    public class MapWorld : MonoBehaviour
    {
        private GameApp _app;
        private MapData _map;
        private List<StageData> _stageLookup;
        private Camera _cam;
        private Canvas _canvas;

        private Transform _playerT;
        private readonly Dictionary<int, SpotView> _spotViews = new Dictionary<int, SpotView>(); // stageId → view
        private SpotView _exitView;
        private TextMeshProUGUI _hudTitle, _hudProgress, _hudHint;
        private RectTransform _hudRoot;

        private const float MoveSpeed = 6f;
        private const float PlayerRadius = 0.30f;
        private const float InteractRadius = 0.65f;

        private class SpotView
        {
            public GameObject Go;
            public SpriteRenderer Bg;
            public TextMeshPro Label;
            public int StageId;      // 출구는 -1
            public Vector2Int Pos;
        }

        private static readonly Color GrassA = new Color(0.72f, 0.80f, 0.58f);
        private static readonly Color GrassB = new Color(0.68f, 0.77f, 0.55f);
        private static readonly Color Road = new Color(0.87f, 0.79f, 0.62f);
        private static readonly Color Tree = new Color(0.38f, 0.50f, 0.34f);
        private static readonly Color House = new Color(0.52f, 0.40f, 0.33f);
        private static readonly Color Water = new Color(0.55f, 0.70f, 0.82f);
        private static readonly Color SpotLocked = new Color(0.72f, 0.70f, 0.66f);
        private static readonly Color SpotOpen = new Color(0.98f, 0.96f, 0.90f);
        private static readonly Color SpotCleared = new Color(1.00f, 0.85f, 0.45f);
        private static readonly Color SpotNext = new Color(0.95f, 0.55f, 0.35f);

        public void Enter(GameApp app, MapData map, List<StageData> stages, Camera cam, Canvas canvas, Vector2? playerPos)
        {
            _app = app;
            _map = map;
            _stageLookup = stages;
            _cam = cam;
            _canvas = canvas;

            BuildTerrain();
            BuildSpots();
            BuildPlayer(playerPos ?? new Vector2(_map.spawn.x, _map.spawn.y));
            BuildHud();
            RefreshStates();

            _cam.orthographicSize = 5f;
            SnapCamera();
        }

        private void OnDestroy()
        {
            if (_hudRoot != null) Destroy(_hudRoot.gameObject);
        }

        // ---- 구축 ----

        private void BuildTerrain()
        {
            for (int y = 0; y < _map.height; y++)
            {
                for (int x = 0; x < _map.width; x++)
                {
                    char t = _map.Tile(x, y);
                    var go = new GameObject($"T_{x}_{y}", typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x, y, 0);
                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sprite = UiFactory.RoundedSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = Vector2.one * 1.02f;
                    sr.sortingOrder = 0;
                    sr.color = t switch
                    {
                        '-' => Road,
                        '#' => ((x + y) % 3 == 0) ? House : Tree, // 임시: 일부는 집, 일부는 나무
                        '~' => Water,
                        _ => ((x + y) % 2 == 0) ? GrassA : GrassB,
                    };
                    if (t == '#' && (x + y) % 3 == 0)
                    {
                        // 임시 기와집 표식
                        var roof = new GameObject("Roof", typeof(SpriteRenderer));
                        roof.transform.SetParent(go.transform, false);
                        roof.transform.localPosition = new Vector3(0, 0.18f, 0);
                        var rs = roof.GetComponent<SpriteRenderer>();
                        rs.sprite = UiFactory.RoundedSprite();
                        rs.drawMode = SpriteDrawMode.Sliced;
                        rs.size = new Vector2(0.95f, 0.45f);
                        rs.color = new Color(0.30f, 0.30f, 0.36f); // 기와색
                        rs.sortingOrder = 1;
                    }
                }
            }
        }

        private void BuildSpots()
        {
            foreach (var (stageId, pos) in _map.spots)
                _spotViews[stageId] = MakeSpot(stageId, pos, LabelFor(stageId));

            _exitView = MakeSpot(-1, _map.exit, "→");
        }

        private string LabelFor(int stageId)
        {
            var stage = _stageLookup.Find(s => s.id == stageId);
            if (stage != null && stage.goals != null && stage.goals.Length > 0 && !string.IsNullOrEmpty(stage.goals[0].display))
                return stage.goals[0].display.Substring(0, 1); // 목표 첫 글자를 지점에 표시
            return stageId.ToString();
        }

        private SpotView MakeSpot(int stageId, Vector2Int pos, string label)
        {
            var go = new GameObject(stageId < 0 ? "Exit" : $"Spot_{stageId}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);

            var bgGo = new GameObject("Bg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.GetComponent<SpriteRenderer>();
            bg.sprite = UiFactory.RoundedSprite();
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(0.85f, 0.85f);
            bg.sortingOrder = 2;

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (UiFactory.KoreanFont != null) tmp.font = UiFactory.KoreanFont;
            tmp.text = label;
            tmp.fontSize = 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiFactory.Ink;
            tmp.rectTransform.sizeDelta = Vector2.one;
            tmp.sortingOrder = 3;

            return new SpotView { Go = go, Bg = bg, Label = tmp, StageId = stageId, Pos = pos };
        }

        private void BuildPlayer(Vector2 pos)
        {
            var go = new GameObject("Player", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = UiFactory.RoundedSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.62f, 0.62f);
            sr.color = new Color(0.25f, 0.35f, 0.55f); // 임시 캐릭터: 쪽빛 두루마기
            sr.sortingOrder = 5;

            var face = new GameObject("Face", typeof(SpriteRenderer));
            face.transform.SetParent(go.transform, false);
            face.transform.localPosition = new Vector3(0, 0.16f, 0);
            var fs = face.GetComponent<SpriteRenderer>();
            fs.sprite = UiFactory.RoundedSprite();
            fs.drawMode = SpriteDrawMode.Sliced;
            fs.size = new Vector2(0.34f, 0.28f);
            fs.color = new Color(0.96f, 0.88f, 0.76f);
            fs.sortingOrder = 6;

            _playerT = go.transform;
        }

        private void BuildHud()
        {
            _hudRoot = UiFactory.CreateEmpty(_canvas.transform, "MapHud");
            UiFactory.Stretch(_hudRoot);

            _hudTitle = UiFactory.CreateText(_hudRoot, "Title", "", 28, UiFactory.Ink, TextAlignmentOptions.Left);
            UiFactory.SetRect(_hudTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(600, 44));

            _hudProgress = UiFactory.CreateText(_hudRoot, "Progress", "", 20, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(_hudProgress.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -62), new Vector2(700, 34));

            _hudHint = UiFactory.CreateText(_hudRoot, "Hint", "", 19, UiFactory.Ink);
            UiFactory.SetRect(_hudHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(900, 34));

            var exitBtn = UiFactory.CreateButton(_hudRoot, "LeaveBtn", "나가기", 19, UiFactory.Paper, UiFactory.Ink, () => _app.LeaveMapToTitle());
            UiFactory.SetRect((RectTransform)exitBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(120, 46));
        }

        // ---- 상태 갱신 ----

        /// <summary>튜토리얼/개방/클리어/출구 상태를 지점 색으로 반영.</summary>
        public void RefreshStates()
        {
            int nextTutorial = MapProgress.NextTutorialStage(_map);
            bool tutorialDone = nextTutorial < 0;

            foreach (var v in _spotViews.Values)
            {
                bool cleared = ProgressStore.GetStars(v.StageId) > 0;
                bool isTutorial = System.Array.IndexOf(_map.tutorialStages, v.StageId) >= 0;
                bool open = tutorialDone || (isTutorial && (cleared || v.StageId == nextTutorial));

                v.Bg.color = cleared ? SpotCleared
                    : v.StageId == nextTutorial ? SpotNext
                    : open ? SpotOpen
                    : SpotLocked;
                v.Label.color = open || cleared ? UiFactory.Ink : new Color(0.45f, 0.44f, 0.42f);
            }

            bool exitOpen = MapProgress.ExitOpen(_map);
            _exitView.Bg.color = exitOpen ? SpotCleared : SpotLocked;
            _exitView.Label.text = exitOpen ? "→" : "🔒";
            if (_exitView.Label.text == "🔒") _exitView.Label.text = "X"; // 폰트 미지원 대비

            _hudTitle.text = $"{_map.title}  —  {_map.theme}";
            int cleared2 = MapProgress.ClearedCount(_map);
            _hudProgress.text = tutorialDone
                ? $"클리어 {cleared2}/{_map.spots.Count} · 다음 지역까지 {Mathf.Max(0, _map.unlockCount - cleared2)}개"
                : $"기초 배우기 진행 중 ({TutorialIndex(nextTutorial) + 1}/{_map.tutorialStages.Length}) — 주황색 지점으로 가세요";
        }

        private int TutorialIndex(int stageId)
            => System.Array.IndexOf(_map.tutorialStages, stageId);

        // ---- 이동/상호작용 ----

        private void Update()
        {
            if (_playerT == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector2 dir = Vector2.zero;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y += 1;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y -= 1;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x += 1;

            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                Vector2 pos = _playerT.localPosition;
                Vector2 next = pos + dir * (MoveSpeed * Time.deltaTime);
                // 축 분리 충돌: 벽에 비비며 미끄러지기
                if (CanStand(new Vector2(next.x, pos.y))) pos.x = next.x;
                if (CanStand(new Vector2(pos.x, next.y))) pos.y = next.y;
                _playerT.localPosition = pos;
                FollowCamera();
            }

            var near = NearestInteractable();
            UpdateHint(near);

            if (near != null && kb.spaceKey.wasPressedThisFrame)
                Interact(near);
        }

        private bool CanStand(Vector2 p)
        {
            // 네 모서리 샘플
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int tx = Mathf.RoundToInt(p.x + dx * PlayerRadius);
                    int ty = Mathf.RoundToInt(p.y + dy * PlayerRadius);
                    if (!_map.Walkable(tx, ty)) return false;
                }
            return true;
        }

        private SpotView NearestInteractable()
        {
            Vector2 pos = _playerT.localPosition;
            SpotView best = null;
            float bestDist = InteractRadius;
            foreach (var v in _spotViews.Values)
            {
                float d = Vector2.Distance(pos, v.Pos);
                if (d < bestDist) { best = v; bestDist = d; }
            }
            float de = Vector2.Distance(pos, _exitView.Pos);
            if (de < bestDist) best = _exitView;
            return best;
        }

        private void UpdateHint(SpotView near)
        {
            if (near == null)
            {
                _hudHint.text = "이동: WASD/방향키";
                return;
            }
            if (near.StageId < 0)
            {
                _hudHint.text = MapProgress.ExitOpen(_map)
                    ? "Space: 다음 지역으로"
                    : $"다음 지역은 잠겨 있습니다 ({MapProgress.ClearedCount(_map)}/{_map.unlockCount} 클리어)";
                return;
            }
            bool open = IsSpotOpen(near.StageId);
            var stage = _stageLookup.Find(s => s.id == near.StageId);
            string title = stage != null ? stage.title : near.StageId.ToString();
            _hudHint.text = open
                ? $"Space: '{title}' 도전" + (ProgressStore.GetStars(near.StageId) > 0 ? " (클리어됨)" : "")
                : "기초 배우기를 먼저 완료하세요 (주황색 지점)";
        }

        private bool IsSpotOpen(int stageId)
        {
            int next = MapProgress.NextTutorialStage(_map);
            if (next < 0) return true;
            bool isTutorial = System.Array.IndexOf(_map.tutorialStages, stageId) >= 0;
            return isTutorial && (ProgressStore.GetStars(stageId) > 0 || stageId == next);
        }

        private void Interact(SpotView spot)
        {
            if (spot.StageId < 0)
            {
                if (MapProgress.ExitOpen(_map)) _app.GoToNextMap();
                return;
            }
            if (!IsSpotOpen(spot.StageId)) return;

            var stage = _stageLookup.Find(s => s.id == spot.StageId);
            if (stage == null) return;
            _app.StartMapStage(stage, _playerT.localPosition);
        }

        private void FollowCamera()
        {
            SnapCamera();
        }

        private void SnapCamera()
        {
            Vector3 p = _playerT.localPosition;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            float x = Mathf.Clamp(p.x, halfW - 0.5f, _map.width - 0.5f - halfW);
            float y = Mathf.Clamp(p.y, halfH - 0.5f, _map.height - 0.5f - halfH);
            if (_map.width < halfW * 2) x = (_map.width - 1) * 0.5f;
            if (_map.height < halfH * 2) y = (_map.height - 1) * 0.5f;
            _cam.transform.position = new Vector3(x, y, -10);
        }
    }
}
