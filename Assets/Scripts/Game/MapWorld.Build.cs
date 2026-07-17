using TMPro;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>MapWorld의 구축·렌더링 부분: 지형, 스팟, 플레이어 스프라이트/애니메이션, 맵 HUD.</summary>
    public partial class MapWorld
    {
        private void BuildTerrain()
        {
            bool joseon = ArtLibrary.JoseonAvailable;
            bool art = !joseon && ArtLibrary.Available;
            for (int y = 0; y < _map.height; y++)
            {
                for (int x = 0; x < _map.width; x++)
                {
                    char t = _map.Tile(x, y);
                    var go = new GameObject($"T_{x}_{y}", typeof(SpriteRenderer));
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(x, y, 0);
                    var sr = go.GetComponent<SpriteRenderer>();
                    sr.sortingOrder = 0;

                    if (joseon)
                    {
                        // 조선풍 타일 (M3-6, PixelLab). 순수 타일만 사용 — 전이 타일은 코너 지형값 로직이 필요해 추후
                        sr.sprite = t switch
                        {
                            '-' => ArtLibrary.JoseonTile("TilesetGrassDirt", 3, 0),  // 흙길
                            '~' => ArtLibrary.JoseonTile("TilesetGrassWater", 3, 0), // 물
                            _ => ArtLibrary.JoseonTile("TilesetGrassDirt", 1, 2),    // 풀밭 (수풀 벽의 바닥 포함)
                        };
                        if (t == '#')
                        {
                            // 수풀 벽: 풀밭 위에 겹쳐 배치 (가장자리 투명 요철 아래로 풀이 비침)
                            var bushGo = new GameObject("Bush", typeof(SpriteRenderer));
                            bushGo.transform.SetParent(go.transform, false);
                            var bush = bushGo.GetComponent<SpriteRenderer>();
                            bush.sprite = ArtLibrary.JoseonBush();
                            bush.sortingOrder = 1;
                        }
                        continue;
                    }

                    if (art)
                    {
                        // Ninja Adventure(CC0) 타일 (D-20). 좌표는 픽셀 분석으로 선정한 균일 셀.
                        sr.sprite = t switch
                        {
                            '-' => ArtLibrary.Tile("TilesetField", 1, 1),   // 흙길
                            '#' => ArtLibrary.Tile("TilesetField", 7, 1),   // 짙은 수풀 (통행 불가)
                            '~' => ArtLibrary.Tile("TilesetWater", 1, 1),   // 물
                            _ => ArtLibrary.Tile("TilesetField", 4, 1),     // 풀밭
                        };
                        if (t == '#') sr.color = new Color(0.72f, 0.72f, 0.72f); // 벽 구분용 어둡게
                        continue;
                    }

                    // 폴백: 절차 생성 도형
                    sr.sprite = UiFactory.RoundedSprite();
                    sr.drawMode = SpriteDrawMode.Sliced;
                    sr.size = Vector2.one * 1.02f;
                    sr.color = t switch
                    {
                        '-' => Road,
                        '#' => ((x + y) % 3 == 0) ? House : Tree,
                        '~' => Water,
                        _ => ((x + y) % 2 == 0) ? GrassA : GrassB,
                    };
                }
            }
        }

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
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);

            var bgGo = new GameObject("Bg", typeof(SpriteRenderer));
            bgGo.transform.SetParent(go.transform, false);
            var bg = bgGo.GetComponent<SpriteRenderer>();
            bg.sprite = UiFactory.RoundedSprite();
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = isShop ? new Vector2(1.15f, 0.85f) : new Vector2(0.85f, 0.85f);
            bg.sortingOrder = 2;

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (UiFactory.KoreanFont != null) tmp.font = UiFactory.KoreanFont;
            tmp.text = label;
            tmp.fontSize = isShop ? 3.2f : 4.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiFactory.Ink;
            tmp.rectTransform.sizeDelta = new Vector2(1.4f, 1);
            tmp.sortingOrder = 3;
            BrokenTextFx.Ensure(tmp); // 상점 등 월드 라벨의 깨진 글자 연출

            return new SpotView { Go = go, Bg = bg, Label = tmp, StageId = stageId, ExitIndexil = exitIndex, IsShop = isShop, Pos = pos };
        }

        private void BuildPlayer(Vector2 pos)
        {
            var go = new GameObject("Player", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            _playerSr = sr;

            if (ArtLibrary.JoseonAvailable)
            {
                sr.sprite = ArtLibrary.JoseonSeonbi("Idle", 0, 0); // 남향
            }
            else if (ArtLibrary.Available)
            {
                sr.sprite = ArtLibrary.Character("Noble", "Idle", 0, 0);
            }
            else
            {
                sr.sprite = UiFactory.RoundedSprite();
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(0.62f, 0.62f);
                sr.color = new Color(0.25f, 0.35f, 0.55f);

                var face = new GameObject("Face", typeof(SpriteRenderer));
                face.transform.SetParent(go.transform, false);
                face.transform.localPosition = new Vector3(0, 0.16f, 0);
                var fs = face.GetComponent<SpriteRenderer>();
                fs.sprite = UiFactory.RoundedSprite();
                fs.drawMode = SpriteDrawMode.Sliced;
                fs.size = new Vector2(0.34f, 0.28f);
                fs.color = new Color(0.96f, 0.88f, 0.76f);
                fs.sortingOrder = 6;
            }

            _playerT = go.transform;
        }

        /// <summary>_playerDir(0아래 1위 2왼 3오른) → 조선 시트의 행/열(남0 동1 북2 서3)</summary>
        private static readonly int[] JoseonDirMap = { 0, 2, 3, 1 };

        /// <summary>4방향 걷기 애니메이션 (코드 구동, 초당 8프레임).</summary>
        private void AnimatePlayer()
        {
            if (_playerSr == null) return;

            if (ArtLibrary.JoseonAvailable)
            {
                if (_playerMoving)
                {
                    int frame = (int)(Time.time * (_playerRunning ? 13f : 8f)) % 4;
                    var s = ArtLibrary.JoseonSeonbi("Walk", JoseonDirMap[_playerDir], frame);
                    if (s != null) _playerSr.sprite = s;
                }
                else
                {
                    var s = ArtLibrary.JoseonSeonbi("Idle", 0, JoseonDirMap[_playerDir]);
                    if (s != null) _playerSr.sprite = s;
                }
                return;
            }

            if (!ArtLibrary.Available) return;
            if (_playerMoving)
            {
                int frame = (int)(Time.time * (_playerRunning ? 13f : 8f)) % 4;
                var s = ArtLibrary.Character("Noble", "Walk", frame, _playerDir);
                if (s != null) _playerSr.sprite = s;
            }
            else
            {
                var s = ArtLibrary.Character("Noble", "Idle", 0, _playerDir);
                if (s != null) _playerSr.sprite = s;
            }
        }

        private void BuildHud()
        {
            _hudRoot = UiFactory.CreateEmpty(_canvas.transform, "MapHud");
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

            BuildConsonantGauge();
            BuildRoomBadges();
        }

        /// <summary>ㄱ~ㅎ 회수 게이지 (진행도 아래). 칸 수가 고정이라 폭을 직접 계산해 배치한다.</summary>
        private void BuildConsonantGauge()
        {
            const float cell = 28f;
            var row = UiFactory.CreateEmpty(_hudRoot, "ConsonantGauge");
            UiFactory.SetRect(row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -98),
                new Vector2(AllConsonants.Length * cell + 66, 32));

            _gaugeCells = new TextMeshProUGUI[AllConsonants.Length];
            for (int i = 0; i < AllConsonants.Length; i++)
            {
                var t = UiFactory.CreateText(row, $"C_{i}", "", 20, UiFactory.Ink);
                UiFactory.SetRect(t.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                    new Vector2(i * cell, 0), new Vector2(cell, 30));
                _gaugeCells[i] = t;
            }

            _gaugeCount = UiFactory.CreateText(row, "Count", "", 17, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(_gaugeCount.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(AllConsonants.Length * cell + 8, 0), new Vector2(60, 30));
        }

        /// <summary>방 진행 배지 (게이지 아래). 방에는 위치 데이터가 없어 월드 표지가 아닌 HUD 목록으로 세운다.</summary>
        private void BuildRoomBadges()
        {
            for (int i = 0; i < _map.rooms.Count; i++)
            {
                var badge = UiFactory.CreatePanel(_hudRoot, $"RoomBadge_{i}", new Color(0.96f, 0.94f, 0.89f, 0.85f));
                UiFactory.SetRect(badge, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(24 + i * 190, -134), new Vector2(180, 30));

                var t = UiFactory.CreateText(badge, "Label", "", 17, UiFactory.Ink);
                UiFactory.Stretch(t.rectTransform);
                _roomBadges.Add(t);
            }
        }
    }
}
