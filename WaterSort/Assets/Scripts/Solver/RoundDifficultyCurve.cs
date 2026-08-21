using System;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// "라운드 번호 → 목표 난이도 → 구체 파라미터" 매핑(범용형 기획서 6.5/6.6,
    /// 물병판 7.5/7.6). 절대 수치(색상 9종 등)가 아니라 **테마의 [min,max] 범위에
    /// 대한 비율**로 구간을 정의해, 색상 수 한도가 다른 소재(예: 6종뿐인 테마)에도
    /// 그대로 재사용된다 — <see cref="ThemeLimits"/>만 테마별로 다르게 넣으면 된다.
    /// </summary>
    public static class RoundDifficultyCurve
    {
        /// <summary>테마마다 다른 파라미터 한도(기획서 6.2/7.2의 예시 범위에 대응).</summary>
        public sealed class ThemeLimits
        {
            public int MinColorCount = 3;
            public int MaxColorCount = 9;
            public int MinSlotCount = 4;
            public int MaxSlotCount = 9;
            public int MinEmptyContainerCount = 1;
            public int MaxEmptyContainerCount = 3;
        }

        // 라운드 구간(기획서 6.5/7.5 표)별: colorCount/slotCount를 테마 범위의
        // 몇 %~몇 % 지점에서 뽑을지, emptyContainerCount는 절대 범위로.
        private readonly struct Tier
        {
            public readonly int MinRound;
            public readonly float FracLow, FracHigh;
            public readonly int EmptyLow, EmptyHigh;
            public readonly float ShuffleMultiplierLow, ShuffleMultiplierHigh;

            public Tier(int minRound, float fracLow, float fracHigh, int emptyLow, int emptyHigh, float shuffleLow, float shuffleHigh)
            {
                MinRound = minRound;
                FracLow = fracLow; FracHigh = fracHigh;
                EmptyLow = emptyLow; EmptyHigh = emptyHigh;
                ShuffleMultiplierLow = shuffleLow; ShuffleMultiplierHigh = shuffleHigh;
            }
        }

        // shuffleMultiplier는 "총 유닛 수(colorCount*slotCount) 대비 역연산 횟수 배수" —
        // 값이 클수록 더 많이 섞는다. 전부 튜닝 대상(placeholder), 자기대전으로 실측 후 조정.
        private static readonly Tier[] Tiers =
        {
            new Tier(minRound: 1,   fracLow: 0.00f, fracHigh: 0.15f, emptyLow: 2, emptyHigh: 3, shuffleLow: 0.8f, shuffleHigh: 1.2f),
            new Tier(minRound: 11,  fracLow: 0.15f, fracHigh: 0.35f, emptyLow: 1, emptyHigh: 2, shuffleLow: 1.2f, shuffleHigh: 1.8f),
            new Tier(minRound: 31,  fracLow: 0.35f, fracHigh: 0.60f, emptyLow: 1, emptyHigh: 2, shuffleLow: 1.8f, shuffleHigh: 2.6f),
            new Tier(minRound: 61,  fracLow: 0.60f, fracHigh: 0.85f, emptyLow: 1, emptyHigh: 1, shuffleLow: 2.6f, shuffleHigh: 3.6f),
            new Tier(minRound: 101, fracLow: 0.85f, fracHigh: 1.00f, emptyLow: 1, emptyHigh: 1, shuffleLow: 3.6f, shuffleHigh: 5.0f),
        };

        // 101+ 구간에서 "막대 개수는 적지만 어려운 변칙 라운드"가 나올 확률(6.5 비고).
        private const float VariantRoundChance = 0.2f;

        public static RoundGenerator.Parameters SampleParameters(int roundId, ThemeLimits limits, Random rng)
        {
            if (roundId < 1) throw new ArgumentOutOfRangeException(nameof(roundId));

            var tier = PickTier(roundId);
            int colorCount = FracToInt(tier.FracLow, tier.FracHigh, limits.MinColorCount, limits.MaxColorCount, rng);
            int slotCount = FracToInt(tier.FracLow, tier.FracHigh, limits.MinSlotCount, limits.MaxSlotCount, rng);
            int emptyContainerCount = RandomIntInclusive(rng,
                Math.Max(limits.MinEmptyContainerCount, tier.EmptyLow),
                Math.Min(limits.MaxEmptyContainerCount, tier.EmptyHigh));

            // 101+ 변칙 라운드: 여유 막대를 최소로 더 줄이고 slotCount는 최대치로 —
            // "막대 개수는 적지만 어려운" 조합을 의도적으로 섞는다.
            if (tier.MinRound == 101 && rng.NextDouble() < VariantRoundChance)
            {
                emptyContainerCount = limits.MinEmptyContainerCount;
                slotCount = limits.MaxSlotCount;
            }

            float shuffleMultiplier = Lerp(tier.ShuffleMultiplierLow, tier.ShuffleMultiplierHigh, (float)rng.NextDouble());
            int shuffleDepth = Math.Max(1, (int)MathF.Round(colorCount * slotCount * shuffleMultiplier));

            return new RoundGenerator.Parameters
            {
                ColorCount = colorCount,
                SlotCount = slotCount,
                EmptyContainerCount = emptyContainerCount,
                ShuffleDepth = shuffleDepth,
                Random = rng
            };
        }

        private static Tier PickTier(int roundId)
        {
            var tier = Tiers[0];
            foreach (var t in Tiers)
                if (roundId >= t.MinRound) tier = t;
            return tier;
        }

        private static int FracToInt(float fracLow, float fracHigh, int min, int max, Random rng)
        {
            float frac = Lerp(fracLow, fracHigh, (float)rng.NextDouble());
            int value = min + (int)MathF.Round(frac * (max - min));
            return Math.Clamp(value, min, max);
        }

        private static int RandomIntInclusive(Random rng, int low, int high)
        {
            if (high < low) (low, high) = (high, low);
            return rng.Next(low, high + 1);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
