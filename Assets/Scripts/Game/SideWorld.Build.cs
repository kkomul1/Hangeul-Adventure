using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// SideWorld의 구축 부분: 지형 콜라이더(연속 구간 병합), 배경 레이어, 스팟, 플레이어, HUD.
    /// 아트 폴백: 사이드뷰 아트 도착 전이므로 색 블록(UiFactory.RoundedSprite)이 기본 —
    /// 기존 MapWorld 폴백 패턴과 동일하고, 아트는 기획 9장 인터페이스만 지키면 병행 가능.
    /// </summary>
    public partial class SideWorld
    {
        // 정렬 순서 (기획 9장): 배경 -10 / 지형 0 / 스팟 2 / 라벨 3 / 플레이어 5 / 전경 8
        private const int OrderBackdrop = -10;
        private const int OrderTerrain = 0;
        private const int OrderSpot = 2;
        private const int OrderLabel = 3;
        private const int OrderPlayer = 5;

        private static PhysicsMaterial2D _noFriction;
        private static PhysicsMaterial2D NoFriction
        {
            get
            {
                if (_noFriction == null)
                    _noFriction = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
                return _noFriction;
            }
        }

        // ---- 지형 ----

        private void BuildTerrain()
        {
            // 솔리드 '#': 행 런 → 동일 x범위 세로 병합. 칸당 콜라이더 1개 금지 — 이음새 걸림 방지 (지시 사항)
            foreach (var r in MergeSolidRects())
            {
                var go = new GameObject($"Solid_{r.x}_{r.y}", typeof(BoxCollider2D), typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(r.x + (r.width - 1) * 0.5f, r.y + (r.height - 1) * 0.5f, 0);
                go.GetComponent<BoxCollider2D>().size = new Vector2(r.width, r.height);

                var sr = go.GetComponent<SpriteRenderer>();
                sr.sprite = UiFactory.RoundedSprite();
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(r.width + 0.04f, r.height + 0.04f);
                sr.color = SolidColor;
                sr.sortingOrder = OrderTerrain;
            }

            // 원웨이 발판 '='(+사다리 꼭대기 'H'): 행 단위 연속 구간 → PlatformEffector2D 원웨이 (14장-4)
            for (int y = 0; y < _map.height; y++)
            {
                int x = 0;
                while (x < _map.width)
                {
                    if (!_map.IsOneWay(x, y)) { x++; continue; }
                    int x0 = x;
                    while (x < _map.width && _map.IsOneWay(x, y)) x++;
                    int w = x - x0;

                    var go = new GameObject($"OneWay_{x0}_{y}", typeof(BoxCollider2D), typeof(PlatformEffector2D));
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x0 + (w - 1) * 0.5f, y, 0);

                    var col = go.GetComponent<BoxCollider2D>();
                    col.size = new Vector2(w, 0.5f);
                    col.offset = new Vector2(0, 0.25f); // 상면 = 칸 윗변 (기획 9장: 아트와 무관하게 불변)
                    col.usedByEffector = true;

                    var eff = go.GetComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 170f; // 상면 근처에서만 충돌 — 측면 걸림 방지

                    var visual = new GameObject("Visual", typeof(SpriteRenderer));
                    visual.transform.SetParent(go.transform, false);
                    visual.transform.localPosition = new Vector3(0, 0.3f, 0);
                    var sr = visual.GetComponent<SpriteRenderer>();
                    sr.sprite = UiFactory.RoundedSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(w + 0.04f, 0.42f);
                    sr.color = PlatformColor;
                    sr.sortingOrder = OrderTerrain;

                    _oneWayCols.Add(col);
                    _oneWaySet.Add(col);
                }
            }

            // 사다리 'H': 열 단위 연속 구간. 존 판정은 그리드 질의(Input.cs)라 콜라이더 불필요 — 비주얼만
            for (int x = 0; x < _map.width; x++)
            {
                int y = 0;
                while (y < _map.height)
                {
                    if (!_map.IsLadder(x, y)) { y++; continue; }
                    int y0 = y;
                    while (y < _map.height && _map.IsLadder(x, y)) y++;
                    int h = y - y0;

                    var go = new GameObject($"Ladder_{x}_{y0}", typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x, y0 + (h - 1) * 0.5f, 0);
                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sprite = UiFactory.RoundedSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = new Vector2(0.5f, h + 0.9f); // 위아래로 살짝 여유 — 바닥·발판에 닿아 보이게
                    sr.color = LadderColor;
                    sr.sortingOrder = OrderTerrain + 1; // 지형보다 앞, 스팟보다 뒤

                    // 가로대(발판 줄) 몇 개로 사다리임을 표시
                    for (int i = 0; i < h; i++)
                    {
                        var rung = new GameObject("Rung", typeof(SpriteRenderer));
                        rung.transform.SetParent(go.transform, false);
                        rung.transform.localPosition = new Vector3(0, i - (h - 1) * 0.5f, 0);
                        var rs = rung.GetComponent<SpriteRenderer>();
                        rs.sprite = UiFactory.RoundedSprite();
                        rs.drawMode = SpriteDrawMode.Sliced;
                        rs.size = new Vector2(0.66f, 0.12f);
                        rs.color = new Color(LadderColor.r * 0.75f, LadderColor.g * 0.75f, LadderColor.b * 0.75f);
                        rs.sortingOrder = OrderTerrain + 1;
                    }
                }
            }
        }

        /// <summary>솔리드 칸을 큰 사각형으로 병합: 행별 런 → 동일 x범위가 이어지는 행끼리 세로 확장.</summary>
        private List<RectInt> MergeSolidRects()
        {
            var rects = new List<RectInt>();
            var open = new Dictionary<(int x, int w), RectInt>(); // 직전 행에서 세로 확장 중인 사각형
            for (int y = 0; y < _map.height; y++)
            {
                var next = new Dictionary<(int, int), RectInt>();
                int x = 0;
                while (x < _map.width)
                {
                    if (!_map.IsSolid(x, y)) { x++; continue; }
                    int x0 = x;
                    while (x < _map.width && _map.IsSolid(x, y)) x++;
                    int w = x - x0;
                    if (open.TryGetValue((x0, w), out var r))
                    {
                        r.height += 1;
                        next[(x0, w)] = r;
                        open.Remove((x0, w));
                    }
                    else
                        next[(x0, w)] = new RectInt(x0, y, w, 1);
                }
                foreach (var r in open.Values) rects.Add(r);
                open = next;
            }
            foreach (var r in open.Values) rects.Add(r);
            return rects;
        }

        // ---- 배경 레이어 (패럴랙스, 기획 14장-5) ----

        private void BuildBackdrops()
        {
            foreach (var bd in _map.backdrops)
            {
                var root = new GameObject($"Backdrop_x{bd.parallax:0.##}").transform;
                root.SetParent(transform, false);
                _backdropRoots.Add((root, bd.parallax));

                // 행 단위 런을 연한 색 블록으로 (충돌 없음)
                for (int y = 0; y < bd.height; y++)
                {
                    int x = 0;
                    while (x < bd.width)
                    {
                        if (bd.Tile(x, y) == '.') { x++; continue; }
                        int x0 = x;
                        while (x < bd.width && bd.Tile(x, y) != '.') x++;
                        int w = x - x0;

                        var go = new GameObject($"Bd_{x0}_{y}", typeof(SpriteRenderer));
                        go.transform.SetParent(root, false);
                        go.transform.localPosition = new Vector3(x0 + (w - 1) * 0.5f, y, 0);
                        var sr = go.GetComponent<SpriteRenderer>();
                        sr.sprite = UiFactory.RoundedSprite();
                        sr.drawMode = SpriteDrawMode.Sliced;
                        sr.size = new Vector2(w + 0.04f, 1.04f);
                        sr.color = BackdropSolid;
                        sr.sortingOrder = OrderBackdrop;
                    }
                }
            }
        }

        // ---- 스팟 (MapWorld와 동일 패턴 — 상태색·라벨·▒는 RefreshStates가 담당) ----

        private void BuildSpots()
        {
            foreach (var (stageId, pos) in _map.spots)
                _spotViews[stageId] = MakeSpot(pos, LabelFor(stageId), stageId: stageId);

            for (int i = 0; i < _map.exits.Count; i++)
                _exitViews.Add(MakeSpot(_map.exits[i].pos, "→", exitIndex: i));

            if (_map.shop.HasValue)
                _shopView = MakeSpot(_map.shop.Value, BrokenText.Apply("상점"), isShop: true);

            if (_map.bossPos.HasValue)
            {
                _bossView = MakeSpot(_map.bossPos.Value, "王", isShop: false);
                _bossView.Bg.size = new Vector2(1.0f, 1.0f);
            }
        }

        private string LabelFor(int stageId)
        {
            var stage = _stageLookup.Find(s => s.id == stageId);
            if (stage != null && stage.goals != null && stage.goals.Length > 0 && !string.IsNullOrEmpty(stage.goals[0].display))
                return stage.goals[0].display.Substring(0, 1);
            return stageId.ToString();
        }

        private SpotView MakeSpot(Vector2Int pos, string label, int stageId = -1, int exitIndex = -1, bool isShop = false)
        {
            var go = new GameObject(isShop ? "Shop" : exitIndex >= 0 ? $"Exit_{exitIndex}" : $"Spot_{stageId}");
            go.transform.SetParent(transform, false);
            // pos = 발 칸. 팻말은 발판 위에 세워진 구조물 (바닥 앵커, 기획 4.1장)
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);

            var bgGo = new GameObject("Bg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.GetComponent<SpriteRenderer>();
            bg.sprite = UiFactory.RoundedSprite();
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = isShop ? new Vector2(1.15f, 0.85f) : new Vector2(0.85f, 0.85f);
            bg.sortingOrder = OrderSpot;

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (UiFactory.KoreanFont != null) tmp.font = UiFactory.KoreanFont;
            tmp.text = label;
            tmp.fontSize = isShop ? 3.2f : 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiFactory.Ink;
            tmp.rectTransform.sizeDelta = new Vector2(1.4f, 1);
            tmp.sortingOrder = OrderLabel;
            BrokenTextFx.Ensure(tmp); // 상점 등 월드 라벨의 깨진 글자 연출

            return new SpotView { Go = go, Bg = bg, Label = tmp, StageId = stageId, ExitIndex = exitIndex, IsShop = isShop, Pos = pos };
        }

        // ---- 플레이어 (Rigidbody2D dynamic, 14장-4 "Unity 물리 우선") ----

        private void BuildPlayer(Vector2 feetPos)
        {
            var go = new GameObject("Player", typeof(Rigidbody2D), typeof(BoxCollider2D));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(feetPos.x, feetPos.y, 0);

            _rb = go.GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 고속 낙하 시 발판 관통 방지
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _normalGravityScale = Gravity / Mathf.Max(0.01f, -Physics2D.gravity.y); // 상수 Gravity(u/s²)를 스케일로 환산
            _rb.gravityScale = _normalGravityScale;

            _bodyCol = go.GetComponent<BoxCollider2D>();
            _bodyCol.size = new Vector2(PlayerWidth, PlayerHeight);
            _bodyCol.offset = new Vector2(0, PlayerHeight * 0.5f); // 기준점 = 발 밑 중앙 (기획 1.1장)
            _bodyCol.sharedMaterial = NoFriction; // 벽에 붙어 낙하 시 마찰로 멈추는 것 방지

            // 색 블록 폴백 (조선 시트는 탑다운 4방향용 — 사이드뷰 시트 도착 전까지 사용 안 함, 기획 9장)
            var body = new GameObject("Body", typeof(SpriteRenderer));
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0, PlayerHeight * 0.5f, 0);
            var bs = body.GetComponent<SpriteRenderer>();
            bs.sprite = UiFactory.RoundedSprite();
            bs.drawMode = SpriteDrawMode.Sliced;
            bs.size = new Vector2(PlayerWidth - 0.02f, PlayerHeight - 0.04f);
            bs.color = new Color(0.25f, 0.35f, 0.55f);
            bs.sortingOrder = OrderPlayer;

            var face = new GameObject("Face", typeof(SpriteRenderer));
            face.transform.SetParent(go.transform, false);
            face.transform.localPosition = new Vector3(0, PlayerHeight - 0.35f, 0);
            var fs = face.GetComponent<SpriteRenderer>();
            fs.sprite = UiFactory.RoundedSprite();
            fs.drawMode = SpriteDrawMode.Sliced;
            fs.size = new Vector2(0.34f, 0.28f);
            fs.color = new Color(0.96f, 0.88f, 0.76f);
            fs.sortingOrder = OrderPlayer + 1;

            _playerT = go.transform;
        }

        // ---- HUD (MapWorld.BuildHud와 동일 패턴) ----

        private void BuildHud()
        {
            _hudRoot = UiFactory.CreateEmpty(_canvas.transform, "SideMapHud");
            UiFactory.Stretch(_hudRoot);

            _hudTitle = UiFactory.CreateText(_hudRoot, "Title", "", 28, UiFactory.Ink, TextAlignmentOptions.Left);
            UiFactory.SetRect(_hudTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -22), new Vector2(600, 44));
            BrokenTextFx.Ensure(_hudTitle); // 깨진 글자 파편 연출 (M4-3)

            _hudProgress = UiFactory.CreateText(_hudRoot, "Progress", "", 20, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(_hudProgress.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -62), new Vector2(700, 34));

            _hudHint = UiFactory.CreateText(_hudRoot, "Hint", "", 19, UiFactory.Ink);
            UiFactory.SetRect(_hudHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(900, 34));
            BrokenTextFx.Ensure(_hudHint); // 출구·장소 이름의 깨진 글자 연출

            var exitBtn = UiFactory.CreateButton(_hudRoot, "LeaveBtn", "나가기", 19, UiFactory.Paper, UiFactory.Ink, () => _app.LeaveMapToTitle());
            UiFactory.SetRect((RectTransform)exitBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(120, 46));

            var bagBtn = UiFactory.CreateButton(_hudRoot, "BagBtn", "가방", 19, UiFactory.Paper, UiFactory.Ink, () => _app.OpenInventory());
            UiFactory.SetRect((RectTransform)bagBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -20), new Vector2(100, 46));

            _hudGold = UiFactory.CreateText(_hudRoot, "Gold", "", 21, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(_hudGold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-266, -26), new Vector2(240, 36));
        }
    }
}
