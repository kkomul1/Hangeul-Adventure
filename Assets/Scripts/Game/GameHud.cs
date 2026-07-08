using System;
using System.Collections.Generic;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 플레이 화면 HUD: 목표 진행도 판, 이동 수, 조작 버튼, 클리어 팝업 (명세 7~9장).
    /// 전부 코드로 생성.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private GameSession _session;
        private TextMeshProUGUI _moveText;
        private TextMeshProUGUI _stageText;
        private RectTransform _goalBar;
        private RectTransform _popup;
        private readonly List<(Button btn, TextMeshProUGUI label, int slotIndex)> _slotButtons
            = new List<(Button, TextMeshProUGUI, int)>();

        public event Action<int> SlotClicked;   // 진행도 판 슬롯 클릭 (수집 지정)
        public event Action UndoClicked;
        public event Action ResetClicked;
        public event Action NextClicked;
        public event Action ExitClicked;

        private static readonly Color SlotEmpty = new Color(0.90f, 0.88f, 0.83f);
        private static readonly Color SlotFilled = new Color(1.00f, 0.83f, 0.45f);
        private static readonly Color StarYellow = new Color(1.00f, 0.78f, 0.18f);
        private static readonly Color StarRuby = new Color(0.92f, 0.20f, 0.24f);
        private static readonly Color StarOff = new Color(0.82f, 0.79f, 0.74f);

        public void Build(Canvas canvas)
        {
            var root = UiFactory.CreateEmpty(canvas.transform, "Hud");
            UiFactory.Stretch(root);

            // 상단 바
            var top = UiFactory.CreatePanel(root, "TopBar", new Color(0, 0, 0, 0));
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 110);
            top.GetComponent<Image>().raycastTarget = false;

            _stageText = UiFactory.CreateText(top, "StageText", "", 28, UiFactory.Ink, TextAlignmentOptions.Left);
            UiFactory.SetRect(_stageText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(300, 60));

            _moveText = UiFactory.CreateText(top, "MoveText", "0", 34, UiFactory.Ink, TextAlignmentOptions.Right);
            UiFactory.SetRect(_moveText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(360, 60));

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
            undoBtn.GetComponent<Image>().sprite = null;

            var resetBtn = UiFactory.CreateButton(bottom, "ResetBtn", "재시작 (R)", 22, UiFactory.Paper, UiFactory.Ink, () => ResetClicked?.Invoke());
            UiFactory.SetRect((RectTransform)resetBtn.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(220, 0), new Vector2(180, 56));

            var exitBtn = UiFactory.CreateButton(bottom, "ExitBtn", "나가기", 22, UiFactory.Paper, UiFactory.Ink, () => ExitClicked?.Invoke());
            UiFactory.SetRect((RectTransform)exitBtn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(150, 56));

            var hint = UiFactory.CreateText(bottom, "Hint", "드래그/방향키: 밀기 · 클릭+Space: 수집 · Q: 합성 필터 · E: 분해 필터", 17, UiFactory.Dim);
            UiFactory.SetRect(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(700, 56));
        }

        public void Bind(GameSession session, int stageNumber, int stageCount)
        {
            _session = session;
            _stageText.text = $"스테이지 {stageNumber}/{stageCount}  {session.Stage.title}";
            BuildGoalBar();
            Refresh();
            HidePopup();
        }

        private void BuildGoalBar()
        {
            foreach (Transform child in _goalBar) Destroy(child.gameObject);
            _slotButtons.Clear();

            int slotIndex = 0;
            foreach (var group in _session.Stage.goals)
            {
                var groupRect = UiFactory.CreateEmpty(_goalBar, $"Goal_{group.display}");
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
                    var btn = UiFactory.CreateButton(groupRect, $"Slot_{captured}", "?", 34, SlotEmpty, UiFactory.Ink,
                        () => SlotClicked?.Invoke(captured));
                    var le = btn.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 68;
                    le.preferredHeight = 68;
                    var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                    _slotButtons.Add((btn, label, captured));
                    slotIndex++;
                }
            }
        }

        /// <summary>이동 수와 슬롯 상태 갱신.</summary>
        public void Refresh()
        {
            _moveText.text = $"이동 수  {_session.MoveCount}";
            foreach (var (btn, label, idx) in _slotButtons)
            {
                bool filled = _session.IsSlotFilled(idx);
                label.text = filled ? _session.SlotChar(idx).ToString() : "";
                btn.GetComponent<Image>().color = filled ? SlotFilled : SlotEmpty;
            }
        }

        // ---- 클리어 팝업 ----

        public void ShowClearPopup()
        {
            HidePopup();
            int stars = _session.Stars();
            bool ruby = _session.IsRuby;

            _popup = UiFactory.CreatePanel(transform.GetComponentInChildren<Canvas>()?.transform ?? transform, "ClearPopup", new Color(0, 0, 0, 0.55f));
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
                $"이동 수 {_session.MoveCount} · 최소 {_session.Stage.minMoves}" + (ruby ? " · 루비!" : ""),
                22, UiFactory.Dim);
            UiFactory.SetRect(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -45), new Vector2(440, 40));

            var next = UiFactory.CreateButton(box, "NextBtn", "다음 스테이지", 24, UiFactory.Accent, Color.white, () => NextClicked?.Invoke());
            UiFactory.SetRect((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-95, 28), new Vector2(200, 60));

            var retry = UiFactory.CreateButton(box, "RetryBtn", "재도전", 24, UiFactory.Paper, UiFactory.Ink, () => { HidePopup(); ResetClicked?.Invoke(); });
            UiFactory.SetRect((RectTransform)retry.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(105, 28), new Vector2(160, 60));
        }

        public void HidePopup()
        {
            if (_popup != null) { Destroy(_popup.gameObject); _popup = null; }
        }
    }
}
