using System;
using System.Collections.Generic;
using System.IO;
using HangeulAdventure.Engine;

// 역방향/양방향 탐색 타당성 분석용 측정 도구 (M6). 솔버 본체는 건드리지 않는다.
//   --atoms [폴더]        : 스테이지별 원자 수지 → 목표 상태 집합 크기의 상계
//   --goalstates <파일>    : 도달 가능한 목표 상태를 완전 열거해 실측 (느린 참조 구현)
//   --invcheck <파일>      : 밀기 규칙의 역표를 정방향 표와 전수 대조 (역방향 탐색 전제 검증)
static class Analyze
{
    // ── 원자 분해 (Solver의 AddAtoms와 같은 정의: 회전류로 환산) ──
    static char RotClass(char c)
    {
        char best = c;
        for (char cur = Hangul.RotateCw(c); cur != Hangul.Empty && cur != c; cur = Hangul.RotateCw(cur))
            if (cur < best) best = cur;
        return best;
    }

    static void AddAtoms(char c, Dictionary<char, int> acc)
    {
        if (c == Hangul.Empty || c == Hangul.Rock) return;
        if (Hangul.IsSyllable(c))
        {
            var (cho, jung, jong) = Hangul.DecomposeSyllable(c);
            AddAtoms(cho, acc); AddAtoms(jung, acc); AddAtoms(jong, acc);
            return;
        }
        if (Hangul.TrySplitCompound(c, out char l, out char r)) { AddAtoms(l, acc); AddAtoms(r, acc); return; }
        char k = RotClass(c);
        acc[k] = acc.TryGetValue(k, out int v) ? v + 1 : 1;
    }

    static string Show(Dictionary<char, int> d)
    {
        if (d.Count == 0) return "(없음)";
        var parts = new List<string>();
        foreach (var kv in d) parts.Add(kv.Value == 1 ? kv.Key.ToString() : $"{kv.Key}x{kv.Value}");
        parts.Sort(StringComparer.Ordinal);
        return string.Join(" ", parts);
    }

    /// <summary>
    /// 원자 수지: 목표를 모두 채우면 보드에 무엇이 남는가.
    ///
    /// 원자(회전류 환산)는 이동/합성/분해로 보존되고 수집으로만 줄어든다 (Solver 생성자 2번 주석의
    /// 불변식). 따라서 '모든 슬롯이 채워진 상태'의 보드 원자 = (초기 보드 원자) - (전체 슬롯 원자).
    /// 이 잔여 다중집합이 비면 목표 상태는 '빈 보드' 하나뿐이다 — 역방향 탐색의 출발 프론티어가
    /// 단일 상태가 된다.
    /// </summary>
    public static int AtomsMode(string folder)
    {
        int exact = 0, leftover = 0, total = 0;
        foreach (string path in Directory.GetFiles(folder, "stage_*.json"))
        {
            var (stage, _) = Program.LoadPublic(File.ReadAllText(path));
            var board = new Dictionary<char, int>();
            foreach (char c in stage.cells) AddAtoms(c, board);
            var slots = new Dictionary<char, int>();
            foreach (char c in stage.AllSlots()) AddAtoms(c, slots);

            var rest = new Dictionary<char, int>(board);
            bool shortfall = false;
            foreach (var kv in slots)
            {
                int have = rest.TryGetValue(kv.Key, out int v) ? v : 0;
                if (have < kv.Value) shortfall = true;
                int left = have - kv.Value;
                if (left > 0) rest[kv.Key] = left; else rest.Remove(kv.Key);
            }

            int cells = 0;
            for (int i = 0; i < stage.cells.Length; i++)
                if (stage.mask[i] && stage.cells[i] != Hangul.Rock) cells++;

            int restCount = 0;
            foreach (var kv in rest) restCount += kv.Value;

            // 잔여 원자 k개를 빈 칸 m개에 놓는 배치 수의 아주 느슨한 상계 (타일로 뭉칠 수도 있어 실제론 더 작다)
            long bound = 1;
            for (int i = 0; i < restCount; i++) bound *= Math.Max(1, cells - i);

            total++;
            if (restCount == 0) exact++; else leftover++;
            Console.WriteLine($"{Path.GetFileName(path),-18} 칸 {cells,2} | 잔여원자 {restCount} {Show(rest),-12}"
                + $" | 목표상태 상계 {(restCount == 0 ? "1 (빈 보드 유일)" : "<=" + bound)}"
                + (shortfall ? "  [자모부족=풀이불가]" : ""));
        }
        Console.WriteLine($"\n합계 {total}개: 잔여 0 (목표상태 유일) {exact}개, 잔여 있음 {leftover}개");
        return 0;
    }

    /// <summary>
    /// 양방향 탐색의 이득을 '정답 그래프'로 시뮬레이션한다 (역방향 규칙 엔진 없이).
    ///
    /// 전체 도달 가능 공간을 정방향으로 소진하며 간선을 다 저장한 뒤,
    ///   distF = 시작에서의 0-1 BFS,  distB = 목표 집합에서 역간선 0-1 BFS
    /// 를 구해 계층별 누적을 센다. 양방향 BFS의 비용은 대략
    ///   min_k ( |distF <= k| + |distB <= D-k| )   (D = minMoves)
    /// 이므로, 이 값을 정방향 전용 비용 |distF <= D| 와 직접 비교하면
    /// '역방향 규칙을 구현할 가치가 있는가'가 구현 전에 결정된다.
    /// 공간을 통째로 소진해야 하므로 작은 보드 전용 (stage_104급).
    /// </summary>
    public static int BidiMode(string file)
    {
        var (stage, _) = Program.LoadPublic(File.ReadAllText(file));
        int w = stage.width, h = stage.height, n = w * h;
        var slotList = stage.AllSlots();
        int sc = slotList.Count;

        var id = new Dictionary<string, int>();
        var isGoal = new List<bool>();
        var ef = new List<int>(); var et = new List<int>(); var ec = new List<int>();
        var cells = new char[n]; var filled = new bool[sc];

        string Enc()
        {
            var a = new char[n + sc];
            Array.Copy(cells, a, n);
            for (int i = 0; i < sc; i++) a[n + i] = filled[i] ? '1' : '0';
            return new string(a);
        }
        int Intern(string s)
        {
            if (id.TryGetValue(s, out int v)) return v;
            v = id.Count; id[s] = v;
            bool g = true;
            for (int i = 0; i < sc; i++) if (s[n + i] != '1') { g = false; break; }
            isGoal.Add(g);
            return v;
        }

        Array.Copy(stage.cells, cells, n);
        string init = Enc();
        int start = Intern(init);
        var stack = new Stack<string>(); stack.Push(init);

        while (stack.Count > 0)
        {
            string s = stack.Pop();
            int si = id[s];
            for (int i = 0; i < n; i++) cells[i] = s[i];
            for (int i = 0; i < sc; i++) filled[i] = s[n + i] == '1';

            void Edge(string next, int cost)
            {
                bool fresh = !id.ContainsKey(next);
                int ti = Intern(next);
                ef.Add(si); et.Add(ti); ec.Add(cost);
                if (fresh) stack.Push(next);
            }

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!stage.mask[y * w + x]) continue;
                char tile = cells[y * w + x];
                if (tile == Hangul.Empty) continue;

                if (!stage.CellPinned(x, y))
                    foreach (Direction d in GameSession.DirectionsAll)
                    {
                        var (dx, dy) = d.Delta();
                        int tx = x + dx, ty = y + dy;
                        bool ex = stage.CellExists(tx, ty);
                        char tgt = ex ? cells[ty * w + tx] : Hangul.Empty;
                        if (PushLogic.Resolve(tile, tgt, ex, d, out char ns, out char nt) == PushResultType.Fail) continue;
                        char os = cells[y * w + x], ot = cells[ty * w + tx];
                        cells[y * w + x] = ns; cells[ty * w + tx] = nt;
                        Edge(Enc(), 1);
                        cells[y * w + x] = os; cells[ty * w + tx] = ot;
                    }

                char rot = Hangul.RotateCw(tile);
                if (rot != Hangul.Empty)
                {
                    cells[y * w + x] = rot; Edge(Enc(), 1); cells[y * w + x] = tile;
                }

                for (int i = 0; i < sc; i++)
                {
                    if (filled[i] || slotList[i] != tile) continue;
                    cells[y * w + x] = Hangul.Empty; filled[i] = true;
                    Edge(Enc(), 0);
                    cells[y * w + x] = tile; filled[i] = false;
                    break;
                }
            }
        }

        int N = id.Count, E = ef.Count;
        Console.WriteLine($"{Path.GetFileName(file)}: 상태 {N:N0}, 간선 {E:N0}");

        // CSR (정방향 / 역방향)
        int[] Csr(List<int> src, out int[] adj, out int[] cost, List<int> dst, List<int> c)
        {
            var head = new int[N + 1];
            foreach (int v in src) head[v + 1]++;
            for (int i = 0; i < N; i++) head[i + 1] += head[i];
            var pos = (int[])head.Clone();
            adj = new int[E]; cost = new int[E];
            for (int i = 0; i < E; i++) { int p = pos[src[i]]++; adj[p] = dst[i]; cost[p] = c[i]; }
            return head;
        }
        var fh = Csr(ef, out int[] fa, out int[] fc, et, ec);
        var bh = Csr(et, out int[] ba, out int[] bc, ef, ec);

        int[] Bfs01(int[] head, int[] adj, int[] cost, IEnumerable<int> sources)
        {
            var dist = new int[N];
            for (int i = 0; i < N; i++) dist[i] = int.MaxValue;
            var dq = new LinkedList<int>();
            foreach (int s in sources) { dist[s] = 0; dq.AddLast(s); }
            while (dq.Count > 0)
            {
                int u = dq.First.Value; dq.RemoveFirst();
                for (int e = head[u]; e < head[u + 1]; e++)
                {
                    int v = adj[e], nd = dist[u] + cost[e];
                    if (nd >= dist[v]) continue;
                    dist[v] = nd;
                    if (cost[e] == 0) dq.AddFirst(v); else dq.AddLast(v);
                }
            }
            return dist;
        }

        var goals = new List<int>();
        for (int i = 0; i < N; i++) if (isGoal[i]) goals.Add(i);
        var distF = Bfs01(fh, fa, fc, new[] { start });
        var distB = Bfs01(bh, ba, bc, goals);

        int D = int.MaxValue;
        foreach (int g in goals) if (distF[g] < D) D = distF[g];
        Console.WriteLine($"목표 상태 {goals.Count:N0}개 | minMoves(정방향) = {D} | distB[시작] = {distB[start]}"
                          + (D == distB[start] ? "  [일치 ✓]" : "  [불일치 ✗]"));

        var cf = new int[D + 2]; var cb = new int[D + 2];
        for (int i = 0; i < N; i++)
        {
            if (distF[i] <= D) cf[distF[i]]++;
            if (distB[i] <= D) cb[distB[i]]++;
        }
        for (int k = 1; k <= D; k++) { cf[k] += cf[k - 1]; cb[k] += cb[k - 1]; }

        Console.WriteLine($"\n{"k",3} {"|distF<=k|",12} {"|distB<=k|",12}");
        for (int k = 0; k <= D; k++) Console.WriteLine($"{k,3} {cf[k],12:N0} {cb[k],12:N0}");

        long best = long.MaxValue; int bestK = 0;
        for (int k = 0; k <= D; k++)
        {
            long tot = (long)cf[k] + cb[D - k];
            if (tot < best) { best = tot; bestK = k; }
        }
        Console.WriteLine($"\n정방향 전용 탐색량 : {cf[D]:N0}");
        Console.WriteLine($"양방향 최적 분할   : k={bestK} → {best:N0}  (이론 이득 {cf[D] / (double)best:F2}배)");
        return 0;
    }

    /// <summary>
    /// 진짜 역방향 BFS 프로토타입 (목표 → 시작). 솔버 본체는 건드리지 않는다.
    ///
    /// 목적 두 가지:
    ///   1) 역방향 탐색이 정방향과 같은 minMoves를 내는지 검증 (독립 오라클)
    ///   2) BidiMode 시뮬레이션이 못 재는 값 — '시작에서 도달 불가능하지만 목표엔 닿는' 상태까지
    ///      포함한 진짜 역방향 계층 크기 — 를 실측
    ///
    /// 잔여 원자가 0인 스테이지 전용 (목표 상태 = 바위만 남은 빈 보드, 유일). 99개 중 56개가 해당.
    /// 문자열 키라 느리다 — 측정용이지 실사용용이 아니다.
    /// </summary>
    public static int BackwardMode(string file, int maxStates)
    {
        var (stage, _) = Program.LoadPublic(File.ReadAllText(file));
        int w = stage.width, h = stage.height, n = w * h;
        var slotList = stage.AllSlots();
        int sc = slotList.Count;

        // 잔여 원자 검사 — 0이어야 목표가 유일하다
        var board = new Dictionary<char, int>(); foreach (char c in stage.cells) AddAtoms(c, board);
        var need = new Dictionary<char, int>(); foreach (char c in slotList) AddAtoms(c, need);
        foreach (var kv in need)
        {
            int have = board.TryGetValue(kv.Key, out int v) ? v : 0;
            if (have != kv.Value) { Console.WriteLine($"{Path.GetFileName(file)}: [미지원] 잔여 원자 != 0"); return 2; }
        }
        foreach (var kv in board)
            if (!need.ContainsKey(kv.Key)) { Console.WriteLine($"{Path.GetFileName(file)}: [미지원] 잔여 원자 != 0"); return 2; }

        // 알파벳 (Solver와 같은 원자 예산 상한)
        var alpha = new List<char>();
        void ConsiderTile(char c)
        {
            if (!Hangul.IsTile(c)) return;
            var a = new Dictionary<char, int>(); AddAtoms(c, a);
            foreach (var kv in a) if (!board.TryGetValue(kv.Key, out int hv) || hv < kv.Value) return;
            alpha.Add(c);
        }
        for (char c = 'ㄱ'; c <= 'ㅣ'; c++) ConsiderTile(c);
        for (char c = '가'; c <= '힣'; c++) ConsiderTile(c);

        // 역표: (d, ns, nt) → 이 결과를 낳는 (mover, target) 전부
        var inv = new Dictionary<(int, char, char), List<(char, char)>>();
        foreach (char mv in alpha)
            for (int d = 0; d < 4; d++)
            {
                foreach (char tg in alpha)
                    if (PushLogic.Resolve(mv, tg, true, (Direction)d, out char a1, out char b1) != PushResultType.Fail)
                        Add(d, a1, b1, mv, tg);
                if (PushLogic.Resolve(mv, Hangul.Empty, true, (Direction)d, out char a2, out char b2) != PushResultType.Fail)
                    Add(d, a2, b2, mv, Hangul.Empty);
            }
        void Add(int d, char ns, char nt, char mv, char tg)
        {
            var k = (d, ns, nt);
            if (!inv.TryGetValue(k, out var l)) inv[k] = l = new List<(char, char)>();
            l.Add((mv, tg));
        }

        var ccw = new Dictionary<char, char>();
        foreach (char c in alpha) { char r = Hangul.RotateCw(c); if (r != Hangul.Empty) ccw[r] = c; }

        string Enc(char[] cs, bool[] fl)
        {
            var a = new char[n + sc];
            Array.Copy(cs, a, n);
            for (int i = 0; i < sc; i++) a[n + i] = fl[i] ? '1' : '0';
            return new string(a);
        }

        // 목표 상태: 바위만 남고 전 슬롯 채움
        var gc = new char[n];
        for (int i = 0; i < n; i++) gc[i] = stage.cells[i] == Hangul.Rock ? Hangul.Rock : Hangul.Empty;
        var gf = new bool[sc]; for (int i = 0; i < sc; i++) gf[i] = true;
        string goal = Enc(gc, gf);

        var startCells = new char[n]; Array.Copy(stage.cells, startCells, n);
        string start = Enc(startCells, new bool[sc]);

        var seen = new HashSet<string> { goal };
        var cur = new List<string> { goal };
        var cells = new char[n]; var filled = new bool[sc];

        // 같은 깊이 안의 0비용(수집) 역간선을 폐포로 흡수 — 정방향 계층 BFS의 거울상
        void Closure(List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                string s = list[i];
                for (int j = 0; j < n; j++) cells[j] = s[j];
                for (int j = 0; j < sc; j++) filled[j] = s[n + j] == '1';

                for (int slot = 0; slot < sc; slot++)
                {
                    if (!filled[slot]) continue;
                    char T = slotList[slot];
                    // 정방향은 '일치하는 최저 인덱스 빈 슬롯'에만 수집한다. 그 규칙을 그대로 뒤집는다:
                    // slot보다 앞선 같은 문자 슬롯이 하나라도 비어 있었다면 slot에 담길 수 없었다.
                    bool ok = true;
                    for (int j = 0; j < slot; j++)
                        if (slotList[j] == T && !filled[j]) { ok = false; break; }
                    if (!ok) continue;

                    filled[slot] = false;
                    for (int A = 0; A < n; A++)
                    {
                        if (!stage.mask[A] || cells[A] != Hangul.Empty) continue;
                        cells[A] = T;
                        string p = Enc(cells, filled);
                        cells[A] = Hangul.Empty;
                        if (seen.Add(p)) list.Add(p);
                    }
                    filled[slot] = true;
                }
            }
        }

        Closure(cur);
        if (seen.Contains(start)) { Console.WriteLine($"{Path.GetFileName(file)}: 역방향 minMoves = 0"); return 0; }

        Console.WriteLine($"{Path.GetFileName(file)}: 알파벳 {alpha.Count}, 역표 항목 {inv.Count:N0}");
        Console.WriteLine($"  [역계층] 0: {cur.Count:N0} (누적 {seen.Count:N0})");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int depth = 1; ; depth++)
        {
            var next = new List<string>();
            foreach (string s in cur)
            {
                for (int j = 0; j < n; j++) cells[j] = s[j];
                for (int j = 0; j < sc; j++) filled[j] = s[n + j] == '1';

                void Pred(string p) { if (seen.Add(p)) next.Add(p); }

                for (int A = 0; A < n; A++)
                {
                    if (!stage.mask[A]) continue;
                    int ax = A % w, ay = A / w;

                    // 역회전 (비용 1) — 정방향 회전은 사슬 칸에서도 가능하므로 핀 검사 없음.
                    // CCW가 게임의 합법 수일 필요는 없다. 우리는 '선행 상태'를 찾을 뿐이다.
                    if (cells[A] != Hangul.Empty && ccw.TryGetValue(cells[A], out char before))
                    {
                        char keep = cells[A];
                        cells[A] = before; Pred(Enc(cells, filled)); cells[A] = keep;
                    }

                    // 역밀기 (비용 1) — 정방향에서 A의 타일을 d로 민 결과가 현재 (A,B)였다고 가정
                    if (stage.CellPinned(ax, ay)) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        var (dx, dy) = ((Direction)d).Delta();
                        int bx = ax + dx, by = ay + dy;
                        if (!stage.CellExists(bx, by)) continue;
                        int B = by * w + bx;
                        if (!inv.TryGetValue((d, cells[A], cells[B]), out var list)) continue;
                        char sa = cells[A], sb = cells[B];
                        foreach (var (mv, tg) in list)
                        {
                            cells[A] = mv; cells[B] = tg;
                            Pred(Enc(cells, filled));
                        }
                        cells[A] = sa; cells[B] = sb;
                    }
                }
            }

            Closure(next);
            Console.WriteLine($"  [역계층] {depth}: +{next.Count:N0} (누적 {seen.Count:N0}, {sw.ElapsedMilliseconds:N0}ms)");
            if (seen.Contains(start))
            {
                Console.WriteLine($"  ==> 역방향 minMoves = {depth} | 역방향 탐색 상태 {seen.Count:N0}");
                return 0;
            }
            if (next.Count == 0) { Console.WriteLine("  ==> 풀이불가(역방향)"); return 1; }
            if (seen.Count > maxStates) { Console.WriteLine($"  ==> [중단] 상태 상한 {maxStates:N0}"); return 1; }
            cur = next;
        }
    }

    /// <summary>
    /// 도달 가능한 목표 상태를 완전 열거해 실측한다 (느린 참조 구현 — 작은 보드 전용).
    /// AtomsMode의 상계가 실제와 맞는지 확인하는 용도.
    /// </summary>
    public static int GoalStatesMode(string file)
    {
        var (stage, _) = Program.LoadPublic(File.ReadAllText(file));
        int w = stage.width, h = stage.height, n = w * h;
        var slotList = stage.AllSlots();
        int sc = slotList.Count;

        string Enc(char[] cells, bool[] filled)
        {
            var a = new char[n + sc];
            Array.Copy(cells, a, n);
            for (int i = 0; i < sc; i++) a[n + i] = filled[i] ? '1' : '0';
            return new string(a);
        }

        var init = Enc(stage.cells, new bool[sc]);
        var seen = new HashSet<string> { init };
        var stack = new Stack<string>();
        stack.Push(init);
        var goals = new List<string>();
        var cells = new char[n];
        var filled = new bool[sc];

        while (stack.Count > 0)
        {
            string s = stack.Pop();
            for (int i = 0; i < n; i++) cells[i] = s[i];
            for (int i = 0; i < sc; i++) filled[i] = s[n + i] == '1';

            bool all = true;
            for (int i = 0; i < sc; i++) if (!filled[i]) { all = false; break; }
            if (all) goals.Add(s);

            void Push(string next)
            {
                if (seen.Add(next)) stack.Push(next);
            }

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!stage.mask[y * w + x]) continue;
                char tile = cells[y * w + x];
                if (tile == Hangul.Empty) continue;

                if (!stage.CellPinned(x, y))
                    foreach (Direction d in GameSession.DirectionsAll)
                    {
                        var (dx, dy) = d.Delta();
                        int tx = x + dx, ty = y + dy;
                        bool ex = stage.CellExists(tx, ty);
                        char tgt = ex ? cells[ty * w + tx] : Hangul.Empty;
                        if (PushLogic.Resolve(tile, tgt, ex, d, out char ns, out char nt) == PushResultType.Fail) continue;
                        char os = cells[y * w + x], ot = cells[ty * w + tx];
                        cells[y * w + x] = ns; cells[ty * w + tx] = nt;
                        Push(Enc(cells, filled));
                        cells[y * w + x] = os; cells[ty * w + tx] = ot;
                    }

                char rot = Hangul.RotateCw(tile);
                if (rot != Hangul.Empty)
                {
                    cells[y * w + x] = rot;
                    Push(Enc(cells, filled));
                    cells[y * w + x] = tile;
                }

                for (int i = 0; i < sc; i++)
                {
                    if (filled[i] || slotList[i] != tile) continue;
                    cells[y * w + x] = Hangul.Empty; filled[i] = true;
                    Push(Enc(cells, filled));
                    cells[y * w + x] = tile; filled[i] = false;
                    break;
                }
            }
        }

        Console.WriteLine($"{Path.GetFileName(file)}: 도달가능 상태 {seen.Count:N0} | " +
                          $"그중 목표 상태(모든 슬롯 채움) {goals.Count:N0}");
        foreach (string g in goals)
        {
            var board = new List<string>();
            for (int i = 0; i < n; i++)
                if (g[i] != Hangul.Empty) board.Add($"{g[i]}@({i % w},{i / w})");
            Console.WriteLine($"   목표상태 남은 타일: {(board.Count == 0 ? "빈 보드" : string.Join(" ", board))}");
        }
        return 0;
    }
}
