using System.Collections.Generic;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// 생성된 보드의 색 분포를 재는 보조 통계. 예전엔 여기에 여러 파라미터를
    /// 가중합해 "난이도 점수"를 추정하는 `Score()`도 있었는데, 실제 난이도를
    /// 결정하는 건 그 가중치 짐작이 아니라 "실제로 몇 수만에 풀리는가"였다
    /// (RoundGenerator를 완전 무작위 배분으로 바꾼 뒤 확인) — 그래서 이제
    /// 난이도는 `RoundBuilder.Result.SolutionMoveCount`(실측 최단 근사 수)로
    /// 직접 보고하고, 이 클래스는 부가 통계(색 분포)만 남긴다.
    /// </summary>
    public static class DifficultyScorer
    {
        /// <summary>생성된 Board에서 colorSpread(색상당 분포 막대 수 평균)를 측정한다.
        /// 값이 클수록 같은 색이 여러 병에 흩어져 있다는 뜻.</summary>
        public static float MeasureColorSpread(Board board)
        {
            var containersPerColor = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < board.Containers.Count; i++)
            {
                foreach (var unit in board.Containers[i].Units)
                {
                    if (!containersPerColor.TryGetValue(unit.Value, out var set))
                    {
                        set = new HashSet<int>();
                        containersPerColor[unit.Value] = set;
                    }
                    set.Add(i);
                }
            }

            if (containersPerColor.Count == 0) return 0f;

            int total = 0;
            foreach (var set in containersPerColor.Values) total += set.Count;
            return (float)total / containersPerColor.Count;
        }
    }
}
