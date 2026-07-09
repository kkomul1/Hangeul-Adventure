using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 탑다운 맵 월드: 지형 렌더, 캐릭터 이동, 퍼즐 지점/출구/상점 상호작용 (임시 비주얼).
    /// 월드는 exits로 연결된 그래프 — 이전 맵으로도 자유롭게 되돌아갈 수 있다 (D-17).
    /// </summary>
    public class MapWorld : MonoBehaviour
    {
        private GameApp _app;
        private MapData _map;
        private List<StageData> _stageLookup;
        private Camera _cam;
        private Canvas _canvas;

        private Transform _playerT;
        private readonly Dictionary<int, SpotView> _spotViews = new Dictionary<int, SpotView>();
        private readonly List<SpotView> _exitViews = new List<SpotView>();
        private SpotView _shopView;
        private TextMeshProUGUI _hudTitle, _hudProgress, _hudHint, _hudGold;
        private RectTransform _hudRoot;

        private const float MoveSpeed = 6f;
        private const float RunSpeed = 10.5f; // Shift 달리기
        private const float PlayerRadius = 0.30f;
        private const float InteractRadius = 0.65f;

        private class SpotView
        {
            public GameObject Go;
            public SpriteRenderer Bg;
            public TextMeshPro Label;
            public int StageId;      // 퍼즐 지점만 사용
            public int ExitIndexil;  // 출구만 사용 (-1이면 출구 아님)
            public bool IsShop;
            public Vector2Int Pos;

            public bool IsExit => ExitIndexil >= 0;
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
        private static readonly Color ShopColor = new Color(0.62f, 0.78f, 0.92f);

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

        public Vector2 PlayerPosition => _playerT != null ? (Vector2)_playerT.localPosition : Vector2.zero;

        private void OnDestroy()
        {
            if (_hudRoot != null) Destroy(_hudRoot.gameObject);
        }

        // ---- 구축 ----

        private void BuildTerrain()
        {
            bool art = ArtLibrary.Available;
            for (int y = 0; y < _map.height; y++)
            {
                for (int x = 0; x < _map.width; x++)
                {
                    char t = _map.Tile(x, y);
                    var go = new GameObject($"T_{x}_{y}", typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x, y, 0);
                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sortingOrder = 0;

                    if (art)
                    {
                        // Ninja Adventure(CC0) 타일 (D-20). 좌표는 픽셀 분석으로 선정한 균일 셀.
                        sr.sprite = t switch
                        {
                            '-' => ArtLibrary.Tile("TilesetField", 1, 1),   // 흙길
                            '#' => ArtLibrary.Tile("TilesetField", 7, 1),   // 짙은 수풀 (통행 불가)
                            '~' => ArtLibrary.Tile("TilesetWater", 1, 1),   // 물
                            _ => ArtLibrary.Tile("TilesetField", 4, 1),     // 풀밭
                        };
                        if (t == '#') sr.color = new Color(0.72f, 0.72f, 0.72f); // 벽 구분용 어둡게
                        continue;
                    }

                    // 폴백: 절차 생성 도형
                    sr.sprite = UiFactory.RoundedSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = Vector2.one * 1.02f;
                    sr.color = t switch
                    {
                        '-' => Road,
                        '#' => ((x + y) % 3 == 0) ? House : Tree,
                        '~' => Water,
                        _ => ((x + y) % 2 == 0) ? GrassA : GrassB,
                    };
                }
            }
        }

        private void BuildSpots()
        {
            foreach (var (stageId, pos) in _map.spots)
                _spotViews[stageId] = MakeSpot(pos, LabelFor(stageId), stageId: stageId);

            for (int i = 0; i < _map.exits.Count; i++)
                _exitViews.Add(MakeSpot(_map.exits[i].pos, "→", exitIndex: i));

            if (_map.shop.HasValue)
                _shopView = MakeSpot(_map.shop.Value, BrokenText.Apply("상점"), isShop: true);

            if (_map.bossPos.HasValue)
            {
                _bossView = MakeSpot(_map.bossPos.Value, "王", isShop: false);
                _bossView.Bg.size = new Vector2(1.0f, 1.0f);
            }
        }

        private SpotView _bossView;

        private string LabelFor(int stageId)
        {
            var stage = _stageLookup.Find(s => s.id == stageId);
            if (stage != null && stage.goals != null && stage.goals.Length > 0 && !string.IsNullOrEmpty(stage.goals[0].display))
                return stage.goals[0].display.Substring(0, 1);
            return stageId.ToString();
        }

        private SpotView MakeSpot(Vector2Int pos, string label, int stageId = -1, int exitIndex = -1, bool isShop = false)
        {
            var go = new GameObject(isShop ? "Shop" : exitIndex >= 0 ? $"Exit_{exitIndex}" : $"Spot_{stageId}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);

            var bgGo = new GameObject("Bg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.GetComponent<SpriteRenderer>();
            bg.sprite = UiFactory.RoundedSprite();
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = isShop ? new Vector2(1.15f, 0.85f) : new Vector2(0.85f, 0.85f);
            bg.sortingOrder = 2;

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (UiFactory.KoreanFont != null) tmp.font = UiFactory.KoreanFont;
            tmp.text = label;
            tmp.fontSize = isShop ? 3.2f : 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiFactory.Ink;
            tmp.rectTransform.sizeDelta = new Vector2(1.4f, 1);
            tmp.sortingOrder = 3;

            return new SpotView { Go = go, Bg = bg, Label = tmp, StageId = stageId, ExitIndexil = exitIndex, IsShop = isShop, Pos = pos };
        }

        private SpriteRenderer _playerSr;
        private int _playerDir;   // 0=아래 1=위 2=왼쪽 3=오른쪽 (Walk 시트 열 순서 가정 — 시각 검증으로 확정)
        private bool _playerMoving;
        private bool _playerRunning;

        private void BuildPlayer(Vector2 pos)
        {
            var go = new GameObject("Player", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            _playerSr = sr;

            if (ArtLibrary.Available)
            {
                sr.sprite = ArtLibrary.Character("Noble", "Idle", 0, 0);
            }
            else
            {
                sr.sprite = UiFactory.RoundedSprite();
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(0.62f, 0.62f);
                sr.color = new Color(0.25f, 0.35f, 0.55f);

                var face = new GameObject("Face", typeof(SpriteRenderer));
                face.transform.SetParent(go.transform, false);
                face.transform.localPosition = new Vector3(0, 0.16f, 0);
                var fs = face.GetComponent<SpriteRenderer>();
                fs.sprite = UiFactory.RoundedSprite();
                fs.drawMode = SpriteDrawMode.Sliced;
                fs.size = new Vector2(0.34f, 0.28f);
                fs.color = new Color(0.96f, 0.88f, 0.76f);
                fs.sortingOrder = 6;
            }

            _playerT = go.transform;
        }

        /// <summary>4방향 걷기 애니메이션 (코드 구동, 초당 8프레임).</summary>
        private void AnimatePlayer()
        {
            if (!ArtLibrary.Available || _playerSr == null) return;
            if (_playerMoving)
            {
                int frame = (int)(Time.time * (_playerRunning ? 13f : 8f)) % 4;
                var s = ArtLibrary.Character("Noble", "Walk", frame, _playerDir);
                if (s != null) _playerSr.sprite = s;
            }
            else
            {
                var s = ArtLibrary.Character("Noble", "Idle", 0, _playerDir);
                if (s != null) _playerSr.sprite = s;
            }
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

            var bagBtn = UiFactory.CreateButton(_hudRoot, "BagBtn", "가방", 19, UiFactory.Paper, UiFactory.Ink, () => _app.OpenInventory());
            UiFactory.SetRect((RectTransform)bagBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -20), new Vector2(100, 46));

            _hudGold = UiFactory.CreateText(_hudRoot, "Gold", "", 21, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(_hudGold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-266, -26), new Vector2(240, 36));
        }

        // ---- 상태 갱신 ----

        public void RefreshStates()
        {
            int nextTutorial = MapProgress.NextTutorialStage(_map);
            bool tutorialDone = nextTutorial < 0;

            foreach (var v in _spotViews.Values)
            {
                bool cleared = ProgressStore.GetStars(v.StageId) > 0;
                bool isTutorial = System.Array.IndexOf(_map.tutorialStages, v.StageId) >= 0;
                bool open = tutorialDone || (isTutorial && (cleared || v.StageId == nextTutorial));

                // 자음 게이트 (D-22): 미회수 자음 스테이지는 깨져 보임
                var stage = _stageLookup.Find(s => s.id == v.StageId);
                bool consonantLocked = stage != null && ProgressStore.MissingConsonants(stage).Length > 0;
                if (consonantLocked && !cleared)
                {
                    v.Bg.color = new Color(0.35f, 0.33f, 0.31f);
                    v.Label.text = "▒";
                    v.Label.color = new Color(0.55f, 0.52f, 0.48f);
                    continue;
                }
                v.Label.text = LabelFor(v.StageId);

                v.Bg.color = cleared ? SpotCleared
                    : v.StageId == nextTutorial ? SpotNext
                    : open ? SpotOpen
                    : SpotLocked;
                v.Label.color = open || cleared ? UiFactory.Ink : new Color(0.45f, 0.44f, 0.42f);
            }

            for (int i = 0; i < _exitViews.Count; i++)
            {
                bool open = MapProgress.ExitOpen(_map, _map.exits[i]);
                _exitViews[i].Bg.color = open ? SpotCleared : SpotLocked;
                _exitViews[i].Label.text = open ? "→" : "X";
            }

            if (_shopView != null) _shopView.Bg.color = ShopColor;
            if (_bossView != null)
                _bossView.Bg.color = ProgressStore.IsBossDefeated(_map.bossConfig?.Replace("boss_", ""))
                    ? SpotCleared : new Color(0.75f, 0.30f, 0.28f); // 미격파 = 붉은색

            // 세계의 글자는 미회수 자음이 깨져 보인다 (D-22 확장: 장소 이름·간판)
            _hudTitle.text = $"{BrokenText.Apply(_map.title)}  —  {BrokenText.Apply(_map.theme)}";
            _hudGold.text = $"골드  {ProgressStore.Gold}";
            int cleared2 = MapProgress.ClearedCount(_map);
            _hudProgress.text = tutorialDone
                ? $"클리어 {cleared2}/{_map.spots.Count}"
                : $"기초 배우기 진행 중 ({System.Array.IndexOf(_map.tutorialStages, nextTutorial) + 1}/{_map.tutorialStages.Length}) — 주황색 지점으로 가세요";
        }

        // ---- 이동/상호작용 ----

        private void Update()
        {
            if (_playerT == null) return;
            if (_app.IsPanelOpen) return; // 상점/가방 열려 있으면 이동 잠금
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector2 dir = Vector2.zero;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y += 1;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y -= 1;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x += 1;

            _playerMoving = dir.sqrMagnitude > 0.01f;
            if (_playerMoving)
            {
                // 바라보는 방향 (지배 축 기준)
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    _playerDir = dir.x > 0 ? 3 : 2;
                else
                    _playerDir = dir.y > 0 ? 1 : 0;

                dir.Normalize();
                _playerRunning = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                float speed = _playerRunning ? RunSpeed : MoveSpeed;
                Vector2 pos = _playerT.localPosition;
                Vector2 next = pos + dir * (speed * Time.deltaTime);
                if (CanStand(new Vector2(next.x, pos.y))) pos.x = next.x;
                if (CanStand(new Vector2(pos.x, next.y))) pos.y = next.y;
                _playerT.localPosition = pos;
                SnapCamera();
            }
            AnimatePlayer();

            var near = NearestInteractable();
            UpdateHint(near);

            if (near != null && kb.spaceKey.wasPressedThisFrame)
                Interact(near);
        }

        private bool CanStand(Vector2 p)
        {
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

            void Consider(SpotView v)
            {
                float d = Vector2.Distance(pos, v.Pos);
                if (d < bestDist) { best = v; bestDist = d; }
            }

            foreach (var v in _spotViews.Values) Consider(v);
            foreach (var v in _exitViews) Consider(v);
            if (_shopView != null) Consider(_shopView);
            if (_bossView != null) Consider(_bossView);
            return best;
        }

        private void UpdateHint(SpotView near)
        {
            if (near == null) { _hudHint.text = "이동: WASD/방향키"; return; }

            if (near == _bossView)
            {
                bool defeated = ProgressStore.IsBossDefeated(_map.bossConfig?.Replace("boss_", ""));
                _hudHint.text = defeated ? "Space: 사천왕 재대결" : "Space: 사천왕에게 도전한다!";
                return;
            }
            if (near.IsShop)
            {
                _hudHint.text = "Space: 상점 열기";
                return;
            }
            if (near.IsExit)
            {
                var exit = _map.exits[near.ExitIndexil];
                string exitName = BrokenText.Apply(exit.label); // 장소 이름도 깨진 글자로
                _hudHint.text = MapProgress.ExitOpen(_map, exit)
                    ? $"Space: {exitName}(으)로 이동"
                    : $"{exitName} — 잠김 (이 지역 {MapProgress.ClearedCount(_map)}/{exit.required} 클리어)";
                return;
            }

            bool open = IsSpotOpen(near.StageId);
            var stage = _stageLookup.Find(s => s.id == near.StageId);
            string missing = stage != null ? ProgressStore.MissingConsonants(stage) : "";
            if (missing.Length > 0 && ProgressStore.GetStars(near.StageId) == 0)
            {
                _hudHint.text = $"이 글자들은 깨져 있다... 필요한 자음: {string.Join(", ", missing.ToCharArray())}";
                return;
            }
            string title = stage != null ? stage.title : near.StageId.ToString();
            string diff = stage != null ? $" · 난이도 {stage.difficulty}" : "";
            _hudHint.text = open
                ? $"Space: '{title}' 도전{diff}" + (ProgressStore.GetStars(near.StageId) > 0 ? " (클리어됨)" : "")
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
            if (spot == _bossView)
            {
                _app.StartMapBattle(_map.bossConfig, _playerT.localPosition);
                return;
            }
            if (spot.IsShop)
            {
                _app.OpenShop();
                return;
            }
            if (spot.IsExit)
            {
                var exit = _map.exits[spot.ExitIndexil];
                if (MapProgress.ExitOpen(_map, exit))
                    _app.TravelTo(exit);
                return;
            }
            if (!IsSpotOpen(spot.StageId)) return;

            var stage = _stageLookup.Find(s => s.id == spot.StageId);
            if (stage == null) return;
            if (ProgressStore.MissingConsonants(stage).Length > 0 && ProgressStore.GetStars(spot.StageId) == 0)
            {
                SfxPlayer.Instance?.Fail(); // 자음 게이트: 진입 불가
                return;
            }
            _app.StartMapStage(stage, _playerT.localPosition);
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
