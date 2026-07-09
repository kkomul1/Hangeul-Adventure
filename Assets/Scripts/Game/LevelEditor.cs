using System.Collections.Generic;
using System.IO;
using System.Text;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 게임 내 레벨 에디터: 보드 편집 → 솔버 검증 → 테스트 플레이 → JSON 저장.
    /// 저장된 스테이지(내 스테이지)는 스테이지 선택 화면에 즉시 나타난다.
    /// 저장 위치: persistentDataPath/CustomStages/*.json (기존 스테이지 포맷 그대로 — 파일 공유 가능)
    /// </summary>
    public class LevelEditor : MonoBehaviour
    {
        public const int CustomIdBase = 1000;
        public const int MaxSize = 5;

        private GameApp _app;
        private RectTransform _panel;
        private RectTransform _gridRect;
        private TextMeshProUGUI _sizeLabel, _brushLabel, _resultText;
        private TMP_InputField _titleInput, _goalInput, _brushInput;

        private int _w = 3, _h = 3;
        private char[] _cells = new char[MaxSize * MaxSize]; // '\0'=빈칸, '#'=벽, 그 외=타일. 인덱스 = 위에서부터 row*MaxSize+col
        private char _brush = 'ㄱ';

        private StageData _validated;   // 마지막 검증 통과 스테이지
        private int _validatedMin;
        private Button _deleteAllBtn;
        private bool _deleteArmed;      // 모두 삭제 2단계 확인

        private static readonly Color WallColor = new Color(0.45f, 0.43f, 0.40f);
        private static readonly Color FloorColor = new Color(0.90f, 0.88f, 0.83f);

        public static string CustomFolder => Path.Combine(Application.persistentDataPath, "CustomStages");

        public void Build(Canvas canvas, GameApp app)
        {
            _app = app;
            _panel = UiFactory.CreatePanel(canvas.transform, "EditorPanel", new Color(0.93f, 0.90f, 0.84f));
            UiFactory.Stretch(_panel);

            var header = UiFactory.CreateText(_panel, "Header", "레벨 에디터", 38, UiFactory.Ink);
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(400, 60));

            var backBtn = UiFactory.CreateButton(_panel, "BackBtn", "← 타이틀", 20, UiFactory.Paper, UiFactory.Ink, () => { Hide(); _app.ShowTitleFromEditor(); });
            UiFactory.SetRect((RectTransform)backBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(140, 48));

            // ---- 왼쪽: 보드 그리드 ----
            _gridRect = UiFactory.CreateEmpty(_panel, "Grid");
            UiFactory.SetRect(_gridRect, new Vector2(0.30f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 420));
            var grid = _gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(6, 6);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            // 크기 조절
            var sizeRow = UiFactory.CreateEmpty(_panel, "SizeRow");
            UiFactory.SetRect(sizeRow, new Vector2(0.30f, 0.13f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 50));
            _sizeLabel = UiFactory.CreateText(sizeRow, "SizeLabel", "", 22, UiFactory.Ink);
            UiFactory.SetRect(_sizeLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 44));
            MakeSizeBtn(sizeRow, "W-", -80, () => Resize(_w - 1, _h));
            MakeSizeBtn(sizeRow, "W+", -28, () => Resize(_w + 1, _h));
            MakeSizeBtn(sizeRow, "H-", 248, () => Resize(_w, _h - 1));
            MakeSizeBtn(sizeRow, "H+", 300, () => Resize(_w, _h + 1));

            // ---- 오른쪽: 브러시/팔레트/설정 ----
            var right = UiFactory.CreateEmpty(_panel, "Right");
            UiFactory.SetRect(right, new Vector2(0.74f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 600));

            _brushLabel = UiFactory.CreateText(right, "BrushLabel", "", 22, UiFactory.Ink, TextAlignmentOptions.Left);
            UiFactory.SetRect(_brushLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(300, 40));

            var specialRow = UiFactory.CreateEmpty(right, "Specials");
            UiFactory.SetRect(specialRow, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -46), new Vector2(560, 46));
            MakePaletteBtn(specialRow, "빈칸", 0, 90, () => SetBrush('\0'));
            MakePaletteBtn(specialRow, "벽", 96, 90, () => SetBrush('#'));

            BuildPaletteRow(right, "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎ", -100);
            BuildPaletteRow(right, "ㅏㅑㅓㅕㅗㅛㅜㅠㅡㅣ", -152);

            _brushInput = UiFactory.CreateInput(right, "BrushInput", "직접 입력 (쌍자음·겹받침·글자, 예: ㄲ 갃 과)", 20);
            UiFactory.SetRect((RectTransform)_brushInput.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -206), new Vector2(560, 48));
            _brushInput.onValueChanged.AddListener(OnBrushTyped);

            var titleLabel = UiFactory.CreateText(right, "TitleLabel", "제목", 20, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -272), new Vector2(120, 36));
            _titleInput = UiFactory.CreateInput(right, "TitleInput", "내 스테이지", 20);
            UiFactory.SetRect((RectTransform)_titleInput.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -266), new Vector2(440, 48));

            var goalLabel = UiFactory.CreateText(right, "GoalLabel", "목표", 20, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(goalLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -330), new Vector2(120, 36));
            _goalInput = UiFactory.CreateInput(right, "GoalInput", "쉼표 구분 (예: 사과  또는  가,나)", 20);
            UiFactory.SetRect((RectTransform)_goalInput.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(120, -324), new Vector2(440, 48));

            // 동작 버튼
            var validateBtn = UiFactory.CreateButton(right, "ValidateBtn", "검증", 24, UiFactory.Paper, UiFactory.Ink, Validate);
            UiFactory.SetRect((RectTransform)validateBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -400), new Vector2(170, 58));
            var testBtn = UiFactory.CreateButton(right, "TestBtn", "테스트 플레이", 24, UiFactory.Paper, UiFactory.Ink, TestPlay);
            UiFactory.SetRect((RectTransform)testBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -400), new Vector2(200, 58));
            var saveBtn = UiFactory.CreateButton(right, "SaveBtn", "저장", 24, UiFactory.Accent, Color.white, Save);
            UiFactory.SetRect((RectTransform)saveBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(390, -400), new Vector2(170, 58));

            // 내 스테이지 관리
            var openBtn = UiFactory.CreateButton(right, "OpenFolderBtn", "내 스테이지 폴더 열기", 18, UiFactory.Paper, UiFactory.Dim, OpenCustomFolder);
            UiFactory.SetRect((RectTransform)openBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -466), new Vector2(230, 44));
            _deleteAllBtn = UiFactory.CreateButton(right, "DeleteAllBtn", "내 스테이지 모두 삭제", 18, UiFactory.Paper, UiFactory.Dim, DeleteAllCustom);
            UiFactory.SetRect((RectTransform)_deleteAllBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, -466), new Vector2(230, 44));

            _resultText = UiFactory.CreateText(right, "Result", "칸을 클릭해 현재 브러시를 놓습니다. 저장하면 스테이지 선택의 '내 스테이지'로 들어갑니다.", 19, UiFactory.Dim, TextAlignmentOptions.TopLeft);
            UiFactory.SetRect(_resultText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -520), new Vector2(560, 110));

            SetBrush('ㄱ');
            RebuildGrid();
        }

        public void Show()
        {
            _panel.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        // ---- UI 헬퍼 ----

        private void MakeSizeBtn(RectTransform parent, string label, float x, System.Action action)
        {
            var b = UiFactory.CreateButton(parent, label, label, 20, UiFactory.Paper, UiFactory.Ink,
                () => { action(); });
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 110, 0), new Vector2(46, 44));
        }

        private void MakePaletteBtn(RectTransform parent, string label, float x, float w, System.Action action)
        {
            var b = UiFactory.CreateButton(parent, "P_" + label, label, 20, UiFactory.Paper, UiFactory.Ink, () => action());
            UiFactory.SetRect((RectTransform)b.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(w, 44));
        }

        private void BuildPaletteRow(RectTransform parent, string chars, float y)
        {
            var row = UiFactory.CreateEmpty(parent, "Palette" + y);
            UiFactory.SetRect(row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, y), new Vector2(560, 46));
            float x = 0;
            foreach (char c in chars)
            {
                char captured = c;
                MakePaletteBtn(row, c.ToString(), x, 37, () => SetBrush(captured));
                x += 40;
            }
        }

        private void SetBrush(char c)
        {
            _brush = c;
            _brushLabel.text = "브러시:  " + (c == '\0' ? "빈칸" : c == '#' ? "벽" : c.ToString());
        }

        private void OnBrushTyped(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            char c = value[value.Length - 1];
            if (Hangul.IsTile(c)) SetBrush(c);
        }

        private void Resize(int w, int h)
        {
            _w = Mathf.Clamp(w, 1, MaxSize);
            _h = Mathf.Clamp(h, 1, MaxSize);
            _validated = null;
            RebuildGrid();
        }

        private void RebuildGrid()
        {
            _sizeLabel.text = $"{_w} x {_h}";
            foreach (Transform child in _gridRect) Destroy(child.gameObject);

            var grid = _gridRect.GetComponent<GridLayoutGroup>();
            grid.constraintCount = _w;
            int cell = Mathf.Min(72, (int)(380f / Mathf.Max(_w, _h)));
            grid.cellSize = new Vector2(cell, cell);

            for (int row = 0; row < _h; row++)
            {
                for (int col = 0; col < _w; col++)
                {
                    int r = row, c = col;
                    char cur = _cells[row * MaxSize + col];
                    var btn = UiFactory.CreateButton(_gridRect, $"Cell_{row}_{col}",
                        cur == '\0' || cur == '#' ? "" : cur.ToString(), 30,
                        cur == '#' ? WallColor : cur == '\0' ? FloorColor : UiFactory.Paper,
                        UiFactory.Ink,
                        () => OnCellClicked(r, c));
                }
            }
        }

        private void OnCellClicked(int row, int col)
        {
            // 같은 브러시를 다시 찍으면 빈칸으로 (지우개 겸용)
            char cur = _cells[row * MaxSize + col];
            _cells[row * MaxSize + col] = (cur == _brush) ? '\0' : _brush;
            _validated = null;
            RebuildGrid();
        }

        // ---- 스테이지 빌드/검증/저장 ----

        private string[] BuildRows()
        {
            var rows = new string[_h];
            for (int row = 0; row < _h; row++)
            {
                var sb = new StringBuilder(_w);
                for (int col = 0; col < _w; col++)
                {
                    char c = _cells[row * MaxSize + col];
                    sb.Append(c == '\0' ? '.' : c);
                }
                rows[row] = sb.ToString();
            }
            return rows;
        }

        private bool TryBuildStage(out StageData stage, out string error)
        {
            stage = null;
            error = null;

            var goalsRaw = (_goalInput.text ?? "").Trim();
            if (goalsRaw.Length == 0) { error = "목표를 입력하세요 (예: 사과)"; return false; }

            var groups = new List<GoalGroup>();
            foreach (string part in goalsRaw.Split(','))
            {
                string g = part.Trim();
                if (g.Length == 0) continue;
                foreach (char c in g)
                    if (!Hangul.IsTile(c)) { error = $"목표에 쓸 수 없는 문자: '{c}'"; return false; }
                groups.Add(new GoalGroup { display = g, slots = g.ToCharArray() });
            }
            if (groups.Count == 0) { error = "목표를 입력하세요"; return false; }

            try { stage = StageBuilder.FromRows(BuildRows(), groups.ToArray()); }
            catch (System.Exception e) { error = e.Message; return false; }

            stage.title = string.IsNullOrWhiteSpace(_titleInput.text) ? "내 스테이지" : _titleInput.text.Trim();
            return true;
        }

        private void Validate()
        {
            if (!TryBuildStage(out var stage, out string error))
            {
                _resultText.text = "⚠ " + error;
                return;
            }

            _resultText.text = "검증 중...";
            var r = Solver.Solve(stage, timeBudgetMs: 8_000);
            if (r.Aborted)
            {
                _resultText.text = "⚠ 검증 시간 초과 — 보드가 너무 복잡합니다. 타일을 줄여보세요.";
                return;
            }
            if (!r.Solvable)
            {
                _resultText.text = "✗ 풀이 불가능한 배치입니다.";
                return;
            }

            stage.minMoves = r.MinMoves;
            stage.starThresholds = StageData.DefaultStarThresholds(r.MinMoves);
            _validated = stage;
            _validatedMin = r.MinMoves;
            int[] th = stage.starThresholds;
            _resultText.text = $"✓ 풀이 가능 — 최소 {r.MinMoves}수 (★★★ {th[2]}수 이하 · ★★ {th[1]}수 이하 · 루비 = {r.MinMoves}수)";
        }

        private void TestPlay()
        {
            Validate();
            if (_validated == null) return;
            Hide();
            _app.StartTestStage(_validated);
        }

        private void Save()
        {
            Validate();
            if (_validated == null) return;

            Directory.CreateDirectory(CustomFolder);
            int id = NextCustomId();
            _validated.id = id;

            string json = ToJson(_validated, BuildRows());
            string path = Path.Combine(CustomFolder, $"custom_{id - CustomIdBase:000}.json");
            File.WriteAllText(path, json);
            _resultText.text = $"✓ 저장됨 — 스테이지 선택의 '내 스테이지 C{id - CustomIdBase}'에서 플레이할 수 있습니다.\n{path}";
        }

        private void OpenCustomFolder()
        {
            Directory.CreateDirectory(CustomFolder);
            Application.OpenURL("file://" + CustomFolder.Replace('\\', '/'));
        }

        private void DeleteAllCustom()
        {
            if (!_deleteArmed)
            {
                _deleteArmed = true;
                _deleteAllBtn.GetComponentInChildren<TextMeshProUGUI>().text = "정말요? 한 번 더";
                _resultText.text = "⚠ 한 번 더 누르면 내 스테이지가 전부 삭제됩니다 (되돌릴 수 없음).";
                return;
            }

            _deleteArmed = false;
            _deleteAllBtn.GetComponentInChildren<TextMeshProUGUI>().text = "내 스테이지 모두 삭제";
            int count = 0;
            if (Directory.Exists(CustomFolder))
            {
                foreach (string f in Directory.GetFiles(CustomFolder, "*.json"))
                {
                    File.Delete(f);
                    count++;
                }
            }
            _resultText.text = $"내 스테이지 {count}개 삭제됨.";
        }

        private static int NextCustomId()
        {
            int max = CustomIdBase;
            if (Directory.Exists(CustomFolder))
            {
                foreach (var s in StageLoader.LoadCustom())
                    if (s.id > max) max = s.id;
            }
            return max + 1;
        }

        private string ToJson(StageData stage, string[] rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"id\": {stage.id},");
            sb.AppendLine($"  \"title\": \"{stage.title}\",");
            sb.Append("  \"rows\": [");
            for (int i = 0; i < rows.Length; i++)
                sb.Append($"\"{rows[i]}\"{(i < rows.Length - 1 ? ", " : "")}");
            sb.AppendLine("],");
            sb.Append("  \"goals\": [");
            for (int i = 0; i < stage.goals.Length; i++)
            {
                var g = stage.goals[i];
                sb.Append($"{{ \"display\": \"{g.display}\", \"slots\": \"{new string(g.slots)}\" }}{(i < stage.goals.Length - 1 ? ", " : "")}");
            }
            sb.AppendLine("],");
            sb.AppendLine($"  \"minMoves\": {stage.minMoves},");
            sb.AppendLine($"  \"stars\": [0, {stage.starThresholds[1]}, {stage.starThresholds[2]}]");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
