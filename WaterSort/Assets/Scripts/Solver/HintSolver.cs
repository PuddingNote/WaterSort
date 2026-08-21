using System;
using System.Collections.Generic;
using System.Text;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// 현재 국면에서 클리어까지의 이동 경로를 찾는다(기획서 3.3 힌트, 6.1/7.1의
    /// 최단 클리어 수 계산에 공용으로 쓰인다).
    ///
    /// 두 가지 탐색을 제공한다 — 상태공간 크기에 따라 용도가 다르다:
    /// - <see cref="FindShortestSolution"/>: BFS, 진짜 최단 경로를 보장하지만
    ///   가지치기가 없어 많이 뒤섞인 큰 보드(9색×9칸 근처 등)에서는 수백만
    ///   states를 뒤져도 못 찾을 수 있다(실제로 겪음 — 못 푸는 게 아니라 이
    ///   알고리즘으로 감당이 안 되는 것).
    /// - <see cref="FindSolutionHeuristic"/>: 가중치 A* 계열. 최단은 보장 못
    ///   하지만 "색 경계(전이) 수"를 휴리스틱으로 써서 큰 보드에서도 실전
    ///   시간 안에 "풀리는 경로 하나"를 찾는다. 라운드 생성 검증(존재 여부만
    ///   필요)과, BFS가 예산 안에 못 찾았을 때 힌트의 폴백으로 쓴다.
    /// 절대 멈추면 안 되므로, 둘 다 <paramref name="maxStates"/>로 예산을
    /// 제한하고 못 찾으면 "모른다"(NotFound)를 조용히 돌려준다.
    /// </summary>
    public static class HintSolver
    {
        public readonly struct Move
        {
            public readonly int FromIndex;
            public readonly int ToIndex;

            public Move(int fromIndex, int toIndex)
            {
                FromIndex = fromIndex;
                ToIndex = toIndex;
            }
        }

        public sealed class SolveResult
        {
            public bool Found { get; }
            public IReadOnlyList<Move> Moves { get; }

            private SolveResult(bool found, IReadOnlyList<Move> moves)
            {
                Found = found;
                Moves = moves;
            }

            public static readonly SolveResult NotFound = new SolveResult(false, Array.Empty<Move>());
            public static SolveResult Of(IReadOnlyList<Move> moves) => new SolveResult(true, moves);
        }

        public static SolveResult FindShortestSolution(Board board, int maxStates = 200_000)
        {
            if (ClearChecker.IsCleared(board)) return SolveResult.Of(Array.Empty<Move>());

            var startKey = ToStateKey(board);
            var visited = new HashSet<string> { startKey };
            var cameFrom = new Dictionary<string, (string parentKey, Move move)>();
            var queue = new Queue<Board>();
            queue.Enqueue(board);

            int explored = 0;
            while (queue.Count > 0 && explored < maxStates)
            {
                var current = queue.Dequeue();
                explored++;
                var currentKey = ToStateKey(current);

                foreach (var (from, to, next, nextKey) in EnumerateMoves(current))
                {
                    if (!visited.Add(nextKey)) continue;
                    cameFrom[nextKey] = (currentKey, new Move(from, to));

                    if (ClearChecker.IsCleared(next))
                        return SolveResult.Of(ReconstructPath(cameFrom, startKey, nextKey));

                    queue.Enqueue(next);
                }
            }

            return SolveResult.NotFound;
        }

        /// <summary>
        /// 가중치 Best-First 탐색(A* 변형). 최단 경로는 보장하지 않고 "풀리는
        /// 경로 하나"의 존재를 실전 시간 안에 찾는 것이 목적이다. weight가
        /// 클수록 휴리스틱(색 전이 수)을 더 신뢰해 그리디에 가까워진다.
        /// </summary>
        public static SolveResult FindSolutionHeuristic(Board board, int maxStates = 300_000, float weight = 4f)
        {
            if (ClearChecker.IsCleared(board)) return SolveResult.Of(Array.Empty<Move>());

            var startKey = ToStateKey(board);
            var visited = new HashSet<string> { startKey };
            var cameFrom = new Dictionary<string, (string parentKey, Move move)>();
            var heap = new MinHeap<(Board board, int depth)>();
            heap.Push(ColorTransitionHeuristic(board), (board, 0));

            int explored = 0;
            while (heap.Count > 0 && explored < maxStates)
            {
                var (current, depth) = heap.Pop();
                explored++;
                var currentKey = ToStateKey(current);

                foreach (var (from, to, next, nextKey) in EnumerateMoves(current))
                {
                    if (!visited.Add(nextKey)) continue;
                    cameFrom[nextKey] = (currentKey, new Move(from, to));

                    if (ClearChecker.IsCleared(next))
                        return SolveResult.Of(ReconstructPath(cameFrom, startKey, nextKey));

                    int nextDepth = depth + 1;
                    float priority = nextDepth + weight * ColorTransitionHeuristic(next);
                    heap.Push(priority, (next, nextDepth));
                }
            }

            return SolveResult.NotFound;
        }

        /// <summary>
        /// 힌트 버튼용 다음 한 수. 상태공간이 작으면 진짜 최적수를, 아니면
        /// (BFS 예산 초과) 휴리스틱 탐색으로 찾은 "유효한 다음 수"를 돌려준다.
        /// 둘 다 실패하면 null — 호출부(UI)는 힌트를 조용히 비활성화해야 한다.
        /// </summary>
        public static Move? FindNextMove(Board board, int bfsMaxStates = 20_000, int heuristicMaxStates = 300_000)
        {
            var exact = FindShortestSolution(board, bfsMaxStates);
            if (exact.Found) return exact.Moves.Count > 0 ? exact.Moves[0] : (Move?)null;

            var fallback = FindSolutionHeuristic(board, heuristicMaxStates);
            return fallback.Found && fallback.Moves.Count > 0 ? fallback.Moves[0] : (Move?)null;
        }

        private static IEnumerable<(int from, int to, Board next, string nextKey)> EnumerateMoves(Board current)
        {
            int n = current.Containers.Count;
            for (int from = 0; from < n; from++)
            {
                if (current.Containers[from].IsEmpty) continue;
                for (int to = 0; to < n; to++)
                {
                    if (!MoveRules.CanMove(current, from, to)) continue;

                    var next = current.Clone();
                    MoveRules.TryMove(next, from, to);
                    yield return (from, to, next, ToStateKey(next));
                }
            }
        }

        /// <summary>인접한 유닛끼리 색이 바뀌는 경계 수. 0이면 모든 막대가 이미
        /// 단색(=클리어) — 해를 향한 "덜 뒤섞인 정도"를 값싸게 근사한다.</summary>
        private static int ColorTransitionHeuristic(Board board)
        {
            int transitions = 0;
            foreach (var container in board.Containers)
            {
                var units = container.Units;
                for (int i = 1; i < units.Count; i++)
                    if (!units[i].Equals(units[i - 1])) transitions++;
            }
            return transitions;
        }

        private static List<Move> ReconstructPath(
            Dictionary<string, (string parentKey, Move move)> cameFrom,
            string startKey,
            string goalKey)
        {
            var moves = new List<Move>();
            var key = goalKey;
            while (key != startKey)
            {
                var (parentKey, move) = cameFrom[key];
                moves.Add(move);
                key = parentKey;
            }
            moves.Reverse();
            return moves;
        }

        private static string ToStateKey(Board board)
        {
            var sb = new StringBuilder();
            foreach (var container in board.Containers)
            {
                foreach (var unit in container.Units)
                {
                    sb.Append(unit.Value);
                    sb.Append(',');
                }
                sb.Append(';');
            }
            return sb.ToString();
        }
    }
}
