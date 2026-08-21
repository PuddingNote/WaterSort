using System;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// 실제 게임(UI/Managers)이 부를 단일 진입점: "이번 라운드 번호를 주면 보드를
    /// 만들어 달라." 내부적으로 곡선에서 목표 난이도를 잡고(<see cref="RoundDifficultyCurve"/>)
    /// 생성 후 실측 난이도가 목표와 너무 벗어나면 shuffleDepth를 조정해 재시도한다
    /// (범용형 기획서 6.6 의사코드의 "adjust params and retry" 단계 그대로).
    /// </summary>
    public static class RoundBuilder
    {
        public sealed class Result
        {
            public Board Board { get; }
            public RoundGenerator.Parameters Parameters { get; }
            public float TargetDifficultyScore { get; }
            public float ActualDifficultyScore { get; }

            public Result(Board board, RoundGenerator.Parameters parameters, float target, float actual)
            {
                Board = board;
                Parameters = parameters;
                TargetDifficultyScore = target;
                ActualDifficultyScore = actual;
            }
        }

        private const int MaxAdjustRetries = 4;
        private const float ToleranceFraction = 0.35f; // 목표 대비 이 비율 넘게 벗어나면 재조정

        public static Result Build(int roundId, RoundDifficultyCurve.ThemeLimits limits, Random rng)
        {
            float target = TargetDifficultyScore(roundId);
            var parameters = RoundDifficultyCurve.SampleParameters(roundId, limits, rng);

            Board lastBoard = null;
            float lastScore = 0f;

            for (int attempt = 0; attempt < MaxAdjustRetries; attempt++)
            {
                var board = RoundGenerator.Generate(parameters);
                float actual = MeasureDifficulty(board, parameters);
                lastBoard = board;
                lastScore = actual;

                if (Math.Abs(actual - target) <= Math.Max(1f, target) * ToleranceFraction)
                    return new Result(board, parameters, target, actual);

                // 실측이 목표보다 낮으면 더 섞고, 높으면 덜 섞는다.
                int direction = actual < target ? 1 : -1;
                int step = Math.Max(1, parameters.ShuffleDepth / 4);
                parameters = CloneWithShuffleDepth(parameters, Math.Max(1, parameters.ShuffleDepth + direction * step));
            }

            return new Result(lastBoard, parameters, target, lastScore);
        }

        /// <summary>기획서 6.5 곡선을 근사하는 로그형 목표 점수. shuffleMultiplier와 마찬가지로
        /// 자기대전 실측으로 계수를 조정해 나가는 것을 전제로 한 초기값이다.</summary>
        private static float TargetDifficultyScore(int roundId)
        {
            return 10f + 35f * MathF.Log(roundId + 1, 120f);
        }

        private static float MeasureDifficulty(Board board, RoundGenerator.Parameters parameters)
        {
            float colorSpread = DifficultyScorer.MeasureColorSpread(board);
            return DifficultyScorer.Score(
                slotCount: parameters.SlotCount,
                colorCount: parameters.ColorCount,
                colorSpread: colorSpread,
                shuffleDepth: parameters.ShuffleDepth,
                emptyContainerCount: parameters.EmptyContainerCount,
                containerCount: parameters.ColorCount + parameters.EmptyContainerCount);
        }

        private static RoundGenerator.Parameters CloneWithShuffleDepth(RoundGenerator.Parameters p, int shuffleDepth)
        {
            return new RoundGenerator.Parameters
            {
                ColorCount = p.ColorCount,
                SlotCount = p.SlotCount,
                EmptyContainerCount = p.EmptyContainerCount,
                ShuffleDepth = shuffleDepth,
                Random = p.Random
            };
        }
    }
}
