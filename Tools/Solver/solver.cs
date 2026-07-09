// HangeulAdventure 스테이지 솔버 (BFS)
// 기준: Docs/퍼즐규칙명세.md (2026-07-09 확정본)
// 빌드: csc /codepage:65001 /out:solver.exe solver.cs   (.NET Framework 4 csc 호환, C# 5)
// 사용: solver.exe stage_001.json stage_002.json ...
//       각 스테이지의 풀이 가능성, 최소 수, 최적 경로 1개를 UTF-8 stdout으로 출력
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Dec {
    public int Axis;   // 0=가로, 1=세로
    public char L;     // 왼쪽/위 성분
    public char R;     // 오른쪽/아래 성분
    public Dec(int axis, char l, char r) { Axis = axis; L = l; R = r; }
}

class Stage {
    public string File;
    public string[] Rows;
    public string Slots; // 모든 goals의 slots 문자를 이어붙인 것
}

class Solver {
    const string CHO  = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
    const string JUNG = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
    const string JONGT = "?ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ"; // [0]은 자리표시자
    const string V_VERT    = "ㅏㅑㅓㅕㅣㅐㅒㅔㅖ";
    const string V_HORIZ   = "ㅗㅛㅜㅠㅡ";
    const string V_COMPLEX = "ㅘㅙㅚㅝㅞㅟㅢ";

    static Dictionary<string, char> pairs = new Dictionary<string, char>();
    static Dictionary<char, string> pairRev = new Dictionary<char, string>();

    static void AddPair(string ab, char r) { pairs[ab] = r; pairRev[r] = ab; }

    static Solver() {
        // 이중모음 표준 쌍
        AddPair("ㅏㅣ", 'ㅐ'); AddPair("ㅑㅣ", 'ㅒ'); AddPair("ㅓㅣ", 'ㅔ'); AddPair("ㅕㅣ", 'ㅖ');
        AddPair("ㅗㅏ", 'ㅘ'); AddPair("ㅗㅐ", 'ㅙ'); AddPair("ㅗㅣ", 'ㅚ');
        AddPair("ㅜㅓ", 'ㅝ'); AddPair("ㅜㅔ", 'ㅞ'); AddPair("ㅜㅣ", 'ㅟ'); AddPair("ㅡㅣ", 'ㅢ');
        // 쌍자음/겹받침 표준 쌍
        AddPair("ㄱㄱ", 'ㄲ'); AddPair("ㄷㄷ", 'ㄸ'); AddPair("ㅂㅂ", 'ㅃ'); AddPair("ㅅㅅ", 'ㅆ'); AddPair("ㅈㅈ", 'ㅉ');
        AddPair("ㄱㅅ", 'ㄳ'); AddPair("ㄴㅈ", 'ㄵ'); AddPair("ㄴㅎ", 'ㄶ');
        AddPair("ㄹㄱ", 'ㄺ'); AddPair("ㄹㅁ", 'ㄻ'); AddPair("ㄹㅂ", 'ㄼ'); AddPair("ㄹㅅ", 'ㄽ');
        AddPair("ㄹㅌ", 'ㄾ'); AddPair("ㄹㅍ", 'ㄿ'); AddPair("ㄹㅎ", 'ㅀ');
    }

    static bool IsSyl(char c) { return c >= 0xAC00 && c <= 0xD7A3; }
    static bool IsCons(char c) { return c >= 0x3131 && c <= 0x314E; }
    static bool IsVowel(char c) { return c >= 0x314F && c <= 0x3163; }

    static void SylParts(char c, out int cho, out int jung, out int jong) {
        int idx = c - 0xAC00;
        cho = idx / 588; jung = (idx % 588) / 28; jong = idx % 28;
    }

    static char ComposeSyl(char cho, char jung, char jong) {
        int ci = CHO.IndexOf(cho);
        int ji = JUNG.IndexOf(jung);
        if (ci < 0 || ji < 0) return '\0';
        int ki = 0;
        if (jong != '\0') { ki = JONGT.IndexOf(jong); if (ki < 1) return '\0'; }
        return (char)(0xAC00 + ci * 588 + ji * 28 + ki);
    }

    // 겉조합 목록 (표준 구조의 최상위 한 단계)
    static List<Dec> OuterDecs(char t) {
        List<Dec> list = new List<Dec>();
        if (IsSyl(t)) {
            int cho, jung, jong;
            SylParts(t, out cho, out jung, out jong);
            char choC = CHO[cho];
            char jungC = JUNG[jung];
            if (jong > 0) {
                char plain = (char)(t - jong); // 종성 제거한 민글자
                list.Add(new Dec(1, plain, JONGT[jong]));
            } else if (V_COMPLEX.IndexOf(jungC) >= 0) {
                list.Add(new Dec(0, choC, jungC)); // 가로: 자음 왼쪽
                list.Add(new Dec(1, choC, jungC)); // 세로: 자음 위
            } else if (V_VERT.IndexOf(jungC) >= 0) {
                list.Add(new Dec(0, choC, jungC));
            } else { // 가로형 모음
                list.Add(new Dec(1, choC, jungC));
            }
        } else if (pairRev.ContainsKey(t)) {
            string ab = pairRev[t];
            list.Add(new Dec(0, ab[0], ab[1])); // 복합 자모는 전부 가로 조합
        }
        return list;
    }

    // 타일 구조 안에 axis 방향 조합이 하나라도 있는가 (이동 가능성 판정)
    static bool HasCombo(char t, int axis) {
        foreach (Dec d in OuterDecs(t)) {
            if (d.Axis == axis) return true;
            if (HasCombo(d.L, axis) || HasCombo(d.R, axis)) return true;
        }
        return false;
    }

    // a가 왼쪽/위, b가 오른쪽/아래일 때의 합성 결과 ('\0'=불가)
    static char Merge(char a, char b, int axis) {
        if (axis == 0) {
            if (IsCons(a) && CHO.IndexOf(a) >= 0 && IsVowel(b) &&
                (V_VERT.IndexOf(b) >= 0 || V_COMPLEX.IndexOf(b) >= 0))
                return ComposeSyl(a, b, '\0');
            string k = "" + a + b;
            if (IsVowel(a) && IsVowel(b) && pairs.ContainsKey(k)) return pairs[k];
            if (IsCons(a) && IsCons(b) && pairs.ContainsKey(k)) return pairs[k];
        } else {
            if (IsCons(a) && CHO.IndexOf(a) >= 0 && IsVowel(b) &&
                (V_HORIZ.IndexOf(b) >= 0 || V_COMPLEX.IndexOf(b) >= 0))
                return ComposeSyl(a, b, '\0');
            if (IsSyl(a)) {
                int cho, jung, jong;
                SylParts(a, out cho, out jung, out jong);
                if (jong == 0 && IsCons(b) && JONGT.IndexOf(b) >= 1)
                    return (char)(a + JONGT.IndexOf(b));
            }
        }
        return '\0';
    }

    // dir: 0=왼쪽 1=오른쪽 2=위 3=아래
    static readonly int[] DR = { 0, 0, -1, 1 };
    static readonly int[] DC = { -1, 1, 0, 0 };
    static readonly string[] DIRNAME = { "왼쪽", "오른쪽", "위", "아래" };

    // 밀기 판정. 성공 시 새 보드와 설명 반환, 실패 시 null.
    static char[] Push(char[] board, int w, int h, int r, int c, int dir, out string note) {
        note = null;
        char t = board[r * w + c];
        if (t == '.' || t == '#') return null;
        int nr = r + DR[dir], nc = c + DC[dir];
        if (nr < 0 || nr >= h || nc < 0 || nc >= w) return null;
        char u = board[nr * w + nc];
        if (u == '#') return null;
        int axis = dir < 2 ? 0 : 1;
        bool tFirst = (dir == 1 || dir == 3); // T가 왼쪽/위 성분이 되는 방향인가

        if (u != '.') {
            // 1. 합성
            char m = tFirst ? Merge(t, u, axis) : Merge(u, t, axis);
            if (m != '\0') {
                char[] nb = (char[])board.Clone();
                nb[r * w + c] = '.';
                nb[nr * w + nc] = m;
                note = "합성→" + m;
                return nb;
            }
            // 2. 분해 + 연쇄 합성
            foreach (Dec d in OuterDecs(t)) {
                if (d.Axis != axis) continue;
                char exiting = (dir == 0 || dir == 2) ? d.L : d.R;
                char remain = (dir == 0 || dir == 2) ? d.R : d.L;
                char m2 = tFirst ? Merge(exiting, u, axis) : Merge(u, exiting, axis);
                if (m2 != '\0') {
                    char[] nb = (char[])board.Clone();
                    nb[r * w + c] = remain;
                    nb[nr * w + nc] = m2;
                    note = "분해+연쇄합성→" + remain + "," + m2;
                    return nb;
                }
                return null; // 나간 성분이 안착 못 함 → 밀기 전체 실패
            }
            return null;
        } else {
            // 2. 분해 (빈칸으로)
            foreach (Dec d in OuterDecs(t)) {
                if (d.Axis != axis) continue;
                char exiting = (dir == 0 || dir == 2) ? d.L : d.R;
                char remain = (dir == 0 || dir == 2) ? d.R : d.L;
                char[] nb = (char[])board.Clone();
                nb[r * w + c] = remain;
                nb[nr * w + nc] = exiting;
                note = "분해→" + remain + "," + exiting;
                return nb;
            }
            // 3. 이동
            if (!HasCombo(t, axis)) {
                char[] nb = (char[])board.Clone();
                nb[r * w + c] = '.';
                nb[nr * w + nc] = t;
                note = "이동";
                return nb;
            }
            return null;
        }
    }

    static string SortStr(string s) {
        char[] a = s.ToCharArray();
        Array.Sort(a);
        return new string(a);
    }

    class Node {
        public string Prev;
        public string Act;
        public int Depth;
    }

    static void Solve(Stage st) {
        int h = st.Rows.Length;
        int w = st.Rows[0].Length;
        foreach (string row in st.Rows) {
            if (row.Length != w) { Console.WriteLine("!! 행 길이 불일치: " + st.File); return; }
        }
        char[] board = new char[w * h];
        for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
                board[r * w + c] = st.Rows[r][c];

        string goals = SortStr(st.Slots);
        string startKey = new string(board) + "|" + goals;

        Dictionary<string, Node> visited = new Dictionary<string, Node>();
        Queue<string> queue = new Queue<string>();
        visited[startKey] = new Node { Prev = null, Act = null, Depth = 0 };
        queue.Enqueue(startKey);
        string goalKey = null;
        int expanded = 0;

        while (queue.Count > 0) {
            string key = queue.Dequeue();
            Node node = visited[key];
            int sep = key.LastIndexOf('|');
            string boardStr = key.Substring(0, sep);
            string remain = key.Substring(sep + 1);
            if (remain.Length == 0) { goalKey = key; break; }
            expanded++;
            if (expanded > 3000000) { Console.WriteLine("!! 상태 폭발(300만 초과): " + st.File); return; }
            char[] cur = boardStr.ToCharArray();

            // 수집
            for (int i = 0; i < cur.Length; i++) {
                char t = cur[i];
                if (t == '.' || t == '#') continue;
                int gi = remain.IndexOf(t);
                if (gi < 0) continue;
                char[] nb = (char[])cur.Clone();
                nb[i] = '.';
                string nremain = remain.Remove(gi, 1);
                string nkey = new string(nb) + "|" + nremain;
                if (!visited.ContainsKey(nkey)) {
                    visited[nkey] = new Node {
                        Prev = key,
                        Act = "수집 " + t + " @(" + (i / w) + "," + (i % w) + ")",
                        Depth = node.Depth + 1
                    };
                    queue.Enqueue(nkey);
                }
            }
            // 밀기
            for (int r = 0; r < h; r++) for (int c = 0; c < w; c++) {
                char t = cur[r * w + c];
                if (t == '.' || t == '#') continue;
                for (int dir = 0; dir < 4; dir++) {
                    string note;
                    char[] nb = Push(cur, w, h, r, c, dir, out note);
                    if (nb == null) continue;
                    string nkey = new string(nb) + "|" + remain;
                    if (!visited.ContainsKey(nkey)) {
                        visited[nkey] = new Node {
                            Prev = key,
                            Act = t + " @(" + r + "," + c + ") " + DIRNAME[dir] + " [" + note + "]",
                            Depth = node.Depth + 1
                        };
                        queue.Enqueue(nkey);
                    }
                }
            }
        }

        Console.WriteLine("=== " + Path.GetFileName(st.File) + " ===");
        for (int r = 0; r < h; r++) Console.WriteLine("  " + st.Rows[r]);
        Console.WriteLine("  목표: " + st.Slots);
        if (goalKey == null) {
            Console.WriteLine("  풀이 불가! (탐색 상태 " + visited.Count + "개)");
            Console.WriteLine();
            return;
        }
        // 경로 복원
        List<string> path = new List<string>();
        string k2 = goalKey;
        while (visited[k2].Prev != null) {
            path.Add(visited[k2].Act);
            k2 = visited[k2].Prev;
        }
        path.Reverse();
        Console.WriteLine("  최소 수: " + path.Count + " (탐색 상태 " + visited.Count + "개)");
        for (int i = 0; i < path.Count; i++)
            Console.WriteLine("  " + (i + 1) + ". " + path[i]);
        Console.WriteLine();
    }

    // 최소한의 JSON 스캐너 (본 프로젝트 스테이지 파일 형식 전용)
    static Stage ParseStage(string file) {
        string txt = File.ReadAllText(file);
        Stage st = new Stage();
        st.File = file;
        List<string> rows = new List<string>();
        int ri = txt.IndexOf("\"rows\"");
        int p = txt.IndexOf('[', ri);
        int end = txt.IndexOf(']', p);
        while (true) {
            int q1 = txt.IndexOf('"', p + 1);
            if (q1 < 0 || q1 > end) break;
            int q2 = txt.IndexOf('"', q1 + 1);
            rows.Add(txt.Substring(q1 + 1, q2 - q1 - 1));
            p = q2;
        }
        st.Rows = rows.ToArray();
        StringBuilder slots = new StringBuilder();
        int si = 0;
        while (true) {
            si = txt.IndexOf("\"slots\"", si);
            if (si < 0) break;
            int q1 = txt.IndexOf('"', si + 7);
            int q2 = txt.IndexOf('"', q1 + 1);
            slots.Append(txt.Substring(q1 + 1, q2 - q1 - 1));
            si = q2;
        }
        st.Slots = slots.ToString();
        return st;
    }

    static void Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        foreach (string f in args) Solve(ParseStage(f));
    }
}
