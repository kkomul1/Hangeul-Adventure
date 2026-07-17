using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 글자 도감 (모험 요소 1). 탭 2개 — 그 이상은 두지 않는다 (차용 시스템 분석 8장 리스크 4).
    /// 반절표: 기본 자음 14 × 기본 모음 10 = 140칸 달성 격자. 행 머리 = 자음 회수 상태 = 자음 사다리.
    /// 만든 글자: 지금까지 만든 모든 글자 나열 (겹받침·복합모음·쌍자음 = 금색).
    /// </summary>
    public class GlyphCodexPanel : MonoBehaviour
    {
        // 반절표 축. 자음은 StageData.UsedBaseConsonants와 같은 기본 14자 —
        // Hangul.Choseong(19자)는 ㄲㄸㅃㅆㅉ를 포함해 자음 회수 보상 체계와 어긋나므로 쓰지 않는다.
        private const string BanConsonants = "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎ";
        private const string BanVowels = "ㅏㅑㅓㅕㅗㅛㅜㅠㅡㅣ";

        private static readonly Color Gold = new Color(1.0f, 0.88f, 0.55f);
        private static readonly Color VowelHead = new Color(0.88f, 0.85f, 0.79f);
        private static readonly Color UnmadeCell = new Color(0.91f, 0.89f, 0.85f);
        private static readonly Color LockedCell = new Color(0.78f, 0.76f, 0.72f);
        private static readonly Color LockedInk = new Color(0.62f, 0.60f, 0.56f);
        private static readonly Color GhostInk = new Color(0.16f, 0.14f, 0.12f, 0.22f);
        private static readonly Color TabIdle = new Color(0.85f, 0.83f, 0.78f);

        private RectTransform _panel;
        private RectTransform _viewport;
        private RectTransform _grid;
        private TextMeshProUGUI _countText;
        private TextMeshProUGUI _emptyText;

        private RectTransform _banTab;
        private RectTransform _listTab;
        private Image _banTabBg;
        private Image _listTabBg;
        private Image[] _headBg;
        private TextMeshProUGUI[] _headText;
        private Image[] _cellBg;
        private TextMeshProUGUI[] _cellText;
        private TextMeshProUGUI _ladderText;
        private bool _showBan = true;

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

            var banBtn = UiFactory.CreateButton(box, "TabBan", "반절표", 18, UiFactory.Paper, UiFactory.Ink, () => SelectTab(true));
            UiFactory.SetRect((RectTransform)banBtn.transform, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(-4, -74), new Vector2(148, 34));
            _banTabBg = banBtn.GetComponent<Image>();

            var listBtn = UiFactory.CreateButton(box, "TabList", "만든 글자", 18, UiFactory.Paper, UiFactory.Ink, () => SelectTab(false));
            UiFactory.SetRect((RectTransform)listBtn.transform, new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(4, -74), new Vector2(148, 34));
            _listTabBg = listBtn.GetComponent<Image>();

            _banTab = UiFactory.CreateEmpty(box, "BanTab");
            UiFactory.SetRect(_banTab, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(720, 448));
            BuildBanTable(_banTab);

            _listTab = UiFactory.CreateEmpty(box, "ListTab");
            UiFactory.SetRect(_listTab, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(720, 448));
            BuildList(_listTab);

            SelectTab(true);
            _panel.gameObject.SetActive(false);
        }

        /// <summary>반절표 165칸(모음 머리 + 자음 머리 + 140칸)은 여기서 1회만 만들고, 갱신은 색·글자만 바꾼다.</summary>
        private void BuildBanTable(RectTransform root)
        {
            var table = UiFactory.CreatePanel(root, "Table", new Color(1, 1, 1, 0.4f));
            UiFactory.SetRect(table, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(510, 419));
            var layout = table.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(40, 25);
            layout.spacing = new Vector2(5, 2);
            layout.padding = new RectOffset(10, 10, 8, 8); // 11열(40*11+5*10=490)+좌우 10 = 510, 15행(25*15+2*14=403)+상하 8 = 419 = 표 크기
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 11;

            UiFactory.CreateEmpty(table, "Corner"); // 표 좌상단 빈 칸 (GridLayoutGroup가 크기를 잡으므로 sizeDelta 불필요)
            foreach (char v in BanVowels)
            {
                var head = UiFactory.CreatePanel(table, $"V_{v}", VowelHead);
                var vt = UiFactory.CreateText(head, "Ch", v.ToString(), 17, UiFactory.Dim);
                UiFactory.Stretch(vt.rectTransform);
            }

            _headBg = new Image[BanConsonants.Length];
            _headText = new TextMeshProUGUI[BanConsonants.Length];
            _cellBg = new Image[BanConsonants.Length * BanVowels.Length];
            _cellText = new TextMeshProUGUI[_cellBg.Length];

            for (int r = 0; r < BanConsonants.Length; r++)
            {
                char cons = BanConsonants[r];
                var head = UiFactory.CreatePanel(table, $"C_{cons}", UiFactory.Paper);
                _headBg[r] = head.GetComponent<Image>();
                _headText[r] = UiFactory.CreateText(head, "Ch", cons.ToString(), 18, UiFactory.Ink);
                UiFactory.Stretch(_headText[r].rectTransform);

                for (int c = 0; c < BanVowels.Length; c++)
                {
                    char syl = Engine.Hangul.ComposeSyllable(cons, BanVowels[c]);
                    var cell = UiFactory.CreatePanel(table, $"G_{syl}", UiFactory.Paper);
                    int i = r * BanVowels.Length + c;
                    _cellBg[i] = cell.GetComponent<Image>();
                    _cellText[i] = UiFactory.CreateText(cell, "Ch", syl.ToString(), 17, UiFactory.Ink);
                    UiFactory.Stretch(_cellText[i].rectTransform);
                }
            }

            var legend = UiFactory.CreateEmpty(root, "Legend");
            UiFactory.SetRect(legend, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(700, 24));
            float x = AddLegend(legend, 0, UiFactory.Paper, UiFactory.Ink, "가", "만든 글자", 76);
            x = AddLegend(legend, x, UnmadeCell, GhostInk, "가", "아직 못 만듦", 100);
            AddLegend(legend, x, LockedCell, LockedInk, "", "자음 미회수", 88);

            _ladderText = UiFactory.CreateText(legend, "Ladder", "", 16, UiFactory.Dim, TextAlignmentOptions.Right);
            UiFactory.SetRect(_ladderText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(180, 20));
        }

        private void BuildList(RectTransform root)
        {
            var viewport = UiFactory.CreatePanel(root, "View", new Color(1, 1, 1, 0.4f));
            UiFactory.SetRect(viewport, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(710, 418));
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

            var legend = UiFactory.CreateEmpty(root, "Legend");
            UiFactory.SetRect(legend, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(700, 24));
            float x = AddLegend(legend, 0, Gold, UiFactory.Ink, "닭", "희귀 — 쌍자음·겹받침·복합모음", 250);
            AddLegend(legend, x, UiFactory.Paper, UiFactory.Ink, "가", "일반", 44);
        }

        /// <summary>범례 한 항목(실제 칸과 같은 색·글자의 견본 + 설명). 반환값 = 다음 항목의 x.</summary>
        private static float AddLegend(RectTransform parent, float x, Color bg, Color fg, string sample, string label, float labelWidth)
        {
            var swatch = UiFactory.CreatePanel(parent, "Sw", bg);
            UiFactory.SetRect(swatch, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(26, 20));
            var st = UiFactory.CreateText(swatch, "Ch", sample, 13, fg);
            UiFactory.Stretch(st.rectTransform);

            var text = UiFactory.CreateText(parent, "L", label, 15, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(text.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x + 30, 0), new Vector2(labelWidth, 20));
            return x + 30 + labelWidth + 12;
        }

        public void Show()
        {
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            Refresh();
        }

        public void Hide() => _panel.gameObject.SetActive(false);

        private void SelectTab(bool ban)
        {
            _showBan = ban;
            _banTab.gameObject.SetActive(ban);
            _listTab.gameObject.SetActive(!ban);
            _banTabBg.color = ban ? UiFactory.Paper : TabIdle;
            _listTabBg.color = ban ? TabIdle : UiFactory.Paper;
            Refresh();
        }

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
            if (_showBan) RefreshBan();
            else RefreshList();
        }

        private void RefreshBan()
        {
            string glyphs = ProgressStore.Glyphs;
            string recovered = ProgressStore.RecoveredConsonants;
            int made = 0, opened = 0;

            for (int r = 0; r < BanConsonants.Length; r++)
            {
                bool open = recovered.IndexOf(BanConsonants[r]) >= 0;
                if (open) opened++;
                _headBg[r].color = open ? Gold : LockedCell;
                _headText[r].color = open ? UiFactory.Ink : LockedInk;

                for (int c = 0; c < BanVowels.Length; c++)
                {
                    int i = r * BanVowels.Length + c;
                    char syl = Engine.Hangul.ComposeSyllable(BanConsonants[r], BanVowels[c]);
                    bool has = glyphs.IndexOf(syl) >= 0;
                    if (has) made++;

                    _cellBg[i].color = has ? UiFactory.Paper : (open ? UnmadeCell : LockedCell);
                    _cellText[i].color = has ? UiFactory.Ink : GhostInk;
                    _cellText[i].text = (has || open) ? syl.ToString() : ""; // 자음 미회수 행은 무엇이 올지도 감춘다
                }
            }

            _countText.text = $"반절표  {made}/{_cellBg.Length}";
            _ladderText.text = $"되찾은 자음  {opened}/{BanConsonants.Length}";
        }

        private void RefreshList()
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

            // 음절 코드 = (초성*21+중성)*28+종성 이므로 단순 오름차순 = 가나다순 (Glyphs 원소는 전부 완성 음절)
            var sorted = glyphs.ToCharArray();
            System.Array.Sort(sorted);

            foreach (char c in sorted)
            {
                bool rare = IsRare(c);
                var cell = UiFactory.CreatePanel(_grid, $"G_{c}", rare ? Gold : UiFactory.Paper);
                var label = UiFactory.CreateText(cell, "Ch", c.ToString(), 36, UiFactory.Ink);
                UiFactory.Stretch(label.rectTransform);
            }
        }
    }
}
