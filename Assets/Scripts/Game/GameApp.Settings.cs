using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>GameApp의 설정 팝업: 배경음악/효과음 볼륨, 사전 출처 표기.</summary>
    public partial class GameApp
    {
        private void ShowSettings()
        {
            var overlay = UiFactory.CreatePanel(_canvas.transform, "SettingsPopup", new Color(0, 0, 0, 0.6f));
            UiFactory.Stretch(overlay);
            var box = UiFactory.CreatePanel(overlay, "Box", UiFactory.Paper);
            UiFactory.SetRect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 360));

            var title = UiFactory.CreateText(box, "T", "설정", 34, UiFactory.Ink);
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(300, 50));

            MakeVolumeRow(box, "배경음악", 0.64f,
                BgmPlayer.Instance != null ? BgmPlayer.Instance.Volume : 0.6f,
                v => { if (BgmPlayer.Instance != null) BgmPlayer.Instance.Volume = v; });
            MakeVolumeRow(box, "효과음", 0.47f,
                SfxPlayer.Instance != null ? SfxPlayer.Instance.Volume : 0.5f,
                v => { if (SfxPlayer.Instance != null) SfxPlayer.Instance.Volume = v; });

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
            var wipe = UiFactory.CreateButton(box, "WipeBtn", "진행 초기화", 15, UiFactory.Paper, UiFactory.Dim, () =>
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                if (_subtitle != null) _subtitle.text = SubtitleDefault;
                RefreshTitleBrokenness(); // 인트로 전 상태로 돌아가므로 온전한 타이틀 복원
            });
            UiFactory.SetRect((RectTransform)wipe.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(26, 28), new Vector2(126, 44));
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
