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

        public static int GetStars(int stageId) => PlayerPrefs.GetInt($"stage_{stageId}_stars", 0);
        public static bool GetRuby(int stageId) => PlayerPrefs.GetInt($"stage_{stageId}_ruby", 0) == 1;
        public static int GetBestMoves(int stageId) => PlayerPrefs.GetInt($"stage_{stageId}_best", -1);

        public static void Record(int stageId, int stars, bool ruby, int moves)
        {
            if (stars > GetStars(stageId))
                PlayerPrefs.SetInt($"stage_{stageId}_stars", stars);
            if (ruby && !GetRuby(stageId))
                PlayerPrefs.SetInt($"stage_{stageId}_ruby", 1);
            int best = GetBestMoves(stageId);
            if (best < 0 || moves < best)
                PlayerPrefs.SetInt($"stage_{stageId}_best", moves);
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
