using System.Collections.Generic;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// GameDesign.md의 DifficultyScore 가중합 산식. 가중치는 아직 플레이테스트로
    /// 실측/튜닝되지 않은 참고안 그대로다 — 값을 바꿀 땐 이 상수만 고치면 되고,
    /// 실측은 헤드리스 자기대전(Solver로 다수 라운드 생성 후 클리어 수 분포 측정)으로 한다.
    /// </summary>
    public static class DifficultyScorer
    {
        public const float SlotCountWeight = 3f;
        public const float ColorCountWeight = 4f;
        public const float ColorSpreadWeight = 5f;
        public const float ShuffleDepthWeight = 1f;
        public const float EmptyContainerWeight = -8f;
        public const float ContainerCountWeight = -1f;

        public static float Score(
            int slotCount,
            int colorCount,
            float colorSpread,
            int shuffleDepth,
            int emptyContainerCount,
            int containerCount)
        {
            return slotCount * SlotCountWeight
                 + colorCount * ColorCountWeight
                 + colorSpread * ColorSpreadWeight
                 + shuffleDepth * ShuffleDepthWeight
                 + emptyContainerCount * EmptyContainerWeight
                 + containerCount * ContainerCountWeight;
        }

        /// <summary>실제로 생성된 Board에서 colorSpread(색상당 분포 막대 수 평균)를 측정한다.</summary>
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
