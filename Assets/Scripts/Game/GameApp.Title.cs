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
        private const int SideProtoMapId = 101; // 사이드뷰 프로토맵 (map_101_side.json, "시작의 숲")
        private GameObject _devMapBtn;

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

            CreateDevMapButton(_titlePanel);
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
                if (_devMapBtn != null) _devMapBtn.SetActive(false);
                RefreshTitleBrokenness();
            });

            // 프리팹에 DevMapBtn 노드가 없으므로 배선이 아니라 절차 생성
            CreateDevMapButton(_titlePanel);

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
            if (_devMapBtn != null) _devMapBtn.SetActive(on);
            if (on) SfxPlayer.Instance?.Collect(); else SfxPlayer.Instance?.Split();
        }

        private void CreateDevMapButton(RectTransform parent)
        {
            var btn = UiFactory.CreateButton(parent, "DevMapBtn", "[DEV] 사이드뷰 프로토맵", 16,
                new Color(0.20f, 0.45f, 0.35f, 0.85f), Color.white, EnterSideProtoMap);
            UiFactory.SetRect((RectTransform)btn.transform, Vector2.zero, Vector2.zero,
                new Vector2(24, 24), new Vector2(230, 46));
            _devMapBtn = btn.gameObject;
            _devMapBtn.SetActive(ProgressStore.DevMode);
        }

        private void EnterSideProtoMap()
        {
            _maps ??= MapLoader.LoadAll();
            int index = _maps.FindIndex(m => m.id == SideProtoMapId);
            if (index < 0) { Debug.LogError($"프로토맵 없음: id {SideProtoMapId}"); return; }

            // StartMap이 last_map을 덮어쓴다 — 개발용 진입이 정식 진행 상태를 오염시키지 않게 복원
            bool hadLast = PlayerPrefs.HasKey("last_map");
            int lastMap = PlayerPrefs.GetInt("last_map", 0);
            StartMap(index, null);
            if (hadLast) PlayerPrefs.SetInt("last_map", lastMap); else PlayerPrefs.DeleteKey("last_map");
            PlayerPrefs.Save();
        }

        /// <summary>깨진 글자 연출 (스토리기획 2장): 오프닝 전에는 온전, 이후엔 미회수 자음 글자가 깨짐.</summary>
        private void RefreshTitleBrokenness()
        {
            if (_titleText == null) return;
            BrokenTextFx.Ensure(_titleText); // 파편 연출 (M4-3: D3 강)
            _titleText.text = IntroSeen ? BrokenText.Apply(TitleString) : TitleString;
        }

        // ---- 오프닝 (4컷 컷신 — 스토리기획 2장 확정본, 묵음 대왕 세계관) ----

        // 컷 = Resources/Art/Opening/opening_01~04.png (1024×503 ≈ 2:1). 캡션은 스토리기획 2장 컷별 내용.
        private static readonly string[] IntroCaptions =
        {
            "어느 날부터, 사람들의 말이 소리가 되지 못했다.\n간판도, 책도, 화면의 글자도 — 하나둘 부서져 사라졌다.",
            "글자가 사라지는 원인은 과거에 있었다.\n나는 훈민정음 해례본을 품에 안고, 1443년으로 향하는 타임머신에 올랐다.",
            "조선의 숲에 불시착한 순간, 충격으로 해례본이 찢겨\n빛나는 파편이 사방으로 흩날렸다. 돌아갈 타임머신은 이미 부서져 있었다.",
            "글자가 쏟아진 하늘을 좇아, 한 사람이 숲에 나와 있었다.\n낯선 옷차림의 나를, 그가 가만히 바라본다.",
        };

        private void ShowIntro(System.Action onDone)
        {
            BgmPlayer.Instance?.Play("bgm_intro"); // 오프닝 전용 곡 (M4-2) — 미도착 시 현재 곡 유지

            // 레터박스 배경(불투명 검정): 2:1 컷을 가로에 맞춰 세로 중앙 배치하면 위아래가 검게 남는다.
            var overlay = UiFactory.CreatePanel(_canvas.transform, "Intro", Color.black);
            UiFactory.Stretch(overlay);

            // 화면 아무 곳이나 눌러도 다음 컷(버튼·Space와 동일). 맨 뒤에 깔고, 위 요소는 raycast를 끈다.
            var clickCatcher = UiFactory.CreateButton(overlay, "ClickCatcher", "", 0, new Color(0, 0, 0, 0), Color.clear, null);
            UiFactory.Stretch((RectTransform)clickCatcher.transform);

            // 컷 뷰(이미지+캡션): 이 그룹을 통째로 페이드해 컷을 전환한다.
            var cutView = UiFactory.CreateEmpty(overlay, "CutView");
            UiFactory.Stretch(cutView);
            var cutGroup = cutView.gameObject.AddComponent<CanvasGroup>();
            cutGroup.blocksRaycasts = false; // 클릭이 뒤의 ClickCatcher로 통과하게

            var cutGo = new GameObject("Cut", typeof(Image), typeof(AspectRatioFitter));
            cutGo.transform.SetParent(cutView, false);
            var img = cutGo.GetComponent<Image>();
            img.raycastTarget = false;
            UiFactory.Stretch((RectTransform)cutGo.transform);
            var fitter = cutGo.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 이미지 전체를 담고 위아래 레터박스

            // 캡션 가독용 하단 암막(전폭) + 내레이션
            var scrim = UiFactory.CreatePanel(cutView, "CaptionScrim", new Color(0, 0, 0, 0.5f));
            scrim.GetComponent<Image>().raycastTarget = false;
            scrim.anchorMin = new Vector2(0, 0);
            scrim.anchorMax = new Vector2(1, 0);
            scrim.pivot = new Vector2(0.5f, 0);
            scrim.sizeDelta = new Vector2(0, 156);
            scrim.anchoredPosition = Vector2.zero;
            var caption = UiFactory.CreateText(cutView, "Caption", IntroCaptions[0], 26, new Color(0.95f, 0.93f, 0.87f));
            UiFactory.SetRect(caption.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 58), new Vector2(1120, 96));

            // 진행/스킵 버튼은 페이드에 포함되지 않게 overlay 직속(항상 위).
            var next = UiFactory.CreateButton(overlay, "Next", "다음", 22, UiFactory.Accent, Color.white, null);
            UiFactory.SetRect((RectTransform)next.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(170, 56));
            var nextLabel = next.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            var skip = UiFactory.CreateButton(overlay, "Skip", "건너뛰기 ▶▶", 16, new Color(0.12f, 0.11f, 0.10f, 0.7f), new Color(0.85f, 0.83f, 0.78f), null);
            UiFactory.SetRect((RectTransform)skip.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-32, -30), new Vector2(150, 46));

            int cut = 0;
            bool transitioning = false;

            void SetCut()
            {
                var sprite = Resources.Load<Sprite>($"Art/Opening/opening_0{cut + 1}");
                if (sprite != null)
                {
                    img.sprite = sprite;
                    fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
                }
                caption.text = IntroCaptions[cut];
                if (nextLabel != null) nextLabel.text = cut == IntroCaptions.Length - 1 ? "출발" : "다음";
            }

            void Finish()
            {
                // 오프닝을 봤으므로 다음부터 타이틀 글자가 깨진다 (스토리기획 4장). 스킵·완주 양쪽에서 보장.
                PlayerPrefs.SetInt("intro_seen", 1);
                PlayerPrefs.Save();
                RefreshTitleBrokenness();
                Destroy(overlay.gameObject);
                onDone();
            }

            System.Collections.IEnumerator CrossFade()
            {
                transitioning = true;
                yield return FadeGroup(cutGroup, 1f, 0f, 0.22f);
                cut++;
                SetCut();
                if (cut == 1) SfxPlayer.Instance?.Split(); // 컷1→2: 세상의 글자가 깨지는 순간의 효과음
                yield return FadeGroup(cutGroup, 0f, 1f, 0.22f);
                transitioning = false;
            }

            void Advance()
            {
                if (transitioning) return;
                if (cut >= IntroCaptions.Length - 1) { Finish(); return; }
                StartCoroutine(CrossFade());
            }

            next.onClick.AddListener(Advance);
            clickCatcher.onClick.AddListener(Advance);
            skip.onClick.AddListener(Finish);

            SetCut();
            cutGroup.alpha = 0f;
            StartCoroutine(IntroFadeIn()); // 첫 컷 페이드 인 (전환 잠금 포함)
            StartCoroutine(SpaceToAdvance(overlay, Advance));   // Space로도 진행

            // 초기 페이드인 동안에도 transitioning을 잠근다 — 안 그러면 첫 0.35초에 클릭 시
            // 페이드인과 CrossFade가 겹쳐 cut 증가가 덮이고 컷이 안 넘어간다 (실측 버그)
            System.Collections.IEnumerator IntroFadeIn()
            {
                transitioning = true;
                yield return FadeGroup(cutGroup, 0f, 1f, 0.35f);
                transitioning = false;
            }
        }

        private static System.Collections.IEnumerator FadeGroup(CanvasGroup g, float from, float to, float dur)
        {
            float t = 0f;
            g.alpha = from;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            g.alpha = to;
        }

        private static System.Collections.IEnumerator SpaceToAdvance(RectTransform overlay, System.Action advance)
        {
            while (overlay != null) // Destroy 후에는 Unity의 fake-null로 루프 종료
            {
                if (Input.GetKeyDown(KeyCode.Space)) advance();
                yield return null;
            }
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
