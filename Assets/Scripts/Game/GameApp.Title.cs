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
            // 프리팹 전환 (M3-2): Resources/UiPrefabs/TitlePanel이 있으면 프리팹 사용, 없으면 코드 생성.
            // 프리팹은 에디터 메뉴 "UI 프리팹 추출 (플레이 중 현재 화면)"로 만든다.
            var prefab = Resources.Load<GameObject>("UiPrefabs/TitlePanel");
            if (prefab != null)
            {
                BuildTitleFromPrefab(prefab);
                return;
            }

            _titlePanel = UiFactory.CreatePanel(_canvas.transform, "TitlePanel", BgColor);
            UiFactory.Stretch(_titlePanel);

            // 타이틀 키아트 (ArtDrop 임포트분): 비율 유지로 화면을 덮고, 텍스트 가독용 어둡기 오버레이
            var art = Resources.Load<Sprite>("Art/title_art");
            bool hasArt = art != null;
            if (hasArt)
            {
                var artGo = new GameObject("TitleArt", typeof(Image), typeof(AspectRatioFitter));
                artGo.transform.SetParent(_titlePanel, false);
                artGo.GetComponent<Image>().sprite = art;
                var fitter = artGo.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = art.rect.width / art.rect.height;

                var dimGo = new GameObject("TitleArtDim", typeof(Image));
                dimGo.transform.SetParent(_titlePanel, false);
                dimGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.30f);
                UiFactory.Stretch((RectTransform)dimGo.transform);
            }

            var title = UiFactory.CreateText(_titlePanel, "Title", TitleString, 76,
                hasArt ? new Color(0.97f, 0.94f, 0.87f) : UiFactory.Ink);
            if (UiFactory.TitleFont != null) title.font = UiFactory.TitleFont; // 정묵바위체 (M4-4)
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 120));
            _titleText = title;
            RefreshTitleBrokenness();

            _subtitle = UiFactory.CreateText(_titlePanel, "Subtitle", SubtitleDefault, 26,
                hasArt ? new Color(0.86f, 0.83f, 0.76f) : UiFactory.Dim);
            UiFactory.SetRect(_subtitle.rectTransform, new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 50));
            if (ProgressStore.DevMode) _subtitle.text = "개발자 모드 ON — 전체 스테이지 잠금 해제";

            // 히든 개발자 모드 토글: 타이틀의 '어' 글자 위에 투명 버튼 배치 (TMP 글리프 좌표 기반)
            StartCoroutine(PlaceDevToggle(title));

            // M4-1 개편 (승인안): 모험 시작을 주인공으로, 목록·도감 나란히, 에디터는 작게,
            // 설정은 우상단 톱니 아이콘, 진행 초기화는 설정 팝업 안으로 이동
            var adventure = UiFactory.CreateButton(_titlePanel, "AdventureBtn", "모험 시작", 34, UiFactory.Accent, Color.white, StartAdventure);
            UiFactory.SetRect((RectTransform)adventure.transform, new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 84));

            var start = UiFactory.CreateButton(_titlePanel, "StartBtn", "스테이지 목록", 22, UiFactory.Paper, UiFactory.Ink, ShowStageSelect);
            UiFactory.SetRect((RectTransform)start.transform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(-115, 0), new Vector2(210, 58));

            var codexBtn = UiFactory.CreateButton(_titlePanel, "CodexBtn", "글자 도감", 22, UiFactory.Paper, UiFactory.Ink, OpenCodex);
            UiFactory.SetRect((RectTransform)codexBtn.transform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), new Vector2(115, 0), new Vector2(210, 58));

            var editorBtn = UiFactory.CreateButton(_titlePanel, "EditorBtn", "레벨 에디터", 18, UiFactory.Paper, UiFactory.Dim, ShowLevelEditor);
            UiFactory.SetRect((RectTransform)editorBtn.transform, new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 48));

            var settings = UiFactory.CreateButton(_titlePanel, "SettingsBtn", "", 0, new Color(0.09f, 0.07f, 0.05f, 0.55f), Color.white, ShowSettings);
            UiFactory.SetRect((RectTransform)settings.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(56, 56));
            var settingsImg = settings.GetComponent<Image>();
            settingsImg.sprite = UiFactory.RoundedSprite();
            settingsImg.type = Image.Type.Sliced;
            var gearSprite = Resources.Load<Sprite>("Art/Ui/gear");
            if (gearSprite != null)
            {
                var gearGo = new GameObject("Icon", typeof(Image));
                gearGo.transform.SetParent(settings.transform, false);
                var gearImg = gearGo.GetComponent<Image>();
                gearImg.sprite = gearSprite;
                gearImg.preserveAspect = true;
                gearImg.raycastTarget = false;
                UiFactory.SetRect((RectTransform)gearGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38));
            }
            else
            {
                var gearLabel = settings.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (gearLabel != null) { gearLabel.text = "설정"; gearLabel.fontSize = 16; }
            }
        }

        /// <summary>추출된 프리팹으로 타이틀 구성: 이름으로 텍스트·버튼을 다시 배선한다
        /// (런타임 AddListener는 프리팹에 저장되지 않음).</summary>
        private void BuildTitleFromPrefab(GameObject prefab)
        {
            var go = Instantiate(prefab, _canvas.transform);
            go.name = "TitlePanel";
            _titlePanel = (RectTransform)go.transform;

            // 절차 생성 스프라이트(RoundedSprite)는 에셋이 아니라 프리팹에 비어 있음 — 복원
            foreach (var img in go.GetComponentsInChildren<Image>(true))
                if (img.sprite == null)
                {
                    img.sprite = UiFactory.RoundedSprite();
                    img.type = Image.Type.Sliced;
                }

            _titleText = FindDeep(go.transform, "Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (_titleText != null && UiFactory.TitleFont != null) _titleText.font = UiFactory.TitleFont;
            _subtitle = FindDeep(go.transform, "Subtitle")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (_subtitle != null && ProgressStore.DevMode) _subtitle.text = "개발자 모드 ON — 전체 스테이지 잠금 해제";

            BindButton(go, "AdventureBtn", StartAdventure);
            BindButton(go, "StartBtn", ShowStageSelect);
            BindButton(go, "EditorBtn", ShowLevelEditor);
            BindButton(go, "CodexBtn", OpenCodex);
            BindButton(go, "SettingsBtn", ShowSettings);
            BindButton(go, "DevToggle", ToggleDevMode);
            BindButton(go, "WipeBtn", () =>
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                if (_subtitle != null) _subtitle.text = SubtitleDefault;
                RefreshTitleBrokenness();
            });

            RefreshTitleBrokenness();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static void BindButton(GameObject root, string name, System.Action onClick)
        {
            var t = FindDeep(root.transform, name);
            var btn = t != null ? t.GetComponent<Button>() : null;
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick());
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
            BrokenTextFx.Ensure(_titleText); // 파편 연출 (M4-3: D3 강)
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
            BgmPlayer.Instance?.Play("bgm_intro"); // 오프닝 전용 곡 (M4-2) — 미도착 시 현재 곡 유지
            var overlay = UiFactory.CreatePanel(_canvas.transform, "Intro", new Color(0.08f, 0.07f, 0.06f, 0.97f));
            UiFactory.Stretch(overlay);

            // 대마왕 일러스트 (1~2페이지 등장 연출, 마지막 페이지에서 숨김)
            Image villain = null;
            var villainSprite = Resources.Load<Sprite>("Art/Portraits/boss_daemawang");
            if (villainSprite != null)
            {
                var vGo = new GameObject("Villain", typeof(Image));
                vGo.transform.SetParent(overlay, false);
                villain = vGo.GetComponent<Image>();
                villain.sprite = villainSprite;
                villain.preserveAspect = true;
                UiFactory.SetRect((RectTransform)vGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(320, 470));
            }

            var text = UiFactory.CreateText(overlay, "Text", IntroPages[0], 30, new Color(0.92f, 0.89f, 0.82f));
            UiFactory.SetRect(text.rectTransform, new Vector2(0.5f, villain != null ? 0.32f : 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 240));

            int page = 0;
            var btn = UiFactory.CreateButton(overlay, "Next", "다음", 24, UiFactory.Accent, Color.white, null);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, villain != null ? 0.12f : 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 60));
            btn.onClick.AddListener(() =>
            {
                page++;
                if (page < IntroPages.Length)
                {
                    text.text = IntroPages[page];
                    if (page == IntroPages.Length - 1)
                    {
                        btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "출발";
                        if (villain != null)
                        {
                            // 마지막 페이지(선비의 출발)에서는 대마왕을 치우고 본문을 중앙으로
                            villain.gameObject.SetActive(false);
                            UiFactory.SetRect(text.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 240));
                        }
                    }
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
