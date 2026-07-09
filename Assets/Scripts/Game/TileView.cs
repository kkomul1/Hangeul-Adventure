using System.Collections;
using HangeulAdventure.Engine;
using TMPro;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>보드 위 타일 하나의 시각 표현 (스프라이트 + 글자 + 애니메이션).</summary>
    public class TileView : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public char Tile { get; private set; }

        private SpriteRenderer _bg;
        private TextMeshPro _label;
        private Coroutine _anim;
        private Vector3 _basePos;
        private static readonly Color ConsonantColor = new Color(0.99f, 0.97f, 0.93f);
        private static readonly Color VowelColor = new Color(0.93f, 0.96f, 0.99f);
        private static readonly Color LetterColor = new Color(1.00f, 0.93f, 0.80f);
        private static readonly Color SelectedOutline = new Color(0.85f, 0.33f, 0.16f);
        private static readonly Color FilterCompose = new Color(0.55f, 0.85f, 0.55f);
        private static readonly Color FilterSplit = new Color(0.60f, 0.75f, 0.98f);

        private SpriteRenderer _outline;

        public static TileView Create(Transform parent, int x, int y, char tile, Vector3 worldPos)
        {
            var go = new GameObject($"Tile_{tile}", typeof(TileView));
            go.transform.SetParent(parent, false);
            var view = go.GetComponent<TileView>();
            view.Build(x, y, tile, worldPos);
            return view;
        }

        private void Build(int x, int y, char tile, Vector3 pos)
        {
            X = x; Y = y; Tile = tile;
            transform.localPosition = pos;
            _basePos = pos;

            // 외곽선 (선택/필터 표시용) — 살짝 큰 뒷면 스프라이트
            var outlineGo = new GameObject("Outline", typeof(SpriteRenderer));
            outlineGo.transform.SetParent(transform, false);
            _outline = outlineGo.GetComponent<SpriteRenderer>();
            _outline.sprite = UiFactory.RoundedSprite();
            _outline.drawMode = SpriteDrawMode.Sliced;
            _outline.size = new Vector2(0.98f, 0.98f);
            _outline.sortingOrder = 1;
            _outline.enabled = false;

            var bgGo = new GameObject("Bg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(transform, false);
            _bg = bgGo.GetComponent<SpriteRenderer>();
            _bg.sprite = UiFactory.RoundedSprite();
            _bg.drawMode = SpriteDrawMode.Sliced;
            _bg.size = new Vector2(0.88f, 0.88f);
            _bg.sortingOrder = 2;

            var textGo = new GameObject("Label", typeof(TextMeshPro));
            textGo.transform.SetParent(transform, false);
            _label = textGo.GetComponent<TextMeshPro>();
            if (UiFactory.KoreanFont != null) _label.font = UiFactory.KoreanFont;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 5.5f;
            _label.color = UiFactory.Ink;
            _label.rectTransform.sizeDelta = new Vector2(1, 1);
            _label.sortingOrder = 3;

            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            SetChar(tile);
        }

        public void SetBoardPosition(int x, int y)
        {
            X = x; Y = y;
        }

        private static readonly Color RockColor = new Color(0.42f, 0.40f, 0.38f);

        public void SetChar(char tile)
        {
            Tile = tile;
            if (tile == Hangul.Rock)
            {
                _label.text = "";
                _bg.color = RockColor;
                return;
            }
            _label.text = tile == Hangul.Empty ? "" : tile.ToString();
            _bg.color = Hangul.IsSyllable(tile) ? LetterColor
                : Hangul.IsVowel(tile) ? VowelColor
                : ConsonantColor;
        }

        public void SetSelected(bool on)
        {
            _outline.enabled = on;
            if (on) _outline.color = SelectedOutline;
        }

        public void SetFilterHighlight(bool composable, bool splittable)
        {
            if (composable) { _outline.enabled = true; _outline.color = FilterCompose; }
            else if (splittable) { _outline.enabled = true; _outline.color = FilterSplit; }
            else _outline.enabled = false;
        }

        public void AnimateMove(Vector3 to, float duration = 0.09f)
        {
            StartAnim(MoveRoutine(to, duration));
            _basePos = to;
        }

        public void AnimateShake()
        {
            StartAnim(ShakeRoutine());
        }

        public void AnimatePop()
        {
            StartAnim(PopRoutine());
        }

        private void StartAnim(IEnumerator routine)
        {
            if (_anim != null) StopCoroutine(_anim);
            transform.localPosition = _basePos;
            transform.localScale = Vector3.one;
            _anim = StartCoroutine(routine);
        }

        private IEnumerator MoveRoutine(Vector3 to, float duration)
        {
            Vector3 from = transform.localPosition;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t / duration));
                yield return null;
            }
            transform.localPosition = to;
        }

        private IEnumerator ShakeRoutine()
        {
            const float dur = 0.18f, amp = 0.07f;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float x = Mathf.Sin(t * 60f) * amp * (1f - t / dur);
                transform.localPosition = _basePos + new Vector3(x, 0, 0);
                yield return null;
            }
            transform.localPosition = _basePos;
        }

        private IEnumerator PopRoutine()
        {
            const float dur = 0.14f;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = 1f + 0.18f * Mathf.Sin(Mathf.PI * (t / dur));
                transform.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
