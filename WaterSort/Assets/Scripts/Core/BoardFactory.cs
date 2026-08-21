using System.Collections.Generic;

namespace ColorSort.Core
{
    /// <summary>Board/Container를 손으로 조립하는 자리 — 테스트와 Solver의 생성 로직이 함께 쓴다.</summary>
    public static class BoardFactory
    {
        /// <summary>
        /// 각 Container를 "바닥 → 입구" 순서의 색상 값 배열로 받아 Board를 만든다.
        /// 예: new[] {1,1,2}는 바닥부터 1,1,2 순으로 쌓여 2가 최상단(입구).
        /// </summary>
        public static Board Create(int slotCount, IReadOnlyList<int[]> containersBottomToTop)
        {
            var containers = new List<Container>(containersBottomToTop.Count);
            foreach (var layers in containersBottomToTop)
            {
                var units = new ColorId[layers.Length];
                for (int i = 0; i < layers.Length; i++) units[i] = new ColorId(layers[i]);
                containers.Add(new Container(slotCount, units));
            }
            return new Board(slotCount, containers);
        }

        public static Board CreateEmpty(int slotCount, int containerCount)
        {
            var containers = new List<Container>(containerCount);
            for (int i = 0; i < containerCount; i++) containers.Add(new Container(slotCount));
            return new Board(slotCount, containers);
        }
    }
}
