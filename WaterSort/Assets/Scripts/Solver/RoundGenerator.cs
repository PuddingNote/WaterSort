using System;
using System.Collections.Generic;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// 라운드 자동 생성 — **완전 무작위 배분** 방식.
    ///
    /// 처음엔 "완성 상태에서 역연산을 반복해 섞는" 방식(역방향 생성)을 썼는데,
    /// 이 방식은 항상 색 덩어리 단위로만 움직여서 실제로는 한 병에 같은 색이
    /// 4~7칸씩 연달아 붙어있는, 겉보기만 복잡하고 실제로는 쉬운 라운드가
    /// 나왔다(실제로 겪은 문제 — 사용자가 스크린샷으로 확인). 진짜 어려운 Water
    /// Sort는 한 병 안에 여러 색이 잘게 섞여 있어야 한다.
    ///
    /// 그래서 전체 유닛(색상 수 × 슬롯 수)을 하나로 모아 진짜로 무작위 섞은 뒤
    /// 병에 나눠 담는다 — 색이 뭉치지 않고 골고루 흩어진다. 이 방식은 "역연산이라
    /// 항상 풀린다"는 수학적 보장이 없으므로, 생성 직후 <see cref="HintSolver"/>로
    /// 실제로 풀리는지 검증하고 안 풀리면 다시 섞어 재시도한다. 그래도 계속
    /// 실패하면 여유 막대를 하나 늘려 재시도한다(여유 공간이 늘수록 풀릴 확률이
    /// 급격히 오른다 — 실측: 색 10종·슬롯 8칸 기준 여유 막대 1개면 거의 항상
    /// 못 풀지만, 2개면 100번 중 100번 다 풀렸다).
    /// </summary>
    public static class RoundGenerator
    {
        public sealed class Parameters
        {
            public int ColorCount;
            public int SlotCount;
            public int EmptyContainerCount;
            public Random Random;
        }

        private const int MaxReshuffleRetries = 40;
        private const int MaxEmptyBoostSteps = 3;
        private const int VerifySearchBudget = 200_000;

        public static Board Generate(Parameters p)
        {
            if (p.ColorCount <= 0) throw new ArgumentOutOfRangeException(nameof(p.ColorCount));
            if (p.SlotCount <= 0) throw new ArgumentOutOfRangeException(nameof(p.SlotCount));
            if (p.EmptyContainerCount < 0) throw new ArgumentOutOfRangeException(nameof(p.EmptyContainerCount));

            var rng = p.Random ?? new Random();
            int emptyContainerCount = p.EmptyContainerCount;

            for (int boost = 0; boost <= MaxEmptyBoostSteps; boost++)
            {
                for (int attempt = 0; attempt < MaxReshuffleRetries; attempt++)
                {
                    var board = DealRandomBoard(p.ColorCount, p.SlotCount, emptyContainerCount, rng);
                    if (HintSolver.FindSolutionHeuristic(board, VerifySearchBudget).Found)
                        return board;
                }
                emptyContainerCount++; // 계속 안 풀리면 여유 막대를 늘려 재시도(안전망)
            }

            // 이론상 여기 도달하지 않아야 하지만, 도달해도 완성 상태 자체는
            // 항상 유효한(0수 클리어) 라운드이므로 게임이 멈추진 않는다.
            return CreateSolvedBoard(p.ColorCount, p.SlotCount, p.EmptyContainerCount);
        }

        private static Board DealRandomBoard(int colorCount, int slotCount, int emptyContainerCount, Random rng)
        {
            var units = new List<int>(colorCount * slotCount);
            for (int color = 0; color < colorCount; color++)
                for (int i = 0; i < slotCount; i++)
                    units.Add(color);

            Shuffle(units, rng);

            var layers = new List<int[]>(colorCount + emptyContainerCount);
            int cursor = 0;
            for (int i = 0; i < colorCount; i++)
            {
                var layer = new int[slotCount];
                for (int s = 0; s < slotCount; s++) layer[s] = units[cursor++];
                layers.Add(layer);
            }
            for (int i = 0; i < emptyContainerCount; i++) layers.Add(Array.Empty<int>());

            return BoardFactory.Create(slotCount, layers);
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static Board CreateSolvedBoard(int colorCount, int slotCount, int emptyContainerCount)
        {
            var layers = new List<int[]>(colorCount + emptyContainerCount);
            for (int color = 0; color < colorCount; color++)
            {
                var layer = new int[slotCount];
                for (int slot = 0; slot < slotCount; slot++) layer[slot] = color;
                layers.Add(layer);
            }
            for (int i = 0; i < emptyContainerCount; i++) layers.Add(Array.Empty<int>());
            return BoardFactory.Create(slotCount, layers);
        }
    }
}
