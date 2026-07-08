using System;
using System.Collections.Generic;

namespace HangeulAdventure.Engine
{
    public readonly struct SolveResult
    {
        public readonly bool Solvable;
        public readonly int MinMoves;
        public readonly bool Aborted; // 상태 수 상한 초과로 중단됨 (풀이 불가 확정 아님)
        public readonly int ExploredStates;

        public SolveResult(bool solvable, int minMoves, bool aborted, int explored)
        {
            Solvable = solvable; MinMoves = minMoves; Aborted = aborted; ExploredStates = explored;
        }
    }

    /// <summary>
    /// BFS 솔버 (명세 10장): 풀이 가능성과 최소 수를 계산한다.
    /// 행동 = 밀기(타일×4방향) + 수집(일치 타일 → 첫 빈 슬롯; 같은 문자 슬롯은 서로 교환 가능하므로 대표 1개만).
    /// </summary>
    public static class Solver
    {
        // 상한 500k: 4x4+슬롯 기준 visited 메모리 수십 MB 수준. 초과(Aborted)는 "풀이 불가 확정"이 아님.
        public static SolveResult Solve(StageData stage, int maxStates = 500_000)
        {
            int w = stage.width, h = stage.height, n = w * h;
            var slotList = stage.AllSlots();
            int slotCount = slotList.Count;
            var slots = new char[slotCount];
            for (int i = 0; i < slotCount; i++) slots[i] = slotList[i];

            var initial = Encode(stage.cells, new bool[slotCount]);
            if (AllFilled(initial, n, slotCount)) return new SolveResult(true, 0, false, 0);

            var visited = new HashSet<string> { initial };
            var queue = new Queue<(string state, int depth)>();
            queue.Enqueue((initial, 0));

            var cells = new char[n];
            var filled = new bool[slotCount];

            while (queue.Count > 0)
            {
                var (state, depth) = queue.Dequeue();
                Decode(state, cells, filled, n, slotCount);

                // 모든 후속 상태 생성
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!stage.mask[y * w + x]) continue;
                        char tile = cells[y * w + x];
                        if (tile == Hangul.Empty) continue;

                        // 밀기 4방향
                        foreach (Direction d in GameSession.DirectionsAll)
                        {
                            var (dx, dy) = d.Delta();
                            int tx = x + dx, ty = y + dy;
                            bool exists = stage.CellExists(tx, ty);
                            char target = exists ? cells[ty * w + tx] : Hangul.Empty;

                            var type = PushLogic.Resolve(tile, target, exists, d, out char ns, out char nt);
                            if (type == PushResultType.Fail) continue;

                            char oldS = cells[y * w + x], oldT = cells[ty * w + tx];
                            cells[y * w + x] = ns;
                            cells[ty * w + tx] = nt;
                            string next = Encode(cells, filled);
                            cells[y * w + x] = oldS;
                            cells[ty * w + tx] = oldT;

                            if (!Enqueue(next, depth + 1, n, slotCount, visited, queue, maxStates, out var solved))
                                return solved;
                        }

                        // 수집: 일치하는 첫 빈 슬롯 (같은 문자 슬롯은 대칭이므로 대표 1개)
                        for (int i = 0; i < slotCount; i++)
                        {
                            if (filled[i] || slots[i] != tile) continue;
                            cells[y * w + x] = Hangul.Empty;
                            filled[i] = true;
                            string next = Encode(cells, filled);
                            cells[y * w + x] = tile;
                            filled[i] = false;

                            if (!Enqueue(next, depth + 1, n, slotCount, visited, queue, maxStates, out var solved))
                                return solved;
                            break; // 대표 슬롯 1개만
                        }
                    }
                }
            }

            return new SolveResult(false, -1, false, visited.Count);
        }

        /// <summary>계속 탐색하면 true. 종료 조건(클리어/상한)이면 false와 결과 반환.</summary>
        private static bool Enqueue(string next, int depth, int n, int slotCount,
            HashSet<string> visited, Queue<(string, int)> queue, int maxStates, out SolveResult result)
        {
            result = default;
            if (!visited.Add(next)) return true;

            if (AllFilled(next, n, slotCount))
            {
                result = new SolveResult(true, depth, false, visited.Count);
                return false;
            }
            if (visited.Count > maxStates)
            {
                result = new SolveResult(false, -1, true, visited.Count);
                return false;
            }
            queue.Enqueue((next, depth));
            return true;
        }

        private static bool AllFilled(string state, int n, int slotCount)
        {
            for (int i = 0; i < slotCount; i++)
                if (state[n + i] != '1') return false;
            return true;
        }

        private static string Encode(char[] cells, bool[] filled)
        {
            var arr = new char[cells.Length + filled.Length];
            Array.Copy(cells, arr, cells.Length);
            for (int i = 0; i < filled.Length; i++)
                arr[cells.Length + i] = filled[i] ? '1' : '0';
            return new string(arr);
        }

        private static void Decode(string state, char[] cells, bool[] filled, int n, int slotCount)
        {
            for (int i = 0; i < n; i++) cells[i] = state[i];
            for (int i = 0; i < slotCount; i++) filled[i] = state[n + i] == '1';
        }
    }
}
