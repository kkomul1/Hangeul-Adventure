using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 앱 루트: 타이틀 → 스테이지 선택 → 플레이 흐름. 씬에는 이 컴포넌트 하나만 있으면 되고
    /// 카메라·캔버스·UI·보드 전부 코드로 만든다.
    /// </summary>
    public class GameApp : MonoBehaviour
    {
        private Camera _cam;
        private Canvas _canvas;
        private List<StageData> _stages;
        private int _currentIndex;

        private RectTransform _titlePanel;
        private RectTransform _selectPanel;
        private BoardView _board;
        private GameHud _hud;
        private GameController _controller;
        private GameObject _gameRoot;

        private static readonly Color BgColor = new Color(0.93f, 0.90f, 0.84f);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            LoadKoreanFont();
            SetupCamera();
            SetupEventSystem();
            gameObject.AddComponent<SfxPlayer>();

            _canvas = UiFactory.CreateCanvas("UiCanvas");
            _stages = StageLoader.LoadAll();

            BuildTitle();
            ShowTitle();
        }

        private void LoadKoreanFont()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/PretendardSDF");
            if (font != null) UiFactory.KoreanFont = font;
            else Debug.LogWarning("한글 TMP 폰트를 찾지 못함: Resources/Fonts/PretendardSDF");
        }

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                _cam = go.GetComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.backgroundColor = BgColor;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.transform.position = new Vector3(0, 0, -10);
        }

        private void SetupEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(transform, false);
        }

        // ---- 타이틀 ----

        private void BuildTitle()
        {
            _titlePanel = UiFactory.CreatePanel(_canvas.transform, "TitlePanel", BgColor);
            UiFactory.Stretch(_titlePanel);

            var title = UiFactory.CreateText(_titlePanel, "Title", "한글 어드벤처", 72, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 120));

            var subtitle = UiFactory.CreateText(_titlePanel, "Subtitle", "자모를 밀어 글자를 만드는 퍼즐 (MVP)", 26, UiFactory.Dim);
            UiFactory.SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 50));

            var start = UiFactory.CreateButton(_titlePanel, "StartBtn", "시작", 30, UiFactory.Accent, Color.white, ShowStageSelect);
            UiFactory.SetRect((RectTransform)start.transform, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 72));

            var wipe = UiFactory.CreateButton(_titlePanel, "WipeBtn", "진행 초기화", 18, UiFactory.Paper, UiFactory.Dim, () =>
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            });
            UiFactory.SetRect((RectTransform)wipe.transform, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 48));
        }

        private void ShowTitle()
        {
            DestroyGame();
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);
            _titlePanel.gameObject.SetActive(true);
        }

        // ---- 스테이지 선택 ----

        private void ShowStageSelect()
        {
            DestroyGame();
            _titlePanel.gameObject.SetActive(false);
            if (_selectPanel != null) Destroy(_selectPanel.gameObject); // 별 표시 갱신 위해 재생성

            _selectPanel = UiFactory.CreatePanel(_canvas.transform, "SelectPanel", BgColor);
            UiFactory.Stretch(_selectPanel);

            var header = UiFactory.CreateText(_selectPanel, "Header", "스테이지 선택", 42, UiFactory.Ink);
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(500, 70));

            var backBtn = UiFactory.CreateButton(_selectPanel, "BackBtn", "← 타이틀", 22, UiFactory.Paper, UiFactory.Ink, ShowTitle);
            UiFactory.SetRect((RectTransform)backBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(150, 52));

            // 그리드
            var gridRect = UiFactory.CreateEmpty(_selectPanel, "Grid");
            UiFactory.SetRect(gridRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1000, 520));
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(88, 88);
            grid.spacing = new Vector2(10, 10);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10; // 50개 = 10열 x 5행

            if (_stages.Count == 0)
            {
                var none = UiFactory.CreateText(_selectPanel, "None", "스테이지가 없습니다 (Resources/Stages)", 28, UiFactory.Dim);
                UiFactory.SetRect(none.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 60));
                return;
            }

            for (int i = 0; i < _stages.Count; i++)
            {
                int index = i;
                var stage = _stages[i];
                bool unlocked = ProgressStore.IsUnlocked(_stages, i);
                int stars = ProgressStore.GetStars(stage.id);
                bool ruby = ProgressStore.GetRuby(stage.id);

                var btn = UiFactory.CreateButton(gridRect, $"Stage_{stage.id}",
                    unlocked ? stage.id.ToString() : "잠김", 30,
                    unlocked ? UiFactory.Paper : new Color(0.82f, 0.80f, 0.76f),
                    unlocked ? UiFactory.Ink : UiFactory.Dim,
                    unlocked ? () => StartStage(index) : (System.Action)null);
                btn.interactable = unlocked;

                if (stars > 0)
                {
                    string starStr = new string('★', stars) + new string('☆', 3 - stars);
                    var starText = UiFactory.CreateText((RectTransform)btn.transform, "Stars", starStr, 20,
                        ruby ? new Color(0.92f, 0.20f, 0.24f) : new Color(0.95f, 0.72f, 0.12f));
                    UiFactory.SetRect(starText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(100, 28));
                }
            }
        }

        // ---- 플레이 ----

        private void StartStage(int index)
        {
            DestroyGame();
            _titlePanel.gameObject.SetActive(false);
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);

            _currentIndex = index;
            var session = new GameSession(_stages[index]);

            _gameRoot = new GameObject("GameRoot");

            var boardGo = new GameObject("Board", typeof(BoardView));
            boardGo.transform.SetParent(_gameRoot.transform, false);
            _board = boardGo.GetComponent<BoardView>();
            _board.Bind(session);
            FitCamera(session.Stage);

            _hud = _gameRoot.AddComponent<GameHud>();
            _hud.Build(_canvas);
            _hud.Bind(session, index + 1, _stages.Count);
            _hud.NextClicked += OnNextStage;
            _hud.ExitClicked += ShowStageSelect;
            _hud.RetryClicked += () => StartStage(index); // 클리어 후 재도전 = 완전 재시작 (소프트락 방지)

            _controller = _gameRoot.AddComponent<GameController>();
            _controller.Bind(session, _board, _hud, _cam);
        }

        private void FitCamera(StageData stage)
        {
            float aspect = (float)Screen.width / Screen.height;
            float needH = stage.height * 0.5f + 2.4f;           // HUD 여백 포함
            float needW = (stage.width * 0.5f + 1.2f) / aspect;
            _cam.orthographicSize = Mathf.Max(needH, needW, 3.5f);
            _cam.transform.position = new Vector3(0, -0.3f, -10);
        }

        private void OnNextStage()
        {
            if (_currentIndex + 1 < _stages.Count) StartStage(_currentIndex + 1);
            else ShowStageSelect();
        }

        private void DestroyGame()
        {
            // HUD UI는 GameHud.OnDestroy가 스스로 정리 (소유권 일원화)
            if (_gameRoot != null) { Destroy(_gameRoot); _gameRoot = null; }
        }

        private int _lastScreenW, _lastScreenH;

        private void Update()
        {
            // 창 크기 변경 시 카메라 재핏
            if (_board != null && _gameRoot != null
                && (Screen.width != _lastScreenW || Screen.height != _lastScreenH))
            {
                _lastScreenW = Screen.width;
                _lastScreenH = Screen.height;
                FitCamera(_board.Session.Stage);
            }
        }
    }
}
