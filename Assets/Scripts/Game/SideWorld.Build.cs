using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// SideWorld의 구축 부분: 지형 콜라이더(연속 구간 병합), 배경 레이어, 스팟, 데코, 플레이어, HUD.
    ///
    /// 아트(Art/Forest, PPU 64)가 있으면 스프라이트로, 없으면 색 블록으로 짓는다 (_art 스위치).
    /// 좌표·앵커는 승인 합성 Tools/Scripts/forest_compose.py를 그대로 옮긴 것 —
    /// 수치를 고칠 일이 생기면 그쪽 합성도 같이 고쳐 룩을 다시 확인할 것.
    ///
    /// 콜라이더는 아트와 무관하게 그대로다 (기획 9장: 판정면은 아트가 바뀌어도 불변).
    /// </summary>
    public partial class SideWorld
    {
        // 정렬 순서 (기획 9장 확장): 하늘 → 능선 → 안개 → 배경나무 → 흙백필 → 지면 → 지형 → 사다리 → 데코 → 스팟 → 플레이어
        private const int OrderSky = -100;
        private const int OrderRidge = -90;
        private const int OrderFog = -80;
        private const int OrderBackdrop = -10;
        private const int OrderBackfill = -2;
        private const int OrderGround = -1;
        private const int OrderTerrain = 0;
        private const int OrderLadder = 1;
        private const int OrderBush = 2;
        private const int OrderDeco = 2;
        private const int OrderSpot = 3;
        private const int OrderLabel = 4;
        private const int OrderPlayer = 5;

        // ── 승인 합성에서 옮겨온 배치 상수 (단위: world u. 픽셀값은 /64) ──
        private const float SurfaceY = 0.5f;                 // 바닥 칸 윗변 = 지면 표면선
        private const float GroundPitch = 210f / 64f;        // 청크 간격 3.28u — 폭 5u를 1.72u 겹쳐 깐다
        private const float GroundChunkW = 320f / 64f;       // 청크 폭 5u
        private const float GroundOverhang = 4f;             // 지면·백필을 맵 밖까지 연장 — 화면비가 넓어 맵 밖이 보여도 하늘이 새지 않게
        private const float BackfillTopY = SurfaceY - 24f / 64f;  // 표면선 24px 아래부터 흙
        private const float BackfillBottomY = -8f;           // 맵 바닥(-0.5)보다 한참 아래 — 시야가 넓어져도 하늘이 비치지 않게
        private const float MoundLiftY = 4f / 64f;           // 단차 블록을 칸 윗변보다 4px 위에서 시작
        private const float PlatformLiftY = 40f / 64f;       // 발판 아트 상단 = 상면보다 40px 위 (풀·이끼 두께)
        private const float PlatformCapW = 96f / 64f;        // 발판 마구리 폭 1.5u
        private const float PlatformMidW = 160f / 64f;       // 발판 중간 타일 유효 간격 2.5u (192px 캔버스 중 겹침 제외)
        private const float BushOverlapY = 6f / 64f;         // 경계 수풀 세로 겹침
        private const float LadderCapSinkY = 6f / 64f;       // 사다리 갓머리를 상면 6px 아래로 묻는다
        private const float DecoSinkY = 3f / 64f;            // 데코 발치를 지면 3px 아래로 묻어 붕 뜸 방지
        private const float TreeBaseY = SurfaceY - 8f / 64f; // 배경 나무 캔버스 하단
        private const float RidgeTopY = 6.0f;                // 원경 능선 상단
        private const float RidgeParallax = 0.25f;
        private const float FogTopY = 4.2f;                  // 안개 띠 윗변
        private const float SignLabelY = 0.93f;              // 팻말 밑동 기준 한지 패널 중앙
        private const int TreeLargeMinWidth = 7;             // 배경 덩어리 이 폭 이상이면 큰 나무

        /// <summary>지면 청크 순서 (승인 합성과 동일). 피벗이 표면선이라 y=0.5에 그냥 놓으면 맞는다.</summary>
        private static readonly string[] GroundChunks = { "ground_flat_03", "ground_flat_02", "ground_flat_04" };

        /// <summary>흙 백필 색 = ground_flat_03의 흙 몸체 실측 픽셀 (125,91,80).</summary>
        private static readonly Color DirtColor = new Color(125f / 255f, 91f / 255f, 80f / 255f);

        private static readonly Vector2Int[] Neighbors4 =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

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

        /// <summary>스프라이트 한 장 배치. 피벗은 임포트 설정이 쥐고 있으므로 좌표만 준다.</summary>
        private SpriteRenderer PlaceSprite(Transform parent, string name, Sprite sprite, Vector2 pos, int order, bool flip = false)
        {
            if (sprite == null) return null;
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.flipX = flip;   // 피벗 기준으로 뒤집힌다
            sr.sortingOrder = order;
            return sr;
        }

        // ---- 지형 ----

        private void BuildTerrain()
        {
            // 솔리드 '#': 행 런 → 동일 x범위 세로 병합. 칸당 콜라이더 1개 금지 — 이음새 걸림 방지 (지시 사항)
            var mounds = new List<RectInt>();
            foreach (var r in MergeSolidRects())
            {
                var go = new GameObject($"Solid_{r.x}_{r.y}", typeof(BoxCollider2D));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(r.x + (r.width - 1) * 0.5f, r.y + (r.height - 1) * 0.5f, 0);
                go.GetComponent<BoxCollider2D>().size = new Vector2(r.width, r.height);

                if (!_art) { BuildSolidBlock(go, r); continue; }

                // 아트는 덩어리의 역할별로 다르다: 바닥 / 경계 벽 / 단차 블록
                if (r.y == 0 && r.width >= 8) BuildGroundStrip(r);
                else if (r.width <= 2 && r.height >= 3) BuildBoundaryBush(r);
                else mounds.Add(r);
            }
            // 단차는 x 순서로 a/b를 번갈아 (병합 결과 순서에 기대지 않도록 정렬 후 배정)
            mounds.Sort((a, b) => a.x.CompareTo(b.x));
            for (int i = 0; i < mounds.Count; i++) BuildStepMound(mounds[i], i);

            BuildOneWayPlatforms();
            BuildLadders();
        }

        /// <summary>아트 없을 때의 솔리드 폴백 (색 블록).</summary>
        private void BuildSolidBlock(GameObject go, RectInt r)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = UiFactory.RoundedSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(r.width + 0.04f, r.height + 0.04f);
            sr.color = SolidColor;
            sr.sortingOrder = OrderTerrain;
        }

        /// <summary>
        /// 바닥: 청크를 겹쳐 깔고 그 뒤에 흙 한 장을 백필한다.
        /// ★생성기가 청크의 좌우 flush를 못 지켜(실측 edge_delta 1~10px) 청크가 섬 모양으로 나온다.
        ///   승인된 "연속 지형" 룩도 심리스가 아니라 이 겹침+백필 착시다 — 아트 재생성으로 풀 문제가 아니다.
        /// </summary>
        private void BuildGroundStrip(RectInt r)
        {
            float left = r.x - 0.5f - GroundOverhang, right = r.x + r.width - 0.5f + GroundOverhang;

            // ① 백필: 청크 사이 틈·청크 아래로 하늘이 비치지 않게 흙을 한 장 깐다 (청크보다 뒤)
            var bf = PlaceSprite(transform, "GroundBackfill", UiFactory.RoundedSprite(),
                new Vector2((left + right) * 0.5f, (BackfillTopY + BackfillBottomY) * 0.5f), OrderBackfill);
            if (bf != null)
            {
                bf.drawMode = SpriteDrawMode.Sliced;
                bf.size = new Vector2(right - left, BackfillTopY - BackfillBottomY);
                bf.color = DirtColor;
            }

            // ② 청크: 폭 5u를 3.28u 간격으로 → 1.72u씩 겹친다. 홀수는 좌우 반전해 반복감 완화
            int n = Mathf.CeilToInt((right - left) / GroundPitch) + 1;
            for (int i = 0; i < n; i++)
                PlaceSprite(transform, $"Ground_{i}", ArtLibrary.ForestTerrain(GroundChunks[i % GroundChunks.Length]),
                    new Vector2(left + GroundChunkW * 0.5f + i * GroundPitch, SurfaceY), OrderGround, flip: (i % 2) == 1);
        }

        /// <summary>경계 벽: 수풀을 세로로 6px씩 겹쳐 쌓는다.</summary>
        private void BuildBoundaryBush(RectInt r)
        {
            var sp = ArtLibrary.ForestTerrain("boundary_bush");
            if (sp == null) return;
            float step = sp.bounds.size.y - BushOverlapY;
            if (step <= 0.01f) return;
            float x = r.x + (r.width - 1) * 0.5f;
            float top = r.y + r.height - 0.5f;
            int i = 0;
            for (float b = r.y - 0.5f; b < top; b += step)
                PlaceSprite(transform, $"Bush_{r.x}_{i++}", sp, new Vector2(x, b), OrderBush);
        }

        /// <summary>단차 블록. 아트는 4x2칸(256x128px) 전용 — 다른 크기 덩어리가 생기면 아트를 늘려야 한다.</summary>
        private void BuildStepMound(RectInt r, int index)
        {
            var sp = ArtLibrary.ForestTerrain(index % 2 == 0 ? "step_mound_a" : "step_mound_b");
            // TopLeft 피벗 → 칸 좌상단
            PlaceSprite(transform, $"Mound_{r.x}_{r.y}", sp,
                new Vector2(r.x - 0.5f, r.y + r.height - 0.5f + MoundLiftY), OrderTerrain);
        }

        /// <summary>원웨이 발판 '='(+사다리 꼭대기 'H'): 행 단위 연속 구간 → PlatformEffector2D 원웨이 (14장-4).</summary>
        private void BuildOneWayPlatforms()
        {
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

                    if (_art) BuildPlatformArt(x0, y, w);
                    else BuildPlatformBlock(go.transform, w);

                    _oneWayCols.Add(col);
                    _oneWaySet.Add(col);
                }
            }
        }

        /// <summary>발판 아트: 좌 마구리 + 중간 타일 반복 + 우 마구리(반전). 전부 TopLeft 피벗.</summary>
        private void BuildPlatformArt(int x0, int y, int w)
        {
            var cap = ArtLibrary.ForestTerrain("platform_earth_cap_L");
            var mid = ArtLibrary.ForestTerrain("platform_earth_mid");
            float left = x0 - 0.5f, right = x0 + w - 0.5f, topY = y + 0.5f + PlatformLiftY;

            PlaceSprite(transform, $"PlatCapL_{x0}_{y}", cap, new Vector2(left, topY), OrderTerrain);
            // 반전하면 피벗(좌상단) 기준으로 왼쪽에 그려진다 → 오른쪽 끝에 걸면 우 마구리가 된다
            PlaceSprite(transform, $"PlatCapR_{x0}_{y}", cap, new Vector2(right, topY), OrderTerrain, flip: true);

            int i = 0;
            for (float px = left + PlatformCapW; px < right - PlatformCapW; px += PlatformMidW)
                PlaceSprite(transform, $"PlatMid_{x0}_{y}_{i++}", mid, new Vector2(px, topY), OrderTerrain);
        }

        private void BuildPlatformBlock(Transform parent, int w)
        {
            var visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(0, 0.3f, 0);
            var sr = visual.GetComponent<SpriteRenderer>();
            sr.sprite = UiFactory.RoundedSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(w + 0.04f, 0.42f);
            sr.color = PlatformColor;
            sr.sortingOrder = OrderTerrain;
        }

        /// <summary>사다리 'H': 열 단위 연속 구간. 존 판정은 그리드 질의(Input.cs)라 콜라이더 불필요 — 비주얼만.</summary>
        private void BuildLadders()
        {
            for (int x = 0; x < _map.width; x++)
            {
                int y = 0;
                while (y < _map.height)
                {
                    if (!_map.IsLadder(x, y)) { y++; continue; }
                    int y0 = y;
                    while (y < _map.height && _map.IsLadder(x, y)) y++;
                    int h = y - y0;

                    if (_art) BuildLadderArt(x, y0 - 0.5f, y0 + h - 0.5f);
                    else BuildLadderBlock(x, y0, h);
                }
            }
        }

        /// <summary>
        /// 사다리 아트: 아래에서 온전한 칸만 쌓고 마지막 한 장을 꼭대기에 딱 맞춰 덧댄다.
        /// ★승인 합성은 여기서 루프 조건이 한 칸 헐거워 발판 위로 1.7u짜리 몸통이 튀어나왔다
        ///   (검수 지적 "사다리가 공중에 뜸"). 상면 위로는 갓머리만 나오는 게 맞다.
        /// </summary>
        private void BuildLadderArt(int x, float bottomY, float topY)
        {
            var seg = ArtLibrary.ForestTerrain("ladder_body_seg");
            if (seg != null)
            {
                float segH = seg.bounds.size.y; // 크롭된 실루엣 높이 (캔버스 여백을 쌓으면 틈이 생긴다 — 실측)
                if (segH > 0.01f)
                {
                    int i = 0;
                    for (float b = bottomY; b + segH <= topY + 0.001f; b += segH)
                        PlaceSprite(transform, $"LadderSeg_{x}_{i++}", seg, new Vector2(x, b), OrderLadder);
                    PlaceSprite(transform, $"LadderSeg_{x}_top", seg, new Vector2(x, topY - segH), OrderLadder);
                }
            }
            PlaceSprite(transform, $"LadderCap_{x}", ArtLibrary.ForestTerrain("ladder_top_cap"),
                new Vector2(x, topY - LadderCapSinkY), OrderLadder);
        }

        private void BuildLadderBlock(int x, int y0, int h)
        {
            var go = new GameObject($"Ladder_{x}_{y0}", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, y0 + (h - 1) * 0.5f, 0);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = UiFactory.RoundedSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.5f, h + 0.9f); // 위아래로 살짝 여유 — 바닥·발판에 닿아 보이게
            sr.color = LadderColor;
            sr.sortingOrder = OrderLadder;

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
                rs.sortingOrder = OrderLadder;
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

                if (_art) BuildBackdropTrees(root, bd);
                else BuildBackdropBlocks(root, bd);
            }
            if (_art) BuildSkyLayers();
        }

        /// <summary>
        /// 배경 ASCII 덩어리 = 나무 한 그루의 "배치 힌트"다 (모양은 스프라이트가 대신 그린다).
        /// ASCII 큰 나무는 8~9u인데 아트는 5u — 배경이라 충돌이 없어 무해하다 (검수에서 고지됨).
        /// </summary>
        private void BuildBackdropTrees(Transform root, BackdropLayer bd)
        {
            int i = 0;
            foreach (var c in BackdropClusters(bd))
            {
                var sp = ArtLibrary.ForestBackdrop(c.width >= TreeLargeMinWidth ? "tree_pine_large" : "tree_pine_small");
                PlaceSprite(root, $"Tree_{i++}", sp, new Vector2(c.x + (c.width - 1) * 0.5f, TreeBaseY), OrderBackdrop);
            }
        }

        /// <summary>
        /// 배경 덩어리 추출 (4-이웃 연결 요소). 가로로 꽉 찬 행(배경 지면선)은 제외한다 —
        /// 포함하면 나무 줄기가 지면선에 붙어 전부 한 덩어리가 된다.
        /// </summary>
        private static List<RectInt> BackdropClusters(BackdropLayer bd)
        {
            var skip = new bool[bd.height];
            for (int y = 0; y < bd.height; y++)
            {
                bool all = true;
                for (int x = 0; x < bd.width && all; x++)
                    if (bd.Tile(x, y) == '.') all = false;
                skip[y] = all;
            }

            var seen = new bool[bd.width * bd.height];
            var result = new List<RectInt>();
            var stack = new Stack<Vector2Int>();
            for (int y = 0; y < bd.height; y++)
            {
                if (skip[y]) continue;
                for (int x = 0; x < bd.width; x++)
                {
                    if (seen[y * bd.width + x] || bd.Tile(x, y) == '.') continue;
                    int minX = x, maxX = x, minY = y, maxY = y;
                    seen[y * bd.width + x] = true;
                    stack.Push(new Vector2Int(x, y));
                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();
                        if (p.x < minX) minX = p.x;
                        if (p.x > maxX) maxX = p.x;
                        if (p.y < minY) minY = p.y;
                        if (p.y > maxY) maxY = p.y;
                        foreach (var d in Neighbors4)
                        {
                            int nx = p.x + d.x, ny = p.y + d.y;
                            if (nx < 0 || nx >= bd.width || ny < 0 || ny >= bd.height) continue;
                            if (skip[ny] || seen[ny * bd.width + nx] || bd.Tile(nx, ny) == '.') continue;
                            seen[ny * bd.width + nx] = true;
                            stack.Push(new Vector2Int(nx, ny));
                        }
                    }
                    result.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
                }
            }
            result.Sort((a, b) => a.x.CompareTo(b.x));
            return result;
        }

        /// <summary>아트 없을 때의 배경 폴백 (행 단위 런을 연한 색 블록으로, 충돌 없음).</summary>
        private void BuildBackdropBlocks(Transform root, BackdropLayer bd)
        {
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

        /// <summary>
        /// 하늘 그라데이션(카메라 고정) · 원경 능선(패럴랙스 0.25) · 안개 띠(카메라 x 추종).
        /// 하늘·안개는 가로로 균일한 8px 띠라 시야 폭까지 늘려 쓴다 — 크기는 매 프레임 Input.cs가 잡는다
        /// (Pixel Perfect가 해상도에 따라 orthographicSize를 다시 정할 수 있어 지을 때 고정하면 안 된다).
        /// </summary>
        private void BuildSkyLayers()
        {
            var sky = ArtLibrary.ForestBackdrop("sky_gradient");
            var skySr = PlaceSprite(transform, "Sky", sky, Vector2.zero, OrderSky);
            if (skySr != null)
            {
                _skyT = skySr.transform;
                _skyBase = sky.bounds.size;
            }

            var fog = ArtLibrary.ForestBackdrop("fog_band");
            var fogSr = PlaceSprite(transform, "Fog", fog, new Vector2(0, FogTopY), OrderFog);
            if (fogSr != null)
            {
                _fogT = fogSr.transform;
                _fogBase = fog.bounds.size;
            }

            var ridge = ArtLibrary.ForestBackdrop("bg_ridge");
            if (ridge == null) return;
            var root = new GameObject("Backdrop_ridge").transform;
            root.SetParent(transform, false);
            _backdropRoots.Add((root, RidgeParallax));

            // 패럴랙스 0.25는 배경 이동을 압축하므로 맵 폭 + 여유만 깔면 화면이 빈다 (좌우 20u씩)
            float tileW = ridge.bounds.size.x;
            int n = Mathf.CeilToInt((_map.width + 40f) / tileW);
            for (int i = 0; i < n; i++)
                PlaceSprite(root, $"Ridge_{i}", ridge, new Vector2(-20f + i * tileW, RidgeTopY), OrderRidge);
        }

        // ---- 스팟 (MapWorld와 동일 패턴 — 상태색·라벨·잠금 표기는 RefreshStates가 담당) ----

        private void BuildSpots()
        {
            foreach (var (stageId, pos) in _map.spots)
                _spotViews[stageId] = MakeSpot(pos, LabelFor(stageId), stageId: stageId, art: SignArt);

            for (int i = 0; i < _map.exits.Count; i++)
                _exitViews.Add(MakeSpot(_map.exits[i].pos, "→", exitIndex: i, art: GateArt));

            // 상점·사천왕은 아직 사이드뷰 아트가 없다 — 색 블록 폴백 유지
            if (_map.shop.HasValue)
                _shopView = MakeSpot(_map.shop.Value, BrokenText.Apply("상점"), isShop: true);

            if (_map.bossPos.HasValue)
            {
                _bossView = MakeSpot(_map.bossPos.Value, "王", isShop: false);
                _bossView.Bg.size = new Vector2(1.0f, 1.0f);
            }
        }

        private Sprite SignArt => _art ? ArtLibrary.ForestProp("spot_sign_hanji") : null;
        private Sprite GateArt => _art ? ArtLibrary.ForestProp("exit_gate_jangseung") : null;

        private string LabelFor(int stageId)
        {
            var stage = _stageLookup.Find(s => s.id == stageId);
            if (stage != null && stage.goals != null && stage.goals.Length > 0 && !string.IsNullOrEmpty(stage.goals[0].display))
                return stage.goals[0].display.Substring(0, 1);
            return stageId.ToString();
        }

        private SpotView MakeSpot(Vector2Int pos, string label, int stageId = -1, int exitIndex = -1, bool isShop = false, Sprite art = null)
        {
            var go = new GameObject(isShop ? "Shop" : exitIndex >= 0 ? $"Exit_{exitIndex}" : $"Spot_{stageId}");
            go.transform.SetParent(transform, false);
            // pos = 발 칸. 팻말·문은 그 칸의 바닥면에 세워진 구조물 (바닥 앵커, 기획 4.1/9장)
            go.transform.localPosition = new Vector3(pos.x, pos.y - 0.5f, 0);

            var bgGo = new GameObject("Bg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.GetComponent<SpriteRenderer>();
            bg.sortingOrder = OrderSpot;

            float labelY;
            if (art != null)
            {
                bg.sprite = art;      // 발치 피벗 → 칸 바닥면에 그대로 선다
                labelY = SignLabelY;  // 한지 패널 한가운데
            }
            else
            {
                bg.sprite = UiFactory.RoundedSprite();
                bg.drawMode = SpriteDrawMode.Sliced;
                bg.size = isShop ? new Vector2(1.15f, 0.85f) : new Vector2(0.85f, 0.85f);
                bgGo.transform.localPosition = new Vector3(0, 0.5f, 0); // 색 블록은 칸 중앙 (기존 위치 유지)
                labelY = 0.5f;
            }

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, labelY, 0);
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (UiFactory.KoreanFont != null) tmp.font = UiFactory.KoreanFont;
            tmp.text = label;
            tmp.fontSize = art != null ? 3.4f : isShop ? 3.2f : 4.5f; // 한지 패널 안에 들어가는 크기
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiFactory.Ink;
            tmp.rectTransform.sizeDelta = new Vector2(1.4f, 1);
            tmp.sortingOrder = OrderLabel;
            BrokenTextFx.Ensure(tmp); // 상점 등 월드 라벨의 깨진 글자 연출

            return new SpotView { Go = go, Bg = bg, Label = tmp, StageId = stageId, ExitIndex = exitIndex, IsShop = isShop, Pos = pos };
        }

        // ---- 데코 (충돌 없는 전경 장식, 맵 데이터 decorations) ----

        private void BuildDecorations()
        {
            if (!_art) return; // 색 블록 폴백에는 장식이 없다 (지형만으로 읽히게)
            for (int i = 0; i < _map.decorations.Count; i++)
            {
                var d = _map.decorations[i];
                var sp = ArtLibrary.ForestProp(d.art);
                if (sp == null) { Debug.LogWarning($"데코 아트 없음: Art/Forest/Props/{d.art}"); continue; }
                PlaceSprite(transform, $"Deco_{i}_{d.art}", sp, new Vector2(d.x, d.y - DecoSinkY), OrderDeco, d.flip);
            }
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

            _playerT = go.transform;

            // 캐릭터 아트: 발바닥 피벗이라 기준점(발 밑 중앙)에 그대로 얹는다.
            // 몸통 106px=1.66u > 콜라이더 1.3u — 머리 0.36u가 판정 밖으로 나오는 건 의도된 것(기획 1.1장)
            if (_art && ArtLibrary.ForestChar(_charWho, "Idle", 0) != null)
            {
                _charSr = PlaceSprite(go.transform, "Sprite", ArtLibrary.ForestChar(_charWho, "Idle", 0),
                    Vector2.zero, OrderPlayer);
                return;
            }

            // 색 블록 폴백 (조선 시트는 탑다운 4방향용 — 사이드뷰 시트가 없을 때만, 기획 9장)
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

            var exitBtn = UiFactory.CreateButton(_hudRoot, "LeaveBtn", "나가기", 19, UiFactory.Paper, UiFactory.Ink,
                () => { DisablePixelPerfect(); _app.LeaveMapToTitle(); });
            UiFactory.SetRect((RectTransform)exitBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(120, 46));

            var bagBtn = UiFactory.CreateButton(_hudRoot, "BagBtn", "가방", 19, UiFactory.Paper, UiFactory.Ink, () => _app.OpenInventory());
            UiFactory.SetRect((RectTransform)bagBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -20), new Vector2(100, 46));

            _hudGold = UiFactory.CreateText(_hudRoot, "Gold", "", 21, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(_hudGold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-266, -26), new Vector2(240, 36));
        }
    }
}
