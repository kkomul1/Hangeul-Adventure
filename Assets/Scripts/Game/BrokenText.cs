using System.Text;
using HangeulAdventure.Engine;

namespace HangeulAdventure.Game
{
    /// <summary>
    /// 깨진 글자 연출 (스토리기획 2장): 아직 되찾지 못한 자음이 초성·종성에 포함된 글자는
    /// 깨진 형태(▒)로 표시된다. 자음을 회수할수록 세상의 글자가 복원된다.
    /// </summary>
    public static class BrokenText
    {
        private const string BrokenGlyph = "<color=#5A544C>▒</color>";

        /// <summary>자음(복합 자모 포함)이 회수되었는가. ㄲ은 ㄱ 회수로 취급, 겹받침은 두 성분 모두 필요.</summary>
        private static bool IsRecovered(char consonant, string recovered)
        {
            if (consonant == '\0') return true;
            if (recovered.IndexOf(consonant) >= 0) return true;
            if (Hangul.TrySplitCompound(consonant, out char l, out char r))
                return IsRecovered(l, recovered) && IsRecovered(r, recovered);
            return false;
        }

        public static bool IsBroken(char c)
        {
            if (!Hangul.IsSyllable(c)) return false;
            string recovered = ProgressStore.RecoveredConsonants;
            var (cho, _, jong) = Hangul.DecomposeSyllable(c);
            return !IsRecovered(cho, recovered) || !IsRecovered(jong, recovered);
        }

        /// <summary>문자열의 깨진 글자를 ▒(리치 텍스트)로 치환.</summary>
        public static string Apply(string text)
        {
            var sb = new StringBuilder(text.Length + 16);
            foreach (char c in text)
                sb.Append(IsBroken(c) ? BrokenGlyph : c.ToString());
            return sb.ToString();
        }
    }
}
