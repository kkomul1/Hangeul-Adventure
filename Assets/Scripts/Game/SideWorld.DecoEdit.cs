using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
#endif

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 데코 배치 편집기 (개발자 모드 전용). 사이드뷰 맵의 전경 장식(바위·나무·그루터기 등)을
    /// 게임 안에서 드래그로 배치하고 맵 JSON(Assets/Resources/Maps)에 저장한다.
    ///
    /// 아트 배치는 손으로 보면서 하는 게 정확하다는 사용자 요청으로 만든 도구다.
    /// 저장은 에디터에서만 동작한다 — 빌드된 게임의 Resources는 읽기 전용.
    ///
    /// 조작: 팔레트에서 프롭 선택 → 빈 곳 클릭 = 새 데코 배치 / 데코 드래그 = 이동 /
    ///       F = 좌우반전 · Del·우클릭 = 삭제 · [ ] = 뒤/앞 정렬 · G = 그리드 스냅 토글 · Ctrl+S·저장 버튼 = 저장.
    /// </summary>
    public partial class SideWorld
    {
        private bool _decoEditMode;
        private RectTransform _decoEditRoot;      // 편집 UI 루트 (편집 모드에서만 생성)
        private string _decoBrush;                // 팔레트에서 고른 프롭 (null이면 배치 대신 선택/이동)
        private int _decoSelected = -1;           // 현재 선택된 데코 인덱스 (-1=없음)
        private bool _decoDragging;
        private Vector2 _decoDragOffset;          // 마우스↔데코 발치 차이 (드래그 중 튐 방지)
        private bool _decoGridSnap;
        private TextMeshProUGUI _decoStatus;
        private readonly List<Button> _paletteButtons = new List<Button>();

        private const float DecoGrid = 0.5f;      // 스냅 격자 (반 칸)

        /// <summary>개발자 모드에서만 데코 편집을 켤 수 있다. GameApp이 진입 시 호출.</summary>
        public void EnableDecoEditing() => _decoEditingAllowed = true;
        private bool _decoEditingAllowed;

        private void ToggleDecoEdit()
        {
            _decoEditMode = !_decoEditMode;
            if (_decoEditMode) BuildDecoEditUi();
            else
            {
                if (_decoEditRoot != null) Destroy(_decoEditRoot.gameObject);
                _decoEditRoot = null;
                SetDecoSelected(-1);
                _decoBrush = null;
            }
        }

        /// <summary>편집 모드 입력. LateUpdate가 아니라 Update 계열에서 부른다 (Input.cs의 Update 말미).</summary>
        private void DecoEditUpdate()
        {
            if (!_decoEditingAllowed) return;

            // F9 = 편집 모드 토글
            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
                ToggleDecoEdit();

            if (!_decoEditMode) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            Vector2 world = _cam.ScreenToWorldPoint(mouse.position.ReadValue());

            // 좌클릭: 브러시 있으면 새 데코, 없으면 데코 선택/드래그 시작
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverEditUi(mouse.position.ReadValue()))
            {
                int hit = DecoAtWorld(world);
                if (hit >= 0)
                {
                    SetDecoSelected(hit);
                    _decoDragging = true;
                    var d = _map.decorations[hit];
                    _decoDragOffset = new Vector2(d.x, d.y) - world;
                }
                else if (!string.IsNullOrEmpty(_decoBrush))
                {
                    AddDeco(_decoBrush, Snap(world));
                }
            }
            if (mouse.leftButton.wasReleasedThisFrame) _decoDragging = false;

            if (_decoDragging && _decoSelected >= 0)
            {
                var d = _map.decorations[_decoSelected];
                Vector2 p = Snap(world + _decoDragOffset);
                d.x = p.x;
                d.y = p.y;
                ApplyDecoTransform(_decoSelected);
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                int hit = DecoAtWorld(world);
                if (hit >= 0) DeleteDeco(hit);
            }

            if (kb != null && _decoSelected >= 0)
            {
                if (kb.deleteKey.wasPressedThisFrame) DeleteDeco(_decoSelected);
                if (kb.fKey.wasPressedThisFrame) { var d = _map.decorations[_decoSelected]; d.flip = !d.flip; ApplyDecoTransform(_decoSelected); }
                if (kb.leftBracketKey.wasPressedThisFrame) { var d = _map.decorations[_decoSelected]; d.order--; ApplyDecoTransform(_decoSelected); }
                if (kb.rightBracketKey.wasPressedThisFrame) { var d = _map.decorations[_decoSelected]; d.order++; ApplyDecoTransform(_decoSelected); }
            }
            if (kb != null)
            {
                if (kb.gKey.wasPressedThisFrame) { _decoGridSnap = !_decoGridSnap; RefreshDecoStatus(); }
                if ((kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) && kb.sKey.wasPressedThisFrame) SaveDecorations();
            }
        }

        private Vector2 Snap(Vector2 p) => _decoGridSnap
            ? new Vector2(Mathf.Round(p.x / DecoGrid) * DecoGrid, Mathf.Round(p.y / DecoGrid) * DecoGrid)
            : p;

        /// <summary>월드 좌표에 있는 데코를 찾는다 (스프라이트 bounds, 앞쪽=나중 인덱스 우선).</summary>
        private int DecoAtWorld(Vector2 world)
        {
            for (int i = _decoViews.Count - 1; i >= 0; i--)
            {
                var sr = _decoViews[i];
                if (sr == null) continue;
                if (sr.bounds.Contains(new Vector3(world.x, world.y, sr.bounds.center.z))) return i;
            }
            return -1;
        }

        private void AddDeco(string art, Vector2 pos)
        {
            _map.decorations.Add(new DecorationJson { art = art, x = pos.x, y = pos.y });
            RebuildDecorations();
            SetDecoSelected(_map.decorations.Count - 1);
            RefreshDecoStatus();
        }

        private void DeleteDeco(int index)
        {
            if (index < 0 || index >= _map.decorations.Count) return;
            _map.decorations.RemoveAt(index);
            RebuildDecorations();
            SetDecoSelected(-1);
            RefreshDecoStatus();
        }

        /// <summary>한 데코의 위치·반전·정렬만 갱신 (전체 재생성 없이).</summary>
        private void ApplyDecoTransform(int index)
        {
            if (index < 0 || index >= _decoViews.Count) return;
            var sr = _decoViews[index];
            if (sr == null) return;
            var d = _map.decorations[index];
            sr.transform.localPosition = new Vector3(d.x, d.y - DecoSinkY, 0);
            sr.flipX = d.flip;
            sr.sortingOrder = OrderDeco + d.order;
        }

        private void RebuildDecorations()
        {
            foreach (var sr in _decoViews) if (sr != null) Destroy(sr.gameObject);
            BuildDecorations();
            HighlightSelected();
        }

        private void SetDecoSelected(int index)
        {
            _decoSelected = index;
            HighlightSelected();
            RefreshDecoStatus();
        }

        private void HighlightSelected()
        {
            for (int i = 0; i < _decoViews.Count; i++)
            {
                var sr = _decoViews[i];
                if (sr == null) continue;
                sr.color = i == _decoSelected ? new Color(1f, 0.9f, 0.5f) : Color.white;
            }
        }

        // ---- 저장 (에디터 전용) ----

        private void SaveDecorations()
        {
#if UNITY_EDITOR
            string path = $"Assets/Resources/Maps/map_{_map.id}_side.json";
            if (!File.Exists(path)) { SetStatus($"저장 실패: {path} 없음"); return; }

            string src = File.ReadAllText(path);
            string block = BuildDecorationsJson();
            string updated = ReplaceDecorationsField(src, block);
            if (updated == null) { SetStatus("저장 실패: decorations 필드 위치를 못 찾음"); return; }

            File.WriteAllText(path, updated);
            UnityEditor.AssetDatabase.ImportAsset(path);
            SetStatus($"저장됨 → {path} (데코 {_map.decorations.Count}개)");
#else
            SetStatus("저장은 에디터에서만 됩니다 (빌드는 Resources 읽기 전용)");
#endif
        }

        /// <summary>decorations 배열을 JSON 조각으로 직렬화 (원본 포맷 스타일에 맞춘 2스페이스 들여쓰기).</summary>
        private string BuildDecorationsJson()
        {
            var sb = new StringBuilder();
            sb.Append("[\n");
            for (int i = 0; i < _map.decorations.Count; i++)
            {
                var d = _map.decorations[i];
                sb.Append("    { \"art\": \"").Append(d.art).Append("\", \"x\": ").Append(Num(d.x));
                if (Mathf.Abs(d.y - 0.5f) > 0.0001f) sb.Append(", \"y\": ").Append(Num(d.y));
                if (d.flip) sb.Append(", \"flip\": true");
                if (d.order != 0) sb.Append(", \"order\": ").Append(d.order);
                sb.Append(" }");
                if (i < _map.decorations.Count - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append("  ]");
            return sb.ToString();
        }

        private static string Num(float v)
        {
            // 정수면 소수점 없이, 아니면 최대 3자리
            if (Mathf.Approximately(v, Mathf.Round(v))) return Mathf.RoundToInt(v).ToString();
            return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 원본 JSON에서 "decorations": [...] 필드만 새 블록으로 교체한다.
        /// 전체 재직렬화(JsonUtility)를 피하는 이유: terrain 배열 포맷·주석·키 순서를 보존해야 diff가 깔끔하다.
        /// decorations 필드가 없으면 파일 끝 } 앞에 추가한다.
        /// </summary>
        private static string ReplaceDecorationsField(string src, string block)
        {
            int key = src.IndexOf("\"decorations\"", System.StringComparison.Ordinal);
            if (key >= 0)
            {
                int br = src.IndexOf('[', key);
                if (br < 0) return null;
                int depth = 0, end = -1;
                for (int i = br; i < src.Length; i++)
                {
                    if (src[i] == '[') depth++;
                    else if (src[i] == ']') { depth--; if (depth == 0) { end = i; break; } }
                }
                if (end < 0) return null;
                return src.Substring(0, br) + block + src.Substring(end + 1);
            }
            // 필드가 없으면 마지막 } 앞에 삽입
            int lastBrace = src.LastIndexOf('}');
            if (lastBrace < 0) return null;
            string head = src.Substring(0, lastBrace).TrimEnd();
            if (!head.EndsWith(",")) head += ",";
            return head + "\n  \"decorations\": " + block + "\n}\n";
        }

        // ---- 편집 UI ----

        private void BuildDecoEditUi()
        {
            _decoEditRoot = UiFactory.CreateEmpty(_canvas.transform, "DecoEditUi");
            UiFactory.Stretch(_decoEditRoot);

            // 상태 표시 (상단)
            _decoStatus = UiFactory.CreateText(_decoEditRoot, "DecoStatus", "", 20, UiFactory.Ink, TextAlignmentOptions.Left);
            UiFactory.SetRect(_decoStatus.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -140), new Vector2(900, 30));

            // 저장 버튼 (상단 우측)
            var saveBtn = UiFactory.CreateButton(_decoEditRoot, "DecoSave", "저장 (Ctrl+S)", 20, UiFactory.Accent, Color.white, SaveDecorations);
            UiFactory.SetRect((RectTransform)saveBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -140), new Vector2(200, 46));

            // 하단 팔레트 (Props 스프라이트 목록)
            var strip = UiFactory.CreatePanel(_decoEditRoot, "Palette", new Color(0f, 0f, 0f, 0.55f));
            strip.anchorMin = new Vector2(0, 0);
            strip.anchorMax = new Vector2(1, 0);
            strip.pivot = new Vector2(0.5f, 0);
            strip.sizeDelta = new Vector2(0, 108);

            var scroll = UiFactory.CreateEmpty(strip, "Row");
            var hlg = scroll.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            UiFactory.Stretch(scroll);

            _paletteButtons.Clear();
            foreach (var name in ArtLibrary.ForestPropNames())
            {
                var sp = ArtLibrary.ForestProp(name);
                if (sp == null) continue;
                var b = UiFactory.CreateButton(scroll, $"P_{name}", "", 0, Color.white, Color.white, null);
                var le = b.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 84; le.preferredHeight = 84;
                var img = b.GetComponent<Image>();
                img.sprite = sp; img.preserveAspect = true;
                string captured = name;
                b.onClick.AddListener(() => { _decoBrush = captured; HighlightPalette(captured); RefreshDecoStatus(); });
                _paletteButtons.Add(b);
            }

            RefreshDecoStatus();
        }

        private void HighlightPalette(string selected)
        {
            foreach (var b in _paletteButtons)
            {
                var img = b.GetComponent<Image>();
                img.color = b.gameObject.name == $"P_{selected}" ? new Color(1f, 0.9f, 0.5f) : Color.white;
            }
        }

        private void RefreshDecoStatus()
        {
            if (_decoStatus == null) return;
            string brush = string.IsNullOrEmpty(_decoBrush) ? "(없음)" : _decoBrush;
            string sel = _decoSelected >= 0 ? _map.decorations[_decoSelected].art : "(없음)";
            _decoStatus.text = $"데코 편집 | 브러시: {brush} · 선택: {sel} · 스냅: {(_decoGridSnap ? "켬" : "끔")}"
                + "  |  클릭=배치 · 드래그=이동 · F=반전 · Del/우클릭=삭제 · [ ]=뒤/앞 · G=스냅 · F9=편집끝";
        }

        private void SetStatus(string msg) { if (_decoStatus != null) _decoStatus.text = msg; Debug.Log("[DecoEdit] " + msg); }

        /// <summary>편집 UI 위에 마우스가 있는지 (배치 클릭이 UI를 뚫지 않게).</summary>
        private bool IsPointerOverEditUi(Vector2 screenPos)
        {
            // 하단 팔레트(108px)와 상단 띠(y>화면-186)만 막으면 충분
            return screenPos.y < 108 || screenPos.y > Screen.height - 186;
        }
    }
}
