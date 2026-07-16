using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 탑다운 맵 월드: 지형 렌더, 캐릭터 이동, 퍼즐 지점/출구/상점 상호작용 (임시 비주얼).
    /// 월드는 exits로 연결된 그래프 — 이전 맵으로도 자유롭게 되돌아갈 수 있다 (D-17).
    /// partial 구성: MapWorld.cs(상태·갱신), Build.cs(지형·스팟·플레이어·HUD 구축), Input.cs(이동·상호작용).
    /// </summary>
    public partial class MapWorld : MonoBehaviour
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
        private SpotView _bossView;
        private TextMeshProUGUI _hudTitle, _hudProgress, _hudHint, _hudGold;
        private RectTransform _hudRoot;

        private SpriteRenderer _playerSr;
        private int _playerDir;   // 0=아래 1=위 2=왼쪽 3=오른쪽 (Walk 시트 열 순서 가정 — 시각 검증으로 확정)
        private bool _playerMoving;
        private bool _playerRunning;

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

        // ---- 상태 갱신 ----

        public void RefreshStates()
        {
            int nextTutorial = MapProgress.NextTutorialStage(_map, _stageLookup);
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

        private bool IsSpotOpen(int stageId)
        {
            int next = MapProgress.NextTutorialStage(_map, _stageLookup);
            if (next < 0) return true;
            bool isTutorial = System.Array.IndexOf(_map.tutorialStages, stageId) >= 0;
            return isTutorial && (ProgressStore.GetStars(stageId) > 0 || stageId == next);
        }
    }
}
