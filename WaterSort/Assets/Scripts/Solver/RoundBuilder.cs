using System;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// 실제 게임(UI/Managers)이 부를 단일 진입점: "이번 라운드 번호를 주면 보드를
    /// 만들어 달라." 내부적으로 곡선에서 파라미터를 뽑고(<see cref="RoundDifficultyCurve"/>),
    /// 생성하고(<see cref="RoundGenerator"/> — 풀리는지 자체 검증까지 포함),
    /// 실제로 몇 수만에 풀리는지 측정해 같이 돌려준다(UI가 로그·디버그용으로 참고 가능).
    /// </summary>
    public static class RoundBuilder
    {
        public sealed class Result
        {
            public Board Board { get; }
            public RoundGenerator.Parameters Parameters { get; }

            /// <summary>실제 이 보드를 풀 때 필요한 수(휴리스틱 탐색 결과 — 최단은
            /// 아닐 수 있지만 체감 난이도를 보여주는 실측값). 못 찾으면 -1.</summary>
            public int SolutionMoveCount { get; }

            public Result(Board board, RoundGenerator.Parameters parameters, int solutionMoveCount)
            {
                Board = board;
                Parameters = parameters;
                SolutionMoveCount = solutionMoveCount;
            }
        }

        public static Result Build(int roundId, RoundDifficultyCurve.ThemeLimits limits, Random rng)
        {
            var parameters = RoundDifficultyCurve.SampleParameters(roundId, limits, rng);
            var board = RoundGenerator.Generate(parameters); // 내부적으로 풀리는지 이미 검증됨

            var solution = HintSolver.FindSolutionHeuristic(board);
            int moveCount = solution.Found ? solution.Moves.Count : -1;

            return new Result(board, parameters, moveCount);
        }
    }
}
