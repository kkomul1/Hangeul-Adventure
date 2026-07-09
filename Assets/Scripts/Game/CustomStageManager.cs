using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 내 스테이지/휴지통 관리 GUI. 탐색기 없이 게임 안에서
    /// 플레이/수정/휴지통 이동/복원/영구 삭제를 처리한다.
    /// 복원 시 del_ 접두사를 벗겨 파일명을 정상화한다.
    /// </summary>
    public class CustomStageManager : MonoBehaviour
    {
        private GameApp _app;
        private LevelEditor _editor;
        private RectTransform _panel;
        private RectTransform _customContent, _trashContent;
        private TextMeshProUGUI _statusText;
        private string _armedDeletePath; // 영구 삭제 2단계 확인 (항목별)

        private static readonly Regex TrashPrefix = new Regex(@"^del_\d{14}_(\d+_)?");

        public void Build(Canvas canvas, GameApp app, LevelEditor editor)
        {
            _app = app;
            _editor = editor;
            _panel = UiFactory.CreatePanel(canvas.transform, "ManagerPanel", new Color(0.93f, 0.90f, 0.84f));
            UiFactory.Stretch(_panel);

            var header = UiFactory.CreateText(_panel, "Header", "내 스테이지 관리", 36, UiFactory.Ink);
            UiFactory.SetRect(header.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(400, 56));

            var backBtn = UiFactory.CreateButton(_panel, "BackBtn", "← 에디터", 20, UiFactory.Paper, UiFactory.Ink, () => { Hide(); _app.ShowEditorFromManager(); });
            UiFactory.SetRect((RectTransform)backBtn.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(140, 48));

            _customContent = BuildColumn("내 스테이지", 0.27f);
            _trashContent = BuildColumn($"휴지통 (보관 {LevelEditor.TrashRetentionDays}일)", 0.73f);

            _statusText = UiFactory.CreateText(_panel, "Status", "", 18, UiFactory.Dim);
            UiFactory.SetRect(_statusText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1100, 36));
        }

        private RectTransform BuildColumn(string title, float anchorX)
        {
            var label = UiFactory.CreateText(_panel, "Col_" + title, title, 24, UiFactory.Ink);
            UiFactory.SetRect(label.rectTransform, new Vector2(anchorX, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(540, 40));

            // 스크롤 뷰
            var viewport = UiFactory.CreatePanel(_panel, "View_" + title, new Color(1, 1, 1, 0.35f));
            UiFactory.SetRect(viewport, new Vector2(anchorX, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -48), new Vector2(560, 440));
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = UiFactory.CreateEmpty(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30;

            return content;
        }

        public void Show()
        {
            _panel.gameObject.SetActive(true);
            _armedDeletePath = null;
            Refresh();
        }

        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        /// <summary>양쪽 목록 재구성.</summary>
        public void Refresh()
        {
            foreach (Transform c in _customContent) Destroy(c.gameObject);
            foreach (Transform c in _trashContent) Destroy(c.gameObject);

            // ---- 내 스테이지 ----
            int customCount = 0;
            if (Directory.Exists(LevelEditor.CustomFolder))
            {
                foreach (string path in Directory.GetFiles(LevelEditor.CustomFolder, "*.json"))
                {
                    StageData stage;
                    try { stage = StageLoader.FromJson(File.ReadAllText(path)); }
                    catch { continue; }
                    BuildCustomRow(path, stage);
                    customCount++;
                }
            }
            if (customCount == 0)
                AddEmptyLabel(_customContent, "아직 만든 스테이지가 없습니다");

            // ---- 휴지통 ----
            int trashCount = 0;
            if (Directory.Exists(LevelEditor.TrashFolder))
            {
                foreach (string path in Directory.GetFiles(LevelEditor.TrashFolder, "*.json"))
                {
                    StageData stage;
                    try { stage = StageLoader.FromJson(File.ReadAllText(path)); }
                    catch { continue; }
                    BuildTrashRow(path, stage);
                    trashCount++;
                }
            }
            if (trashCount == 0)
                AddEmptyLabel(_trashContent, "휴지통이 비어 있습니다");
        }

        private void AddEmptyLabel(RectTransform parent, string text)
        {
            var t = UiFactory.CreateText(parent, "Empty", text, 19, UiFactory.Dim);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
        }

        private RectTransform BuildRowBase(RectTransform parent, string title, string subtitle)
        {
            var row = UiFactory.CreatePanel(parent, "Row", UiFactory.Paper);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 72;

            var titleText = UiFactory.CreateText(row, "Title", title, 21, UiFactory.Ink, TextAlignmentOptions.Left);
            UiFactory.SetRect(titleText.rectTransform, new Vector2(0, 0.68f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(280, 32));

            var subText = UiFactory.CreateText(row, "Sub", subtitle, 15, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(subText.rectTransform, new Vector2(0, 0.26f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(280, 26));
            return row;
        }

        private Button AddRowButton(RectTransform row, string name, string label, float xFromRight, float width, Color bg, Color fg, System.Action onClick)
        {
            var btn = UiFactory.CreateButton(row, name, label, 16, bg, fg, onClick);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-xFromRight, 0), new Vector2(width, 46));
            return btn;
        }

        private void BuildCustomRow(string path, StageData stage)
        {
            var row = BuildRowBase(_customContent,
                $"C{stage.id - LevelEditor.CustomIdBase}  {stage.title}",
                $"{stage.width}x{stage.height} · 최소 {stage.minMoves}수 · {StarInfo(stage.id)}");

            AddRowButton(row, "Play", "플레이", 10, 80, UiFactory.Accent, Color.white,
                () => { Hide(); _app.StartCustomPlay(stage); });
            AddRowButton(row, "Edit", "수정", 96, 66, UiFactory.Paper, UiFactory.Ink,
                () => { Hide(); _app.ShowEditorFromManager(); _editor.LoadForEdit(stage, path); });
            AddRowButton(row, "Trash", "휴지통", 168, 76, UiFactory.Paper, UiFactory.Dim,
                () =>
                {
                    LevelEditor.MoveToTrashPublic(path);
                    _statusText.text = $"'{stage.title}'을(를) 휴지통으로 옮겼습니다 ({LevelEditor.TrashRetentionDays}일 보관).";
                    Refresh();
                });
        }

        private void BuildTrashRow(string path, StageData stage)
        {
            System.DateTime deletedAt = ParseDeletedAt(path);
            int daysLeft = Mathf.Max(0, LevelEditor.TrashRetentionDays - (int)(System.DateTime.Now - deletedAt).TotalDays);

            var row = BuildRowBase(_trashContent,
                $"C{stage.id - LevelEditor.CustomIdBase}  {stage.title}",
                $"삭제 {deletedAt:MM-dd HH:mm} · {daysLeft}일 후 영구 삭제");

            AddRowButton(row, "Restore", "복원", 10, 76, UiFactory.Accent, Color.white, () =>
            {
                string name = TrashPrefix.Replace(Path.GetFileName(path), "");
                string target = Path.Combine(LevelEditor.CustomFolder, name);
                int n = 1;
                while (File.Exists(target))
                    target = Path.Combine(LevelEditor.CustomFolder, $"{Path.GetFileNameWithoutExtension(name)}_{n++}.json");
                File.Move(path, target);
                _statusText.text = $"'{stage.title}' 복원됨.";
                Refresh();
            });

            AddRowButton(row, "Delete", _armedDeletePath == path ? "정말요?" : "영구 삭제", 92, 90,
                UiFactory.Paper, new Color(0.75f, 0.20f, 0.20f), () =>
                {
                    if (_armedDeletePath != path)
                    {
                        _armedDeletePath = path;
                        _statusText.text = "⚠ 한 번 더 누르면 영구 삭제됩니다 (되돌릴 수 없음).";
                        Refresh();
                        return;
                    }
                    File.Delete(path);
                    _armedDeletePath = null;
                    _statusText.text = $"'{stage.title}' 영구 삭제됨.";
                    Refresh();
                });
        }

        private string StarInfo(int stageId)
        {
            int stars = ProgressStore.GetStars(stageId);
            if (stars == 0) return "미클리어";
            return new string('★', stars) + (ProgressStore.GetRuby(stageId) ? " 루비" : "");
        }

        private static System.DateTime ParseDeletedAt(string path)
        {
            string name = Path.GetFileName(path);
            if (name.StartsWith("del_") && name.Length > 18
                && System.DateTime.TryParseExact(name.Substring(4, 14), "yyyyMMddHHmmss",
                    null, System.Globalization.DateTimeStyles.None, out var parsed))
                return parsed;
            return File.GetLastWriteTime(path);
        }
    }
}
