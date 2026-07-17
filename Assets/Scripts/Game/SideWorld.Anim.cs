using System.Collections.Generic;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// SideWorld의 캐릭터 애니메이션 (Art/Forest/Char, PPU 64).
    ///
    /// 상태기는 물리 상태에서 직접 읽는다 (Animator·상태 저장 없음 — 사이드뷰 상태는 이미 Input.cs가 갖고 있다).
    ///   사다리 = Climb(북쪽 시트) / 공중 = Jump / 수평 이동 = Run|Walk / 그 외 = Idle
    ///
    /// 스프라이트는 발바닥 피벗이라 플레이어 기준점(발 밑 중앙, 기획 1.1장)에 그대로 얹힌다.
    /// 아트는 동쪽(→) 기준이라 좌향은 flipX.
    /// </summary>
    public partial class SideWorld
    {
        private const string CharHanbok = "SeonbiHanbok"; // 세종 조우 후 (현재 기본)
        private const string CharModern = "PlayerModern"; // 세종 조우 전 — idle/walk만 있다

        // 애니메이션 속도 (프레임/초)
        private const float FpsIdle = 6f;
        private const float FpsWalk = 10f;
        private const float FpsRun = 14f;
        private const float FpsClimb = 8f;
        private const float RunAnimSpeed = 0.35f;   // 이 속도 이상이면 보행 애니 (u/s)
        private const float JumpLaunchTime = 0.09f; // 도약 프레임 유지 시간 (s)

        private SpriteRenderer _charSr;
        private string _charWho = CharHanbok;
        private string _anim = "";
        private float _animTime;
        private int _animFrame;
        private float _airTime;
        private int _facing = 1;
        private readonly Dictionary<string, int> _frameCount = new Dictionary<string, int>();

        /// <summary>
        /// 복장 전환. 기획상 세종 조우(sejong_first_talk_done) 전에는 현대복, 후에는 한복이지만
        /// 스토리 트리거가 아직 없어 지금은 한복 고정이다 — 트리거가 생기면 이 호출만 붙이면 된다.
        /// </summary>
        public void SetPlayerCostume(bool hanbok)
        {
            string who = hanbok ? CharHanbok : CharModern;
            if (who == _charWho) return;
            _charWho = who;
            _anim = ""; // 다음 갱신에서 새 시트로 다시 잡는다
        }

        /// <summary>프레임 수 캐시 (Resources.LoadAll을 매 프레임 돌지 않도록).</summary>
        private int FrameCount(string anim)
        {
            string key = _charWho + "/" + anim;
            if (!_frameCount.TryGetValue(key, out int n))
            {
                n = ArtLibrary.ForestCharFrames(_charWho, anim);
                _frameCount[key] = n;
            }
            return n;
        }

        /// <summary>LateUpdate에서 호출 (물리 상태는 FixedUpdate가, 표시 위치는 보간이 이미 정리해 둔 시점).</summary>
        private void UpdateCharAnim()
        {
            if (_charSr == null) return;

            if (!_onLadder && Mathf.Abs(_inputX) > 0.01f) _facing = _inputX > 0 ? 1 : -1;
            _charSr.flipX = !_onLadder && _facing < 0; // 사다리는 등을 보이는 북쪽 시트라 반전하지 않는다

            _airTime = (_grounded || _onLadder) ? 0f : _airTime + Time.deltaTime;

            string want;
            float fps;
            if (_onLadder)
            {
                want = "Climb";
                fps = Mathf.Abs(_rb.linearVelocity.y) > 0.05f ? FpsClimb : 0f; // 멈추면 프레임도 멈춘다
            }
            else if (!_grounded)
            {
                want = "Jump";
                fps = 0f; // 프레임을 시간이 아니라 수직 속도로 고른다
            }
            else if (Mathf.Abs(_rb.linearVelocity.x) > RunAnimSpeed)
            {
                want = _running ? "Run" : "Walk";
                fps = _running ? FpsRun : FpsWalk;
            }
            else
            {
                want = "Idle";
                fps = FpsIdle;
            }

            // 현대복은 idle/walk만 있다 (세종 조우 전 평지 구간용) — 없는 애니는 걷기로 때운다
            if (FrameCount(want) == 0)
            {
                want = FrameCount("Walk") > 0 ? "Walk" : "Idle";
                fps = FpsWalk;
            }

            if (want != _anim) { _anim = want; _animTime = 0f; _animFrame = 0; }

            int n = FrameCount(_anim);
            if (n == 0) return;

            if (_anim == "Jump" && n >= 6)
            {
                // 0~2는 웅크림(도약 전 예비 동작)이라 즉발 점프에는 쓸 자리가 없다.
                // 3=도약 / 4=상승 / 5=하강. 7~9번은 갓이 사라져 임포트 단계에서 잘려 있다 (검수 지시)
                _animFrame = _airTime < JumpLaunchTime ? 3 : _rb.linearVelocity.y > -1f ? 4 : 5;
            }
            else if (fps > 0f)
            {
                _animTime += Time.deltaTime * fps;
                _animFrame = Mathf.FloorToInt(_animTime) % n;
            }

            var sp = ArtLibrary.ForestChar(_charWho, _anim, Mathf.Clamp(_animFrame, 0, n - 1));
            if (sp != null) _charSr.sprite = sp;
        }
    }
}
