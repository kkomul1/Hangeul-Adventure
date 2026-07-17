using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// SideWorld의 입력·이동 부분 (사이드뷰 전환 기획 1~2장 + 14장 승인 수정):
    /// A/D·←→ 좌우, Shift 달리기, Space 점프(코요테+버퍼+가변), 갖신 보유 시 2단 점프,
    /// W/S(↑↓) 사다리, ↓+Space 원웨이 하강, ↑ 상호작용(사다리 존 밖에서만 — 존 안은 사다리 우선).
    /// 입력은 Update에서 읽고 물리는 FixedUpdate에서 적용한다.
    /// </summary>
    public partial class SideWorld
    {
        // 입력 상태 (Update에서 기록 → FixedUpdate에서 소비)
        private float _inputX;
        private bool _running, _inputUpHeld, _inputDownHeld;
        private float _jumpBufferTimer, _coyoteTimer;
        private bool _jumpCutQueued, _airJumpUsed;
        private int _ladderQueue; // +1=위로 진입 시도, -1=아래로 진입 시도

        // 접지·사다리·원웨이 상태
        private bool _grounded, _standingSolid;
        private bool _onLadder;
        private float _ladderX, _ladderTopY, _ladderBottomY;
        private float _dropTimer;
        private readonly List<Collider2D> _standingOneWay = new List<Collider2D>();
        private readonly List<Collider2D> _droppedCols = new List<Collider2D>();
        private readonly ContactPoint2D[] _contacts = new ContactPoint2D[16];

        // 카메라
        private float _camBaseY, _camVelX, _camVelY;

        // ---- 입력 (Update) ----

        private void Update()
        {
            if (_rb == null) return;
            var kb = Keyboard.current;
            if (_app.IsPanelOpen || kb == null)
            {
                // 상점/가방 열림: 입력만 정지 (물리는 계속 — 낙하 중이면 착지까지 정리됨)
                _inputX = 0f;
                _running = _inputUpHeld = _inputDownHeld = false;
                return;
            }

            _inputX = 0f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) _inputX -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) _inputX += 1f;
            _running = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            _inputUpHeld = kb.upArrowKey.isPressed || kb.wKey.isPressed;
            _inputDownHeld = kb.downArrowKey.isPressed || kb.sKey.isPressed;

            if (kb.spaceKey.wasPressedThisFrame) _jumpBufferTimer = JumpBufferTime; // 점프 버퍼 (기획 1.3장)
            if (kb.spaceKey.wasReleasedThisFrame && !_onLadder) _jumpCutQueued = true; // 가변 점프

            bool upPressed = kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
            bool downPressed = kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;

            var near = NearestInteractable();
            UpdateHint(near);

            if (upPressed && !_onLadder)
            {
                // ↑ 중의성 해결 (14장 지시): 사다리 존 안에서는 사다리 우선, 존 밖에서만 상호작용
                if (CanAttachLadderUp()) _ladderQueue = 1;
                else if (near != null) Interact(near);
            }
            if (downPressed && !_onLadder && CanAttachLadderDown()) _ladderQueue = -1;
        }

        // ---- 물리 (FixedUpdate) ----

        private void FixedUpdate()
        {
            if (_rb == null) return;
            float dt = Time.fixedDeltaTime;

            UpdateContacts();
            UpdateDropTimer(dt);

            if (!_onLadder && _ladderQueue != 0)
            {
                TryAttachLadder(_ladderQueue > 0);
                _ladderQueue = 0;
            }

            if (_onLadder)
            {
                LadderFixed(dt);
                return;
            }

            var v = _rb.linearVelocity;

            // 수평: 목표 속도로 가속 (공중 제어 100% — 기획 1.2장)
            float target = _inputX * (_running ? RunSpeed : WalkSpeed);
            v.x = Mathf.MoveTowards(v.x, target, (_grounded ? GroundAccel : AirAccel) * dt);

            // 코요테·점프 버퍼
            _coyoteTimer = _grounded ? CoyoteTime : _coyoteTimer - dt;
            _jumpBufferTimer -= dt;
            if (_grounded) _airJumpUsed = false;

            if (_jumpBufferTimer > 0f)
            {
                if (_grounded && _inputDownHeld && _standingOneWay.Count > 0 && !_standingSolid)
                {
                    StartDropThrough(); // ↓+점프 = 딛고 있는 원웨이 발판 하강 (기획 1.4장)
                    _jumpBufferTimer = 0f;
                }
                else if (_coyoteTimer > 0f)
                {
                    v.y = JumpVelocity;
                    _coyoteTimer = 0f;
                    _jumpBufferTimer = 0f;
                }
                else if (!_airJumpUsed && ItemStore.HasDoubleJump)
                {
                    v.y = DoubleJumpVelocity; // 2단 점프: 갖신(상점 아이템) 보유 시에만 (14장-3)
                    _airJumpUsed = true;
                    _jumpBufferTimer = 0f;
                }
            }

            if (_jumpCutQueued)
            {
                if (v.y > 0f) v.y *= JumpCutMultiplier; // 상승 중 점프 키 해제 = 짧은 점프
                _jumpCutQueued = false;
            }

            if (v.y < -MaxFallSpeed) v.y = -MaxFallSpeed;
            _rb.linearVelocity = v;
        }

        /// <summary>접촉점에서 접지·딛고 있는 발판 종류 갱신 (법선이 위를 향하는 접촉 = 발밑).</summary>
        private void UpdateContacts()
        {
            int n = _rb.GetContacts(_contacts);
            _grounded = false;
            _standingSolid = false;
            _standingOneWay.Clear();
            for (int i = 0; i < n; i++)
            {
                var c = _contacts[i];
                if (c.normal.y < 0.5f) continue;
                _grounded = true;
                if (_oneWaySet.Contains(c.collider))
                {
                    if (!_standingOneWay.Contains(c.collider)) _standingOneWay.Add(c.collider);
                }
                else _standingSolid = true;
            }
        }

        // ---- 원웨이 하강 (↓+점프) ----

        private void StartDropThrough()
        {
            foreach (var col in _standingOneWay)
            {
                Physics2D.IgnoreCollision(_bodyCol, col, true);
                if (!_droppedCols.Contains(col)) _droppedCols.Add(col);
            }
            _dropTimer = DropThroughTime;
        }

        private void UpdateDropTimer(float dt)
        {
            if (_dropTimer <= 0f) return;
            _dropTimer -= dt;
            if (_dropTimer > 0f) return;
            if (!_onLadder) // 사다리 중이면 이탈 시 SetOneWayIgnored(false)가 일괄 복구
                foreach (var col in _droppedCols)
                    Physics2D.IgnoreCollision(_bodyCol, col, false);
            _droppedCols.Clear();
        }

        /// <summary>사다리 승강 중 원웨이 발판(사다리가 관통하는 층)과의 충돌 일괄 토글.</summary>
        private void SetOneWayIgnored(bool ignored)
        {
            foreach (var col in _oneWayCols)
            {
                if (!ignored && _dropTimer > 0f && _droppedCols.Contains(col)) continue; // ↓점프 통과 중이면 유지
                Physics2D.IgnoreCollision(_bodyCol, col, ignored);
            }
        }

        // ---- 사다리 (트리거 존 = 'H' 칸 집합을 그리드로 질의 · 중력 해제, 14장-4) ----

        private bool CanAttachLadderUp()
        {
            Vector2 feet = _rb.position;
            int cx = Mathf.RoundToInt(feet.x);
            if (Mathf.Abs(feet.x - cx) > LadderSnapRange) return false;
            int lo = Mathf.RoundToInt(feet.y + 0.1f);
            int hi = Mathf.RoundToInt(feet.y + PlayerHeight - 0.1f);
            for (int cy = lo; cy <= hi; cy++)
                if (_map.IsLadder(cx, cy)) return true; // 몸이 H 칸과 겹침 (기획 1.5장)
            return false;
        }

        private bool CanAttachLadderDown()
        {
            Vector2 feet = _rb.position;
            int cx = Mathf.RoundToInt(feet.x);
            if (Mathf.Abs(feet.x - cx) > LadderSnapRange) return false;
            // 발 밑 칸이 사다리(꼭대기) — 위에서 ↓로 재진입 (기획 1.5장)
            return _grounded && _map.IsLadder(cx, Mathf.RoundToInt(feet.y - 0.5f));
        }

        private void TryAttachLadder(bool up)
        {
            Vector2 feet = _rb.position;
            int cx = Mathf.RoundToInt(feet.x);
            if (Mathf.Abs(feet.x - cx) > LadderSnapRange) return;

            int refCell = int.MinValue;
            if (up)
            {
                int lo = Mathf.RoundToInt(feet.y + 0.1f);
                int hi = Mathf.RoundToInt(feet.y + PlayerHeight - 0.1f);
                for (int cy = lo; cy <= hi; cy++)
                    if (_map.IsLadder(cx, cy)) { refCell = cy; break; }
                if (refCell == int.MinValue) return;
            }
            else
            {
                if (!_grounded) return;
                refCell = Mathf.RoundToInt(feet.y - 0.5f);
                if (!_map.IsLadder(cx, refCell)) return;
            }

            // 기둥 범위 (수직 연속 H)
            int top = refCell, bottom = refCell;
            while (_map.IsLadder(cx, top + 1)) top++;
            while (_map.IsLadder(cx, bottom - 1)) bottom--;
            _ladderX = cx;
            _ladderTopY = top + 0.5f;       // 꼭대기 칸 윗변 = 올라설 면
            _ladderBottomY = bottom - 0.5f; // 맨 아래 칸 아랫변

            _onLadder = true;
            _airJumpUsed = false; // 사다리 = 접지 취급, 2단 점프 리셋
            _rb.position = new Vector2(cx, Mathf.Clamp(feet.y - (up ? 0f : 0.05f), _ladderBottomY, _ladderTopY - 0.01f));
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f; // 존 안 = 중력 해제
            SetOneWayIgnored(true);
        }

        private void LadderFixed(float dt)
        {
            _jumpBufferTimer -= dt;

            // 이탈 ①: 점프 (수직 = 점프 초속의 60%) — 14장 승인: 좌우 입력 or 점프로 이탈
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer = 0f;
                ExitLadder(new Vector2(_inputX * LadderExitPushVx, LadderExitJumpVy));
                return;
            }
            // 이탈 ②: 좌우 입력
            if (Mathf.Abs(_inputX) > 0.01f)
            {
                ExitLadder(new Vector2(Mathf.Sign(_inputX) * LadderExitPushVx, 0f));
                return;
            }

            float climb = (_inputUpHeld ? 1f : 0f) - (_inputDownHeld ? 1f : 0f);
            _rb.linearVelocity = new Vector2(0f, climb * ClimbSpeed);

            float feetY = _rb.position.y;
            if (climb > 0f && feetY >= _ladderTopY - 0.01f)
            {
                // 꼭대기: 발판 위로 자연스럽게 올라서기 (스텝업 — 기획 1.5장)
                _rb.position = new Vector2(_ladderX, _ladderTopY);
                ExitLadder(Vector2.zero);
                return;
            }
            if (climb < 0f && feetY <= _ladderBottomY + 0.01f)
            {
                _rb.position = new Vector2(_ladderX, _ladderBottomY);
                ExitLadder(Vector2.zero);
            }
        }

        private void ExitLadder(Vector2 velocity)
        {
            _onLadder = false;
            _rb.gravityScale = _normalGravityScale;
            SetOneWayIgnored(false);
            _rb.linearVelocity = velocity;
        }

        // ---- 상호작용 (탑다운 반경 → 사이드뷰 박스 판정, 기획 3.5장) ----

        private SpotView NearestInteractable()
        {
            Vector2 feet = _rb.position;
            SpotView best = null;
            float bestDx = float.MaxValue;

            void Consider(SpotView v)
            {
                float dx = Mathf.Abs(feet.x - v.Pos.x);
                float dy = Mathf.Abs(feet.y - (v.Pos.y - 0.5f)); // 스팟 발 칸의 바닥면 기준
                if (dx <= InteractHalfW && dy <= InteractHalfH && dx < bestDx)
                {
                    best = v;
                    bestDx = dx;
                }
            }

            foreach (var v in _spotViews.Values) Consider(v);
            foreach (var v in _exitViews) Consider(v);
            if (_shopView != null) Consider(_shopView);
            if (_bossView != null) Consider(_bossView);
            return best;
        }

        private void UpdateHint(SpotView near)
        {
            if (near == null)
            {
                _hudHint.text = "이동: ←→/AD · 점프: Space · 사다리: W/S";
                return;
            }

            if (near == _bossView)
            {
                bool defeated = ProgressStore.IsBossDefeated(_map.bossConfig?.Replace("boss_", ""));
                _hudHint.text = defeated ? "↑: 사천왕 재대결" : "↑: 사천왕에게 도전한다!";
                return;
            }
            if (near.IsShop)
            {
                _hudHint.text = "↑: 상점 열기";
                return;
            }
            if (near.IsExit)
            {
                var exit = _map.exits[near.ExitIndex];
                string exitName = BrokenText.Apply(exit.label); // 장소 이름도 깨진 글자로
                _hudHint.text = MapProgress.ExitOpen(_map, exit)
                    ? $"↑: {exitName}(으)로 이동"
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
                ? $"↑: '{title}' 도전{diff}" + (ProgressStore.GetStars(near.StageId) > 0 ? " (클리어됨)" : "")
                : "기초 배우기를 먼저 완료하세요 (주황색 지점)";
        }

        private void Interact(SpotView spot)
        {
            if (spot == _bossView)
            {
                _app.StartMapBattle(_map.bossConfig, _rb.position);
                return;
            }
            if (spot.IsShop)
            {
                _app.OpenShop();
                return;
            }
            if (spot.IsExit)
            {
                var exit = _map.exits[spot.ExitIndex];
                if (MapProgress.ExitOpen(_map, exit))
                    _app.TravelTo(exit);
                return;
            }
            if (!IsSpotOpen(spot.StageId)) return;

            var stage = _stageLookup.Find(s => s.id == spot.StageId);
            if (stage == null) return;
            if (ProgressStore.MissingConsonants(stage).Length > 0 && ProgressStore.GetStars(spot.StageId) == 0)
            {
                SfxPlayer.Instance?.Fail(); // 자음 게이트: 진입 불가 (D-22)
                return;
            }
            _app.StartMapStage(stage, _rb.position);
        }

        // ---- 카메라 (좌우 추적 + 상하 데드존 + 맵 경계 클램프, 기획 5장) ----

        private void LateUpdate()
        {
            if (_rb == null) return;
            Vector2 feet = _playerT.position; // 보간된 표시 위치 기준 (Interpolate)

            // 기준 y = 마지막 접지 y (사다리 중엔 현재 y). 점프 상승은 따라가지 않는다
            if (_grounded || _onLadder) _camBaseY = feet.y;
            else
            {
                float camBottom = _cam.transform.position.y - _cam.orthographicSize;
                if (feet.y < camBottom + _cam.orthographicSize * 2f * CamFallCatchRatio)
                    _camBaseY = feet.y; // 낙하로 화면 하단 아래 — 기준 y 즉시 갱신
            }

            var target = ClampToMap(feet.x, _camBaseY + CamYOffset);
            var p = _cam.transform.position;
            float nx = Mathf.SmoothDamp(p.x, target.x, ref _camVelX, CamFollowSmooth);
            float ny = Mathf.SmoothDamp(p.y, target.y, ref _camVelY, CamFollowSmooth);
            _cam.transform.position = new Vector3(nx, ny, -10);

            UpdateBackdrops();
        }

        private void SnapCameraImmediate()
        {
            Vector2 feet = _rb.position;
            _camBaseY = feet.y;
            _camVelX = _camVelY = 0f;
            var target = ClampToMap(feet.x, _camBaseY + CamYOffset);
            _cam.transform.position = new Vector3(target.x, target.y, -10);
            UpdateBackdrops();
        }

        private Vector2 ClampToMap(float x, float y)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            float cx = Mathf.Clamp(x, halfW - 0.5f, _map.width - 0.5f - halfW);
            float cy = Mathf.Clamp(y, halfH - 0.5f, _map.height - 0.5f - halfH);
            if (_map.width < halfW * 2) cx = (_map.width - 1) * 0.5f;
            if (_map.height < halfH * 2) cy = (_map.height - 1) * 0.5f;
            return new Vector2(cx, cy);
        }

        /// <summary>배경 레이어 패럴랙스: 맵 중앙 기준 카메라 이동량 × (1 − 계수)만큼 따라간다.</summary>
        private void UpdateBackdrops()
        {
            if (_backdropRoots.Count == 0) return;
            var center = new Vector2((_map.width - 1) * 0.5f, (_map.height - 1) * 0.5f);
            Vector2 cam = _cam.transform.position;
            foreach (var (root, parallax) in _backdropRoots)
            {
                Vector2 off = (cam - center) * (1f - parallax);
                root.localPosition = new Vector3(off.x, off.y, 0);
            }
        }
    }
}
