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
    /// 0-1 BFS 솔버 (명세 10장): 풀이 가능성과 최소 행동 수를 계산한다.
    /// 간선 비용: 밀기 = 1, 수집 = 0 (수집은 행동 수에 포함되지 않음, D-15).
    /// 0비용 간선은 덱 앞에, 1비용 간선은 뒤에 넣어 최단성을 보장한다.
    /// 상태 인코딩 (M3-8): string 대신 ulong[] 패킹 — 셀당 16비트 + 슬롯당 1비트, 해시는 생성 시 1회
    /// 계산해 캐시. 후보 생성은 현재 상태 워드를 복사해 마스크 패치(재인코딩 없음), 저장 시에만 복제.
    /// Mono(에디터) 환경의 상태당 힙 할당·O(길이) 해시 재계산을 제거한다. 알고리즘 자체는 무변경.
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

            int cellWords = (n + 3) >> 2;                          // 셀 4개 = 1워드
            int slotWords = Math.Max(1, (slotCount + 63) >> 6);    // 슬롯 64개 = 1워드 (0개여도 1워드)
            int words = cellWords + slotWords;

            // 완료 판정: 슬롯 비트가 전부 1인 워드와 비교
            var full = new ulong[slotWords];
            for (int i = 0; i < slotCount; i++) full[i >> 6] |= 1UL << (i & 63);

            var initial = new ulong[words];
            for (int i = 0; i < n; i++) initial[i >> 2] |= (ulong)stage.cells[i] << ((i & 3) << 4);

            var initKey = new Key(initial, Hash(initial));
            var dist = new Dictionary<Key, int>(1024) { [initKey] = 0 };
            var deque = new Deque(1024);
            deque.PushFront(initKey, 0);

            var cells = new char[n];
            var filled = new bool[slotCount];
            var scratch = new ulong[words];

            while (deque.Count > 0)
            {
                if (sw.ElapsedMilliseconds > timeBudgetMs)
                    return new SolveResult(false, -1, true, dist.Count);

                deque.PopFront(out Key state, out int depth);
                if (depth > dist[state]) continue; // 더 짧은 경로로 이미 처리됨

                if (AllFilled(state.W, cellWords, full))
                    return new SolveResult(true, depth, false, dist.Count);

                Decode(state.W, cells, filled, n, slotCount, cellWords);

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

                            Array.Copy(state.W, scratch, words);
                            SetCell(scratch, y * w + x, ns);
                            SetCell(scratch, ty * w + tx, nt);
                            Relax(scratch, words, depth + 1, front: false, dist, ref deque);
                        }

                        // 회전 (비용 1, D-21)
                        char rotated = Hangul.RotateCw(tile);
                        if (rotated != Hangul.Empty)
                        {
                            Array.Copy(state.W, scratch, words);
                            SetCell(scratch, y * w + x, rotated);
                            Relax(scratch, words, depth + 1, front: false, dist, ref deque);
                        }

                        // 수집: 일치하는 첫 빈 슬롯 (비용 0; 같은 문자 슬롯은 대칭이므로 대표 1개)
                        for (int i = 0; i < slotCount; i++)
                        {
                            if (filled[i] || slots[i] != tile) continue;
                            Array.Copy(state.W, scratch, words);
                            SetCell(scratch, y * w + x, Hangul.Empty);
                            scratch[cellWords + (i >> 6)] |= 1UL << (i & 63);
                            Relax(scratch, words, depth, front: true, dist, ref deque);
                            break;
                        }
                    }
                }

                if (dist.Count > maxStates)
                    return new SolveResult(false, -1, true, dist.Count);
            }

            return new SolveResult(false, -1, false, dist.Count);
        }

        /// <summary>패킹 상태 키: 워드 배열 참조 + 캐시된 해시.</summary>
        private readonly struct Key : IEquatable<Key>
        {
            public readonly ulong[] W;
            private readonly int _hash;

            public Key(ulong[] w, int hash) { W = w; _hash = hash; }

            public bool Equals(Key other)
            {
                if (_hash != other._hash || W.Length != other.W.Length) return false;
                for (int i = 0; i < W.Length; i++)
                    if (W[i] != other.W[i]) return false;
                return true;
            }

            public override bool Equals(object obj) => obj is Key k && Equals(k);
            public override int GetHashCode() => _hash;
        }

        private static void Relax(ulong[] scratch, int words, int depth, bool front,
            Dictionary<Key, int> dist, ref Deque deque)
        {
            var probe = new Key(scratch, Hash(scratch));
            if (dist.TryGetValue(probe, out int known) && known <= depth) return;

            // scratch는 재사용 버퍼이므로 저장/큐 적재 전에 복제 (덱에 살아있는 동안 변조 방지)
            var stored = new ulong[words];
            Array.Copy(scratch, stored, words);
            var key = new Key(stored, probe.GetHashCode());
            dist[key] = depth;
            deque.Push(key, depth, front);
        }

        private static bool AllFilled(ulong[] state, int cellWords, ulong[] full)
        {
            for (int i = 0; i < full.Length; i++)
                if (state[cellWords + i] != full[i]) return false;
            return true;
        }

        private static void Decode(ulong[] state, char[] cells, bool[] filled, int n, int slotCount, int cellWords)
        {
            for (int i = 0; i < n; i++)
                cells[i] = (char)((state[i >> 2] >> ((i & 3) << 4)) & 0xFFFF);
            for (int i = 0; i < slotCount; i++)
                filled[i] = ((state[cellWords + (i >> 6)] >> (i & 63)) & 1UL) != 0;
        }

        private static void SetCell(ulong[] state, int index, char value)
        {
            int wi = index >> 2, shift = (index & 3) << 4;
            state[wi] = (state[wi] & ~(0xFFFFUL << shift)) | ((ulong)value << shift);
        }

        // FNV-1a 변형 (워드 단위)
        private static int Hash(ulong[] w)
        {
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < w.Length; i++)
            {
                hash ^= w[i];
                hash *= 1099511628211UL;
            }
            return (int)(hash ^ (hash >> 32));
        }

        /// <summary>배열 기반 덱 (0-1 BFS 전용): LinkedList의 노드 할당을 제거한다.</summary>
        private struct Deque
        {
            private Key[] _keys;
            private int[] _depths;
            private int _head;   // 첫 원소 위치
            private int _count;
            private int _mask;   // 용량-1 (용량은 2의 거듭제곱)

            public Deque(int capacity)
            {
                int cap = 4;
                while (cap < capacity) cap <<= 1;
                _keys = new Key[cap];
                _depths = new int[cap];
                _head = 0; _count = 0; _mask = cap - 1;
            }

            public int Count => _count;

            public void Push(Key key, int depth, bool front)
            {
                if (front) PushFront(key, depth);
                else PushBack(key, depth);
            }

            public void PushFront(Key key, int depth)
            {
                if (_count == _keys.Length) Grow();
                _head = (_head - 1) & _mask;
                _keys[_head] = key;
                _depths[_head] = depth;
                _count++;
            }

            public void PushBack(Key key, int depth)
            {
                if (_count == _keys.Length) Grow();
                int tail = (_head + _count) & _mask;
                _keys[tail] = key;
                _depths[tail] = depth;
                _count++;
            }

            public void PopFront(out Key key, out int depth)
            {
                key = _keys[_head];
                depth = _depths[_head];
                _keys[_head] = default; // 참조 해제 (GC)
                _head = (_head + 1) & _mask;
                _count--;
            }

            private void Grow()
            {
                int cap = _keys.Length << 1;
                var nk = new Key[cap];
                var nd = new int[cap];
                for (int i = 0; i < _count; i++)
                {
                    int src = (_head + i) & _mask;
                    nk[i] = _keys[src];
                    nd[i] = _depths[src];
                }
                _keys = nk; _depths = nd;
                _head = 0; _mask = cap - 1;
            }
        }
    }
}
