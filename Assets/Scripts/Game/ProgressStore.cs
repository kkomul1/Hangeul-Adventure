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

        /// <summary>자음 게이트 (D-22): 스테이지가 쓰는 자음이 전부 회수됐는가. 미회수 자음 목록 반환 (빈 문자열 = 통과).</summary>
        public static string MissingConsonants(Engine.StageData stage)
        {
            if (DevMode) return "";
            string recovered = RecoveredConsonants;
            var sb = new System.Text.StringBuilder();
            foreach (char c in stage.UsedBaseConsonants())
                if (recovered.IndexOf(c) < 0) sb.Append(c);
            return sb.ToString();
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

        // ---- 화폐 (금·은·동) ----

        public const int CoinPerSilver = 100;
        public const int CoinPerGold = CoinPerSilver * 100;

        /// <summary>잔액은 총 동전 수 하나로만 저장한다 — 금/은은 표시할 때만 나눈다 (3키 동기화·빌림 버그 방지).</summary>
        public static int Coins => PlayerPrefs.GetInt("coins", 0);

        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt("coins", Mathf.Max(0, Coins + amount));
            PlayerPrefs.Save();
        }

        public static bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            AddCoins(-amount);
            return true;
        }

        /// <summary>동전 수를 "금 1 은 2 동 50"으로. 0인 자릿수는 생략, 전부 0이면 "동 0".</summary>
        public static string Format(int coins)
        {
            string t = "";
            if (coins / CoinPerGold > 0) t += $"금 {coins / CoinPerGold} ";
            if ((coins % CoinPerGold) / CoinPerSilver > 0) t += $"은 {(coins % CoinPerGold) / CoinPerSilver} ";
            if (coins % CoinPerSilver > 0 || t.Length == 0) t += $"동 {coins % CoinPerSilver}";
            return t.TrimEnd();
        }

        /// <summary>보상 = 별수(루비 = 4번째 별) × (1 + (난이도-1) × 0.5) 동전. 배율 (d+1)/2를 정수로 풀고 .5는 올림 —
        /// float로 계산해 내림하면 별이 올라도 차액이 0으로 뭉개지는 구간이 생긴다.</summary>
        private static int CoinValue(int stars, bool ruby, int difficulty)
        {
            int eff = Mathf.Clamp(stars, 0, 3) + (ruby ? 1 : 0);
            return (eff * (Mathf.Max(1, difficulty) + 1) + 1) / 2;
        }

        /// <summary>
        /// 클리어 기록 + 동전 지급. 동전은 "이 스테이지에서 지금까지 받은 총액과의 차액"만 지급 —
        /// 최초 클리어에 별 개수만큼, 이후 기록 갱신(별↑/루비 달성) 시 차액. 재클리어 파밍 불가.
        /// 반환: 이번에 지급된 동전.
        /// </summary>
        public static int Record(int stageId, int stars, bool ruby, int moves, int difficulty)
        {
            int prevValue = CoinValue(GetStars(stageId), GetRuby(stageId), difficulty);

            if (stars > GetStars(stageId))
                PlayerPrefs.SetInt($"stage_{stageId}_stars", stars);
            if (ruby && !GetRuby(stageId))
                PlayerPrefs.SetInt($"stage_{stageId}_ruby", 1);
            int best = GetBestMoves(stageId);
            if (best < 0 || moves < best)
                PlayerPrefs.SetInt($"stage_{stageId}_best", moves);

            int newValue = CoinValue(GetStars(stageId), GetRuby(stageId), difficulty);
            int earned = Mathf.Max(0, newValue - prevValue);
            if (earned > 0) AddCoins(earned);
            else PlayerPrefs.Save();
            return earned;
        }

        // ---- 구 "골드" 잔액 변환 ----

        private const int CurrencyVer = 1;

        /// <summary>정적 생성자에서 1회 변환 — 부팅 훅에 의존하면 호출을 빠뜨렸을 때 구 잔액이 조용히 사라진다.</summary>
        static ProgressStore()
        {
            if (PlayerPrefs.GetInt("currency_ver", 0) >= CurrencyVer) return;
            if (PlayerPrefs.HasKey("gold"))   // 이 가드가 없으면 신규·초기화 직후 세이브에도 변환이 돈다
            {
                PlayerPrefs.SetInt("coins", Coins + (PlayerPrefs.GetInt("gold", 0) + 5) / 10);  // 구 가격을 1/10로 리베이스했으므로 잔액도 1/10 (구매력 유지)
                PlayerPrefs.DeleteKey("gold");
            }
            PlayerPrefs.SetInt("currency_ver", CurrencyVer);
            PlayerPrefs.Save();
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
