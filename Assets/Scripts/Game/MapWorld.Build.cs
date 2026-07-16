using TMPro;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>MapWorld의 구축·렌더링 부분: 지형, 스팟, 플레이어 스프라이트/애니메이션, 맵 HUD.</summary>
    public partial class MapWorld
    {
        private void BuildTerrain()
        {
            bool art = ArtLibrary.Available;
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

            if (ArtLibrary.Available)
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

        /// <summary>4방향 걷기 애니메이션 (코드 구동, 초당 8프레임).</summary>
        private void AnimatePlayer()
        {
            if (!ArtLibrary.Available || _playerSr == null) return;
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

            _hudProgress = UiFactory.CreateText(_hudRoot, "Progress", "", 20, UiFactory.Dim, TextAlignmentOptions.Left);
            UiFactory.SetRect(_hudProgress.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -62), new Vector2(700, 34));

            _hudHint = UiFactory.CreateText(_hudRoot, "Hint", "", 19, UiFactory.Ink);
            UiFactory.SetRect(_hudHint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(900, 34));

            var exitBtn = UiFactory.CreateButton(_hudRoot, "LeaveBtn", "나가기", 19, UiFactory.Paper, UiFactory.Ink, () => _app.LeaveMapToTitle());
            UiFactory.SetRect((RectTransform)exitBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(120, 46));

            var bagBtn = UiFactory.CreateButton(_hudRoot, "BagBtn", "가방", 19, UiFactory.Paper, UiFactory.Ink, () => _app.OpenInventory());
            UiFactory.SetRect((RectTransform)bagBtn.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -20), new Vector2(100, 46));

            _hudGold = UiFactory.CreateText(_hudRoot, "Gold", "", 21, new Color(0.72f, 0.55f, 0.12f), TextAlignmentOptions.Right);
            UiFactory.SetRect(_hudGold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-266, -26), new Vector2(240, 36));
        }
    }
}
