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

        private TMPro.TextMeshProUGUI _subtitle;
        private TMPro.TextMeshProUGUI _titleText;
        private const string TitleString = "한글 어드벤처";
        private const string SubtitleDefault = "자모를 밀어 글자를 만드는 퍼즐 (MVP)";

        private static bool IntroSeen => PlayerPrefs.GetInt("intro_seen", 0) == 1;

        private void BuildTitle()
        {
            _titlePanel = UiFactory.CreatePanel(_canvas.transform, "TitlePanel", BgColor);
            UiFactory.Stretch(_titlePanel);

            var title = UiFactory.CreateText(_titlePanel, "Title", TitleString, 72, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 120));
            _titleText = title;
            RefreshTitleBrokenness();

            _subtitle = UiFactory.CreateText(_titlePanel, "Subtitle", SubtitleDefault, 26, UiFactory.Dim);
            UiFactory.SetRect(_subtitle.rectTransform, new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 50));
            if (ProgressStore.DevMode) _subtitle.text = "개발자 모드 ON — 전체 스테이지 잠금 해제";

            // 히든 개발자 모드 토글: 타이틀의 '어' 글자 위에 투명 버튼 배치 (TMP 글리프 좌표 기반)
            StartCoroutine(PlaceDevToggle(title));

            var adventure = UiFactory.CreateButton(_titlePanel, "AdventureBtn", "모험 시작", 30, UiFactory.Accent, Color.white, StartAdventure);
            UiFactory.SetRect((RectTransform)adventure.transform, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 72));

            var start = UiFactory.CreateButton(_titlePanel, "StartBtn", "스테이지 목록", 22, UiFactory.Paper, UiFactory.Ink, ShowStageSelect);
            UiFactory.SetRect((RectTransform)start.transform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 56));

            var editorBtn = UiFactory.CreateButton(_titlePanel, "EditorBtn", "레벨 에디터", 22, UiFactory.Paper, UiFactory.Ink, ShowLevelEditor);
            UiFactory.SetRect((RectTransform)editorBtn.transform, new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 56));

            var codexBtn = UiFactory.CreateButton(_titlePanel, "CodexBtn", "글자 도감", 22, UiFactory.Paper, UiFactory.Ink, OpenCodex);
            UiFactory.SetRect((RectTransform)codexBtn.transform, new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f), new Vector2(220, 0), new Vector2(180, 56));

            var wipe = UiFactory.CreateButton(_titlePanel, "WipeBtn", "진행 초기화", 18, UiFactory.Paper, UiFactory.Dim, () =>
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                _subtitle.text = SubtitleDefault;
                RefreshTitleBrokenness(); // 인트로 전 상태로 돌아가므로 온전한 타이틀 복원
            });
            UiFactory.SetRect((RectTransform)wipe.transform, new Vector2(0.5f, 0.11f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 48));
        }

        private System.Collections.IEnumerator PlaceDevToggle(TMPro.TextMeshProUGUI title)
        {
            yield return null; // TMP 레이아웃 완료 대기
            title.ForceMeshUpdate();
            var info = title.textInfo;
            int charIdx = -1;
            for (int i = 0; i < info.characterCount; i++)
                if (info.characterInfo[i].character == '어') { charIdx = i; break; }
            if (charIdx < 0)
            {
                // 깨진 글자 연출로 '어'가 사라진 경우: 타이틀 중앙 부근 고정 위치로 폴백
                var fb = new GameObject("DevToggle", typeof(RectTransform), typeof(Image), typeof(Button));
                fb.transform.SetParent(title.transform, false);
                fb.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                var frt = (RectTransform)fb.transform;
                frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
                frt.anchoredPosition = new Vector2(-36, 0);
                frt.sizeDelta = new Vector2(70, 80);
                fb.GetComponent<Button>().onClick.AddListener(ToggleDevMode);
                yield break;
            }

            var ci = info.characterInfo[charIdx];
            Vector2 center = (ci.bottomLeft + (Vector3)new Vector2(ci.topRight.x, ci.topRight.y)) * 0.5f;
            Vector2 size = new Vector2(ci.topRight.x - ci.bottomLeft.x, ci.topRight.y - ci.bottomLeft.y);

            var go = new GameObject("DevToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(title.transform, false);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0); // 투명 히트 영역
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = size + new Vector2(10, 10);
            go.GetComponent<Button>().onClick.AddListener(ToggleDevMode);
        }

        private void ToggleDevMode()
        {
            bool on = !ProgressStore.DevMode;
            ProgressStore.DevMode = on;
            _subtitle.text = on ? "개발자 모드 ON — 전체 스테이지 잠금 해제" : SubtitleDefault;
            if (on) SfxPlayer.Instance?.Collect(); else SfxPlayer.Instance?.Split();
        }

        /// <summary>깨진 글자 연출 (스토리기획 2장): 오프닝 전에는 온전, 이후엔 미회수 자음 글자가 깨짐.</summary>
        private void RefreshTitleBrokenness()
        {
            if (_titleText == null) return;
            _titleText.text = IntroSeen ? BrokenText.Apply(TitleString) : TitleString;
        }

        // ---- 오프닝 ----

        private static readonly string[] IntroPages =
        {
            "조선의 어느 날.\n가나다 대마왕이 나타나 훈민정음 해례본을 훔쳐\n산산조각 내버렸다.",
            "그가 흩뿌린 어둠에 세상의 글자들이 하나둘 깨져나갔다.\n\n남은 것은  ㄱ, ㄴ, ㄷ  과 모음들뿐...",
            "어린 선비인 당신은 잃어버린 자음을 되찾아\n훈민정음을 복원하기 위해 길을 나선다.\n\n— 시작의 숲에서 —",
        };

        private void ShowIntro(System.Action onDone)
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "Intro", new Color(0.08f, 0.07f, 0.06f, 0.97f));
            UiFactory.Stretch(overlay);

            var text = UiFactory.CreateText(overlay, "Text", IntroPages[0], 30, new Color(0.92f, 0.89f, 0.82f));
            UiFactory.SetRect(text.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 240));

            int page = 0;
            var btn = UiFactory.CreateButton(overlay, "Next", "다음", 24, UiFactory.Accent, Color.white, null);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 60));
            btn.onClick.AddListener(() =>
            {
                page++;
                if (page < IntroPages.Length)
                {
                    text.text = IntroPages[page];
                    if (page == IntroPages.Length - 1)
                        btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "출발";
                    if (page == 1)
                    {
                        // 이 순간 세상의 글자가 깨진다
                        PlayerPrefs.SetInt("intro_seen", 1);
                        PlayerPrefs.Save();
                        RefreshTitleBrokenness();
                        SfxPlayer.Instance?.Split();
                    }
                }
                else
                {
                    Destroy(overlay.gameObject);
                    onDone();
                }
            });
        }

        private GlyphCodexPanel _codex;

        private void OpenCodex()
        {
            if (_codex == null)
            {
                var go = new GameObject("GlyphCodex");
                go.transform.SetParent(transform, false);
                _codex = go.AddComponent<GlyphCodexPanel>();
                _codex.Build(_canvas);
            }
            _codex.Show();
        }

        private void ShowTitle()
        {
            RefreshTitleBrokenness();
            DestroyGame();
            if (_selectPanel != null) _selectPanel.gameObject.SetActive(false);
            _titlePanel.gameObject.SetActive(true);
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
        private MapWorld _mapWorld;
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
            var go = new GameObject("MapWorld", typeof(MapWorld));
            _mapWorld = go.GetComponent<MapWorld>();
            _mapWorld.Enter(this, _maps[index], _stages, _cam, _canvas, playerPos);

            // 방 보상 (D-23): 이 맵의 방을 완주했으면 자음 회수 연출
            var room = MapProgress.PendingRoomReward(_maps[index]);
            if (room != null)
            {
                ProgressStore.RecoverConsonant(room.reward[0]);
                ShowConsonantPopup(room);
            }
        }

        private void ShowConsonantPopup(RoomJson room)
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "RoomRewardPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300));

            string where = string.IsNullOrEmpty(room.label) ? "이곳" : $"'{room.label}'";
            var title = UiFactory.CreateText(box, "T", $"{where}의 글자 조각을 모두 맞췄다!", 30, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(520, 60));

            var msg = UiFactory.CreateText(box, "M",
                $"잃어버린 자음  '{room.reward}'  을(를) 되찾았다!\n깨져 있던 글자들이 조금 돌아왔다...", 24, UiFactory.Dim);
            UiFactory.SetRect(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(500, 90));

            var ok = UiFactory.CreateButton(box, "Ok", "계속", 24, UiFactory.Accent, Color.white, () =>
            {
                Destroy(overlay.gameObject);
                if (_mapWorld != null) _mapWorld.RefreshStates();
                RefreshTitleBrokenness();
            });
            UiFactory.SetRect((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(180, 58));
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
                _itemPanels.Closed += () => { if (_mapWorld != null) _mapWorld.RefreshStates(); };
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

        private void ShowVictoryPopup(BattleConfig config)
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "VictoryPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300));

            var title = UiFactory.CreateText(box, "T", $"{config.name} 격파!", 38, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(500, 60));

            var msg = UiFactory.CreateText(box, "M",
                $"잃어버린 자음  '{config.rewardConsonant}'  을(를) 되찾았다!\n세상의 글자가 조금 돌아왔다...", 24, UiFactory.Dim);
            UiFactory.SetRect(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(500, 90));

            var ok = UiFactory.CreateButton(box, "Ok", "계속", 24, UiFactory.Accent, Color.white, () =>
            {
                Destroy(overlay.gameObject);
                ReturnToMap();
            });
            UiFactory.SetRect((RectTransform)ok.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(180, 58));
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

        // ---- 스테이지 선택 ----

        private void ShowStageSelect()
        {
            DestroyGame();
            _titlePanel.gameObject.SetActive(false);
            if (_editor != null) _editor.Hide();
            if (_selectPanel != null) Destroy(_selectPanel.gameObject); // 별 표시 갱신 위해 재생성

            // 내 스테이지 포함해 목록 재구성
            _stages = StageLoader.LoadAll();
            _stages.AddRange(StageLoader.LoadCustom());

            _selectPanel = UiFactory.CreatePanel(_canvas.transform, "SelectPanel", BgColor);
            UiFactory.Stretch(_selectPanel);

            var header = UiFactory.CreateText(_selectPanel, "Header", "스테이지 선택", 42, UiFactory.Ink);
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(500, 70));

            var gold = UiFactory.CreateText(_selectPanel, "Gold", $"골드  {ProgressStore.Gold}", 24, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(gold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -40), new Vector2(300, 44));

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
                bool isCustom = stage.id >= LevelEditor.CustomIdBase;
                bool consonantLocked = !isCustom && ProgressStore.GetStars(stage.id) == 0
                    && ProgressStore.MissingConsonants(stage).Length > 0; // 자음 게이트 (D-22)
                bool unlocked = (isCustom || ProgressStore.IsUnlocked(_stages, i)) && !consonantLocked;
                int stars = ProgressStore.GetStars(stage.id);
                bool ruby = ProgressStore.GetRuby(stage.id);
                string label = consonantLocked ? "▒" : isCustom ? $"C{stage.id - LevelEditor.CustomIdBase}" : stage.id.ToString();

                var btn = UiFactory.CreateButton(gridRect, $"Stage_{stage.id}",
                    unlocked ? label : consonantLocked ? "▒" : "잠김", 30,
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

                if (unlocked)
                {
                    // 난이도 표시 (오른쪽 아래)
                    var diff = UiFactory.CreateText((RectTransform)btn.transform, "Diff", stage.difficulty.ToString(), 14,
                        stage.difficulty >= 5 ? new Color(0.85f, 0.30f, 0.20f) : UiFactory.Dim, TextAlignmentOptions.BottomRight);
                    UiFactory.SetRect(diff.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-7, 4), new Vector2(30, 20));
                }
            }
        }

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
