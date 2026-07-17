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
    /// 계층 BFS 솔버 (명세 10장): 풀이 가능성과 최소 행동 수를 계산한다.
    /// 간선 비용: 밀기/회전 = 1, 수집 = 0 (수집은 행동 수에 포함되지 않음, D-15).
    ///
    /// 알고리즘 (M6 최적화, 결과는 기존 0-1 BFS와 동일):
    ///   0비용(수집) 간선을 계층 내부에서 폐포(closure)로 흡수해 균일비용 BFS로 환원한다.
    ///   깊이 L의 모든 상태를 (수집 폐포까지 포함해) 확정한 뒤 L+1로 넘어가므로,
    ///   상태의 첫 방문이 곧 최단이다 → 거리표(dist) 없이 방문 집합만 있으면 된다.
    ///
    /// 성능의 핵심은 알고리즘이 아니라 상태 표현이다 (측정 결과 병목은 분기 수가 아니라
    /// 상태당 메모리와 해시였다. 99 스테이지 전수: 59.6초 → 23.7초, 상태당 ~180B → ~25B):
    ///   1) 타일 알파벳 압축 — 이 스테이지에서 나올 수 있는 문자만 원자 예산으로 추려 작은 id로
    ///      매핑 (아래 생성자 2번 참조). char 16비트 → 보통 5~8비트.
    ///   2) 상태에 넣을 칸만 인코딩 — 없는 칸('#')과 바위 칸(@)을 제외 (생성자 1번 참조).
    ///   3) 평면 해시 테이블 — 상태당 ulong[] 힙 할당과 Dictionary 엔트리를 없애고
    ///      연속 배열(_states)에 인라인 저장. id는 삽입 순서라 재해싱에도 불변이므로
    ///      프런티어가 id만 들고 있어도 된다.
    ///   4) 해시 확산 — 개방 주소법 + 선형 탐사는 해시 하위 비트를 그대로 버킷 인덱스로 쓴다.
    ///      FNV-1a만 쓰면 군집이 생겨 일부 스테이지가 5배 느려졌다 (Mix 참조).
    ///
    /// 규칙 판정(PushLogic.Resolve / Hangul.RotateCw)은 전이표에 캐시해 핫패스에서 Dictionary
    /// 조회가 0회다. 규칙 자체는 무변경 — 표는 언제나 Resolve가 채운다.
    ///
    /// 한계: 비용은 '수(minMoves)'가 아니라 '깊이 minMoves까지의 도달 가능 상태 수'로 정해진다.
    /// 개방된 5×4 보드는 계층마다 상태가 ~3.5배씩 늘어 10수 근처에서 벽에 부딪히고,
    /// 바위·사슬로 분기를 묶은 보드(예: stage_104)는 전체 공간이 70만이라 27수(그 보드의 지름)를
    /// 2초에 완전 소진한다. 즉 깊은 스테이지를 만들려면 보드를 넓히지 말고 통로를 좁혀야 한다.
    /// </summary>
    public static class Solver
    {
        // 상한 500k: 기본값 유지. 초과(Aborted)는 "풀이 불가 확정"이 아님.
        // timeBudgetMs: 탐색 시간 상한 (에디터 프리즈 방지, D-07). 초과 시 Aborted.
        public static SolveResult Solve(StageData stage, int maxStates = 500_000, int timeBudgetMs = 30_000)
            => new Search(stage).Run(maxStates, timeBudgetMs);

        // 진단 출력 (SolverCli 측정용). 프로세스당 1회만 읽는다 — 힌트 기능은 후보마다
        // Solve를 다시 부르므로 Solve당 환경변수 조회를 남기지 않는다.
        private static readonly bool StatsOn = Environment.GetEnvironmentVariable("SOLVER_STATS") == "1";
        private static readonly bool TraceOn = Environment.GetEnvironmentVariable("SOLVER_TRACE") == "1";

        private sealed class Search
        {
            // ── 보드 정적 정보 (압축 칸 인덱스 기준) ──
            private readonly int _m;               // 존재하는 칸 수
            private readonly int[] _neigh;         // [ci*4+d] → 이웃 압축 인덱스 (없는 칸이면 -1)
            private readonly bool[] _pinned;       // [ci] 사슬 칸 (D-24): 밀 수 없다
            private readonly ushort[] _initCells;  // [ci] → 초기 타일 id

            // ── 목표 슬롯 ──
            private readonly int _slotCount;
            private readonly ulong _slotFull;      // 전 슬롯 채움 비트
            private readonly ulong[] _slotMaskOf;  // [tileId] → 그 문자를 요구하는 슬롯 비트마스크

            // ── 규칙 전이표 ──
            private readonly int _tiles;           // 알파벳 크기 (id 0 = 빈칸)
            private readonly char[] _chars;        // [tileId] → 문자
            private readonly Dictionary<char, int> _idOf;
            private readonly int[][] _pushRows;     // [d*_tiles + mover] → [target] → (ns<<16)|nt, 0=미계산, -1=Fail
            private readonly int[] _rotTab;        // [tileId] → 회전 결과 id (0이면 회전 불가)

            /// <summary>밀기 판정 (지연 캐시). 규칙은 PushLogic.Resolve가 그대로 결정한다.</summary>
            private int PushEntry(int d, int mover, int target)
            {
                int[] row = _pushRows[d * _tiles + mover];
                if (row == null) _pushRows[d * _tiles + mover] = row = new int[_tiles];
                int e = row[target];
                if (e == 0)
                {
                    e = PushLogic.Resolve(_chars[mover], _chars[target], true, (Direction)d,
                            out char ns, out char nt) == PushResultType.Fail
                        ? -1 : (_idOf[ns] << 16) | _idOf[nt];
                    row[target] = e;
                }
                return e;
            }

            /// <summary>회전 순환의 대표 문자 (회전으로 서로 오갈 수 있는 자모를 한 부류로 센다).</summary>
            private static char RotClass(char c)
            {
                char best = c;
                for (char cur = Hangul.RotateCw(c); cur != Hangul.Empty && cur != c; cur = Hangul.RotateCw(cur))
                    if (cur < best) best = cur;
                return best;
            }

            /// <summary>타일을 기본 자모(원자)로 완전 분해해 회전류별 개수를 누적한다.</summary>
            private static void AddAtoms(char c, Dictionary<char, int> acc)
            {
                if (c == Hangul.Empty || c == Hangul.Rock) return;
                if (Hangul.IsSyllable(c))
                {
                    var (cho, jung, jong) = Hangul.DecomposeSyllable(c);
                    AddAtoms(cho, acc); AddAtoms(jung, acc); AddAtoms(jong, acc);
                    return;
                }
                if (Hangul.TrySplitCompound(c, out char l, out char r))
                {
                    AddAtoms(l, acc); AddAtoms(r, acc);
                    return;
                }
                char k = RotClass(c);
                acc[k] = acc.TryGetValue(k, out int v) ? v + 1 : 1;
            }

            // ── 상태 인코딩: 비트0..slotCount-1 = 슬롯, 그 위로 칸당 _cellBits ──
            private readonly int _cellBits;
            private readonly ulong _cellMask;
            private readonly int _words;

            // ── 방문 테이블 (개방 주소법 + 인라인 저장) ──
            private ulong[] _states;   // id*_words 오프셋에 상태 워드
            private int[] _buckets;    // 값 = id+1, 0 = 빈 슬롯
            private int _bmask;
            private int _count;

            // ── 작업 버퍼 ──
            private readonly ulong[] _base;
            private readonly ulong[] _probe;
            private readonly ushort[] _cells;
            private bool _goalFound;
            private bool _overflow;

            // ulong[] 하나의 요소 수 상한. 2GB(=2^28 요소)를 넘기려면 SolverCli처럼
            // gcAllowVeryLargeObjects가 켜져 있어야 한다. 실제 제동은 maxStates가 건다.
            private const long MaxArray = 1L << 31;

            public Search(StageData stage)
            {
                var prep = System.Diagnostics.Stopwatch.StartNew();
                int w = stage.width, h = stage.height, n = w * h;

                // 1) 상태에 넣을 칸만 압축 인덱스로.
                //    없는 칸('#')은 물론 바위 칸(@)도 제외한다 — 바위는 밀 수도, 밀려날 수도,
                //    회전·수집될 수도 없고(D-21) 어떤 타일도 그 칸에 들어갈 수 없다
                //    (바위를 도착지로 미는 Resolve는 언제나 Fail). 즉 바위 칸은 '없는 칸'과
                //    행동적으로 완전히 동등하므로, 상태에서 빼도 탐색 결과가 바뀌지 않는다.
                var compact = new int[n];
                for (int i = 0; i < n; i++)
                    compact[i] = stage.mask[i] && stage.cells[i] != Hangul.Rock ? _m++ : -1;

                _neigh = new int[_m * 4];
                _pinned = new bool[_m];
                _initCells = new ushort[_m];
                var cellOfCi = new int[_m];
                for (int i = 0; i < n; i++)
                {
                    int ci = compact[i];
                    if (ci < 0) continue;
                    cellOfCi[ci] = i;
                    _pinned[ci] = stage.CellPinned(i % w, i / w);
                    for (int d = 0; d < 4; d++)
                    {
                        var (dx, dy) = ((Direction)d).Delta();
                        int tx = i % w + dx, ty = i / w + dy;
                        _neigh[ci * 4 + d] = stage.CellExists(tx, ty) ? compact[ty * w + tx] : -1; // 바위 이웃 = -1
                    }
                }

                // 2) 타일 알파벳: 원자(기본 자모) 예산으로 상한을 잡는다.
                //
                //    불변식 — 어떤 타일도 초기 보드의 원자 예산을 넘을 수 없다:
                //      · 이동은 보드의 원자 다중집합을 그대로 둔다.
                //      · 합성/분해는 원자를 재배치할 뿐 보존한다 (명세 4·6장의 조합쌍이
                //        정확히 역연산 관계라, atoms(합성물) = atoms(왼쪽) ⊎ atoms(오른쪽)).
                //      · 회전은 원자를 같은 회전 순환({ㅏㅜㅓㅗ}, {ㅑㅠㅕㅛ}, {ㅣㅡ}) 안에서만
                //        바꾼다 → 회전류(RotClass)로 세면 불변.
                //      · 수집은 원자를 덜어내기만 한다.
                //    따라서 임의 시점의 임의 타일 t에 대해 atoms(t) ⊆ (보드 원자) ⊆ (초기 예산).
                //
                //    이는 상한이므로 실제로는 안 나오는 문자가 섞일 수 있다(무해 — id만 낭비).
                //    반대로 나올 수 있는 문자가 빠지는 일은 없다. 만에 하나 빠지면 PushEntry의
                //    _idOf 조회가 예외를 던지므로 조용한 오답이 되지 않는다.
                //
                //    옛 방식(모든 (mover,target) 쌍의 밀기 폐포)은 O(S²) Resolve 호출이라
                //    알파벳이 큰 스테이지에서 준비가 탐색보다 비쌌다 (097: 637ms/Solve).
                //    힌트 기능은 후보마다 Solve를 다시 부르므로 그 비용이 그대로 곱해진다.
                var budget = new Dictionary<char, int>();
                foreach (int i in cellOfCi) AddAtoms(stage.cells[i], budget);

                var idOf = new Dictionary<char, int> { [Hangul.Empty] = 0 };
                var chars = new List<char> { Hangul.Empty };
                var atoms = new Dictionary<char, int>();
                for (char c = 'ㄱ'; c <= 'ㅣ'; c++) Consider(c);   // 호환 자모
                for (char c = '가'; c <= '힣'; c++) Consider(c);  // 완성 음절

                void Consider(char c)
                {
                    if (!Hangul.IsTile(c)) return;
                    atoms.Clear();
                    AddAtoms(c, atoms);
                    foreach (var kv in atoms)
                        if (!budget.TryGetValue(kv.Key, out int have) || have < kv.Value) return;
                    idOf[c] = chars.Count;
                    chars.Add(c);
                }

                _tiles = chars.Count;
                _chars = chars.ToArray();
                _idOf = idOf;

                for (int ci = 0; ci < _m; ci++) _initCells[ci] = (ushort)idOf[stage.cells[cellOfCi[ci]]];

                // 3) 전이표 사전 계산 — 핫패스에서 규칙 판정 0회
                _rotTab = new int[_tiles];
                for (int a = 1; a < _tiles; a++)
                {
                    char r = Hangul.RotateCw(chars[a]);
                    _rotTab[a] = r == Hangul.Empty ? 0 : idOf[r];
                }
                // 밀기표는 mover별 행을 지연 할당·지연 계산 (0 = 미계산, -1 = Fail).
                // 알파벳이 크면 S²가 수백만이라 통짜 배열은 Solve마다 수십 MB를 잡는데,
                // 한 보드에 동시에 존재하는 서로 다른 타일은 기껏해야 칸 수(≤25)뿐이다.
                // 성공 결과는 nt != 빈칸이라 (ns<<16)|nt 가 0이 될 수 없어 미계산과 겹치지 않는다.
                _pushRows = new int[4 * _tiles][];

                // 4) 슬롯 (목표 단어는 최대 몇 글자 — 64슬롯이면 충분하고도 남는다)
                var slotList = stage.AllSlots();
                _slotCount = slotList.Count;
                if (_slotCount > 64) throw new NotSupportedException("목표 슬롯은 64개까지 지원합니다.");
                _slotFull = _slotCount == 64 ? ulong.MaxValue : (1UL << _slotCount) - 1;
                _slotMaskOf = new ulong[_tiles];
                for (int i = 0; i < _slotCount; i++)
                    if (idOf.TryGetValue(slotList[i], out int id)) _slotMaskOf[id] |= 1UL << i;
                // 알파벳에 없는 슬롯 문자 = 절대 만들 수 없음 → 풀이 불가 (탐색 없이 확정된다)

                // 5) 인코딩 폭 결정
                _cellBits = 1;
                while ((1 << _cellBits) < _tiles) _cellBits++;
                _cellMask = (1UL << _cellBits) - 1;
                // 워드는 최소 1개 — 슬롯도 칸도 없는 퇴화 보드에서 s[0] 접근이 깨지지 않게 한다
                _words = Math.Max(1, (_slotCount + _m * _cellBits + 63) >> 6);

                _base = new ulong[_words];
                _probe = new ulong[_words];
                _cells = new ushort[_m];
                if (StatsOn)
                    Console.Error.WriteLine($"  [stats] m={_m} tiles={_tiles} cellBits={_cellBits}" +
                                            $" words={_words} prep={prep.ElapsedMilliseconds}ms");
            }

            // ── 비트 접근 ──
            private int BitOf(int ci) => _slotCount + ci * _cellBits;

            private ushort GetCell(ulong[] s, int ci)
            {
                int b = BitOf(ci), w = b >> 6, o = b & 63;
                ulong v = s[w] >> o;
                if (o + _cellBits > 64) v |= s[w + 1] << (64 - o);
                return (ushort)(v & _cellMask);
            }

            private void SetCell(ulong[] s, int ci, ushort val)
            {
                int b = BitOf(ci), w = b >> 6, o = b & 63;
                s[w] = (s[w] & ~(_cellMask << o)) | ((ulong)val << o);
                if (o + _cellBits > 64)
                    s[w + 1] = (s[w + 1] & ~(_cellMask >> (64 - o))) | ((ulong)val >> (64 - o));
            }

            // ── 방문 테이블 ──
            // 개방 주소법 + 선형 탐사는 해시의 하위 비트를 버킷 인덱스로 그대로 쓴다.
            // FNV-1a는 하위 비트가 약해 군집(clustering)이 생기므로 splitmix64 확산을 덧댄다.
            // (Dictionary는 소수 모듈로가 이를 가려주지만 여기선 직접 해야 한다)
            private static ulong Mix(ulong z)
            {
                z += 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }

            private int Hash(ulong[] s)
            {
                ulong hash = 0;
                for (int i = 0; i < _words; i++) hash = Mix(hash ^ s[i]);
                return (int)hash & 0x7FFFFFFF;
            }

            private int HashStored(int id)
            {
                ulong hash = 0;
                int off = id * _words;
                for (int i = 0; i < _words; i++) hash = Mix(hash ^ _states[off + i]);
                return (int)hash & 0x7FFFFFFF;
            }

            private bool StoredEquals(int id, ulong[] s)
            {
                int off = id * _words;
                for (int i = 0; i < _words; i++)
                    if (_states[off + i] != s[i]) return false;
                return true;
            }

            /// <summary>새 상태면 id를 list에 넣고 true. 이미 있으면 false.</summary>
            private bool AddIfNew(ulong[] s, IntList list)
            {
                int i = Hash(s) & _bmask;
                while (true)
                {
                    int e = _buckets[i];
                    if (e == 0) break;
                    if (StoredEquals(e - 1, s)) return false;
                    i = (i + 1) & _bmask;
                }

                if ((_count + 1) * (long)_words > _states.Length)
                {
                    long want = Math.Min(MaxArray, _states.Length * 2L);
                    if ((_count + 1) * (long)_words > want)
                    {
                        // 배열 한계 도달 — 상태를 버리면 오답(풀이불가 오판)이 되므로 중단 신호를 세운다
                        _overflow = true;
                        return false;
                    }
                    var grown = new ulong[want];
                    Array.Copy(_states, grown, _states.Length);
                    _states = grown;
                }
                int id = _count++;
                Array.Copy(s, 0, _states, id * _words, _words);
                _buckets[i] = id + 1;
                list.Add(id);

                if ((s[0] & _slotFull) == _slotFull) _goalFound = true;
                if (_count * 4L >= _buckets.Length * 3L) GrowBuckets(); // 부하율 0.75
                return true;
            }

            private void GrowBuckets()
            {
                _buckets = new int[_buckets.Length * 2];
                _bmask = _buckets.Length - 1;
                for (int id = 0; id < _count; id++)
                {
                    int i = HashStored(id) & _bmask;
                    while (_buckets[i] != 0) i = (i + 1) & _bmask;
                    _buckets[i] = id + 1;
                }
            }

            public SolveResult Run(int maxStates, int timeBudgetMs)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                // 작게 시작해 2배씩 늘린다. id는 삽입 순서라 재할당·재해싱에도 불변이므로
                // 프런티어(IntList)가 들고 있는 id가 무효화되지 않는다.
                _buckets = new int[1024];
                _bmask = _buckets.Length - 1;
                _states = new ulong[256 * _words];

                var cur = new IntList();
                var next = new IntList();

                Array.Clear(_probe, 0, _words);
                for (int ci = 0; ci < _m; ci++) SetCell(_probe, ci, _initCells[ci]);
                AddIfNew(_probe, cur);
                Closure(cur);
                if (_goalFound) return new SolveResult(true, 0, false, _count);

                for (int depth = 1; ; depth++)
                {
                    next.Clear();
                    for (int i = 0; i < cur.Count; i++)
                    {
                        // 계층이 끝나기 전에도 상한을 본다 — 한 계층이 수천만 상태를 더할 수 있어
                        // 계층 경계에서만 검사하면 상한을 크게 넘겨 메모리를 초과한다
                        if ((i & 0x3FF) == 0 &&
                            (_count > maxStates || sw.ElapsedMilliseconds > timeBudgetMs))
                            return new SolveResult(false, -1, true, _count);
                        ExpandPushes(cur[i], next);
                    }
                    if (_overflow) return new SolveResult(false, -1, true, _count);
                    if (next.Count == 0) return new SolveResult(false, -1, false, _count);
                    Closure(next);
                    if (_goalFound) return new SolveResult(true, depth, false, _count);
                    if (TraceOn) Console.Error.WriteLine($"  [layer] {depth}: +{next.Count:N0} (누적 {_count:N0}, {sw.ElapsedMilliseconds:N0}ms)");
                    if (_overflow || _count > maxStates) return new SolveResult(false, -1, true, _count);
                    if (sw.ElapsedMilliseconds > timeBudgetMs) return new SolveResult(false, -1, true, _count);

                    var tmp = cur; cur = next; next = tmp;
                }
            }

            /// <summary>같은 깊이 안에서 0비용(수집) 간선을 전이적으로 흡수한다. list가 자라며 순회한다.</summary>
            private void Closure(IntList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Load(list[i]);
                    ulong filled = _base[0] & _slotFull;
                    for (int ci = 0; ci < _m; ci++)
                    {
                        ushort t = _cells[ci];
                        if (t == 0) continue;
                        ulong avail = _slotMaskOf[t] & ~filled;
                        if (avail == 0) continue;
                        // 같은 문자의 슬롯은 대칭이므로 최저 인덱스 1개만 대표로 (원본 솔버와 동일)
                        int si = TrailingZeros(avail);
                        Array.Copy(_base, _probe, _words);
                        SetCell(_probe, ci, 0);
                        _probe[0] |= 1UL << si;
                        AddIfNew(_probe, list);
                    }
                }
            }

            private void ExpandPushes(int id, IntList next)
            {
                Load(id);
                for (int ci = 0; ci < _m; ci++)
                {
                    ushort t = _cells[ci];
                    if (t == 0) continue;

                    // 밀기 4방향 (비용 1). 사슬 칸(D-24)의 타일은 밀 수 없다
                    if (!_pinned[ci])
                        for (int d = 0; d < 4; d++)
                        {
                            int nci = _neigh[ci * 4 + d];
                            if (nci < 0) continue; // 보드 밖/없는 칸 → Resolve가 Fail
                            int e = PushEntry(d, t, _cells[nci]);
                            if (e < 0) continue;
                            Array.Copy(_base, _probe, _words);
                            SetCell(_probe, ci, (ushort)(e >> 16));
                            SetCell(_probe, nci, (ushort)(e & 0xFFFF));
                            AddIfNew(_probe, next);
                        }

                    // 회전 (비용 1, D-21) — 사슬 칸에서도 가능
                    int r = _rotTab[t];
                    if (r != 0)
                    {
                        Array.Copy(_base, _probe, _words);
                        SetCell(_probe, ci, (ushort)r);
                        AddIfNew(_probe, next);
                    }
                }
            }

            /// <summary>상태를 작업 버퍼로 복사·해제. (이후 AddIfNew가 _states를 재할당해도 안전)</summary>
            private void Load(int id)
            {
                Array.Copy(_states, id * _words, _base, 0, _words);
                for (int ci = 0; ci < _m; ci++) _cells[ci] = GetCell(_base, ci);
            }

            private static int TrailingZeros(ulong v)
            {
                int c = 0;
                while ((v & 1) == 0) { v >>= 1; c++; }
                return c;
            }
        }

        /// <summary>증가형 int 배열 (계층 프런티어 전용).</summary>
        private sealed class IntList
        {
            private int[] _a = new int[1024];
            public int Count;
            public int this[int i] => _a[i];
            public void Add(int v)
            {
                if (Count == _a.Length) Array.Resize(ref _a, _a.Length * 2);
                _a[Count++] = v;
            }
            public void Clear() => Count = 0;
        }
    }
}
