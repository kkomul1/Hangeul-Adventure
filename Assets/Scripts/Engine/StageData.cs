using System;
using System.Collections.Generic;

namespace HangeulAdventure.Engine
{
    /// <summary>
    /// 스테이지 정의 (명세 1, 7, 8, 10장). 직렬화 포맷과 무관한 순수 데이터.
    /// 좌표계: x는 오른쪽+, y는 위쪽+. (0,0)이 왼쪽 아래.
    /// </summary>
    [Serializable]
    public class StageData
    {
        public int id;
        public string title = "";
        public int width;
        public int height;

        /// <summary>칸 존재 여부 (없는 칸=false). 길이 = width*height. index = y*width + x.</summary>
        public bool[] mask;

        /// <summary>초기 타일. '\0'=빈칸. 길이 = width*height.</summary>
        public char[] cells;

        /// <summary>사슬 칸 (D-24): 이 칸의 타일은 밀 수 없다 (회전·합성 대상·수집은 가능). null = 없음.</summary>
        public bool[] pinnedMask;

        /// <summary>목표 그룹 (표시 단위). 단일 글자면 슬롯 1개, 단어면 글자당 슬롯.</summary>
        public GoalGroup[] goals;

        /// <summary>솔버 검증 최소 수. 루비 별 기준.</summary>
        public int minMoves;

        /// <summary>
        /// 난이도 1~5. 기준은 "일반 클리어(별 1개)의 어려움" — 최적해가 아니라 클리어 자체가
        /// 어려운 정도 (보드 밀집도, 이동 제약 등). 골드 배율 (별당 10×난이도, 루비 +20×난이도).
        /// </summary>
        public int difficulty = 1;

        /// <summary>튜토리얼 안내 문구 (빈 문자열이면 미표시).</summary>
        public string hint = "";

        /// <summary>별 기준: 이동 수 <= 값. [1별, 2별, 3별]</summary>
        public int[] starThresholds;

        public int Index(int x, int y) => y * width + x;

        public bool InBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

        public bool CellExists(int x, int y) => InBounds(x, y) && mask[Index(x, y)];

        public bool CellPinned(int x, int y) => pinnedMask != null && InBounds(x, y) && pinnedMask[Index(x, y)];

        /// <summary>모든 목표 슬롯을 평탄화한 목록.</summary>
        public IReadOnlyList<char> AllSlots()
        {
            var list = new List<char>();
            if (goals == null) return list;
            foreach (var g in goals)
            {
                if (g?.slots == null) continue;
                foreach (var s in g.slots)
                    list.Add(s);
            }
            return list;
        }

        /// <summary>
        /// 이 스테이지가 사용하는 기본 자음 집합 (보드 타일 + 목표, 복합 자모는 성분으로 분해).
        /// 자음 게이트(D-22): 미회수 자음이 포함된 스테이지는 깨져 보이며 진입 불가.
        /// </summary>
        public string UsedBaseConsonants()
        {
            var set = new System.Collections.Generic.HashSet<char>();
            foreach (char c in cells) Collect(c, set);
            foreach (char s in AllSlots()) Collect(s, set);
            var sb = new System.Text.StringBuilder();
            foreach (char c in "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎ")
                if (set.Contains(c)) sb.Append(c);
            return sb.ToString();

            static void Collect(char c, System.Collections.Generic.HashSet<char> set)
            {
                if (c == Hangul.Empty || c == Hangul.Rock) return;
                if (Hangul.IsSyllable(c))
                {
                    var (cho, jung, jong) = Hangul.DecomposeSyllable(c);
                    Collect(cho, set);
                    Collect(jung, set);
                    if (jong != Hangul.Empty) Collect(jong, set);
                    return;
                }
                if (Hangul.TrySplitCompound(c, out char l, out char r))
                {
                    Collect(l, set);
                    Collect(r, set);
                    return;
                }
                if (Hangul.IsConsonant(c)) set.Add(c);
            }
        }

        /// <summary>기본 별 기준 공식 (명세 7장): 3별 = 최소수 + max(1, round(최소수*0.1))</summary>
        public static int[] DefaultStarThresholds(int minMoves)
        {
            int three = minMoves + Math.Max(1, (int)Math.Round(minMoves * 0.1));
            int two = minMoves + Math.Max(2, (int)Math.Round(minMoves * 0.3));
            int one = int.MaxValue; // 1별 = 클리어 자체
            return new[] { one, two, three };
        }
    }

    [Serializable]
    public class GoalGroup
    {
        /// <summary>진행도 판에 표시할 이름 (예: "사과", "가").</summary>
        public string display = "";

        /// <summary>수집해야 하는 타일들 (정확 일치).</summary>
        public char[] slots;
    }
}
