using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 깨진 글자 파편 연출 (M4-3, 승인 보드 결정: "D3 파편 강").
    /// BrokenText.Apply가 &lt;link="brk"&gt;로 표시한 글자를 찾아 원본 글리프를 숨기고,
    /// SDF 아틀라스에서 추출한 글리프 비트맵을 네 조각으로 어긋내고 절반을 모자이크한
    /// 스프라이트를 그 자리에 겹쳐 그린다. UI(TextMeshProUGUI)와 월드(TextMeshPro) 모두 지원.
    /// </summary>
    public class BrokenTextFx : MonoBehaviour
    {
        private TMP_Text _tmp;
        private string _lastText;
        private readonly List<GameObject> _overlays = new List<GameObject>();

        /// <summary>대상 TMP에 연출 컴포넌트를 1회 부착.</summary>
        public static void Ensure(TMP_Text text)
        {
            if (text != null && text.GetComponent<BrokenTextFx>() == null)
                text.gameObject.AddComponent<BrokenTextFx>();
        }

        private void Awake() => _tmp = GetComponent<TMP_Text>();

        private void LateUpdate()
        {
            if (_tmp == null) return;
            if (_tmp.text == _lastText) return;
            _lastText = _tmp.text;
            Rebuild();
        }

        private void OnDisable() => ClearOverlays();

        private void ClearOverlays()
        {
            foreach (var go in _overlays)
                if (go != null) Destroy(go);
            _overlays.Clear();
            _lastText = null; // 재활성화 시 다시 그리도록
        }

        private void Rebuild()
        {
            ClearOverlays();
            _lastText = _tmp.text;
            if (string.IsNullOrEmpty(_tmp.text) || !_tmp.text.Contains("\"brk\"")) return;

            // 파편이 옆 글자와 겹치지 않도록 자간 확보 (확인 카드 피드백)
            if (_tmp.characterSpacing < 6f) _tmp.characterSpacing = 6f;

            _tmp.ForceMeshUpdate();
            var info = _tmp.textInfo;

            for (int li = 0; li < info.linkCount; li++)
            {
                var link = info.linkInfo[li];
                if (link.GetLinkID() != "brk") continue;

                for (int ci = link.linkTextfirstCharacterIndex;
                     ci < link.linkTextfirstCharacterIndex + link.linkTextLength; ci++)
                {
                    var ch = info.characterInfo[ci];
                    if (!ch.isVisible) continue;

                    var sprite = BrokenGlyphs.GetSprite(ch.character, _tmp.font);
                    if (sprite == null) continue; // 추출 실패 시 원형 유지

                    HideGlyph(info, ci);
                    PlaceOverlay(ch, sprite);
                }
            }
            // 숨김(정점 색) 반영
            _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private static void HideGlyph(TMP_TextInfo info, int charIndex)
        {
            var ch = info.characterInfo[charIndex];
            var colors = info.meshInfo[ch.materialReferenceIndex].colors32;
            for (int i = 0; i < 4; i++)
            {
                var c = colors[ch.vertexIndex + i];
                c.a = 0;
                colors[ch.vertexIndex + i] = c;
            }
        }

        private void PlaceOverlay(TMP_CharacterInfo ch, Sprite sprite)
        {
            Vector2 bl = ch.bottomLeft, tr = ch.topRight;
            var centerLocal = new Vector3((bl.x + tr.x) * 0.5f, (bl.y + tr.y) * 0.5f, 0);
            float w = tr.x - bl.x, h = tr.y - bl.y;
            // 파편이 어긋난 만큼 여유 (스프라이트 캔버스가 글리프보다 큼)
            float pad = 1f + 2f * BrokenGlyphs.OffsetRatio;
            Color tint = (Color)ch.color;

            var go = new GameObject("BrokenFx");
            _overlays.Add(go);

            if (_tmp is TextMeshProUGUI)
            {
                go.transform.SetParent(transform, false);
                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.color = tint;
                img.raycastTarget = false;
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(w * pad, h * pad);
                go.transform.position = transform.TransformPoint(centerLocal);
            }
            else
            {
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tint;
                var meshRenderer = GetComponent<MeshRenderer>();
                sr.sortingOrder = meshRenderer != null ? meshRenderer.sortingOrder + 1 : 10;
                go.transform.localPosition = centerLocal;
                // 스프라이트 크기(월드) = tex/PPU → 글리프 쿼드 크기에 맞춰 스케일
                float spriteH = sprite.rect.height / sprite.pixelsPerUnit;
                float scale = spriteH > 0 ? h * pad / spriteH : 1f;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    /// <summary>
    /// 깨진 글리프 스프라이트 팩토리: TMP SDF 아틀라스에서 글리프 커버리지를 추출해
    /// "D3 파편 강" 효과(4조각 어긋남 + 대각 2조각 모자이크 + 알파 74%)를 구운 스프라이트를 캐시.
    /// </summary>
    public static class BrokenGlyphs
    {
        /// <summary>글리프 높이 대비 조각 어긋남 비율 (2026-07-17 확인 카드 피드백: "더 깨지게" 상향).</summary>
        public const float OffsetRatio = 16f / 110f;
        private const float MosaicRatio = 14f / 110f; // 모자이크 블록 크기 비율
        private const float Alpha = 0.62f;
        private const float LostQuadAlpha = 0.22f;    // 소실 조각(글자당 1개)의 잔상 알파

        private static readonly Dictionary<TMP_FontAsset, Dictionary<char, Sprite>> _cache
            = new Dictionary<TMP_FontAsset, Dictionary<char, Sprite>>();
        private static bool _warned;

        public static Sprite GetSprite(char c, TMP_FontAsset font)
        {
            if (font == null) return null;
            if (!_cache.TryGetValue(font, out var perFont))
                _cache[font] = perFont = new Dictionary<char, Sprite>();
            if (perFont.TryGetValue(c, out var cached)) return cached;

            Sprite sprite = null;
            try { sprite = Build(c, font); }
            catch (System.Exception e)
            {
                if (!_warned) { Debug.LogWarning($"깨진 글자 추출 실패({c}): {e.Message}"); _warned = true; }
            }
            perFont[c] = sprite; // 실패도 캐시 (반복 시도 방지)
            return sprite;
        }

        private static Sprite Build(char c, TMP_FontAsset font)
        {
            // 동적 아틀라스에 글리프 보장 (렌더 후 호출되므로 보통 이미 존재)
            if (!font.HasCharacter(c) && !font.TryAddCharacters(c.ToString())) return null;
            if (!font.characterLookupTable.TryGetValue(c, out var tmpChar)) return null;

            var glyph = tmpChar.glyph;
            var gr = glyph.glyphRect;
            if (gr.width <= 0 || gr.height <= 0) return null;
            int atlasIndex = glyph.atlasIndex;
            if (font.atlasTextures == null || atlasIndex >= font.atlasTextures.Length) return null;
            var atlas = font.atlasTextures[atlasIndex];

            // SDF 커버리지 추출 (0.5 경계) — 동적 아틀라스는 CPU 읽기 가능
            var src = atlas.GetPixels(gr.x, gr.y, gr.width, gr.height);
            int gw = gr.width, gh = gr.height;
            bool[] mask = new bool[gw * gh];
            for (int i = 0; i < src.Length; i++) mask[i] = src[i].a >= 0.5f;

            int off = Mathf.Max(2, Mathf.RoundToInt(gh * OffsetRatio));
            int block = Mathf.Max(2, Mathf.RoundToInt(gh * MosaicRatio));
            int w = gw + off * 2, h = gh + off * 2;
            var pixels = new Color32[w * h];

            var rng = new System.Random(c); // 글자별 고정 시드 (프레임 간 안정)
            int lostQuad = rng.Next(4);     // 글자당 조각 하나는 거의 소실 ("더 깨지게" 피드백)

            for (int qx = 0; qx < 2; qx++)
            {
                for (int qy = 0; qy < 2; qy++)
                {
                    int sx0 = qx * gw / 2, sy0 = qy * gh / 2;
                    int sx1 = (qx + 1) * gw / 2, sy1 = (qy + 1) * gh / 2;
                    int dx = off + rng.Next(-off, off + 1);
                    int dy = off + rng.Next(-off, off + 1);
                    bool mosaic = (qx + qy) % 2 == 0;
                    bool lost = (qx * 2 + qy) == lostQuad;
                    byte a = (byte)((lost ? LostQuadAlpha : Alpha) * 255);
                    if (lost) mosaic = true; // 소실 조각은 항상 뭉개짐

                    for (int y = sy0; y < sy1; y++)
                    {
                        for (int x = sx0; x < sx1; x++)
                        {
                            bool on = mosaic
                                ? mask[Mathf.Min(sy0 + (y - sy0) / block * block + block / 2, sy1 - 1) * gw
                                       + Mathf.Min(sx0 + (x - sx0) / block * block + block / 2, sx1 - 1)]
                                : mask[y * gw + x];
                            if (!on) continue;
                            int px = x - sx0 + sx0 + dx, py = y - sy0 + sy0 + dy;
                            if (px < 0 || px >= w || py < 0 || py >= h) continue;
                            pixels[py * w + px] = new Color32(255, 255, 255, a);
                        }
                    }
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            tex.filterMode = FilterMode.Point; // 모자이크 각을 살림
            tex.hideFlags = HideFlags.DontSave;

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
