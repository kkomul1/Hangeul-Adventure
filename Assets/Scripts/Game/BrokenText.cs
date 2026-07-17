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

        /// <summary>
        /// 깨진 글자를 &lt;link="brk"&gt; 태그로 감싼다 (원형 유지 — M4-3 보드 결정 "D3 파편 강").
        /// 실제 파편 연출은 해당 TMP에 붙은 BrokenTextFx가 그린다. 태그는 레이아웃에 영향이 없어
        /// 문자열 합성(제목+테마 등) 어디에 끼어도 안전하며, 연출 컴포넌트가 없으면 원형이 보인다
        /// — 표시 대상 TMP에는 BrokenTextFx.Ensure()를 붙일 것.
        /// </summary>
        public static string Apply(string text)
        {
            var sb = new StringBuilder(text.Length + 16);
            foreach (char c in text)
            {
                if (IsBroken(c)) sb.Append("<link=\"brk\">").Append(c).Append("</link>");
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
