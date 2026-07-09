using System;
using System.Collections.Generic;

namespace HangeulAdventure.Engine
{
    /// <summary>
    /// 문자열 행 배열로 StageData를 만드는 헬퍼 (테스트/스테이지 제작용).
    /// 행은 위→아래 순서. '.'=빈칸, '#'=없는 칸, 그 외=타일.
    /// </summary>
    public static class StageBuilder
    {
        public static StageData FromRows(string[] rows, params GoalGroup[] goals)
        {
            if (rows == null || rows.Length == 0)
                throw new ArgumentException("rows가 비어 있음");
            int height = rows.Length;
            int width = rows[0].Length;
            foreach (var r in rows)
                if (r.Length != width)
                    throw new ArgumentException("모든 행의 길이가 같아야 합니다.");

            var mask = new bool[width * height];
            var cells = new char[width * height];

            for (int rowIdx = 0; rowIdx < height; rowIdx++)
            {
                int y = height - 1 - rowIdx; // 첫 행이 가장 위 (y 최대)
                for (int x = 0; x < width; x++)
                {
                    char c = rows[rowIdx][x];
                    int i = y * width + x;
                    if (c == '#') { mask[i] = false; cells[i] = Hangul.Empty; }
                    else if (c == '.') { mask[i] = true; cells[i] = Hangul.Empty; }
                    else if (c == Hangul.Rock) { mask[i] = true; cells[i] = Hangul.Rock; } // 바위 장애물
                    else
                    {
                        if (!Hangul.IsTile(c))
                            throw new ArgumentException($"유효한 타일이 아님: '{c}'");
                        mask[i] = true;
                        cells[i] = c;
                    }
                }
            }

            return new StageData
            {
                width = width,
                height = height,
                mask = mask,
                cells = cells,
                goals = goals.Length > 0 ? goals : Array.Empty<GoalGroup>(),
            };
        }

        /// <summary>단일 글자/자모 목표들. 예: Goals("가", "나") 또는 단어 Goals("사과").</summary>
        public static GoalGroup Goal(string display)
        {
            var slots = new List<char>();
            foreach (char c in display) slots.Add(c);
            return new GoalGroup { display = display, slots = slots.ToArray() };
        }
    }
}
