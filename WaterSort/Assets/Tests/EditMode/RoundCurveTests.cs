using System;
using ColorSort.Core;
using ColorSort.Solver;
using NUnit.Framework;

namespace ColorSort.Tests
{
    public class RoundCurveTests
    {
        private static readonly RoundDifficultyCurve.ThemeLimits Limits = new RoundDifficultyCurve.ThemeLimits
        {
            MinColorCount = 3,
            MaxColorCount = 9,
            MinSlotCount = 4,
            MaxSlotCount = 9,
            MinEmptyContainerCount = 1,
            MaxEmptyContainerCount = 3
        };

        [Test]
        public void SampleParameters_항상_테마_범위_안에_있다()
        {
            var rng = new Random(7);
            foreach (var roundId in new[] { 1, 10, 11, 30, 31, 60, 61, 100, 101, 200 })
            {
                var p = RoundDifficultyCurve.SampleParameters(roundId, Limits, rng);

                Assert.GreaterOrEqual(p.ColorCount, Limits.MinColorCount);
                Assert.LessOrEqual(p.ColorCount, Limits.MaxColorCount);
                Assert.GreaterOrEqual(p.SlotCount, Limits.MinSlotCount);
                Assert.LessOrEqual(p.SlotCount, Limits.MaxSlotCount);
                Assert.GreaterOrEqual(p.EmptyContainerCount, Limits.MinEmptyContainerCount);
                Assert.LessOrEqual(p.EmptyContainerCount, Limits.MaxEmptyContainerCount);
                Assert.Greater(p.ShuffleDepth, 0);
            }
        }

        [Test]
        public void SampleParameters_라운드가_오를수록_평균_색상수가_증가한다()
        {
            float Average(int roundId)
            {
                var rng = new Random(123);
                int sum = 0;
                const int trials = 40;
                for (int i = 0; i < trials; i++)
                    sum += RoundDifficultyCurve.SampleParameters(roundId, Limits, rng).ColorCount;
                return (float)sum / trials;
            }

            float early = Average(5);
            float mid = Average(50);
            float late = Average(150);

            Assert.Less(early, mid);
            Assert.Less(mid, late);
        }

        [Test]
        public void RoundBuilder_Build_실제_클리어_가능한_보드를_돌려준다()
        {
            var rng = new Random(99);
            foreach (var roundId in new[] { 1, 40, 90, 140 })
            {
                var result = RoundBuilder.Build(roundId, Limits, rng);

                Assert.IsFalse(ClearChecker.IsCleared(result.Board), $"roundId={roundId}: 이미 완성 상태로 나오면 안 됨(난이도 저하)");
                Assert.IsTrue(HintSolver.FindSolutionHeuristic(result.Board).Found, $"roundId={roundId}: 클리어 경로가 있어야 함");
            }
        }
    }
}
