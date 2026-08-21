using System;
using System.Collections.Generic;
using ColorSort.Core;

namespace ColorSort.Solver
{
    /// <summary>
    /// 라운드 자동 생성 — 범용형 기획서 6.1의 역방향(Reverse) 생성 방식.
    /// "완성 상태"에서 시작해 정방향 이동 규칙의 역연산을 반복해 섞는다.
    ///
    /// 역연산 한 번의 규칙: 출발(src) 막대의 최상단 덩어리 일부를, 자리만
    /// 있으면 도착(dst) 막대의 색과 무관하게 위에 얹는다 — 도착 색을
    /// 맞춰야 한다는 제약을 걸면(정방향 CanMove를 그대로 흉내내면) 한 막대
    /// 안에 서로 다른 색이 절대 섞이지 않는다(항상 도착이 비어있거나 같은
    /// 색일 때만 놓을 수 있으므로, 실제 Water Sort의 "여러 색이 뒤섞인
    /// 시작 배치"가 나올 수 없음 — 이 프로젝트에서 실제로 겪은 버그).
    /// 대신 src 쪽만 안전 조건을 지킨다: 덩어리를 전부 걷어내 다른 색이
    /// 드러나면 안 되므로 최소 1칸은 남긴다(<see cref="ComputeSafeUpperBound"/>).
    /// 이 src쪽 불변식만으로 "역연산을 LIFO로 그대로 되감으면(=일반 플레이
    /// 이동) 항상 완성 상태에 도달한다"는 것은 증명되지만, 같은 도착 막대에
    /// 여러 번 겹쳐 쌓이는 경우까지 손으로 완벽히 증명하진 않았으므로 —
    /// 생성 직후 <see cref="HintSolver"/>로 실제 클리어 경로가 있는지
    /// 검증하고, 없으면 섞는 강도를 낮춰 재시도한다(재사용 노트의 "실패해도
    /// 안전한 쪽으로" 원칙 — shuffleDepth가 0이 되면 완성 상태 그대로라
    /// 반드시 성공하므로 무한 실패는 불가능하다).
    /// </summary>
    public static class RoundGenerator
    {
        public sealed class Parameters
        {
            public int ColorCount;
            public int SlotCount;
            public int EmptyContainerCount;
            public int ShuffleDepth;
            public Random Random;
        }

        private const int MaxVerifyRetries = 8;
        private const int VerifySearchBudget = 150_000;

        public static Board Generate(Parameters p)
        {
            if (p.ColorCount <= 0) throw new ArgumentOutOfRangeException(nameof(p.ColorCount));
            if (p.SlotCount <= 0) throw new ArgumentOutOfRangeException(nameof(p.SlotCount));
            if (p.EmptyContainerCount < 0) throw new ArgumentOutOfRangeException(nameof(p.EmptyContainerCount));

            var rng = p.Random ?? new Random();
            int shuffleDepth = p.ShuffleDepth;

            for (int attempt = 0; attempt < MaxVerifyRetries; attempt++)
            {
                var board = CreateSolvedBoard(p.ColorCount, p.SlotCount, p.EmptyContainerCount);
                ReverseShuffle(board, shuffleDepth, rng);

                // 존재 여부만 필요하므로(최단 경로는 불필요) 큰 보드에서도 실전
                // 시간 안에 끝나는 휴리스틱 탐색으로 검증한다.
                if (HintSolver.FindSolutionHeuristic(board, VerifySearchBudget).Found)
                    return board;

                shuffleDepth = Math.Max(0, shuffleDepth - Math.Max(1, shuffleDepth / 4));
            }

            // 안전망: 이론상 여기 도달하지 않아야 하지만, 도달하더라도 완성
            // 상태 자체는 항상 유효한(0수 클리어) 라운드이므로 게임이 멈추진 않는다.
            return CreateSolvedBoard(p.ColorCount, p.SlotCount, p.EmptyContainerCount);
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

        /// <param name="depth">목표 역연산 횟수(shuffleDepth, M). 유효한 쌍을 못 찾는 시도는
        /// 세지 않되, 무한루프를 막기 위해 depth의 20배까지만 시도한다.</param>
        private static void ReverseShuffle(Board board, int depth, Random rng)
        {
            int containerCount = board.Containers.Count;
            int performed = 0;
            int attemptsLeft = Math.Max(depth, 1) * 20;

            while (performed < depth && attemptsLeft-- > 0)
            {
                int from = rng.Next(containerCount);
                int to = rng.Next(containerCount);
                if (from == to) continue;

                var fromContainer = board.Containers[from];
                var toContainer = board.Containers[to];
                if (fromContainer.IsEmpty) continue;
                if (toContainer.FreeSlots <= 0) continue;
                // 도착 막대의 기존 색과 맞는지는 일부러 확인하지 않는다 — 클래스
                // 주석 참고: 여기서 맞춰버리면 색이 섞인 시작 배치가 나올 수 없다.

                int upperBound = ComputeSafeUpperBound(fromContainer);
                if (upperBound < 1) continue;

                int maxK = Math.Min(upperBound, toContainer.FreeSlots);
                if (maxK < 1) continue;

                int k = maxK == 1 ? 1 : rng.Next(1, maxK + 1);
                var color = fromContainer.TopColor.Value;
                fromContainer.PopRange(k);
                toContainer.PushRange(color, k);
                performed++;
            }
        }

        /// <summary>
        /// 최상단 덩어리를 전부 걷어내면 다른 색이 드러나는 경우, 최소 1칸은
        /// 남겨야 역연산이 안전(=정방향으로 되돌릴 수 있음)하다. 컨테이너
        /// 전체가 한 색뿐이면(바닥까지 비워도 다른 색이 안 드러남) 전부 옮겨도 안전하다.
        /// </summary>
        private static int ComputeSafeUpperBound(Container container)
        {
            int runLength = container.TopRunLength();
            bool wholeContainerIsOneColor = runLength == container.Count;
            return wholeContainerIsOneColor ? runLength : runLength - 1;
        }
    }
}
