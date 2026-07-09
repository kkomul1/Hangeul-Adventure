using System;
using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 전투 설정 (Resources/Battles/*.json). 사천왕/보스 정의 (스토리기획 4장·4.5절).
    /// trials: 몬스터가 제시하는 퍼즐 시련 목록 (순환).
    /// </summary>
    [Serializable]
    public class BattleConfig
    {
        public string id;
        public string name;          // 예: "받침 사범"
        public string intro = "";    // 등장 대사
        public int maxHp = 30;
        public int atk = 5;
        public int def = 1;
        public string rewardConsonant = ""; // 격파 시 회수하는 자음
        public TrialConfig[] trials;
    }

    [Serializable]
    public class TrialConfig
    {
        public string[] rows;    // 스테이지 rows 형식
        public string goals;     // 쉼표 구분
        public int moveLimit;    // 제한 이동 수 (전조로 미리 표시)
    }

    public enum BattleAction { Attack, Guard, Skill }

    public enum BattlePhase { PlayerTurn, Trial, Victory, Defeat }

    /// <summary>
    /// 전투 상태 머신 (UnityEngine 비의존 로직 — 테스트 가능).
    /// 흐름: 플레이어 행동 → 몬스터가 시련(퍼즐) 제시(전조: 제한 수 공개) →
    /// 제한 내 클리어 = 카운터 / 초과 = 초과분만큼 강한 피격 (수비 시 경감).
    /// </summary>
    public class BattleState
    {
        public const int PlayerBaseHp = 20;
        public const int PlayerBaseAtk = 3;

        public BattleConfig Config { get; }
        public int PlayerHp { get; private set; }
        public int PlayerMaxHp { get; private set; }
        public int EnemyHp { get; private set; }
        public BattlePhase Phase { get; private set; } = BattlePhase.PlayerTurn;

        public int PlayerAtk { get; }
        public int PlayerDef { get; }

        private bool _guarding;          // 수비: 다음 피격 절반 + 다음 시련 제한 수 +2
        private bool _skillCharged;      // 기술(된소리 일격): 다음 카운터 2배, 다음 시련 제한 수 -2
        private int _trialIndex = -1;

        public string Log { get; private set; } = "";

        public BattleState(BattleConfig config, int weaponAtk, int armorDef)
        {
            Config = config;
            PlayerMaxHp = PlayerBaseHp + armorDef * 2; // 방어구가 체력에도 기여
            PlayerHp = PlayerMaxHp;
            EnemyHp = config.maxHp;
            PlayerAtk = PlayerBaseAtk + weaponAtk;
            PlayerDef = armorDef;
        }

        /// <summary>플레이어 행동 처리. 이후 Phase가 Trial이면 CurrentTrial 퍼즐 진행.</summary>
        public void Act(BattleAction action)
        {
            if (Phase != BattlePhase.PlayerTurn) return;

            switch (action)
            {
                case BattleAction.Attack:
                    int dmg = Mathf.Max(1, PlayerAtk - Config.def);
                    EnemyHp -= dmg;
                    Log = $"공격! {Config.name}에게 {dmg} 피해";
                    break;
                case BattleAction.Guard:
                    _guarding = true;
                    Log = "수비 태세 — 다음 피해 절반, 다음 시련 여유 +2수";
                    break;
                case BattleAction.Skill:
                    _skillCharged = true;
                    int sdmg = Mathf.Max(1, (int)(PlayerAtk * 1.5f) - Config.def);
                    EnemyHp -= sdmg;
                    Log = $"된소리 일격! {sdmg} 피해 — 다음 시련이 혹독해진다 (제한 -2수)";
                    break;
            }

            if (EnemyHp <= 0)
            {
                EnemyHp = 0;
                Phase = BattlePhase.Victory;
                return;
            }
            Phase = BattlePhase.Trial;
        }

        /// <summary>다음 시련 (전조 포함). Act 이후 호출.</summary>
        public (TrialConfig trial, int effectiveLimit) NextTrial()
        {
            _trialIndex = (_trialIndex + 1) % Config.trials.Length;
            var trial = Config.trials[_trialIndex];
            int limit = trial.moveLimit + (_guarding ? 2 : 0) + (_skillCharged ? -2 : 0);
            return (trial, Mathf.Max(1, limit));
        }

        /// <summary>시련 결과 판정. 반환: 플레이어에게 들어간 피해 (0이면 카운터 성공).</summary>
        public int ResolveTrial(int movesUsed, int effectiveLimit, bool cleared)
        {
            int damageTaken = 0;

            if (cleared && movesUsed <= effectiveLimit)
            {
                // 카운터 펀치!
                int counter = Mathf.Max(1, PlayerAtk - Config.def);
                if (_skillCharged) counter *= 2;
                EnemyHp -= counter;
                Log = $"제한 내 해결 — 카운터! {counter} 피해";
            }
            else
            {
                int overage = cleared ? movesUsed - effectiveLimit : 4; // 포기/미해결 = 큰 초과 취급
                int raw = Config.atk + Mathf.Max(0, overage) - PlayerDef;
                damageTaken = Mathf.Max(1, raw);
                if (_guarding) damageTaken = Mathf.Max(1, damageTaken / 2);
                PlayerHp -= damageTaken;
                Log = cleared
                    ? $"{overage}수 초과 — {Config.name}의 공격! {damageTaken} 피해"
                    : $"시련 실패 — {Config.name}의 강타! {damageTaken} 피해";
            }

            _guarding = false;
            _skillCharged = false;

            if (EnemyHp <= 0) { EnemyHp = 0; Phase = BattlePhase.Victory; }
            else if (PlayerHp <= 0) { PlayerHp = 0; Phase = BattlePhase.Defeat; }
            else Phase = BattlePhase.PlayerTurn;

            return damageTaken;
        }
    }
}
