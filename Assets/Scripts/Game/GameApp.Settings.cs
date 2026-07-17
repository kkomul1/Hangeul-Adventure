using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>GameApp의 설정 팝업: 배경음악/효과음 볼륨, 포커스 토글, 타이틀로·종료, 진행 초기화, 사전 출처 표기.</summary>
    public partial class GameApp
    {
        /// <summary>진행 초기화 확인 문구 — 사용자가 이 문자열을 그대로 타이핑해야 실행된다.</summary>
        private const string ResetConfirmPhrase = "모든 진행사항을 초기화합니다";

        private const string FocusAfterSplitKey = "focus_after_split";

        private void ShowSettings()
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "SettingsPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 480));

            var title = UiFactory.CreateText(box, "T", "설정", 34, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(300, 50));

            MakeVolumeRow(box, "배경음악", 0.77f,
                BgmPlayer.Instance != null ? BgmPlayer.Instance.Volume : 0.6f,
                v => { if (BgmPlayer.Instance != null) BgmPlayer.Instance.Volume = v; });
            MakeVolumeRow(box, "효과음", 0.66f,
                SfxPlayer.Instance != null ? SfxPlayer.Instance.Volume : 0.5f,
                v => { if (SfxPlayer.Instance != null) SfxPlayer.Instance.Volume = v; });

            MakeToggleRow(box, "분해 후 결과 글자로 포커스", 0.54f, FocusAfterSplitKey);

            var toTitle = UiFactory.CreateButton(box, "ToTitleBtn", "타이틀로", 20, UiFactory.Paper, UiFactory.Ink, () =>
            {
                PlayerPrefs.Save();
                Destroy(overlay.gameObject); // overlay는 _canvas 직속이라 DestroyGame()으로 사라지지 않는다
                ShowTitle();
            });
            UiFactory.SetRect((RectTransform)toTitle.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-105, 150), new Vector2(180, 50));

            var quit = UiFactory.CreateButton(box, "QuitBtn", "바탕화면으로 종료", 20, UiFactory.Paper, UiFactory.Ink, () =>
            {
                PlayerPrefs.Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // Application.Quit()은 에디터에서 무동작
#else
                Application.Quit();
#endif
            });
            UiFactory.SetRect((RectTransform)quit.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(105, 150), new Vector2(210, 50));

            // 표준국어대사전 뜻풀이 사용 고지 (사용자 방침: 그대로 사용 + 출처 표기)
            var credit = UiFactory.CreateText(box, "Credit", "사전 뜻풀이는 표준국어대사전을 사용했습니다", 16, UiFactory.Dim);
            UiFactory.SetRect(credit.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 96), new Vector2(520, 30));

            var close = UiFactory.CreateButton(box, "Close", "닫기", 24, UiFactory.Accent, Color.white, () =>
            {
                PlayerPrefs.Save();
                Destroy(overlay.gameObject);
            });
            UiFactory.SetRect((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(160, 54));

            // 진행 초기화 — 타이틀에서 이곳으로 이동 (M4-1 승인안)
            var wipe = UiFactory.CreateButton(box, "WipeBtn", "진행 초기화", 15, UiFactory.Paper, UiFactory.Dim, ShowWipeConfirm);
            UiFactory.SetRect((RectTransform)wipe.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 28), new Vector2(126, 44));
        }

        /// <summary>진행 초기화 2중 확인: 확인 문구를 그대로 타이핑해야 실행 버튼이 열린다.</summary>
        private void ShowWipeConfirm()
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "WipeConfirmPopup", new Color(0, 0, 0, 0.75f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 400));

            // 이 팝업의 글자에는 BrokenTextFx를 걸지 않는다 — 깨진 문구는 따라 칠 수 없다
            var title = UiFactory.CreateText(box, "T", "진행 초기화", 32, UiFactory.Accent);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(520, 50));

            var warn = UiFactory.CreateText(box, "Warn",
                "별·루비·골드·글자 도감·되찾은 자음·아이템·볼륨 설정이\n모두 사라집니다. 되돌릴 수 없습니다.", 20, UiFactory.Ink);
            UiFactory.SetRect(warn.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(540, 64));

            var ask = UiFactory.CreateText(box, "Ask",
                $"계속하려면 아래 문구를 그대로 입력하세요\n\"{ResetConfirmPhrase}\"", 18, UiFactory.Dim);
            UiFactory.SetRect(ask.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -172), new Vector2(540, 60));

            var input = UiFactory.CreateInput(box, "ConfirmInput", "여기에 입력", 20);
            UiFactory.SetRect((RectTransform)input.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -244), new Vector2(460, 48));

            var confirm = UiFactory.CreateButton(box, "Confirm", "초기화", 22, UiFactory.Accent, Color.white, () =>
            {
                if (input.text.Trim() != ResetConfirmPhrase) return;
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                if (_subtitle != null) _subtitle.text = SubtitleDefault;
                RefreshTitleBrokenness(); // 인트로 전 상태로 돌아가므로 온전한 타이틀 복원
                Destroy(overlay.gameObject);
            });
            UiFactory.SetRect((RectTransform)confirm.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-100, 30), new Vector2(180, 54));
            confirm.interactable = false;

            var cancel = UiFactory.CreateButton(box, "Cancel", "취소", 22, UiFactory.Paper, UiFactory.Ink, () => Destroy(overlay.gameObject));
            UiFactory.SetRect((RectTransform)cancel.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(100, 30), new Vector2(180, 54));

            input.onValueChanged.AddListener(v => confirm.interactable = v.Trim() == ResetConfirmPhrase);
        }

        /// <summary>PlayerPrefs 플래그(기본 켜짐) 토글 행. 상태는 버튼 라벨·배경색으로 표시.</summary>
        private void MakeToggleRow(RectTransform box, string label, float anchorY, string prefsKey)
        {
            var text = UiFactory.CreateText(box, $"L_{label}", label, 20, UiFactory.Ink, TMPro.TextAlignmentOptions.Left);
            UiFactory.SetRect(text.rectTransform, new Vector2(0, anchorY), new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(320, 40));

            bool on = PlayerPrefs.GetInt(prefsKey, 1) == 1;
            var btn = UiFactory.CreateButton(box, $"T_{label}", "", 20, UiFactory.Paper, UiFactory.Ink, null);
            UiFactory.SetRect((RectTransform)btn.transform, new Vector2(1, anchorY), new Vector2(1, 0.5f), new Vector2(-48, 0), new Vector2(100, 40));

            var img = btn.GetComponent<Image>();
            var value = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            System.Action<bool> paint = v =>
            {
                value.text = v ? "켜짐" : "꺼짐";
                value.color = v ? Color.white : UiFactory.Dim;
                img.color = v ? UiFactory.Accent : new Color(0.85f, 0.82f, 0.76f);
            };
            paint(on);

            btn.onClick.AddListener(() =>
            {
                bool next = PlayerPrefs.GetInt(prefsKey, 1) == 0;
                PlayerPrefs.SetInt(prefsKey, next ? 1 : 0);
                PlayerPrefs.Save();
                paint(next);
            });
        }

        private void MakeVolumeRow(RectTransform box, string label, float anchorY, float initial, System.Action<float> onChange)
        {
            var text = UiFactory.CreateText(box, $"L_{label}", label, 22, UiFactory.Ink, TMPro.TextAlignmentOptions.Left);
            UiFactory.SetRect(text.rectTransform, new Vector2(0, anchorY), new Vector2(0, 0.5f), new Vector2(48, 0), new Vector2(140, 40));

            MakeSlider(box, $"S_{label}", anchorY, initial, onChange);
        }

        /// <summary>절차 생성 슬라이더 (UiFactory 스프라이트 재사용).</summary>
        private Slider MakeSlider(RectTransform parent, string name, float anchorY, float initial, System.Action<float> onChange)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.62f, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(260, 26);

            var bg = new GameObject("Bg", typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = UiFactory.RoundedSprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.85f, 0.82f, 0.76f);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0, 0.28f);
            bgRt.anchorMax = new Vector2(1, 0.72f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRt = (RectTransform)fillArea.transform;
            faRt.anchorMin = new Vector2(0, 0.28f);
            faRt.anchorMax = new Vector2(1, 0.72f);
            faRt.offsetMin = faRt.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.GetComponent<Image>();
            fillImg.sprite = UiFactory.RoundedSprite();
            fillImg.type = Image.Type.Sliced;
            fillImg.color = UiFactory.Accent;
            var fillRt = (RectTransform)fill.transform;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRt = (RectTransform)handleArea.transform;
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = haRt.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hImg = handle.GetComponent<Image>();
            hImg.sprite = UiFactory.RoundedSprite();
            hImg.type = Image.Type.Sliced;
            hImg.color = UiFactory.Ink;
            var hRt = (RectTransform)handle.transform;
            hRt.sizeDelta = new Vector2(18, 4);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = hImg;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initial;
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }
    }
}
