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
    }
}
