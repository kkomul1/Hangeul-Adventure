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
    /// partial 구성: GameApp.cs(부트스트랩·맵·전투·플레이), Title.cs(타이틀·오프닝·도감),
    /// StageSelect.cs(스테이지 선택), Popups.cs(자음 회수·승리 팝업).
    /// </summary>
    public partial class GameApp : MonoBehaviour
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
            gameObject.AddComponent<BgmPlayer>();

            _canvas = UiFactory.CreateCanvas("UiCanvas");
            LevelEditor.PurgeExpiredTrash(); // 휴지통 30일 자동 정리
            _stages = StageLoader.LoadAll();

            BuildTitle();
            ShowTitle();
        }

        private void LoadKoreanFont()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/PretendardSDF");
            if (font != null) UiFactory.KoreanFont = font;
            else Debug.LogWarning("한글 TMP 폰트를 찾지 못함: Resources/Fonts/PretendardSDF");

            // 타이틀·연출용 서체 (M4-4: 정묵바위체). 없으면 본문 폰트로 폴백
            UiFactory.TitleFont = Resources.Load<TMP_FontAsset>("Fonts/SSRockSDF");
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

        private void ShowTitle()
        {
            RefreshTitleBrokenness();
            DestroyGame();
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);
            _titlePanel.gameObject.SetActive(true);
            BgmPlayer.Instance?.Play("bgm_title");
        }

        // ---- 레벨 에디터 ----

        private LevelEditor _editor;

        private void ShowLevelEditor()
        {
            DestroyGame();
            _titlePanel.gameObject.SetActive(false);
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);

            if (_editor == null)
            {
                var go = new GameObject("LevelEditor");
                go.transform.SetParent(transform, false);
                _editor = go.AddComponent<LevelEditor>();
                _editor.Build(_canvas, this);
            }
            _editor.Show();
        }

        public void ShowTitleFromEditor() => ShowTitle();

        // ---- 맵 모드 (모험) ----

        private List<MapData> _maps;
        private MapWorld _mapWorld;   // v1 탑다운
        private SideWorld _sideWorld; // v2 사이드뷰 (사이드뷰 전환 기획 10장 — version 필드가 공존 스위치)
        private int _currentMapIndex;
        private Vector2? _mapReturnPos;
        private Engine.StageData _mapStage; // 맵에서 진입한 스테이지 (재도전용)

        private void StartAdventure()
        {
            _maps ??= MapLoader.LoadAll();
            if (_maps.Count == 0) { Debug.LogError("맵이 없습니다 (Resources/Maps)"); return; }

            // 최초 1회: 오프닝 (대마왕의 습격 — 이후 타이틀 글자가 깨진다)
            if (!IntroSeen)
            {
                _titlePanel.gameObject.SetActive(false);
                ShowIntro(() => StartMap(0, null)); // 시작의 숲부터
                return;
            }

            // 마지막으로 있던 맵에서 이어서 (월드는 그래프라 이동은 출구로만)
            int lastId = PlayerPrefs.GetInt("last_map", _maps[0].id);
            int index = Mathf.Max(0, _maps.FindIndex(m => m.id == lastId));
            StartMap(index, null);
        }

        /// <summary>출구를 통한 맵 간 이동 (양방향, D-17).</summary>
        public void TravelTo(ExitData exit)
        {
            int index = _maps.FindIndex(m => m.id == exit.toMapId);
            if (index < 0) { Debug.LogError($"목적지 맵 없음: {exit.toMapId}"); return; }
            Vector2? arrive = exit.arrive.HasValue
                ? MapLoader.ResolveArrive(_maps[index], exit.arrive.Value)
                : (Vector2?)null;
            StartMap(index, arrive);
        }

        private void StartMap(int index, Vector2? playerPos)
        {
            DestroyGame();
            _titlePanel.gameObject.SetActive(false);
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);
            if (_editor != null) _editor.Hide();
            if (_manager != null) _manager.Hide();

            _currentMapIndex = index;
            PlayerPrefs.SetInt("last_map", _maps[index].id);
            if (_maps[index].version >= 2)
            {
                // v2 = 사이드뷰 (사이드뷰 전환 기획 10장: version 필드가 공존 스위치 — 한 맵씩 이전)
                var go = new GameObject("SideWorld", typeof(SideWorld));
                _sideWorld = go.GetComponent<SideWorld>();
                _sideWorld.Enter(this, _maps[index], _stages, _cam, _canvas, playerPos);
            }
            else
            {
                var go = new GameObject("MapWorld", typeof(MapWorld));
                _mapWorld = go.GetComponent<MapWorld>();
                _mapWorld.Enter(this, _maps[index], _stages, _cam, _canvas, playerPos);
            }
            BgmPlayer.Instance?.Play(string.IsNullOrEmpty(_maps[index].bgm) ? "bgm_forest" : _maps[index].bgm);

            // 방 보상 (D-23): 이 맵의 방을 완주했으면 자음 회수 연출
            var room = MapProgress.PendingRoomReward(_maps[index]);
            if (room != null)
            {
                ProgressStore.RecoverConsonant(room.reward[0]);
                ShowConsonantPopup(room);
            }
        }

        // ---- 상점/가방 ----

        private ItemPanels _itemPanels;

        public bool IsPanelOpen => _itemPanels != null && _itemPanels.IsOpen;

        private ItemPanels EnsurePanels()
        {
            if (_itemPanels == null)
            {
                var go = new GameObject("ItemPanels");
                go.transform.SetParent(transform, false);
                _itemPanels = go.AddComponent<ItemPanels>();
                _itemPanels.Build(_canvas);
                _itemPanels.Closed += () =>
                {
                    if (_mapWorld != null) _mapWorld.RefreshStates();
                    if (_sideWorld != null) _sideWorld.RefreshStates();
                };
            }
            return _itemPanels;
        }

        public void OpenShop() => EnsurePanels().OpenShop();
        public void OpenInventory() => EnsurePanels().OpenInventory();

        // ---- 전투 (사천왕) ----

        /// <summary>맵의 사천왕 지점에서 전투 시작. 종료 시 같은 위치로 맵 복귀.</summary>
        public void StartMapBattle(string configName, Vector2 playerPos)
        {
            var asset = Resources.Load<TextAsset>($"Battles/{configName}");
            if (asset == null) { Debug.LogError($"전투 설정 없음: {configName}"); return; }
            var config = JsonUtility.FromJson<BattleConfig>(asset.text);

            _mapReturnPos = playerPos;
            DestroyGame();
            if (_editor != null) _editor.Hide();
            if (_manager != null) _manager.Hide();

            var go = new GameObject("BattleScreen");
            go.transform.SetParent(transform, false);
            BgmPlayer.Instance?.Play(string.IsNullOrEmpty(config.bgm) ? "bgm_boss" : config.bgm); // 사천왕별 곡 (M4)
            var screen = go.AddComponent<BattleScreen>();
            screen.Finished += victory =>
            {
                if (victory && !string.IsNullOrEmpty(config.rewardConsonant))
                    ShowVictoryPopup(config);
                else
                    ReturnToMap();
            };
            screen.Begin(this, _canvas, _cam, config);
        }

        /// <summary>맵에서 퍼즐 지점 진입. 종료/클리어 시 같은 위치로 복귀.</summary>
        public void StartMapStage(Engine.StageData stage, Vector2 playerPos)
        {
            _mapReturnPos = playerPos;
            _mapStage = stage;
            StartSession(stage, -3);
        }

        private void ReturnToMap()
        {
            DestroyGame();
            StartMap(_currentMapIndex, _mapReturnPos);
        }

        public void LeaveMapToTitle()
        {
            DestroyMapWorld();
            ShowTitle();
        }

        private void DestroyMapWorld()
        {
            if (_mapWorld != null) { Destroy(_mapWorld.gameObject); _mapWorld = null; }
            if (_sideWorld != null) { Destroy(_sideWorld.gameObject); _sideWorld = null; }
        }

        // ---- 내 스테이지 관리 ----

        private CustomStageManager _manager;

        public void ShowManagerFromEditor()
        {
            if (_editor != null) _editor.Hide();
            if (_manager == null)
            {
                var go = new GameObject("CustomStageManager");
                go.transform.SetParent(transform, false);
                _manager = go.AddComponent<CustomStageManager>();
                _manager.Build(_canvas, this, _editor);
            }
            _manager.Show();
        }

        public void ShowEditorFromManager()
        {
            if (_manager != null) _manager.Hide();
            ShowLevelEditor();
        }

        private void ShowManagerAgain()
        {
            DestroyGame();
            if (_manager != null) _manager.Show();
            else ShowLevelEditor();
        }

        /// <summary>에디터의 테스트 플레이: 클리어/나가기 시 에디터로 복귀.</summary>
        public void StartTestStage(Engine.StageData stage) => StartSession(stage, -1);

        /// <summary>관리 화면에서 커스텀 스테이지 플레이: 종료 시 관리 화면 복귀. 별 기록됨.</summary>
        public void StartCustomPlay(Engine.StageData stage) => StartSession(stage, -2);

        // ---- 플레이 ----

        private void StartStage(int index) => StartSession(_stages[index], index);

        /// <summary>index: 0+ = 목록 스테이지, -1 = 에디터 테스트 (에디터 복귀), -2 = 관리 플레이 (관리 복귀).</summary>
        private void StartSession(Engine.StageData stage, int index)
        {
            DestroyGame();
            _titlePanel.gameObject.SetActive(false);
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);
            if (_editor != null) _editor.Hide();
            if (_manager != null) _manager.Hide();

            bool isTest = index < 0;
            _currentIndex = index;
            var session = new GameSession(stage);

            _gameRoot = new GameObject("GameRoot");

            var boardGo = new GameObject("Board", typeof(BoardView));
            boardGo.transform.SetParent(_gameRoot.transform, false);
            _board = boardGo.GetComponent<BoardView>();
            _board.Bind(session);
            FitCamera(session.Stage);

            _hud = _gameRoot.AddComponent<GameHud>();
            _hud.Build(_canvas);
            string nextLabel = index == -1 ? "에디터로" : index == -2 ? "목록으로" : index == -3 ? "지도로" : null;
            _hud.Bind(session, isTest ? 0 : index + 1, _stages.Count, nextLabel);
            _hud.SettingsClicked += ShowSettings; // 인게임 상시 설정 버튼 (A-⑯)
            if (index == -1)
            {
                _hud.NextClicked += ShowLevelEditor;
                _hud.ExitClicked += ShowLevelEditor;
                _hud.RetryClicked += () => StartTestStage(stage);
            }
            else if (index == -2)
            {
                _hud.NextClicked += ShowManagerAgain;
                _hud.ExitClicked += ShowManagerAgain;
                _hud.RetryClicked += () => StartCustomPlay(stage);
            }
            else if (index == -3)
            {
                _hud.NextClicked += ReturnToMap;
                _hud.ExitClicked += ReturnToMap;
                _hud.RetryClicked += () => StartSession(_mapStage, -3);
            }
            else
            {
                _hud.NextClicked += OnNextStage;
                _hud.ExitClicked += ShowStageSelect;
                _hud.RetryClicked += () => StartStage(index); // 클리어 후 재도전 = 완전 재시작 (소프트락 방지)
            }

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
            DestroyMapWorld();
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
