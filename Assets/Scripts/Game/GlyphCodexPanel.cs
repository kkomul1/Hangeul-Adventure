using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 글자 도감 (모험 요소 1): 지금까지 만들어본 모든 글자를 격자로 전시.
    /// 겹받침·복합모음이 든 희귀 글자는 금색 표시.
    /// </summary>
    public class GlyphCodexPanel : MonoBehaviour
    {
        private RectTransform _panel;
        private RectTransform _viewport;
        private RectTransform _grid;
        private TextMeshProUGUI _countText;
        private TextMeshProUGUI _emptyText;

        public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

        public void Build(Canvas canvas)
        {
            _panel = UiFactory.CreatePanel(canvas.transform, "CodexPanel", new Color(0, 0, 0, 0.5f));
            UiFactory.Stretch(_panel);

            var box = UiFactory.CreatePanel(_panel, "Box", new Color(0.95f, 0.93f, 0.87f));
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 580));

            var title = UiFactory.CreateText(box, "Title", "글자 도감", 32, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(300, 48));

            _countText = UiFactory.CreateText(box, "Count", "", 20, UiFactory.Dim, TextAlignmentOptions.Right);
            UiFactory.SetRect(_countText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -30), new Vector2(300, 36));

            var close = UiFactory.CreateButton(box, "Close", "닫기", 20, UiFactory.Paper, UiFactory.Ink, Hide);
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(100, 44));

            // 스크롤 격자
            var viewport = UiFactory.CreatePanel(box, "View", new Color(1, 1, 1, 0.4f));
            UiFactory.SetRect(viewport, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(710, 450));
            viewport.gameObject.AddComponent<RectMask2D>();
            _viewport = viewport;

            _grid = UiFactory.CreateEmpty(viewport, "Content");
            _grid.anchorMin = new Vector2(0, 1);
            _grid.anchorMax = new Vector2(1, 1);
            _grid.pivot = new Vector2(0.5f, 1);
            _grid.sizeDelta = Vector2.zero; // CreateEmpty 기본 크기(100) 잔여값 제거 — 남기면 뷰포트보다 100px 넓어져 좌우로 넘침
            var layout = _grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(62, 62);
            layout.spacing = new Vector2(8, 8);
            layout.padding = new RectOffset(8, 8, 12, 12); // 10열(62*10+8*9=692)+좌우 8 = 708 ≤ 뷰포트 710
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            var fitter = _grid.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _grid;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;

            _panel.gameObject.SetActive(false);
        }

        public void Show()
        {
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            Refresh();
        }

        public void Hide() => _panel.gameObject.SetActive(false);

        /// <summary>겹받침 또는 복합모음이 들어간 글자 = 희귀 (금색 표시).</summary>
        private static bool IsRare(char c)
        {
            var (cho, jung, jong) = Engine.Hangul.DecomposeSyllable(c);
            return Engine.Hangul.IsWrapVowel(jung)
                || Engine.Hangul.IsCompoundJamo(jung)
                || (jong != '\0' && Engine.Hangul.IsCompoundJamo(jong))
                || Engine.Hangul.IsCompoundJamo(cho);
        }

        private void Refresh()
        {
            foreach (Transform c in _grid) Destroy(c.gameObject);
            if (_emptyText != null) { Destroy(_emptyText.gameObject); _emptyText = null; }

            string glyphs = ProgressStore.Glyphs;
            _countText.text = $"만든 글자  {glyphs.Length}개";

            if (glyphs.Length == 0)
            {
                // 그리드 밖(뷰포트 중앙)에 배치 — 그리드에 넣으면 62x62 셀에 구겨져 세로로 깨진다
                _emptyText = UiFactory.CreateText(_viewport, "Empty",
                    "아직 만든 글자가 없습니다 — 퍼즐에서 글자를 합성해보세요!", 20, UiFactory.Dim);
                UiFactory.SetRect(_emptyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660, 80));
                return;
            }

            foreach (char c in glyphs)
            {
                bool rare = IsRare(c);
                var cell = UiFactory.CreatePanel(_grid, $"G_{c}",
                    rare ? new Color(1.0f, 0.88f, 0.55f) : UiFactory.Paper);
                var label = UiFactory.CreateText(cell, "Ch", c.ToString(), 30, UiFactory.Ink);
                UiFactory.Stretch(label.rectTransform);
            }
        }
    }
}
