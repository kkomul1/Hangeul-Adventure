using System;
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

            // 목표 수 상시 표시 (A-⑰): 별3·루비 기준을 플레이 내내 보여준다
            _targetText = UiFactory.CreateText(top, "TargetText", "", 19, UiFactory.Dim, TextAlignmentOptions.Right);
            UiFactory.SetRect(_targetText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-80, -30), new Vector2(360, 26));

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
            int star3 = th[2], ruby = _session.Stage.minMoves;
            int used = _session.MoveCount;

            string star3Tag = used <= star3 ? ColorTag(StarYellow) : ColorTag(StarOff);
            string rubyTag = used <= ruby ? ColorTag(StarRuby) : ColorTag(StarOff);
            _targetText.text = $"{rubyTag}◆ {ruby}</color>   {star3Tag}★★★ {star3}</color>";
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
                + (earnedGold > 0 ? $"  ·  +{earnedGold} 골드!" : $"  ·  보유 {ProgressStore.Gold} 골드"),
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
            if (_popup == null || Time.frameCount == _popupFrame) return;
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) NextClicked?.Invoke();
        }
    }
}
