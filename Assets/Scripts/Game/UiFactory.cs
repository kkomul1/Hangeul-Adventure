using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>프로그램 방식 uGUI 생성 헬퍼. 씬 배선 없이 전부 코드로 만든다.</summary>
    public static class UiFactory
    {
        public static TMP_FontAsset KoreanFont { get; set; }

        public static readonly Color Ink = new Color(0.16f, 0.14f, 0.12f);
        public static readonly Color Paper = new Color(0.96f, 0.94f, 0.89f);
        public static readonly Color Accent = new Color(0.85f, 0.33f, 0.16f); // 주황
        public static readonly Color Dim = new Color(0.55f, 0.52f, 0.47f);

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        public static RectTransform CreateEmpty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (KoreanFont != null) tmp.font = KoreanFont;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            float fontSize, Color bg, Color fg, System.Action onClick)
        {
            var rect = CreatePanel(parent, name, bg);
            var btn = rect.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.92f, 0.90f, 0.85f);
            colors.pressedColor = new Color(0.88f, 0.85f, 0.78f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var text = CreateText(rect, "Label", label, fontSize, fg);
            Stretch(text.rectTransform);
            return btn;
        }

        public static void Stretch(RectTransform rt, float margin = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        public static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static TMP_InputField CreateInput(Transform parent, string name, string placeholder, float fontSize)
        {
            var rect = CreatePanel(parent, name, Color.white);
            var input = rect.gameObject.AddComponent<TMP_InputField>();

            var area = CreateEmpty(rect, "TextArea");
            Stretch(area, 8);
            area.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            var ph = CreateText(area, "Placeholder", placeholder, fontSize, Dim, TextAlignmentOptions.Left);
            Stretch(ph.rectTransform);
            var txt = CreateText(area, "Text", "", fontSize, Ink, TextAlignmentOptions.Left);
            Stretch(txt.rectTransform);

            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            return input;
        }

        // ---- 절차 생성 스프라이트 ----

        private static Sprite _roundedSprite;

        /// <summary>9-slice 둥근 사각형 스프라이트 (타일/패널 공용, 런타임 생성).</summary>
        public static Sprite RoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;

            const int size = 64, radius = 14;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    int cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    int cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist > radius) a = 0f;
                    else if (dist > radius - 1.5f) a = (radius - dist) / 1.5f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;

            _roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 64, extrude: 0, meshType: SpriteMeshType.FullRect,
                border: new Vector4(20, 20, 20, 20));
            _roundedSprite.hideFlags = HideFlags.DontSave;
            return _roundedSprite;
        }
    }
}
