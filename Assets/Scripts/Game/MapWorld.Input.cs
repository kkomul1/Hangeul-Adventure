using UnityEngine;
using UnityEngine.InputSystem;

namespace HangeulAdventure.Game
{
    /// <summary>MapWorld의 입력·이동·상호작용 부분: WASD 이동, 충돌, 힌트, 진입, 카메라 추적.</summary>
    public partial class MapWorld
    {
        private void Update()
        {
            if (_playerT == null) return;
            if (_app.IsPanelOpen) return; // 상점/가방 열려 있으면 이동 잠금
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector2 dir = Vector2.zero;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) dir.y += 1;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) dir.y -= 1;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir.x -= 1;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir.x += 1;

            _playerMoving = dir.sqrMagnitude > 0.01f;
            if (_playerMoving)
            {
                // 바라보는 방향 (지배 축 기준)
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    _playerDir = dir.x > 0 ? 3 : 2;
                else
                    _playerDir = dir.y > 0 ? 1 : 0;

                dir.Normalize();
                _playerRunning = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                float speed = _playerRunning ? RunSpeed : MoveSpeed;
                Vector2 pos = _playerT.localPosition;
                Vector2 next = pos + dir * (speed * Time.deltaTime);
                if (CanStand(new Vector2(next.x, pos.y))) pos.x = next.x;
                if (CanStand(new Vector2(pos.x, next.y))) pos.y = next.y;
                _playerT.localPosition = pos;
                SnapCamera();
            }
            AnimatePlayer();

            var near = NearestInteractable();
            UpdateHint(near);

            if (near != null && kb.spaceKey.wasPressedThisFrame)
                Interact(near);
        }

        private bool CanStand(Vector2 p)
        {
            for (int dx = -1; dx <= 1; dx += 2)
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    int tx = Mathf.RoundToInt(p.x + dx * PlayerRadius);
                    int ty = Mathf.RoundToInt(p.y + dy * PlayerRadius);
                    if (!_map.Walkable(tx, ty)) return false;
                }
            return true;
        }

        private SpotView NearestInteractable()
        {
            Vector2 pos = _playerT.localPosition;
            SpotView best = null;
            float bestDist = InteractRadius;

            void Consider(SpotView v)
            {
                float d = Vector2.Distance(pos, v.Pos);
                if (d < bestDist) { best = v; bestDist = d; }
            }

            foreach (var v in _spotViews.Values) Consider(v);
            foreach (var v in _exitViews) Consider(v);
            if (_shopView != null) Consider(_shopView);
            if (_bossView != null) Consider(_bossView);
            return best;
        }

        private void UpdateHint(SpotView near)
        {
            if (near == null) { _hudHint.text = "이동: WASD/방향키"; return; }

            if (near == _bossView)
            {
                bool defeated = ProgressStore.IsBossDefeated(_map.bossConfig?.Replace("boss_", ""));
                _hudHint.text = defeated ? "Space: 사천왕 재대결" : "Space: 사천왕에게 도전한다!";
                return;
            }
            if (near.IsShop)
            {
                _hudHint.text = "Space: 상점 열기";
                return;
            }
            if (near.IsExit)
            {
                var exit = _map.exits[near.ExitIndexil];
                string exitName = BrokenText.Apply(exit.label); // 장소 이름도 깨진 글자로
                _hudHint.text = MapProgress.ExitOpen(_map, exit)
                    ? $"Space: {exitName}(으)로 이동"
                    : $"{exitName} — 잠김 (이 지역 {MapProgress.ClearedCount(_map)}/{exit.required} 클리어)";
                return;
            }

            bool open = IsSpotOpen(near.StageId);
            var stage = _stageLookup.Find(s => s.id == near.StageId);
            string missing = stage != null ? ProgressStore.MissingConsonants(stage) : "";
            if (missing.Length > 0 && ProgressStore.GetStars(near.StageId) == 0)
            {
                _hudHint.text = $"이 글자들은 깨져 있다... 필요한 자음: {string.Join(", ", missing.ToCharArray())}";
                return;
            }
            string title = stage != null ? stage.title : near.StageId.ToString();
            string diff = stage != null ? $" · 난이도 {stage.difficulty}" : "";
            _hudHint.text = open
                ? $"Space: '{title}' 도전{diff}" + (ProgressStore.GetStars(near.StageId) > 0 ? " (클리어됨)" : "")
                : "기초 배우기를 먼저 완료하세요 (주황색 지점)";
        }

        private void Interact(SpotView spot)
        {
            if (spot == _bossView)
            {
                _app.StartMapBattle(_map.bossConfig, _playerT.localPosition);
                return;
            }
            if (spot.IsShop)
            {
                _app.OpenShop();
                return;
            }
            if (spot.IsExit)
            {
                var exit = _map.exits[spot.ExitIndexil];
                if (MapProgress.ExitOpen(_map, exit))
                    _app.TravelTo(exit);
                return;
            }
            if (!IsSpotOpen(spot.StageId)) return;

            var stage = _stageLookup.Find(s => s.id == spot.StageId);
            if (stage == null) return;
            if (ProgressStore.MissingConsonants(stage).Length > 0 && ProgressStore.GetStars(spot.StageId) == 0)
            {
                SfxPlayer.Instance?.Fail(); // 자음 게이트: 진입 불가
                return;
            }
            _app.StartMapStage(stage, _playerT.localPosition);
        }

        private void SnapCamera()
        {
            Vector3 p = _playerT.localPosition;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            float x = Mathf.Clamp(p.x, halfW - 0.5f, _map.width - 0.5f - halfW);
            float y = Mathf.Clamp(p.y, halfH - 0.5f, _map.height - 0.5f - halfH);
            if (_map.width < halfW * 2) x = (_map.width - 1) * 0.5f;
            if (_map.height < halfH * 2) y = (_map.height - 1) * 0.5f;
            _cam.transform.position = new Vector3(x, y, -10);
        }
    }
}
