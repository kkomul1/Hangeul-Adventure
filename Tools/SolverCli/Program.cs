using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using HangeulAdventure.Engine;

// 엔진 코드를 그대로 컴파일해 전체 스테이지를 고속 검증하고 minMoves를 기입하는 CLI.
// (Unity 에디터의 Mono 런타임이 느려서 대형 보드 검증용으로 사용 — 동일한 엔진 코드가 기준)
// 모드:
//   (없음)            : Stages 폴더 전수 검증 + minMoves 기입
//   --consonants      : 스테이지별 사용 기본 자음
//   --path <파일경로>  : 단일 스테이지의 최적 경로 출력 (설계 검토용)
//   <폴더>            : 해당 폴더 전수 검증
class Program
{
    static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--path")
            return PathMode(args[1]);
        if (args.Length >= 1 && args[0] == "--maps")
            return MapsMode();

        bool audit = args.Length > 0 && args[0] == "--consonants";
        string folder = args.Length > (audit ? 1 : 0) ? args[audit ? 1 : 0]
            : Path.Combine(RepoRoot(), "Assets", "Resources", "Stages");
        int ok = 0, updated = 0, bad = 0;

        foreach (string path in Directory.GetFiles(folder, "stage_*.json"))
        {
            string json = File.ReadAllText(path);
            var (stage, declared) = Load(json);

            if (audit)
            {
                Console.WriteLine($"{Path.GetFileName(path)}\t{stage.UsedBaseConsonants()}");
                continue;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var r = Solver.Solve(stage, maxStates: 5_000_000, timeBudgetMs: 120_000);
            sw.Stop();

            string name = Path.GetFileName(path);
            if (r.Aborted) { Console.WriteLine($"[중단] {name} 탐색 {r.ExploredStates} ({sw.ElapsedMilliseconds}ms)"); bad++; continue; }
            if (!r.Solvable) { Console.WriteLine($"[풀이불가] {name}"); bad++; continue; }

            if (declared != r.MinMoves)
            {
                string newJson = Regex.Replace(json, "\"minMoves\"\\s*:\\s*\\d+", $"\"minMoves\": {r.MinMoves}");
                File.WriteAllText(path, newJson);
                Console.WriteLine($"[기입] {name}: {declared} → {r.MinMoves} (상태 {r.ExploredStates}, {sw.ElapsedMilliseconds}ms)");
                updated++;
            }
            else
            {
                Console.WriteLine($"[일치] {name}: {r.MinMoves} ({sw.ElapsedMilliseconds}ms)");
                ok++;
            }
        }

        Console.WriteLine($"결과: 일치 {ok}, 기입 {updated}, 문제 {bad}");
        return bad > 0 ? 1 : 0;
    }

    // ── 맵/스테이지 무결성 검증 모드 ─────────────────────────────
    static int MapsMode()
    {
        string res = Path.Combine(RepoRoot(), "Assets", "Resources");
        var fails = new List<string>();

        var stageIds = new Dictionary<int, int>();
        foreach (string path in Directory.GetFiles(Path.Combine(res, "Stages"), "stage_*.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            int id = root.GetProperty("id").GetInt32();
            stageIds[id] = stageIds.TryGetValue(id, out int n) ? n + 1 : 1;
            var rows = root.GetProperty("rows");
            int w = new System.Globalization.StringInfo(rows[0].GetString()).LengthInTextElements;
            for (int i = 0; i < rows.GetArrayLength(); i++)
                if (new System.Globalization.StringInfo(rows[i].GetString()).LengthInTextElements != w)
                    fails.Add($"{Path.GetFileName(path)} rows 길이 불일치");
            if (root.TryGetProperty("pins", out var pins) && pins.ValueKind == JsonValueKind.Array)
            {
                if (pins.GetArrayLength() != rows.GetArrayLength()) fails.Add($"{Path.GetFileName(path)} pins 행 수 불일치");
                for (int i = 0; i < pins.GetArrayLength(); i++)
                    if (pins[i].GetString().Length != w) fails.Add($"{Path.GetFileName(path)} pins 길이 불일치 (행 {i})");
            }
            if (rows.GetArrayLength() > 5 || w > 5) fails.Add($"{Path.GetFileName(path)} 보드 5x5 초과");
        }
        foreach (var kv in stageIds) if (kv.Value > 1) fails.Add($"스테이지 id 중복: {kv.Key} x{kv.Value}");

        foreach (string path in Directory.GetFiles(Path.Combine(res, "Maps"), "map_*.json"))
        {
            string name = Path.GetFileName(path);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            // version 2(사이드뷰)는 v1 걷기 규칙('.'/'-')이 적용되지 않으므로 검증 스킵 (사이드뷰 전환 기획 3장)
            if (root.TryGetProperty("version", out var ver) && ver.GetInt32() >= 2)
            {
                Console.WriteLine($"[스킵] {name}: version {ver.GetInt32()} (사이드뷰) — v1 검증 규칙 미적용");
                continue;
            }

            var terrain = root.GetProperty("terrain");
            int h = terrain.GetArrayLength();
            var t = new string[h];
            for (int i = 0; i < h; i++) t[i] = terrain[i].GetString();
            int w = t[0].Length;
            for (int i = 0; i < h; i++)
                if (t[i].Length != w) fails.Add($"{name} terrain 행 {i} 길이 {t[i].Length} != {w}");

            void Walk(int x, int y, string what)
            {
                char c = (y >= 0 && y < h && x >= 0 && x < w) ? t[y][x] : '#';
                if (c != '.' && c != '-') fails.Add($"{name} {what} ({x},{y}) 걷기 불가 '{c}'");
            }

            var seen = new Dictionary<(int, int), int>();
            var spotIds = new HashSet<int>();
            if (root.TryGetProperty("spots", out var spots))
                foreach (var s in spots.EnumerateArray())
                {
                    int sid = s.GetProperty("stage").GetInt32();
                    int x = s.GetProperty("pos")[0].GetInt32(), y = s.GetProperty("pos")[1].GetInt32();
                    Walk(x, y, $"spot{sid}");
                    if (seen.TryGetValue((x, y), out int other)) fails.Add($"{name} 스팟 위치 중복: {sid} vs {other}");
                    seen[(x, y)] = sid;
                    spotIds.Add(sid);
                    if (!stageIds.ContainsKey(sid)) fails.Add($"{name} 스팟 {sid}: 스테이지 파일 없음");
                }
            if (root.TryGetProperty("exits", out var exits))
                foreach (var e in exits.EnumerateArray())
                    Walk(e.GetProperty("pos")[0].GetInt32(), e.GetProperty("pos")[1].GetInt32(), "exit");
            if (root.TryGetProperty("boss", out var boss) && boss.ValueKind == JsonValueKind.Object)
            {
                int x = boss.GetProperty("pos")[0].GetInt32(), y = boss.GetProperty("pos")[1].GetInt32();
                Walk(x, y, "boss");
                if (seen.ContainsKey((x, y))) fails.Add($"{name} 보스가 스팟과 겹침");
            }
            if (root.TryGetProperty("shop", out var shop) && shop.ValueKind == JsonValueKind.Array)
            {
                int x = shop[0].GetInt32(), y = shop[1].GetInt32();
                Walk(x, y, "shop");
                if (seen.ContainsKey((x, y))) fails.Add($"{name} 상점이 스팟과 겹침");
            }
            var spawn = root.GetProperty("spawn");
            Walk(spawn[0].GetInt32(), spawn[1].GetInt32(), "spawn");
            if (root.TryGetProperty("rooms", out var rooms))
                foreach (var r in rooms.EnumerateArray())
                    foreach (var sid in r.GetProperty("stages").EnumerateArray())
                        if (!spotIds.Contains(sid.GetInt32()))
                            fails.Add($"{name} room '{r.GetProperty("label").GetString()}' 스테이지 {sid.GetInt32()} 스팟 없음");
            if (root.TryGetProperty("tutorialStages", out var tut))
                foreach (var sid in tut.EnumerateArray())
                    if (!spotIds.Contains(sid.GetInt32()))
                        fails.Add($"{name} tutorialStage {sid.GetInt32()} 스팟 없음");
        }

        foreach (string f in fails) Console.WriteLine($"[FAIL] {f}");
        Console.WriteLine(fails.Count == 0 ? "전체 통과" : $"실패 {fails.Count}건");
        return fails.Count == 0 ? 0 : 1;
    }

    // 저장소 루트: 실행 파일 위치에서 위로 올라가며 Assets 폴더를 찾는다 (bin/Release 등 어디서 실행해도 동작)
    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets")))
            dir = dir.Parent;
        if (dir == null) throw new DirectoryNotFoundException("Assets 폴더를 찾지 못함 — 저장소 안에서 실행하세요.");
        return dir.FullName;
    }

    static (StageData, int) Load(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var rowsEl = root.GetProperty("rows");
        var rows = new string[rowsEl.GetArrayLength()];
        for (int i = 0; i < rows.Length; i++) rows[i] = rowsEl[i].GetString();

        string[] pins = null;
        if (root.TryGetProperty("pins", out var pinsEl) && pinsEl.ValueKind == JsonValueKind.Array)
        {
            pins = new string[pinsEl.GetArrayLength()];
            for (int i = 0; i < pins.Length; i++) pins[i] = pinsEl[i].GetString();
        }

        var goalsEl = root.GetProperty("goals");
        var goals = new GoalGroup[goalsEl.GetArrayLength()];
        for (int i = 0; i < goals.Length; i++)
        {
            string slots = goalsEl[i].GetProperty("slots").GetString();
            string display = goalsEl[i].TryGetProperty("display", out var d) ? d.GetString() : slots;
            goals[i] = new GoalGroup { display = display, slots = slots.ToCharArray() };
        }

        int declared = root.TryGetProperty("minMoves", out var mm) ? mm.GetInt32() : 0;
        return (StageBuilder.FromRows(rows, pins, goals), declared);
    }

    // ── 경로 출력 모드: 부모 포인터 0-1 BFS ─────────────────────────────
    static int PathMode(string file)
    {
        string json = File.ReadAllText(file);
        var (stage, _) = Load(json);
        int w = stage.width, h = stage.height, n = w * h;
        var slotList = stage.AllSlots();
        int slotCount = slotList.Count;

        string Encode(char[] cells, bool[] filled)
        {
            var arr = new char[n + slotCount];
            Array.Copy(cells, arr, n);
            for (int i = 0; i < slotCount; i++) arr[n + i] = filled[i] ? '1' : '0';
            return new string(arr);
        }

        var initCells = (char[])stage.cells.Clone();
        var initFilled = new bool[slotCount];
        string initial = Encode(initCells, initFilled);

        var dist = new Dictionary<string, int> { [initial] = 0 };
        var parent = new Dictionary<string, (string prev, string move)>();
        var deque = new LinkedList<(string state, int depth)>();
        deque.AddFirst((initial, 0));

        var cells = new char[n];
        var filled = new bool[slotCount];
        string goalState = null;

        void Relax(string next, int depth, bool front, string from, string move)
        {
            if (dist.TryGetValue(next, out int known) && known <= depth) return;
            dist[next] = depth;
            parent[next] = (from, move);
            if (front) deque.AddFirst((next, depth));
            else deque.AddLast((next, depth));
        }

        while (deque.Count > 0)
        {
            var (state, depth) = deque.First.Value;
            deque.RemoveFirst();
            if (depth > dist[state]) continue;

            bool all = true;
            for (int i = 0; i < slotCount; i++) if (state[n + i] != '1') { all = false; break; }
            if (all) { goalState = state; Console.WriteLine($"최소 수: {depth} (상태 {dist.Count})"); break; }

            for (int i = 0; i < n; i++) cells[i] = state[i];
            for (int i = 0; i < slotCount; i++) filled[i] = state[n + i] == '1';

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!stage.mask[y * w + x]) continue;
                char tile = cells[y * w + x];
                if (tile == Hangul.Empty) continue;
                int row = h - 1 - y; // JSON 행 표기

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

                    string arrow = d switch { Direction.Up => "↑", Direction.Down => "↓", Direction.Left => "←", _ => "→" };
                    Relax(next, depth + 1, false, state, $"{tile}({row},{x}){arrow}[{type}]");
                }

                char rotated = Hangul.RotateCw(tile);
                if (rotated != Hangul.Empty)
                {
                    cells[y * w + x] = rotated;
                    string next = Encode(cells, filled);
                    cells[y * w + x] = tile;
                    Relax(next, depth + 1, false, state, $"{tile}({row},{x})회전→{rotated}");
                }

                for (int i = 0; i < slotCount; i++)
                {
                    if (filled[i] || slotList[i] != tile) continue;
                    cells[y * w + x] = '\0';
                    filled[i] = true;
                    string next = Encode(cells, filled);
                    cells[y * w + x] = tile;
                    filled[i] = false;
                    Relax(next, depth, true, state, $"수집 {tile}({row},{x})");
                    break;
                }
            }

            if (dist.Count > 5_000_000) { Console.WriteLine("[중단] 상태 상한"); return 1; }
        }

        if (goalState == null) { Console.WriteLine("[풀이불가]"); return 1; }

        var moves = new List<string>();
        string cur = goalState;
        while (parent.TryGetValue(cur, out var p)) { moves.Add(p.move); cur = p.prev; }
        moves.Reverse();
        for (int i = 0; i < moves.Count; i++) Console.WriteLine($"{i + 1}. {moves[i]}");
        return 0;
    }
}
