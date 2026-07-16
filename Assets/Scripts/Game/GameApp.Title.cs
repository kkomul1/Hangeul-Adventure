using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>GameApp의 타이틀 화면·오프닝 인트로·글자 도감 진입 부분.</summary>
    public partial class GameApp
    {
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

        // ---- 글자 도감 ----

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
    }
}
