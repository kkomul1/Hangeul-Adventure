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

        // 지형 구조물(단차·발판·사다리) 편집 상태 — 격자(_map.tiles)를 직접 편집하고 재생성한다
        private List<Vector2Int> _terrainSel;      // 선택된 구조물의 칸들 (null=없음)
        private char _terrainSelType;
        private bool _terrainDragging;
        private Vector2Int _terrainDragStartCell;
        private List<Vector2Int> _terrainDragOrig;
        private SpriteRenderer _terrainHighlight;
        private bool _mapDirty;                     // 지형이 편집됐으면 저장 시 격자도 쓴다

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
                ClearTerrainSel();
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

            // 좌클릭: 데코 우선 → 지형 구조물 → (브러시 있으면) 새 데코
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverEditUi(mouse.position.ReadValue()))
            {
                int hit = DecoAtWorld(world);
                if (hit >= 0)
                {
                    SetDecoSelected(hit);
                    ClearTerrainSel();
                    _decoDragging = true;
                    var d = _map.decorations[hit];
                    _decoDragOffset = new Vector2(d.x, d.y) - world;
                }
                else
                {
                    var tcells = TerrainStructAt(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
                    if (tcells != null)
                    {
                        SelectTerrain(tcells, GetTile(tcells[0].x, tcells[0].y));
                        _terrainDragging = true;
                        _terrainDragStartCell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
                        _terrainDragOrig = new List<Vector2Int>(tcells);
                    }
                    else if (!string.IsNullOrEmpty(_decoBrush))
                    {
                        AddDeco(_decoBrush, Snap(world));
                    }
                }
            }
            if (mouse.leftButton.wasReleasedThisFrame) { _decoDragging = false; _terrainDragging = false; }

            if (_decoDragging && _decoSelected >= 0)
            {
                var d = _map.decorations[_decoSelected];
                Vector2 p = Snap(world + _decoDragOffset);
                d.x = p.x;
                d.y = p.y;
                ApplyDecoTransform(_decoSelected);
            }

            if (_terrainDragging && _terrainSel != null)
            {
                var cur = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
                var delta = cur - _terrainDragStartCell;
                var desired = new List<Vector2Int>(_terrainDragOrig.Count);
                foreach (var c in _terrainDragOrig) desired.Add(c + delta);
                if (!SameCells(desired, _terrainSel)) RelocateTerrain(desired);
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

        // ---- 지형 구조물 편집 (격자 직접 편집 + 재생성) ----

        private char GetTile(int x, int y)
            => (x < 0 || x >= _map.width || y < 0 || y >= _map.height) ? '#' : _map.tiles[y * _map.width + x];

        private void SetTile(int x, int y, char c)
        {
            if (x >= 0 && x < _map.width && y >= 0 && y < _map.height) _map.tiles[y * _map.width + x] = c;
        }

        /// <summary>이동 가능한 지형 칸: 단차·발판·사다리(#/=/H). 지면(y=0)·경계벽(x=0/끝)은 고정.</summary>
        private bool MovableTerrainCell(int x, int y)
        {
            if (y < 1 || x <= 0 || x >= _map.width - 1) return false;
            char c = GetTile(x, y);
            return c == '#' || c == '=' || c == 'H';
        }

        /// <summary>클릭한 칸에서 같은 종류의 지형 구조물을 4방향 플러드필로 찾는다.</summary>
        private List<Vector2Int> TerrainStructAt(int cx, int cy)
        {
            if (!MovableTerrainCell(cx, cy)) return null;
            char type = GetTile(cx, cy);
            var result = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            var start = new Vector2Int(cx, cy);
            stack.Push(start); seen.Add(start);
            while (stack.Count > 0)
            {
                var p = stack.Pop();
                result.Add(p);
                foreach (var d in Neighbors4)
                {
                    var n = new Vector2Int(p.x + d.x, p.y + d.y);
                    if (!seen.Contains(n) && MovableTerrainCell(n.x, n.y) && GetTile(n.x, n.y) == type)
                    { seen.Add(n); stack.Push(n); }
                }
            }
            return result;
        }

        private static bool SameCells(List<Vector2Int> a, List<Vector2Int> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            var set = new HashSet<Vector2Int>(b);
            foreach (var c in a) if (!set.Contains(c)) return false;
            return true;
        }

        private void SelectTerrain(List<Vector2Int> cells, char type)
        {
            _terrainSel = cells;
            _terrainSelType = type;
            SetDecoSelected(-1); // 데코 선택 해제 (하이라이트 정리)
            UpdateTerrainHighlight();
            RefreshDecoStatus();
        }

        private void ClearTerrainSel()
        {
            _terrainSel = null;
            _terrainDragging = false;
            if (_terrainHighlight != null) { Destroy(_terrainHighlight.gameObject); _terrainHighlight = null; }
        }

        /// <summary>구조물을 desired 칸으로 옮긴다. 대상이 전부 빈칸(이동칸)이어야 성공.</summary>
        private bool RelocateTerrain(List<Vector2Int> desired)
        {
            foreach (var c in _terrainSel) SetTile(c.x, c.y, '.'); // 현재 위치를 비운다 (자기 겹침 허용)
            bool ok = true;
            foreach (var c in desired)
            {
                if (c.y < 1 || c.x <= 0 || c.x >= _map.width - 1 || GetTile(c.x, c.y) != '.') { ok = false; break; }
            }
            if (ok)
            {
                foreach (var c in desired) SetTile(c.x, c.y, _terrainSelType);
                _terrainSel = desired;
                _mapDirty = true;
                RebuildTerrain();
                UpdateTerrainHighlight();
                return true;
            }
            foreach (var c in _terrainSel) SetTile(c.x, c.y, _terrainSelType); // 실패 → 원복
            return false;
        }

        /// <summary>지형 오브젝트만 파괴하고 격자에서 다시 짓는다 (데코·배경·스팟·플레이어 유지).</summary>
        private void RebuildTerrain()
        {
            var kill = new List<GameObject>();
            foreach (Transform child in transform)
            {
                string n = child.name;
                if (n.StartsWith("Solid_") || n.StartsWith("Ground") || n.StartsWith("Bush_")
                    || n.StartsWith("Mound_") || n.StartsWith("OneWay_") || n.StartsWith("Plat")
                    || n.StartsWith("Ladder"))
                    kill.Add(child.gameObject);
            }
            // DestroyImmediate: 콜라이더가 한 프레임 겹쳐 플레이어를 밀지 않게 즉시 제거
            foreach (var g in kill) DestroyImmediate(g);
            _oneWayCols.Clear();
            _oneWaySet.Clear();
            BuildTerrain();
        }

        private void UpdateTerrainHighlight()
        {
            if (_terrainSel == null || _terrainSel.Count == 0) { if (_terrainHighlight != null) { Destroy(_terrainHighlight.gameObject); _terrainHighlight = null; } return; }
            int minx = int.MaxValue, miny = int.MaxValue, maxx = int.MinValue, maxy = int.MinValue;
            foreach (var c in _terrainSel)
            {
                if (c.x < minx) minx = c.x; if (c.x > maxx) maxx = c.x;
                if (c.y < miny) miny = c.y; if (c.y > maxy) maxy = c.y;
            }
            if (_terrainHighlight == null)
            {
                var go = new GameObject("TerrainHighlight", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                _terrainHighlight = go.GetComponent<SpriteRenderer>();
                _terrainHighlight.sprite = UiFactory.RoundedSprite();
                _terrainHighlight.drawMode = SpriteDrawMode.Sliced;
                _terrainHighlight.color = new Color(1f, 0.9f, 0.4f, 0.35f);
                _terrainHighlight.sortingOrder = OrderPlayer + 1; // 지형·플레이어 위
            }
            _terrainHighlight.transform.localPosition = new Vector3((minx + maxx) * 0.5f, (miny + maxy) * 0.5f, 0);
            _terrainHighlight.size = new Vector2(maxx - minx + 1f, maxy - miny + 1f);
        }

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
            string updated = ReplaceDecorationsField(src, BuildDecorationsJson());
            if (updated == null) { SetStatus("저장 실패: decorations 필드 위치를 못 찾음"); return; }

            if (_mapDirty) // 지형을 편집했으면 playfield 격자도 쓴다
            {
                updated = ReplacePlayfieldTerrain(updated);
                if (updated == null) { SetStatus("저장 실패: playfield terrain 위치를 못 찾음"); return; }
            }

            File.WriteAllText(path, updated);
            UnityEditor.AssetDatabase.ImportAsset(path);
            SetStatus($"저장됨 → {path} (데코 {_map.decorations.Count}개{(_mapDirty ? " + 지형" : "")})");
            _mapDirty = false;
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

        /// <summary>playfield 레이어의 terrain 배열을 현재 격자로 교체한다 (backdrop terrain은 건드리지 않는다).</summary>
        private string ReplacePlayfieldTerrain(string src)
        {
            int pf = src.IndexOf("\"playfield\"", System.StringComparison.Ordinal);
            if (pf < 0) return null;
            int key = src.IndexOf("\"terrain\"", pf, System.StringComparison.Ordinal);
            if (key < 0) return null;
            int br = src.IndexOf('[', key);
            if (br < 0) return null;
            int depth = 0, end = -1;
            for (int i = br; i < src.Length; i++)
            {
                if (src[i] == '[') depth++;
                else if (src[i] == ']') { depth--; if (depth == 0) { end = i; break; } }
            }
            if (end < 0) return null;

            // 격자 → 행 문자열 (저장 시 y를 뒤집는다: JSON 행 0 = 화면 위 = 격자 y = height-1-row)
            var sb = new StringBuilder();
            sb.Append("[\n");
            for (int row = 0; row < _map.height; row++)
            {
                int y = _map.height - 1 - row;
                sb.Append("        \"");
                for (int x = 0; x < _map.width; x++) sb.Append(_map.tiles[y * _map.width + x]);
                sb.Append('"');
                if (row < _map.height - 1) sb.Append(',');
                sb.Append('\n');
            }
            sb.Append("      ]");
            return src.Substring(0, br) + sb + src.Substring(end + 1);
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
            string sel;
            if (_terrainSel != null)
                sel = _terrainSelType == '#' ? "지형:단차" : _terrainSelType == '=' ? "지형:발판" : "지형:사다리";
            else
                sel = _decoSelected >= 0 ? _map.decorations[_decoSelected].art : "(없음)";
            _decoStatus.text = $"편집 | 브러시: {brush} · 선택: {sel} · 스냅: {(_decoGridSnap ? "켬" : "끔")}"
                + "  |  데코: 클릭배치·드래그이동·F반전·Del삭제·[ ]앞뒤  ·  지형(단차/발판/사다리): 드래그로 칸단위 이동  ·  W/A/S/D·Space 이동 · G스냅 · Ctrl+S저장 · F9끝";
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
