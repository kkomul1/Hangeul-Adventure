using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>GameApp의 스테이지 선택 그리드 부분.</summary>
    public partial class GameApp
    {
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

            var gold = UiFactory.CreateText(_selectPanel, "Gold", ProgressStore.Format(ProgressStore.Coins), 24, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(gold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -40), new Vector2(300, 44));

            var backBtn = UiFactory.CreateButton(_selectPanel, "BackBtn", "← 타이틀", 22, UiFactory.Paper, UiFactory.Ink, ShowTitle);
            UiFactory.SetRect((RectTransform)backBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(150, 52));

            // 스크롤 그리드 (M4-5): 스테이지 98개+커스텀 — 고정 판에 안 들어가므로 세로 스크롤
            var viewport = UiFactory.CreatePanel(_selectPanel, "GridView", new Color(0, 0, 0, 0)); // 투명하지만 휠/드래그 레이캐스트 대상
            UiFactory.SetRect(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1000, 520));
            viewport.gameObject.AddComponent<RectMask2D>();

            var gridRect = UiFactory.CreateEmpty(viewport, "Grid");
            gridRect.anchorMin = new Vector2(0, 1);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.pivot = new Vector2(0.5f, 1);
            gridRect.sizeDelta = Vector2.zero; // CreateEmpty 기본 크기 잔여 제거 (도감 버그와 동일 함정)
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(88, 88);
            grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(0, 0, 4, 4);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            var fitter = gridRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = gridRect;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;

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
                // consonantLocked면 73행에서 unlocked=false가 되므로 label은 쓰이지 않는다
                string label = isCustom ? $"C{stage.id - LevelEditor.CustomIdBase}" : stage.id.ToString();

                var lockSprite = unlocked ? null : Resources.Load<Sprite>("Art/Ui/lock");
                var btn = UiFactory.CreateButton(gridRect, $"Stage_{stage.id}",
                    unlocked ? label : lockSprite != null ? "" : "잠김", 30, // 잠금은 아이콘, 없으면 텍스트 폴백 (A-⑥)
                    unlocked ? UiFactory.Paper : new Color(0.82f, 0.80f, 0.76f),
                    unlocked ? UiFactory.Ink : UiFactory.Dim,
                    unlocked ? () => StartStage(index) : (System.Action)null);
                btn.interactable = unlocked;

                if (lockSprite != null)
                {
                    var go = new GameObject("LockIcon", typeof(Image));
                    go.transform.SetParent(btn.transform, false);
                    var img = go.GetComponent<Image>();
                    img.sprite = lockSprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    // 자음 게이트는 진행 잠금보다 어둡게 — 두 잠금의 구분은 기존처럼 색이 담당
                    img.color = consonantLocked ? new Color(0.45f, 0.43f, 0.41f) : UiFactory.Dim;
                    UiFactory.SetRect((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(30, 30));
                }

                if (stars > 0)
                {
                    string starStr = new string('★', stars) + new string('☆', 3 - stars);
                    var starText = UiFactory.CreateText((RectTransform)btn.transform, "Stars", starStr, 20,
                        new Color(0.95f, 0.72f, 0.12f));
                    UiFactory.SetRect(starText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(100, 28));
                }

                if (unlocked)
                {
                    // 난이도 표시 (오른쪽 아래)
                    var diff = UiFactory.CreateText((RectTransform)btn.transform, "Diff", stage.difficulty.ToString(), 14,
                        stage.difficulty >= 5 ? new Color(0.85f, 0.30f, 0.20f) : UiFactory.Dim, TextAlignmentOptions.BottomRight);
                    UiFactory.SetRect(diff.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-7, 4), new Vector2(30, 20));
                }

                if (ruby)
                {
                    // 루비 별 = 스테이지 번호 위 루비 아이콘. 아이콘이 없으면 ◆ 글리프로 폴백
                    var rubySprite = Resources.Load<Sprite>("Art/Ui/ruby");
                    if (rubySprite != null)
                    {
                        var go = new GameObject("RubyIcon", typeof(Image));
                        go.transform.SetParent(btn.transform, false);
                        var img = go.GetComponent<Image>();
                        img.sprite = rubySprite;
                        img.preserveAspect = true;
                        img.raycastTarget = false;
                        UiFactory.SetRect((RectTransform)go.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                            new Vector2(0, -8), new Vector2(22, 22));
                    }
                    else
                    {
                        var mark = UiFactory.CreateText((RectTransform)btn.transform, "RubyMark", "◆", 16,
                            new Color(0.92f, 0.20f, 0.24f));
                        UiFactory.SetRect(mark.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                            new Vector2(0, -10), new Vector2(30, 22));
                    }
                }
            }
        }
    }
}
