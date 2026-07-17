using NUnit.Framework;
using HangeulAdventure.Engine;

namespace HangeulAdventure.Engine.Tests
{
    public class HangulTests
    {
        [Test]
        public void 음절_조합과_분해_왕복()
        {
            Assert.AreEqual('가', Hangul.ComposeSyllable('ㄱ', 'ㅏ'));
            Assert.AreEqual('간', Hangul.ComposeSyllable('ㄱ', 'ㅏ', 'ㄴ'));
            Assert.AreEqual('꽝', Hangul.ComposeSyllable('ㄲ', 'ㅘ', 'ㅇ'));
            Assert.AreEqual('갃', Hangul.ComposeSyllable('ㄱ', 'ㅏ', 'ㄳ'));

            Assert.AreEqual(('ㄱ', 'ㅏ', '\0'), Hangul.DecomposeSyllable('가'));
            Assert.AreEqual(('ㄱ', 'ㅏ', 'ㄴ'), Hangul.DecomposeSyllable('간'));
            Assert.AreEqual(('ㄲ', 'ㅘ', 'ㅇ'), Hangul.DecomposeSyllable('꽝'));
            Assert.AreEqual(('ㄱ', 'ㅏ', 'ㄳ'), Hangul.DecomposeSyllable('갃'));
        }

        [Test]
        public void 전체_음절_왕복_불변식()
        {
            // 11172개 전 음절에 대해 분해→조합이 원본과 일치
            for (char s = (char)0xAC00; s <= (char)0xD7A3; s++)
            {
                var (cho, jung, jong) = Hangul.DecomposeSyllable(s);
                Assert.AreEqual(s, Hangul.ComposeSyllable(cho, jung, jong), $"왕복 실패: {s}");
            }
        }

        [Test]
        public void 모음_분류()
        {
            foreach (char c in "ㅏㅑㅓㅕㅣㅐㅒㅔㅖ") Assert.IsTrue(Hangul.IsVerticalVowel(c), c.ToString());
            foreach (char c in "ㅗㅛㅜㅠㅡ") Assert.IsTrue(Hangul.IsHorizontalVowel(c), c.ToString());
            foreach (char c in "ㅘㅙㅚㅝㅞㅟㅢ") Assert.IsTrue(Hangul.IsWrapVowel(c), c.ToString());
        }

        [Test]
        public void 역할_제약_초성_종성()
        {
            // ㄸㅃㅉ: 초성 전용
            foreach (char c in "ㄸㅃㅉ")
            {
                Assert.IsTrue(Hangul.CanBeChoseong(c), c.ToString());
                Assert.IsFalse(Hangul.CanBeJongseong(c), c.ToString());
            }
            // 겹받침: 종성 전용
            foreach (char c in "ㄳㄵㄶㄺㄻㄼㄽㄾㄿㅀㅄ")
            {
                Assert.IsFalse(Hangul.CanBeChoseong(c), c.ToString());
                Assert.IsTrue(Hangul.CanBeJongseong(c), c.ToString());
            }
            // ㄲㅆ: 겸용
            foreach (char c in "ㄲㅆ")
            {
                Assert.IsTrue(Hangul.CanBeChoseong(c), c.ToString());
                Assert.IsTrue(Hangul.CanBeJongseong(c), c.ToString());
            }
        }

        [Test]
        public void 이중모음_표준쌍만_결합()
        {
            Assert.AreEqual('ㅘ', Hangul.CombineVowels('ㅗ', 'ㅏ'));
            Assert.AreEqual('ㅙ', Hangul.CombineVowels('ㅗ', 'ㅐ'));
            Assert.AreEqual('ㅞ', Hangul.CombineVowels('ㅜ', 'ㅔ'));
            Assert.AreEqual('ㅐ', Hangul.CombineVowels('ㅏ', 'ㅣ'));
            Assert.AreEqual('ㅢ', Hangul.CombineVowels('ㅡ', 'ㅣ'));

            // 비표준 경로 금지 (명세 3장)
            Assert.AreEqual('\0', Hangul.CombineVowels('ㅝ', 'ㅣ'), "ㅝ+ㅣ→ㅞ 불가");
            Assert.AreEqual('\0', Hangul.CombineVowels('ㅘ', 'ㅣ'), "ㅘ+ㅣ→ㅙ 불가");
            // 역순 금지
            Assert.AreEqual('\0', Hangul.CombineVowels('ㅏ', 'ㅗ'));
            Assert.AreEqual('\0', Hangul.CombineVowels('ㅣ', 'ㅏ'));
        }

        [Test]
        public void 자음_표준쌍만_결합()
        {
            Assert.AreEqual('ㄲ', Hangul.CombineConsonants('ㄱ', 'ㄱ'));
            Assert.AreEqual('ㄻ', Hangul.CombineConsonants('ㄹ', 'ㅁ'));
            Assert.AreEqual('ㅄ', Hangul.CombineConsonants('ㅂ', 'ㅅ'));

            // 실제 한글에 없는 조합 금지 (폐기된 아이디어: ㄱ+ㄷ=ㄹ 등)
            Assert.AreEqual('\0', Hangul.CombineConsonants('ㄱ', 'ㄷ'));
            Assert.AreEqual('\0', Hangul.CombineConsonants('ㄷ', 'ㄴ'));
            // 역순 금지
            Assert.AreEqual('\0', Hangul.CombineConsonants('ㅅ', 'ㄱ'));
            Assert.AreEqual('\0', Hangul.CombineConsonants('ㅁ', 'ㄹ'));
        }

        [Test]
        public void 복합자모_분해쌍()
        {
            Assert.IsTrue(Hangul.TrySplitCompound('ㅞ', out char l, out char r));
            Assert.AreEqual(('ㅜ', 'ㅔ'), (l, r));
            Assert.IsTrue(Hangul.TrySplitCompound('ㄳ', out l, out r));
            Assert.AreEqual(('ㄱ', 'ㅅ'), (l, r));
            Assert.IsFalse(Hangul.TrySplitCompound('ㄱ', out _, out _));
            Assert.IsFalse(Hangul.TrySplitCompound('ㅏ', out _, out _));
        }

        [Test]
        public void 조합축_분석()
        {
            // 가: 가로만
            Assert.AreEqual((true, false), Hangul.CompositionAxes('가'));
            // 고: 세로만
            Assert.AreEqual((false, true), Hangul.CompositionAxes('고'));
            // 간: 양축 (겉 받침=세로, 속 ㄱ+ㅏ=가로)
            Assert.AreEqual((true, true), Hangul.CompositionAxes('간'));
            // 곤: 세로만 (받침 + 세로 조합 민글자) → 가로 이동 가능
            Assert.AreEqual((false, true), Hangul.CompositionAxes('곤'));
            // 과: 양축 (복합모음)
            Assert.AreEqual((true, true), Hangul.CompositionAxes('과'));
            // 개: 가로만 (ㅐ 내부도 가로)
            Assert.AreEqual((true, false), Hangul.CompositionAxes('개'));
            // 꼬: 세로만 (겉조합 ㄲ위+ㅗ아래) → 가로 이동 가능. ㄲ 내부 가로는 축 판정에 무관 (명세 5장 표 106행)
            Assert.AreEqual((false, true), Hangul.CompositionAxes('꼬'));
            // 몫: 세로만 (겉조합 받침 + 속 ㅁ위+ㅗ아래) → 가로 이동 가능. ㄳ 내부 가로도 무관 (표 108행)
            Assert.AreEqual((false, true), Hangul.CompositionAxes('몫'));
            // 값: 양축 (받침=세로 + 속 ㄱ+ㅏ=가로) → 겹받침이어도 속이 가로면 잠김 (표 107행)
            Assert.AreEqual((true, true), Hangul.CompositionAxes('값'));
            // 낱자모: 없음
            Assert.AreEqual((false, false), Hangul.CompositionAxes('ㄱ'));
            Assert.AreEqual((false, false), Hangul.CompositionAxes('ㅗ'));
            // 복합 자모: 가로만
            Assert.AreEqual((true, false), Hangul.CompositionAxes('ㄲ'));
            Assert.AreEqual((true, false), Hangul.CompositionAxes('ㅘ'));
        }
    }
}
