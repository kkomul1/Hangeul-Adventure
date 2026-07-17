using System;
using System.Collections;
using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 플레이 화면 HUD: 목표 진행도 판, 행동 수, 조작 버튼, 클리어 팝업 (명세 7~9장).
    /// 전부 코드로 생성.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private GameSession _session;
        private Canvas _canvas;
        private RectTransform _root;
        private bool _isTest;
        private TextMeshProUGUI _tutorialHint;
        private TextMeshProUGUI _moveText;
        private TextMeshProUGUI _targetText;
        private TextMeshProUGUI _stageText;
        private TextMeshProUGUI _hint;
        private RectTransform _goalBar;
        private RectTransform _popup;
        private int _popupFrame = -1;
        private bool _isLastStage;
        private RectTransform _hintPopup;       // 클리어 팝업과 슬롯을 공유하면 서로 파괴하므로 별도 보관
        private TextMeshProUGUI _hintStatus;
        private Coroutine _hintStatusClear;
        private readonly List<(Button btn, TextMeshProUGUI label, Image bg, int slotIndex)> _slotButtons
            = new List<(Button, TextMeshProUGUI, Image, int)>();
        private readonly HashSet<int> _clueSlots = new HashSet<int>(); // 뜻풀이 목표: 정답 글자 숨김 슬롯

        public event Action<int> SlotClicked;   // 진행도 판 슬롯 클릭 (수집 지정)
        public event Action UndoClicked;
        public event Action ResetClicked;       // 플레이 중 재시작 (R)
        public event Action RetryClicked;       // 클리어 후 재도전 (스테이지 재시작)
        public event Action NextClicked;
        public event Action ExitClicked;
        public event Action SettingsClicked;    // 인게임 상시 설정 버튼 (A-⑯)
        public event Action HintClicked;

        /// <summary>힌트 팝업 입력. 좌표는 엔진 좌표계(y 위쪽+). 순수 데이터 — 워커 스레드에서 만든다.</summary>
        public readonly struct HintView
        {
            public readonly char[] Before, After;    // 길이 = width*height
            public readonly int FromX, FromY, ToX, ToY;
            public readonly string Desc;
            public readonly bool NotOptimal;

            public HintView(char[] before, char[] after, int fx, int fy, int tx, int ty, string desc, bool notOptimal)
            {
                Before = before; After = after;
                FromX = fx; FromY = fy; ToX = tx; ToY = ty;
                Desc = desc; NotOptimal = notOptimal;
            }
        }

        private static readonly Color SlotEmpty = new Color(0.90f, 0.88f, 0.83f);
        private static readonly Color SlotFilled = new Color(1.00f, 0.83f, 0.45f);
        private static readonly Color StarYellow = new Color(1.00f, 0.78f, 0.18f);
        private static readonly Color StarRuby = new Color(0.92f, 0.20f, 0.24f);
        private static readonly Color StarOff = new Color(0.82f, 0.79f, 0.74f);

        // ---- 조작 학습 기록 (A-⑨): 아직 안 배운 조작은 하단 힌트에 띄우지 않는다 ----

        public const string ActCollect = "collect", ActCompose = "compose", ActSplit = "split", ActRotate = "rotate";

        /// <summary>이 조작을 이미 해봤는가. 개발자 모드면 전부 표시.</summary>
        public static bool HasLearned(string act)
            => ProgressStore.DevMode || PlayerPrefs.GetString("learned_actions", "").Contains(act + ";");

        /// <summary>조작을 학습 기록에 등록. 구분자 ';'는 부분문자열 충돌 방지용.</summary>
        public static void Learn(string act)
        {
            string learned = PlayerPrefs.GetString("learned_actions", "");
            if (learned.Contains(act + ";")) return;
            PlayerPrefs.SetString("learned_actions", learned + act + ";");
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            // UI는 캔버스 밑에 생성되므로 컴포넌트 파괴 시 함께 정리 (소유권 일원화)
            if (_root != null) Destroy(_root.gameObject);
            HidePopup();
            HideHintPopup();
        }

        public void Build(Canvas canvas)
        {
            _canvas = canvas;
            var root = UiFactory.CreateEmpty(canvas.transform, "Hud");
            _root = root;
            UiFactory.Stretch(root);

            // 상단 바
            var top = UiFactory.CreatePanel(root, "TopBar", new Color(0, 0, 0, 0));
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 110);
            top.GetComponent<Image>().raycastTarget = false;

            _stageText = UiFactory.CreateText(top, "StageText", "", 28, UiFactory.Ink, TextAlignmentOptions.Left);
            // 폭 460 — "스테이지 31/98  뽀뽀  ·  난이도 1"이 한 줄에 들어가야 한다 (300이면 줄바꿈)
            UiFactory.SetRect(_stageText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(460, 60));

            // 설정 버튼 (A-⑯): 우상단 톱니. 타이틀 화면(GameApp.Title.cs)과 같은 구성
            var gearBtn = UiFactory.CreateButton(top, "SettingsBtn", "", 0, UiFactory.Paper, UiFactory.Ink,
                () => SettingsClicked?.Invoke());
            UiFactory.SetRect((RectTransform)gearBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-24, 0), new Vector2(48, 48));
            var gearSprite = Resources.Load<Sprite>("Art/Ui/gear");
            if (gearSprite != null)
            {
                var gearGo = new GameObject("Icon", typeof(Image));
                gearGo.transform.SetParent(gearBtn.transform, false);
                var gearImg = gearGo.GetComponent<Image>();
                gearImg.sprite = gearSprite;
                gearImg.color = UiFactory.Ink;
                gearImg.preserveAspect = true;
                gearImg.raycastTarget = false;
                UiFactory.SetRect((RectTransform)gearGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(32, 32));
            }
            else
            {
                var gearLabel = gearBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (gearLabel != null) { gearLabel.text = "설정"; gearLabel.fontSize = 15; }
            }

            _moveText = UiFactory.CreateText(top, "MoveText", "0", 34, UiFactory.Ink, TextAlignmentOptions.Right);
            UiFactory.SetRect(_moveText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-80, 0), new Vector2(360, 60));

            // 목표 수 상시 표시 (A-⑰): 루비·별3·별2 기준을 플레이 내내 보여준다
            _targetText = UiFactory.CreateText(top, "TargetText", "", 18, UiFactory.Dim, TextAlignmentOptions.Right);
            UiFactory.SetRect(_targetText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-80, -30), new Vector2(420, 26));

            // 목표 진행도 판 (상단 중앙)
            _goalBar = UiFactory.CreateEmpty(root, "GoalBar");
            UiFactory.SetRect(_goalBar, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(600, 84));
            var layout = _goalBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // 하단 바
            var bottom = UiFactory.CreateEmpty(root, "BottomBar");
            bottom.anchorMin = new Vector2(0, 0);
            bottom.anchorMax = new Vector2(1, 0);
            bottom.pivot = new Vector2(0.5f, 0);
            bottom.sizeDelta = new Vector2(0, 90);

            var undoBtn = UiFactory.CreateButton(bottom, "UndoBtn", "되돌리기 (Z)", 22, UiFactory.Paper, UiFactory.Ink, () => UndoClicked?.Invoke());
            UiFactory.SetRect((RectTransform)undoBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(180, 56));

            var resetBtn = UiFactory.CreateButton(bottom, "ResetBtn", "재시작 (R)", 22, UiFactory.Paper, UiFactory.Ink, () => ResetClicked?.Invoke());
            UiFactory.SetRect((RectTransform)resetBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(220, 0), new Vector2(180, 56));

            var hintBtn = UiFactory.CreateButton(bottom, "HintBtn", "힌트 (H)", 22, UiFactory.Paper, UiFactory.Ink, () => HintClicked?.Invoke());
            UiFactory.SetRect((RectTransform)hintBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(416, 0), new Vector2(180, 56));

            // 힌트 대기/오류 표시. 모달이 아니다 — 계산 중에도 플레이어는 계속 움직일 수 있어야 한다.
            // 힌트 버튼과 나가기 버튼 사이를 늘려 잡는다 (고정 폭이면 좁은 화면비에서 나가기와 겹친다)
            _hintStatus = UiFactory.CreateText(bottom, "HintStatus", "", 18, UiFactory.Dim, TextAlignmentOptions.Left);
            var hintStatusRect = _hintStatus.rectTransform;
            hintStatusRect.anchorMin = new Vector2(0, 0.5f);
            hintStatusRect.anchorMax = new Vector2(1, 0.5f);
            hintStatusRect.pivot = new Vector2(0.5f, 0.5f);
            hintStatusRect.offsetMin = new Vector2(612, -28);
            hintStatusRect.offsetMax = new Vector2(-190, 28);

            var exitBtn = UiFactory.CreateButton(bottom, "ExitBtn", "나가기", 22, UiFactory.Paper, UiFactory.Ink, () => ExitClicked?.Invoke());
            UiFactory.SetRect((RectTransform)exitBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(150, 56));

            _hint = UiFactory.CreateText(bottom, "Hint", "", 17, UiFactory.Dim);
            UiFactory.SetRect(_hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(900, 30));
            RefreshHintBar();
        }

        private string _nextLabelOverride;

        public void Bind(GameSession session, int stageNumber, int stageCount, string nextLabel = null)
        {
            _session = session;
            _isTest = stageNumber <= 0;
            _isLastStage = stageNumber >= stageCount;
            _nextLabelOverride = nextLabel;
            _stageText.text = (stageNumber <= 0
                ? session.Stage.title
                : $"스테이지 {stageNumber}/{stageCount}  {session.Stage.title}")
                + $"  ·  난이도 {session.Stage.difficulty}";

            // 튜토리얼 안내 문구 (목표 판 아래)
            if (_tutorialHint != null) { Destroy(_tutorialHint.gameObject); _tutorialHint = null; }
            if (!string.IsNullOrEmpty(session.Stage.hint))
            {
                _tutorialHint = UiFactory.CreateText(_root, "TutorialHint", session.Stage.hint, 21,
                    new Color(0.55f, 0.35f, 0.15f));
                UiFactory.SetRect(_tutorialHint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -108), new Vector2(1000, 36));
            }
            BuildGoalBar();
            Refresh();
            HidePopup();
            HideHintPopup();
            HideHintPending();
        }

        private void BuildGoalBar()
        {
            foreach (Transform child in _goalBar) Destroy(child.gameObject);
            _slotButtons.Clear();
            _clueSlots.Clear();

            int slotIndex = 0;
            foreach (var group in _session.Stage.goals)
            {
                RectTransform host = _goalBar;
                bool hasClue = !string.IsNullOrEmpty(group.clue);
                if (hasClue)
                {
                    // 뜻풀이 목표 (M3-3): 힌트 문구 위, 빈 슬롯 아래. 글자수는 슬롯 수에서 자동 산출
                    var wrap = UiFactory.CreateEmpty(_goalBar, $"Clue_{group.display}");
                    var vLayout = wrap.gameObject.AddComponent<VerticalLayoutGroup>();
                    vLayout.childAlignment = TextAnchor.MiddleCenter;
                    vLayout.spacing = 2;
                    vLayout.childForceExpandWidth = false;
                    vLayout.childForceExpandHeight = false;
                    var wrapFitter = wrap.gameObject.AddComponent<ContentSizeFitter>();
                    wrapFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    wrapFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var clueText = UiFactory.CreateText(wrap, "ClueText",
                        $"{group.clue} ({group.slots.Length}글자)", 18, new Color(0.45f, 0.30f, 0.12f));
                    clueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
                    host = wrap;
                }

                var groupRect = UiFactory.CreateEmpty(host, $"Goal_{group.display}");
                var groupLayout = groupRect.gameObject.AddComponent<HorizontalLayoutGroup>();
                groupLayout.childAlignment = TextAnchor.MiddleCenter;
                groupLayout.spacing = 4;
                groupLayout.childForceExpandWidth = false;
                groupLayout.childForceExpandHeight = false;
                var fitter = groupRect.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                foreach (char slotChar in group.slots)
                {
                    int captured = slotIndex;
                    var btn = UiFactory.CreateButton(groupRect, $"Slot_{captured}", "", 34, SlotEmpty, UiFactory.Ink,
                        () => SlotClicked?.Invoke(captured));
                    var le = btn.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 68;
                    le.preferredHeight = 68;
                    var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (hasClue) _clueSlots.Add(captured);
                    _slotButtons.Add((btn, label, btn.GetComponent<Image>(), captured));
                    slotIndex++;
                }
            }
        }

        /// <summary>배운 조작만 하단 힌트에 노출 (A-⑨). 미학습 조작은 스테이지 hint가 가르친다.
        /// 밀기는 이것 없이는 첫 스테이지가 잠기므로 무조건 표시.</summary>
        private void RefreshHintBar()
        {
            var segs = new List<string> { "드래그/방향키: 밀기" };
            if (HasLearned(ActCollect)) segs.Add("Space: 수집");
            if (HasLearned(ActRotate)) segs.Add("X/우클릭: 회전");
            if (HasLearned(ActCompose)) segs.Add("Q: 합성 필터");
            if (HasLearned(ActSplit)) segs.Add("E: 분해 필터");
            _hint.text = string.Join(" · ", segs);
        }

        /// <summary>행동 수와 슬롯 상태 갱신.</summary>
        public void Refresh()
        {
            _moveText.text = $"행동 수  {_session.MoveCount}";
            RefreshTargetText();
            RefreshHintBar();
            foreach (var (_, label, bg, idx) in _slotButtons)
            {
                bool filled = _session.IsSlotFilled(idx);
                // 미수집 슬롯에도 목표 글자를 흐릿하게 표시 (뭘 만들어야 하는지 항상 보이도록).
                // 뜻풀이 목표(clue) 슬롯은 정답이 퍼즐이므로 채워질 때까지 숨긴다 (M3-3)
                label.text = !filled && _clueSlots.Contains(idx) ? "" : _session.SlotChar(idx).ToString();
                label.color = filled ? UiFactory.Ink : new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.22f);
                bg.color = filled ? SlotFilled : SlotEmpty;
            }
        }

        /// <summary>
        /// 목표 수 상시 표시 (A-⑰): 별3·루비 기준을 플레이 내내 보여준다.
        /// 루비 = 최소 수와 정확히 일치 (GameSession.IsRuby). 이미 넘긴 기준은 흐리게 처리.
        /// </summary>
        private void RefreshTargetText()
        {
            int[] th = _session.Stage.starThresholds ?? Engine.StageData.DefaultStarThresholds(_session.Stage.minMoves);
            int star3 = th[2], star2 = th[1], ruby = _session.Stage.minMoves;
            int used = _session.MoveCount;

            string rubyTag = used <= ruby ? ColorTag(StarRuby) : ColorTag(StarOff);
            string star3Tag = used <= star3 ? ColorTag(StarYellow) : ColorTag(StarOff);
            string star2Tag = used <= star2 ? ColorTag(StarYellow) : ColorTag(StarOff);
            _targetText.text = $"{rubyTag}◆ {ruby}</color>  {star3Tag}★★★ {star3}</color>  {star2Tag}★★ {star2}</color>";
        }

        private static string ColorTag(Color c) => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>";

        // ---- 클리어 팝업 ----

        public void ShowClearPopup(int earnedGold = 0)
        {
            HidePopup();
            int stars = _session.Stars();
            bool ruby = _session.IsRuby;

            _popup = UiFactory.CreatePanel(_canvas.transform, "ClearPopup", new Color(0, 0, 0, 0.55f));
            _popupFrame = Time.frameCount;
            UiFactory.Stretch(_popup);

            var box = UiFactory.CreatePanel(_popup, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 380));

            UiFactory.SetRect(
                UiFactory.CreateText(box, "Title", ruby ? "완벽한 풀이!" : "클리어!", 44, UiFactory.Ink).rectTransform,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(400, 70));

            // 별 3개 (루비면 빨간색)
            var starRow = UiFactory.CreateEmpty(box, "Stars");
            UiFactory.SetRect(starRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(320, 90));
            var starLayout = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starLayout.childAlignment = TextAnchor.MiddleCenter;
            starLayout.spacing = 14;
            for (int i = 0; i < 3; i++)
            {
                var star = UiFactory.CreateText(starRow, $"Star{i}", "★", 64,
                    i < stars ? (ruby ? StarRuby : StarYellow) : StarOff);
                var le = star.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 80;
                le.preferredHeight = 80;
            }

            var info = UiFactory.CreateText(box, "Info",
                $"행동 수 {_session.MoveCount}" + (ruby ? " · 루비!" : "")
                + (earnedGold > 0 ? $"  ·  +{ProgressStore.Format(earnedGold)}!" : $"  ·  보유 {ProgressStore.Format(ProgressStore.Coins)}"),
                22, UiFactory.Dim);
            UiFactory.SetRect(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -38), new Vector2(440, 34));

            // 별 기준 표시 (별별 요구 최대 행동 수)
            int[] th = _session.Stage.starThresholds ?? Engine.StageData.DefaultStarThresholds(_session.Stage.minMoves);
            var criteria = UiFactory.CreateText(box, "Criteria",
                $"★★★ {th[2]}수 이하 · ★★ {th[1]}수 이하 · 루비 = 최소 {_session.Stage.minMoves}수",
                18, UiFactory.Dim);
            UiFactory.SetRect(criteria.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -68), new Vector2(460, 30));

            var next = UiFactory.CreateButton(box, "NextBtn",
                _nextLabelOverride ?? (_isTest ? "에디터로" : _isLastStage ? "스테이지 선택" : "다음 스테이지"), 24,
                UiFactory.Accent, Color.white, () => NextClicked?.Invoke());
            UiFactory.SetRect((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(95, 28), new Vector2(200, 60));

            var retry = UiFactory.CreateButton(box, "RetryBtn", "재도전", 24, UiFactory.Paper, UiFactory.Ink, () => RetryClicked?.Invoke());
            UiFactory.SetRect((RectTransform)retry.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-105, 28), new Vector2(160, 60));
        }

        public void HidePopup()
        {
            if (_popup != null) { Destroy(_popup.gameObject); _popup = null; }
        }

        /// <summary>클리어 팝업에서 Space = 다음 (A-②). 팝업을 띄운 프레임은 건너뛴다 —
        /// 수집 Space(GameController)가 같은 프레임에 흘러들어와 팝업을 즉시 넘기는 것을 막기 위함.</summary>
        private void Update()
        {
            if (_hintPopup != null)
            {
                var hk = Keyboard.current;
                if (hk != null && hk.escapeKey.wasPressedThisFrame) HideHintPopup();
                return;
            }
            if (_popup == null || Time.frameCount == _popupFrame) return;
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) NextClicked?.Invoke();
        }

        // ---- 힌트 ----

        // BoardView의 바닥색과 맞춘 값 (BoardView.FloorColor/PinnedFloorColor는 private)
        private static readonly Color MiniFloor = new Color(0.88f, 0.85f, 0.79f);
        private static readonly Color MiniPinnedFloor = new Color(0.72f, 0.64f, 0.52f);

        public bool HintPopupOpen => _hintPopup != null;

        public void ShowHintPending()
        {
            if (_hintStatusClear != null) { StopCoroutine(_hintStatusClear); _hintStatusClear = null; }
            _hintStatus.color = UiFactory.Dim;
            _hintStatus.text = "힌트 요청 중…";
        }

        public void HideHintPending()
        {
            if (_hintStatusClear != null) { StopCoroutine(_hintStatusClear); _hintStatusClear = null; }
            _hintStatus.text = "";
        }

        /// <summary>힌트 실패 안내. 3초 후 자동으로 사라진다.</summary>
        public void ShowHintMessage(string msg)
        {
            HideHintPending();
            _hintStatus.color = UiFactory.Accent;
            _hintStatus.text = msg;
            _hintStatusClear = StartCoroutine(ClearHintStatusAfter(3f));
        }

        private IEnumerator ClearHintStatusAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _hintStatus.text = "";
            _hintStatusClear = null;
        }

        public void ShowHintPopup(HintView v)
        {
            HideHintPopup();
            HideHintPending();

            _hintPopup = UiFactory.CreatePanel(_canvas.transform, "HintPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(_hintPopup);

            var box = UiFactory.CreatePanel(_hintPopup, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 520));

            UiFactory.SetRect(UiFactory.CreateText(box, "Title", "힌트", 40, UiFactory.Ink).rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 232), new Vector2(400, 44));

            if (v.NotOptimal)
                UiFactory.SetRect(UiFactory.CreateText(box, "NotOptimal",
                        "현재 상태는 최적이 아닙니다 — 지금부터의 최선을 보여줍니다", 19, UiFactory.Accent).rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 192), new Vector2(680, 28));

            UiFactory.SetRect(UiFactory.CreateText(box, "BeforeLabel", "이동 전", 20, UiFactory.Dim).rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-170, 158), new Vector2(200, 24));
            UiFactory.SetRect(UiFactory.CreateText(box, "AfterLabel", "이동 후", 20, UiFactory.Dim).rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170, 158), new Vector2(200, 24));

            BuildMiniBoard(box, "BeforeBoard", v.Before, v.FromX, v.FromY, new Vector2(-170, 20));
            BuildMiniBoard(box, "AfterBoard", v.After, v.ToX, v.ToY, new Vector2(170, 20));

            UiFactory.SetRect(UiFactory.CreateText(box, "Arrow", "→", 44, UiFactory.Ink).rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(60, 60));

            UiFactory.SetRect(UiFactory.CreateText(box, "Desc", v.Desc, 24, UiFactory.Ink).rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(660, 34));

            var close = UiFactory.CreateButton(box, "CloseBtn", "닫기", 24, UiFactory.Accent, Color.white,
                HideHintPopup);
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -212), new Vector2(160, 52));
        }

        public void HideHintPopup()
        {
            if (_hintPopup != null) { Destroy(_hintPopup.gameObject); _hintPopup = null; }
        }

        /// <summary>
        /// 팝업용 미니 보드 (uGUI). BoardView는 SpriteRenderer 월드공간이라 캔버스 안에서 못 쓴다.
        /// 엔진 y는 위쪽+인데 GridLayout은 위→아래로 채우므로 y를 역순으로 순회한다.
        /// </summary>
        private void BuildMiniBoard(Transform parent, string name, char[] cells, int hlX, int hlY, Vector2 pos)
        {
            const float span = 240f, gap = 4f;
            var st = _session.Stage;

            var grid = UiFactory.CreateEmpty(parent, name);
            UiFactory.SetRect(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(span, span));

            int longest = Mathf.Max(st.width, st.height);
            float cellSize = Mathf.Min(48f, (span - gap * (longest - 1)) / longest);

            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(gap, gap);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = st.width;
            layout.childAlignment = TextAnchor.MiddleCenter;

            for (int y = st.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < st.width; x++)
                {
                    if (!st.CellExists(x, y))
                    {
                        UiFactory.CreateEmpty(grid, $"X_{x}_{y}"); // 없는 칸도 격자 자리는 차지해야 정렬이 맞는다
                        continue;
                    }

                    // 강조는 바깥 테두리로: 바깥 칸을 Accent로 칠하고 안쪽 바닥을 4px 들여 덮는다
                    bool highlight = x == hlX && y == hlY;
                    var cell = UiFactory.CreatePanel(grid, $"C_{x}_{y}",
                        highlight ? UiFactory.Accent : new Color(0, 0, 0, 0));
                    Slice(cell);

                    var inner = UiFactory.CreatePanel(cell, "In",
                        st.CellPinned(x, y) ? MiniPinnedFloor : MiniFloor);
                    Slice(inner);
                    UiFactory.Stretch(inner, 4);

                    char c = cells[y * st.width + x];
                    if (c == Hangul.Empty) continue;
                    var glyph = UiFactory.CreateText(inner, "T", c.ToString(), cellSize * 0.62f, UiFactory.Ink);
                    UiFactory.Stretch(glyph.rectTransform);
                }
            }
        }

        private static void Slice(RectTransform rt)
        {
            var img = rt.GetComponent<Image>();
            img.sprite = UiFactory.RoundedSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
        }
    }
}
