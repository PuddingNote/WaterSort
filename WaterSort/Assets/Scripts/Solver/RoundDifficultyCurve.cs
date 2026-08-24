using System;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// "라운드 번호 → 목표 난이도 → 구체 파라미터" 매핑. 절대 수치가 아니라
    /// **테마의 [min,max] 범위에 대한 비율**로 정의해, 다른 소재(색 수·용량 한도가
    /// 다른 테마)에도 그대로 재사용된다 — <see cref="ThemeLimits"/>만 바꿔 끼우면 된다.
    ///
    /// 난이도는 라운드 1→<see cref="MaxDifficultyRoundId"/> 사이에서 선형으로
    /// 오르고, 그 이후 라운드는 전부 최고 난이도 그대로 고정된다(무한 라운드,
    /// 난이도 상한만 존재).
    ///
    /// **여유 막대 수는 낮추지 않는다.** 처음엔 "여유 막대를 최소치로 줄이는 게
    /// 주된 난이도 레버"라고 생각했는데, `RoundGenerator`를 완전 무작위 배분
    /// 방식으로 바꾼 뒤 실측해보니 여유 막대가 1개면(색 10종·슬롯 8칸 기준)
    /// 100번 중 거의 못 풀고, 2개면 100번 다 풀렸다 — 여유 막대는 "적을수록
    /// 어렵다"가 아니라 "일정 수 밑으로는 그냥 안 풀리는" 문턱값에 가깝다.
    /// 그래서 여유 막대는 테마의 [min,max] 범위 안에서만 살짝 무작위로 주고,
    /// **색상 수 × 슬롯 수(=총 유닛 수)를 라운드 진행의 주된 난이도 레버**로
    /// 쓴다 — 실측상 이것만으로도 15수(1라운드대)에서 70수 이상(최고 난이도)까지
    /// 자연스럽게 늘어난다.
    /// </summary>
    public static class RoundDifficultyCurve
    {
        public const int MaxDifficultyRoundId = 100;

        /// <summary>테마마다 다른 파라미터 한도.</summary>
        public sealed class ThemeLimits
        {
            public int MinColorCount = 3;
            public int MaxColorCount = 9;
            public int MinSlotCount = 4;
            public int MaxSlotCount = 9;

            /// <summary>여유 막대 수 범위. 너무 낮추면(테마에 따라 다르지만 보통 1개)
            /// 무작위 배분 자체가 거의 안 풀리는 지경이 되므로, 실측으로 "거의 항상
            /// 풀리는" 하한을 확인하고 그 값으로 맞춰야 한다.</summary>
            public int MinEmptyContainerCount = 2;
            public int MaxEmptyContainerCount = 3;

            /// <summary>막대 총 개수(colorCount+emptyContainerCount)의 허용 범위.
            /// 화면 배치(위/아래 줄 균형 등)를 위해 테마가 요구하는 하한/상한.</summary>
            public int MinContainerCount = 4;
            public int MaxContainerCount = 12;
        }

        /// <summary>라운드 1이면 0, <see cref="MaxDifficultyRoundId"/> 이상이면 1로 고정되는 진행도.</summary>
        public static float ProgressFraction(int roundId)
        {
            if (roundId < 1) throw new ArgumentOutOfRangeException(nameof(roundId));
            return Math.Clamp((roundId - 1) / (float)(MaxDifficultyRoundId - 1), 0f, 1f);
        }

        public static RoundGenerator.Parameters SampleParameters(int roundId, ThemeLimits limits, Random rng)
        {
            float progress = ProgressFraction(roundId);

            int colorCount = Lerp(limits.MinColorCount, limits.MaxColorCount, progress);
            int slotCount = Lerp(limits.MinSlotCount, limits.MaxSlotCount, progress);
            int emptyContainerCount = RandomIntInclusive(rng, limits.MinEmptyContainerCount, limits.MaxEmptyContainerCount);

            (colorCount, emptyContainerCount) = ClampContainerCount(colorCount, emptyContainerCount, limits);

            return new RoundGenerator.Parameters
            {
                ColorCount = colorCount,
                SlotCount = slotCount,
                EmptyContainerCount = emptyContainerCount,
                Random = rng
            };
        }

        /// <summary>막대 총 개수가 [MinContainerCount, MaxContainerCount] 범위를 벗어나면
        /// 여유 막대부터 조정하고, 그래도 모자라거나 넘치면 색상 수로 마저 맞춘다.</summary>
        private static (int colorCount, int emptyContainerCount) ClampContainerCount(
            int colorCount, int emptyContainerCount, ThemeLimits limits)
        {
            int total = colorCount + emptyContainerCount;

            if (total < limits.MinContainerCount)
            {
                int shortfall = limits.MinContainerCount - total;
                int emptyRoom = Math.Max(0, limits.MaxEmptyContainerCount - emptyContainerCount);
                int addToEmpty = Math.Min(shortfall, emptyRoom);
                emptyContainerCount += addToEmpty;
                shortfall -= addToEmpty;
                if (shortfall > 0)
                    colorCount = Math.Min(limits.MaxColorCount, colorCount + shortfall);
            }
            else if (total > limits.MaxContainerCount)
            {
                int excess = total - limits.MaxContainerCount;
                int emptyReducible = Math.Max(0, emptyContainerCount - limits.MinEmptyContainerCount);
                int removeFromEmpty = Math.Min(excess, emptyReducible);
                emptyContainerCount -= removeFromEmpty;
                excess -= removeFromEmpty;
                if (excess > 0)
                    colorCount = Math.Max(limits.MinColorCount, colorCount - excess);
            }

            return (colorCount, emptyContainerCount);
        }

        private static int Lerp(int min, int max, float t) => min + (int)MathF.Round((max - min) * t);

        private static int RandomIntInclusive(Random rng, int low, int high)
        {
            if (high < low) (low, high) = (high, low);
            return rng.Next(low, high + 1);
        }
    }
}
