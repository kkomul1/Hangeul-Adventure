using System;
using System.Collections.Generic;

namespace HangeulAdventure.Engine
{
    public readonly struct SolveResult
    {
        public readonly bool Solvable;
        public readonly int MinMoves;
        public readonly bool Aborted; // 상태/시간 상한 초과로 중단됨 (풀이 불가 확정 아님)
        public readonly int ExploredStates;

        public SolveResult(bool solvable, int minMoves, bool aborted, int explored)
        {
            Solvable = solvable; MinMoves = minMoves; Aborted = aborted; ExploredStates = explored;
        }
    }

    /// <summary>
    /// 0-1 BFS 솔버 (명세 10장): 풀이 가능성과 최소 이동 수를 계산한다.
    /// 간선 비용: 밀기 = 1, 수집 = 0 (수집은 이동 수에 포함되지 않음, D-15).
    /// 0비용 간선은 덱 앞에, 1비용 간선은 뒤에 넣어 최단성을 보장한다.
    /// </summary>
    public static class Solver
    {
        // 상한 500k: 4x4+슬롯 기준 방문 메모리 수십 MB 수준. 초과(Aborted)는 "풀이 불가 확정"이 아님.
        // timeBudgetMs: 탐색 시간 상한 (에디터 프리즈 방지, D-07). 초과 시 Aborted.
        public static SolveResult Solve(StageData stage, int maxStates = 500_000, int timeBudgetMs = 30_000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            int w = stage.width, h = stage.height, n = w * h;
            var slotList = stage.AllSlots();
            int slotCount = slotList.Count;
            var slots = new char[slotCount];
            for (int i = 0; i < slotCount; i++) slots[i] = slotList[i];

            var initial = Encode(stage.cells, new bool[slotCount]);

            var dist = new Dictionary<string, int> { [initial] = 0 };
            var deque = new LinkedList<(string state, int depth)>();
            deque.AddFirst((initial, 0));

            var cells = new char[n];
            var filled = new bool[slotCount];

            while (deque.Count > 0)
            {
                if (sw.ElapsedMilliseconds > timeBudgetMs)
                    return new SolveResult(false, -1, true, dist.Count);

                var (state, depth) = deque.First.Value;
                deque.RemoveFirst();
                if (depth > dist[state]) continue; // 더 짧은 경로로 이미 처리됨

                if (AllFilled(state, n, slotCount))
                    return new SolveResult(true, depth, false, dist.Count);

                Decode(state, cells, filled, n, slotCount);

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!stage.mask[y * w + x]) continue;
                        char tile = cells[y * w + x];
                        if (tile == Hangul.Empty) continue;

                        // 밀기 4방향 (비용 1). 사슬 칸(D-24)의 타일은 밀 수 없다
                        if (!stage.CellPinned(x, y))
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

                            Relax(next, depth + 1, front: false, dist, deque);
                        }

                        // 회전 (비용 1, D-21)
                        char rotated = Hangul.RotateCw(tile);
                        if (rotated != Hangul.Empty)
                        {
                            cells[y * w + x] = rotated;
                            string rotNext = Encode(cells, filled);
                            cells[y * w + x] = tile;
                            Relax(rotNext, depth + 1, front: false, dist, deque);
                        }

                        // 수집: 일치하는 첫 빈 슬롯 (비용 0; 같은 문자 슬롯은 대칭이므로 대표 1개)
                        for (int i = 0; i < slotCount; i++)
                        {
                            if (filled[i] || slots[i] != tile) continue;
                            cells[y * w + x] = Hangul.Empty;
                            filled[i] = true;
                            string next = Encode(cells, filled);
                            cells[y * w + x] = tile;
                            filled[i] = false;

                            Relax(next, depth, front: true, dist, deque);
                            break;
                        }
                    }
                }

                if (dist.Count > maxStates)
                    return new SolveResult(false, -1, true, dist.Count);
            }

            return new SolveResult(false, -1, false, dist.Count);
        }

        private static void Relax(string next, int depth, bool front,
            Dictionary<string, int> dist, LinkedList<(string, int)> deque)
        {
            if (dist.TryGetValue(next, out int known) && known <= depth) return;
            dist[next] = depth;
            if (front) deque.AddFirst((next, depth));
            else deque.AddLast((next, depth));
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
