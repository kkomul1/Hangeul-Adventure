using UnityEngine;

namespace HangeulAdventure.Game
{
    /// <summary>스테이지 진행 저장 (PlayerPrefs). 별/루비/최고 기록, 잠금 해제.</summary>
    public static class ProgressStore
    {
        /// <summary>개발자 모드: 모든 스테이지 잠금 해제 (타이틀 '어' 클릭 토글, 진행 초기화 시 함께 해제됨).</summary>
        public static bool DevMode
        {
            get => PlayerPrefs.GetInt("dev_mode", 0) == 1;
            set { PlayerPrefs.SetInt("dev_mode", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // ---- 글자 도감 (모험 요소 1) ----

        /// <summary>지금까지 만들어본 모든 글자. 최초 합성 시 등록.</summary>
        public static string Glyphs
        {
            get => PlayerPrefs.GetString("glyphs", "");
            private set { PlayerPrefs.SetString("glyphs", value); PlayerPrefs.Save(); }
        }

        /// <summary>합성으로 만든 글자를 도감에 등록. 최초 등록이면 true.</summary>
        public static bool RegisterGlyph(char c)
        {
            string g = Glyphs;
            if (g.IndexOf(c) >= 0) return false;
            Glyphs = g + c;
            return true;
        }

        // ---- 자음 회수 (스토리 진행) ----

        /// <summary>되찾은 자음들. 시작 상태 = ㄱㄴㄷ (스토리기획 1장).</summary>
        public static string RecoveredConsonants
        {
            get => PlayerPrefs.GetString("consonants", "ㄱㄴㄷ");
            private set { PlayerPrefs.SetString("consonants", value); PlayerPrefs.Save(); }
        }

        public static void RecoverConsonant(char c)
        {
            if (RecoveredConsonants.IndexOf(c) < 0)
                RecoveredConsonants += c;
        }

        public static bool IsBossDefeated(string bossId) => PlayerPrefs.GetInt($"boss_{bossId}", 0) == 1;

        public static void SetBossDefeated(string bossId)
        {
            PlayerPrefs.SetInt($"boss_{bossId}", 1);
            PlayerPrefs.Save();
        }

        public static int GetStars(int stageId) => PlayerPrefs.GetInt($"stage_{stageId}_stars", 0);
        public static bool GetRuby(int stageId) => PlayerPrefs.GetInt($"stage_{stageId}_ruby", 0) == 1;
        public static int GetBestMoves(int stageId) => PlayerPrefs.GetInt($"stage_{stageId}_best", -1);

        // ---- 골드 ----

        public const int GoldPerStar = 10;    // 별당 10 × 난이도
        public const int GoldPerRuby = 20;    // 루비 +20 × 난이도 (스테이지 최대 = 50 × 난이도)

        public static int Gold => PlayerPrefs.GetInt("gold", 0);

        public static void AddGold(int amount)
        {
            PlayerPrefs.SetInt("gold", Mathf.Max(0, Gold + amount));
            PlayerPrefs.Save();
        }

        public static bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            AddGold(-amount);
            return true;
        }

        private static int GoldValue(int stars, bool ruby, int difficulty)
            => (stars * GoldPerStar + (ruby ? GoldPerRuby : 0)) * Mathf.Max(1, difficulty);

        /// <summary>
        /// 클리어 기록 + 골드 지급. 골드는 "이 스테이지에서 지금까지 받은 총액과의 차액"만 지급 —
        /// 최초 클리어에 별 개수만큼, 이후 기록 갱신(별↑/루비 달성) 시 차액. 재클리어 파밍 불가.
        /// 반환: 이번에 지급된 골드.
        /// </summary>
        public static int Record(int stageId, int stars, bool ruby, int moves, int difficulty)
        {
            int prevValue = GoldValue(GetStars(stageId), GetRuby(stageId), difficulty);

            if (stars > GetStars(stageId))
                PlayerPrefs.SetInt($"stage_{stageId}_stars", stars);
            if (ruby && !GetRuby(stageId))
                PlayerPrefs.SetInt($"stage_{stageId}_ruby", 1);
            int best = GetBestMoves(stageId);
            if (best < 0 || moves < best)
                PlayerPrefs.SetInt($"stage_{stageId}_best", moves);

            int newValue = GoldValue(GetStars(stageId), GetRuby(stageId), difficulty);
            int earned = Mathf.Max(0, newValue - prevValue);
            if (earned > 0) AddGold(earned);
            else PlayerPrefs.Save();
            return earned;
        }

        /// <summary>목록 순서 기준: 첫 스테이지와 "클리어한 것 다음"까지 열림. 개발자 모드면 전부.</summary>
        public static bool IsUnlocked(System.Collections.Generic.List<Engine.StageData> stages, int index)
        {
            if (DevMode) return true;
            if (index <= 0) return true;
            return GetStars(stages[index - 1].id) > 0;
        }
    }
}
