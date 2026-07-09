namespace HangeulAdventure.Engine
{
    public enum Direction { Up, Down, Left, Right }

    public enum PushResultType
    {
        Fail,          // 아무 일도 일어나지 않음 (이동 수 미증가)
        Move,          // 통째 이동
        Compose,       // 도착지 타일과 합성
        SplitMove,     // 분해, 나간 성분이 빈칸에 안착
        SplitCompose,  // 분해 + 나간 성분이 도착지 타일과 연쇄 합성
    }

    public static class DirectionUtil
    {
        public static (int dx, int dy) Delta(this Direction d) => d switch
        {
            Direction.Up => (0, 1),
            Direction.Down => (0, -1),
            Direction.Left => (-1, 0),
            _ => (1, 0),
        };

        public static bool IsHorizontal(this Direction d) => d == Direction.Left || d == Direction.Right;
    }

    /// <summary>
    /// 스테이지별 특수 규칙 플래그 (명세 11장 — MVP에서는 전부 기본값).
    /// 엔진에 자리만 확보해두어 이후 규칙 추가 시 시그니처 변경을 피한다.
    /// </summary>
    [System.Serializable]
    public struct RuleFlags
    {
        public bool disableMove;    // 이동 금지
        public bool disableSplit;   // 분해 금지
        public static RuleFlags Default => default;
    }

    /// <summary>
    /// 밀기 4단계 판정 (퍼즐규칙명세 5장). 보드와 무관한 순수 함수:
    /// (미는 타일, 도착지 상태, 방향) → 결과.
    /// </summary>
    public static class PushLogic
    {
        /// <summary>
        /// mover를 방향 d로 민다. targetExists=false면 보드 밖/없는 칸.
        /// target은 도착지 타일('\0'이면 빈칸).
        /// 반환: 판정 결과. newSource/newTarget은 성공 시의 두 칸 상태.
        /// </summary>
        public static PushResultType Resolve(char mover, char target, bool targetExists, Direction d,
            out char newSource, out char newTarget, RuleFlags flags = default)
        {
            newSource = mover;
            newTarget = target;

            if (!targetExists || mover == Hangul.Empty || mover == Hangul.Rock)
                return PushResultType.Fail; // 바위는 밀 수 없다 (D-21)

            // 1. 합성: 도착지 타일과 전체 합성
            if (target != Hangul.Empty)
            {
                char composed = TryCompose(mover, target, d);
                if (composed != Hangul.Empty)
                {
                    newSource = Hangul.Empty;
                    newTarget = composed;
                    return PushResultType.Compose;
                }
            }

            // 2. 분해: d축이 겉조합 축이면 미는 쪽 성분이 나간다
            if (!flags.disableSplit && TrySplit(mover, d, out char stays, out char exits))
            {
                if (target == Hangul.Empty)
                {
                    newSource = stays;
                    newTarget = exits;
                    return PushResultType.SplitMove;
                }
                char chain = TryCompose(exits, target, d); // 연쇄는 1단계로 완결
                if (chain != Hangul.Empty)
                {
                    newSource = stays;
                    newTarget = chain;
                    return PushResultType.SplitCompose;
                }
                return PushResultType.Fail; // 나간 성분이 안착도 합성도 못 하면 분해 불발
            }

            // 3. 이동: 빈칸이고 d축에 조합이 전혀 없으면
            if (!flags.disableMove && target == Hangul.Empty)
            {
                var (h, v) = Hangul.CompositionAxes(mover);
                bool lockedOnAxis = d.IsHorizontal() ? h : v;
                if (!lockedOnAxis)
                {
                    newSource = Hangul.Empty;
                    newTarget = mover;
                    return PushResultType.Move;
                }
            }

            // 4. 실패 (줄밀기 없음 — 도착지 타일은 절대 밀려나지 않음)
            return PushResultType.Fail;
        }

        /// <summary>
        /// mover가 방향 d로 target 위에 도달할 때의 합성 판정 (명세 4장, 공간 배치 기준).
        /// 실패 시 '\0'.
        /// </summary>
        public static char TryCompose(char mover, char target, Direction d)
        {
            // 최종 공간 관계로 환원: mover가 d 방향으로 접근하므로
            // d=Right → mover가 왼쪽 / d=Left → mover가 오른쪽 / d=Down → mover가 위 / d=Up → mover가 아래
            return d switch
            {
                Direction.Right => ComposeHorizontal(left: mover, right: target),
                Direction.Left => ComposeHorizontal(left: target, right: mover),
                Direction.Down => ComposeVertical(top: mover, bottom: target),
                _ => ComposeVertical(top: target, bottom: mover),
            };
        }

        /// <summary>왼쪽+오른쪽 가로 결합.</summary>
        public static char ComposeHorizontal(char left, char right)
        {
            // 자음(초성 가능) + 세로형/복합형 모음 → 민글자
            if (Hangul.CanBeChoseong(left)
                && (Hangul.IsVerticalVowel(right) || Hangul.IsWrapVowel(right)))
                return Hangul.ComposeSyllable(left, right);

            // 모음 + 모음 (표준 쌍)
            if (Hangul.IsVowel(left) && Hangul.IsVowel(right))
                return Hangul.CombineVowels(left, right);

            // 자음 + 자음 (표준 쌍)
            if (Hangul.IsConsonant(left) && Hangul.IsConsonant(right))
                return Hangul.CombineConsonants(left, right);

            return Hangul.Empty;
        }

        /// <summary>위+아래 세로 결합.</summary>
        public static char ComposeVertical(char top, char bottom)
        {
            // 자음(초성 가능) + 가로형/복합형 모음 → 민글자
            if (Hangul.CanBeChoseong(top)
                && (Hangul.IsHorizontalVowel(bottom) || Hangul.IsWrapVowel(bottom)))
                return Hangul.ComposeSyllable(top, bottom);

            // 민글자 + 자음(종성 가능) → 받침글자 (겹받침은 미리 조립된 것만)
            if (Hangul.IsSyllable(top) && !Hangul.HasJongseong(top)
                && Hangul.IsConsonant(bottom) && Hangul.CanBeJongseong(bottom))
            {
                var (cho, jung, _) = Hangul.DecomposeSyllable(top);
                return Hangul.ComposeSyllable(cho, jung, bottom);
            }

            return Hangul.Empty;
        }

        /// <summary>
        /// d축이 타일의 겉조합 축이면 분해 (명세 6장). 미는 방향 쪽 성분이 exits.
        /// </summary>
        public static bool TrySplit(char t, Direction d, out char stays, out char exits)
        {
            stays = exits = Hangul.Empty;

            if (Hangul.IsSyllable(t))
            {
                var (cho, jung, jong) = Hangul.DecomposeSyllable(t);

                if (jong != Hangul.Empty)
                {
                    // 겉조합 = 받침(세로): 위 성분 = 민글자, 아래 성분 = 종성
                    if (d.IsHorizontal()) return false;
                    char base_ = Hangul.ComposeSyllable(cho, jung);
                    if (d == Direction.Up) { exits = base_; stays = jong; }
                    else { exits = jong; stays = base_; }
                    return true;
                }

                // 민글자: 겉조합 축 = 모음 유형
                if (Hangul.IsVerticalVowel(jung))
                {
                    if (!d.IsHorizontal()) return false;
                    if (d == Direction.Left) { exits = cho; stays = jung; }
                    else { exits = jung; stays = cho; }
                    return true;
                }
                if (Hangul.IsHorizontalVowel(jung))
                {
                    if (d.IsHorizontal()) return false;
                    if (d == Direction.Up) { exits = cho; stays = jung; }
                    else { exits = jung; stays = cho; }
                    return true;
                }
                // 복합형: 4방향 모두 분해. 위/왼쪽 = 자음, 아래/오른쪽 = 모음
                if (d == Direction.Up || d == Direction.Left) { exits = cho; stays = jung; }
                else { exits = jung; stays = cho; }
                return true;
            }

            // 복합 자모 (ㄲ, ㄳ, ㅘ, ㅐ...): 가로 겉조합
            if (Hangul.TrySplitCompound(t, out char l, out char r))
            {
                if (!d.IsHorizontal()) return false;
                if (d == Direction.Left) { exits = l; stays = r; }
                else { exits = r; stays = l; }
                return true;
            }

            return false;
        }
    }
}
