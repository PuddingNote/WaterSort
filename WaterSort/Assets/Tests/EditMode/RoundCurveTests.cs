using System;
using ColorSort.Core;
using ColorSort.Solver;
using NUnit.Framework;

namespace ColorSort.Tests
{
    public class RoundCurveTests
    {
        // WaterSort 실제 스펙(사용자 확정): 색 5~10, 슬롯 4~8, 막대 총 7~12개.
        // 여유 막대 최소치는 2(1이면 무작위 배분이 거의 안 풀림 — RoundGenerator 참고).
        private static readonly RoundDifficultyCurve.ThemeLimits Limits = new RoundDifficultyCurve.ThemeLimits
        {
            MinColorCount = 5,
            MaxColorCount = 10,
            MinSlotCount = 4,
            MaxSlotCount = 8,
            MinEmptyContainerCount = 2,
            MaxEmptyContainerCount = 3,
            MinContainerCount = 7,
            MaxContainerCount = 12
        };

        [Test]
        public void SampleParameters_항상_테마_범위_안에_있다()
        {
            var rng = new Random(7);
            int cap = RoundDifficultyCurve.MaxDifficultyRoundId;
            foreach (var roundId in new[] { 1, cap / 2, cap, cap + 1, cap * 4 })
            {
                var p = RoundDifficultyCurve.SampleParameters(roundId, Limits, rng);
                int containerCount = p.ColorCount + p.EmptyContainerCount;

                Assert.GreaterOrEqual(p.ColorCount, Limits.MinColorCount);
                Assert.LessOrEqual(p.ColorCount, Limits.MaxColorCount);
                Assert.GreaterOrEqual(p.SlotCount, Limits.MinSlotCount);
                Assert.LessOrEqual(p.SlotCount, Limits.MaxSlotCount);
                Assert.GreaterOrEqual(containerCount, Limits.MinContainerCount);
                Assert.LessOrEqual(containerCount, Limits.MaxContainerCount);
            }
        }

        [Test]
        public void SampleParameters_라운드가_오를수록_색상_슬롯_수가_증가한다()
        {
            var rng = new Random(1);
            int cap = RoundDifficultyCurve.MaxDifficultyRoundId;
            var early = RoundDifficultyCurve.SampleParameters(1, Limits, rng);
            var mid = RoundDifficultyCurve.SampleParameters(cap / 2, Limits, rng);
            var late = RoundDifficultyCurve.SampleParameters(cap, Limits, rng);

            Assert.LessOrEqual(early.ColorCount, mid.ColorCount);
            Assert.LessOrEqual(mid.ColorCount, late.ColorCount);
            Assert.LessOrEqual(early.SlotCount, mid.SlotCount);
            Assert.LessOrEqual(mid.SlotCount, late.SlotCount);
        }

        [Test]
        public void SampleParameters_상한_라운드_이후는_난이도가_그대로_고정된다()
        {
            int cap = RoundDifficultyCurve.MaxDifficultyRoundId;
            var rng1 = new Random(1);
            var rng2 = new Random(1);

            var atCap = RoundDifficultyCurve.SampleParameters(cap, Limits, rng1);
            var wayPastCap = RoundDifficultyCurve.SampleParameters(cap * 10, Limits, rng2);

            Assert.AreEqual(atCap.ColorCount, wayPastCap.ColorCount);
            Assert.AreEqual(atCap.SlotCount, wayPastCap.SlotCount);
        }

        [Test]
        public void SampleParameters_MaxContainerCount를_넘지_않는다()
        {
            var tightLimits = new RoundDifficultyCurve.ThemeLimits
            {
                MinColorCount = 8,
                MaxColorCount = 10,
                MinSlotCount = 4,
                MaxSlotCount = 8,
                MinEmptyContainerCount = 2,
                MaxEmptyContainerCount = 3,
                MinContainerCount = 7,
                MaxContainerCount = 11 // MinColorCount+MinEmptyContainerCount(8+2)보다는 커야 애초에 성립 가능 — 그 위에서 상한 클램프를 유도
            };

            var rng = new Random(3);
            int cap = RoundDifficultyCurve.MaxDifficultyRoundId;
            for (int roundId = 1; roundId <= cap * 2; roundId += Math.Max(1, cap / 10))
            {
                var p = RoundDifficultyCurve.SampleParameters(roundId, tightLimits, rng);
                int containerCount = p.ColorCount + p.EmptyContainerCount;

                Assert.LessOrEqual(containerCount, tightLimits.MaxContainerCount, $"roundId={roundId}");
                Assert.GreaterOrEqual(containerCount, tightLimits.MinContainerCount, $"roundId={roundId}");
            }
        }

        [Test]
        public void RoundBuilder_Build_실제_클리어_가능한_보드를_돌려준다()
        {
            var rng = new Random(99);
            int cap = RoundDifficultyCurve.MaxDifficultyRoundId;
            foreach (var roundId in new[] { 1, cap / 2, cap, cap * 3 })
            {
                var result = RoundBuilder.Build(roundId, Limits, rng);

                Assert.IsFalse(ClearChecker.IsCleared(result.Board), $"roundId={roundId}: 이미 완성 상태로 나오면 안 됨(난이도 저하)");
                Assert.Greater(result.SolutionMoveCount, 0, $"roundId={roundId}: 클리어 경로가 있어야 함");
            }
        }

        [Test]
        public void RoundBuilder_라운드가_오를수록_실제_클리어_난이도도_오른다()
        {
            var rng = new Random(7);
            int cap = RoundDifficultyCurve.MaxDifficultyRoundId;

            var early = RoundBuilder.Build(1, Limits, rng);
            var late = RoundBuilder.Build(cap, Limits, rng);

            Assert.Less(early.SolutionMoveCount, late.SolutionMoveCount,
                $"1라운드({early.SolutionMoveCount}수)가 최고난도({late.SolutionMoveCount}수)보다 쉽지 않음");
        }

        [Test]
        public void RoundBuilder_1라운드부터_너무_쉽지_않다()
        {
            // 실제로 겪은 문제: 색이 큰 덩어리로만 뭉쳐 있어 사실상 1~2수로 풀리는
            // 시시한 라운드가 나왔었다(역방향 셔플 방식의 한계) — 완전 무작위 배분으로
            // 바꾼 뒤에는 1라운드도 최소 이 정도 수는 필요해야 한다.
            var rng = new Random(42);
            var result = RoundBuilder.Build(1, Limits, rng);

            Assert.GreaterOrEqual(result.SolutionMoveCount, 8,
                $"1라운드 실측 클리어 수가 {result.SolutionMoveCount}수뿐 — 너무 쉬움");
        }
    }
}
