using System.Collections.Generic;

namespace HangeulAdventure.Engine
{
    /// <summary>
    /// 한글 자모 분류, 표준 조합쌍, 음절 조합/분해 (퍼즐규칙명세 2~3장).
    /// 모든 타일은 char 하나로 표현된다: 호환 자모(U+3131~) 또는 완성 음절(U+AC00~).
    /// </summary>
    public static class Hangul
    {
        public const char Empty = '\0';

        // 유니코드 음절 조합 순서와 동일한 인덱스 테이블
        public const string Choseong = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";           // 19
        public const string Jungseong = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";        // 21
        public const string Jongseong = "ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ"; // 27 (음절 종성 인덱스 = 위치+1)

        // 모음 분류 (명세 4장)
        private static readonly HashSet<char> VerticalVowels = new HashSet<char>("ㅏㅑㅓㅕㅣㅐㅒㅔㅖ"); // 자음이 왼쪽에서 결합
        private static readonly HashSet<char> HorizontalVowels = new HashSet<char>("ㅗㅛㅜㅠㅡ");         // 자음이 위에서 결합
        private static readonly HashSet<char> WrapVowels = new HashSet<char>("ㅘㅙㅚㅝㅞㅟㅢ");           // 양축(위/왼쪽) 결합

        private static readonly HashSet<char> AllConsonants = new HashSet<char>("ㄱㄲㄳㄴㄵㄶㄷㄸㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅃㅄㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ");
        private static readonly HashSet<char> AllVowels = new HashSet<char>("ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ");

        // 표준 조합쌍 (명세 3장) — 이 쌍으로만 합성/분해된다
        private static readonly Dictionary<int, char> VowelPairs = new Dictionary<int, char>
        {
            { Key('ㅏ','ㅣ'), 'ㅐ' }, { Key('ㅑ','ㅣ'), 'ㅒ' }, { Key('ㅓ','ㅣ'), 'ㅔ' }, { Key('ㅕ','ㅣ'), 'ㅖ' },
            { Key('ㅗ','ㅏ'), 'ㅘ' }, { Key('ㅗ','ㅐ'), 'ㅙ' }, { Key('ㅗ','ㅣ'), 'ㅚ' },
            { Key('ㅜ','ㅓ'), 'ㅝ' }, { Key('ㅜ','ㅔ'), 'ㅞ' }, { Key('ㅜ','ㅣ'), 'ㅟ' }, { Key('ㅡ','ㅣ'), 'ㅢ' },
        };

        private static readonly Dictionary<int, char> ConsonantPairs = new Dictionary<int, char>
        {
            { Key('ㄱ','ㄱ'), 'ㄲ' }, { Key('ㄷ','ㄷ'), 'ㄸ' }, { Key('ㅂ','ㅂ'), 'ㅃ' }, { Key('ㅅ','ㅅ'), 'ㅆ' }, { Key('ㅈ','ㅈ'), 'ㅉ' },
            { Key('ㄱ','ㅅ'), 'ㄳ' }, { Key('ㄴ','ㅈ'), 'ㄵ' }, { Key('ㄴ','ㅎ'), 'ㄶ' },
            { Key('ㄹ','ㄱ'), 'ㄺ' }, { Key('ㄹ','ㅁ'), 'ㄻ' }, { Key('ㄹ','ㅂ'), 'ㄼ' }, { Key('ㄹ','ㅅ'), 'ㄽ' },
            { Key('ㄹ','ㅌ'), 'ㄾ' }, { Key('ㄹ','ㅍ'), 'ㄿ' }, { Key('ㄹ','ㅎ'), 'ㅀ' }, { Key('ㅂ','ㅅ'), 'ㅄ' },
        };

        // 역방향 (분해용): 복합 자모 → (왼쪽 성분, 오른쪽 성분)
        private static readonly Dictionary<char, (char left, char right)> CompoundSplit = new Dictionary<char, (char, char)>();

        static Hangul()
        {
            foreach (var kv in VowelPairs) CompoundSplit[kv.Value] = UnKey(kv.Key);
            foreach (var kv in ConsonantPairs) CompoundSplit[kv.Value] = UnKey(kv.Key);
        }

        private static int Key(char a, char b) => (a << 16) | b;
        private static (char, char) UnKey(int k) => ((char)(k >> 16), (char)(k & 0xFFFF));

        // ---- 분류 ----

        public static bool IsConsonant(char c) => AllConsonants.Contains(c);
        public static bool IsVowel(char c) => AllVowels.Contains(c);
        public static bool IsSyllable(char c) => c >= 0xAC00 && c <= 0xD7A3;
        public static bool IsTile(char c) => c != Empty && (IsConsonant(c) || IsVowel(c) || IsSyllable(c));

        public static bool IsVerticalVowel(char c) => VerticalVowels.Contains(c);
        public static bool IsHorizontalVowel(char c) => HorizontalVowels.Contains(c);
        public static bool IsWrapVowel(char c) => WrapVowels.Contains(c);

        /// <summary>초성으로 쓸 수 있는가 (겹받침 불가, ㄸㅃㅉ 가능)</summary>
        public static bool CanBeChoseong(char c) => ChoIndex.ContainsKey(c);

        /// <summary>종성으로 쓸 수 있는가 (ㄸㅃㅉ 불가, 겹받침 가능)</summary>
        public static bool CanBeJongseong(char c) => JongIndex.ContainsKey(c);

        public static bool IsCompoundJamo(char c) => CompoundSplit.ContainsKey(c);

        // ---- 음절 조합/분해 ----

        // 솔버 핫패스용 인덱스 캐시 (IndexOf 선형 탐색 제거)
        private static readonly Dictionary<char, int> ChoIndex = BuildIndex(Choseong);
        private static readonly Dictionary<char, int> JungIndex = BuildIndex(Jungseong);
        private static readonly Dictionary<char, int> JongIndex = BuildIndex(Jongseong);

        private static Dictionary<char, int> BuildIndex(string s)
        {
            var d = new Dictionary<char, int>(s.Length);
            for (int i = 0; i < s.Length; i++) d[s[i]] = i;
            return d;
        }

        public static char ComposeSyllable(char cho, char jung, char jong = Empty)
        {
            if (!ChoIndex.TryGetValue(cho, out int ci)) return Empty;
            if (!JungIndex.TryGetValue(jung, out int vi)) return Empty;
            int ji = 0;
            if (jong != Empty)
            {
                if (!JongIndex.TryGetValue(jong, out int j)) return Empty;
                ji = j + 1;
            }
            return (char)(0xAC00 + (ci * 21 + vi) * 28 + ji);
        }

        /// <summary>음절 → (초성, 중성, 종성). 종성 없으면 '\0'. 음절이 아니면 전부 '\0'.</summary>
        public static (char cho, char jung, char jong) DecomposeSyllable(char s)
        {
            if (!IsSyllable(s)) return (Empty, Empty, Empty);
            int v = s - 0xAC00;
            int ji = v % 28;
            int vi = (v / 28) % 21;
            int ci = v / 28 / 21;
            return (Choseong[ci], Jungseong[vi], ji == 0 ? Empty : Jongseong[ji - 1]);
        }

        public static bool HasJongseong(char s) => IsSyllable(s) && (s - 0xAC00) % 28 != 0;

        // ---- 자모 결합 (표준 쌍) ----

        /// <summary>모음+모음 (가로, 표준 쌍만). 실패 시 '\0'.</summary>
        public static char CombineVowels(char left, char right)
            => VowelPairs.TryGetValue(Key(left, right), out char r) ? r : Empty;

        /// <summary>자음+자음 (가로, 표준 쌍만). 실패 시 '\0'.</summary>
        public static char CombineConsonants(char left, char right)
            => ConsonantPairs.TryGetValue(Key(left, right), out char r) ? r : Empty;

        /// <summary>복합 자모의 표준 분해쌍. 없으면 false.</summary>
        public static bool TrySplitCompound(char c, out char left, out char right)
        {
            if (CompoundSplit.TryGetValue(c, out var pair)) { left = pair.left; right = pair.right; return true; }
            left = right = Empty;
            return false;
        }

        // ---- 회전 (초안 4장 채용, D-21: 유효한 자모가 되는 회전만 허용) ----

        /// <summary>시계 방향 90도 회전. 유효한 자모가 되지 않으면 '\0' (회전 불가).</summary>
        public static char RotateCw(char c) => c switch
        {
            'ㅏ' => 'ㅜ', 'ㅜ' => 'ㅓ', 'ㅓ' => 'ㅗ', 'ㅗ' => 'ㅏ',
            'ㅑ' => 'ㅠ', 'ㅠ' => 'ㅕ', 'ㅕ' => 'ㅛ', 'ㅛ' => 'ㅑ',
            'ㅣ' => 'ㅡ', 'ㅡ' => 'ㅣ',
            _ => Empty, // 자음·복합모음·글자·바위는 회전 불가
        };

        /// <summary>바위 (밀기·합성·수집·회전 전부 불가한 장애물 타일).</summary>
        public const char Rock = '@';

        // ---- 축 분석 (이동 잠김 판정용, 명세 5장) ----

        /// <summary>
        /// 타일의 전체 조합 트리에 가로/세로 조합이 존재하는지. (한 단계라도 있으면 그 축으로 통째 이동 불가)
        /// </summary>
        public static (bool hasHorizontal, bool hasVertical) CompositionAxes(char t)
        {
            bool h = false, v = false;
            Accumulate(t, ref h, ref v);
            return (h, v);
        }

        private static void Accumulate(char t, ref bool h, ref bool v)
        {
            if (IsSyllable(t))
            {
                var (cho, jung, jong) = DecomposeSyllable(t);
                if (jong != Empty) v = true;                       // 받침 결합은 세로
                if (IsVerticalVowel(jung)) h = true;               // 자음+세로모음은 가로
                else if (IsHorizontalVowel(jung)) v = true;        // 자음+가로모음은 세로
                else if (IsWrapVowel(jung)) { h = true; v = true; } // 복합모음은 양축
                Accumulate(cho, ref h, ref v);
                Accumulate(jung, ref h, ref v);
                if (jong != Empty) Accumulate(jong, ref h, ref v);
            }
            else if (IsCompoundJamo(t))
            {
                h = true; // 복합 자모는 전부 가로 결합
                var (l, r) = CompoundSplit[t];
                Accumulate(l, ref h, ref v);
                Accumulate(r, ref h, ref v);
            }
        }
    }
}
