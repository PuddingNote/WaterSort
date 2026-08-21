using System;
using ColorSort.Core;
using ColorSort.Solver;
using NUnit.Framework;

namespace ColorSort.Tests
{
    public class SolverTests
    {
        [Test]
        public void RoundGenerator_생성된_보드는_기본_구조를_만족한다()
        {
            var board = RoundGenerator.Generate(new RoundGenerator.Parameters
            {
                ColorCount = 4,
                SlotCount = 5,
                EmptyContainerCount = 2,
                ShuffleDepth = 30,
                Random = new Random(1)
            });

            Assert.AreEqual(4 + 2, board.Containers.Count);

            int totalUnits = 0;
            foreach (var container in board.Containers)
            {
                Assert.LessOrEqual(container.Count, container.Capacity);
                totalUnits += container.Count;
            }
            Assert.AreEqual(4 * 5, totalUnits);
        }

        [Test]
        public void RoundGenerator_생성된_보드는_항상_클리어_가능하다_여러_시드에서()
        {
            for (int seed = 0; seed < 30; seed++)
            {
                var board = RoundGenerator.Generate(new RoundGenerator.Parameters
                {
                    ColorCount = 3,
                    SlotCount = 4,
                    EmptyContainerCount = 1,
                    ShuffleDepth = 15,
                    Random = new Random(seed)
                });

                var result = HintSolver.FindShortestSolution(board);

                Assert.IsTrue(result.Found, $"seed={seed}에서 클리어 경로를 찾지 못함");
            }
        }

        [Test]
        public void RoundGenerator_최고난도_구간에서도_짧은_시간안에_풀리는_라운드를_만든다()
        {
            // 기획서 6.5의 "101+" 최고난도 구간 근처(9색×9칸). 예전엔 이 조합에서
            // BFS 검증이 못 찾아 강도를 계속 낮추다 결국 완성 상태(=난이도 0)로
            // 저하됐었다 — 휴리스틱 검증으로 바뀐 뒤에도 여전히 "진짜 섞인" 라운드가
            // 나오는지 확인한다.
            var board = RoundGenerator.Generate(new RoundGenerator.Parameters
            {
                ColorCount = 9,
                SlotCount = 9,
                EmptyContainerCount = 2,
                ShuffleDepth = 300,
                Random = new Random(42)
            });

            Assert.IsFalse(ClearChecker.IsCleared(board), "강도 저하로 완성 상태까지 후퇴하면 안 됨");

            var result = HintSolver.FindSolutionHeuristic(board);
            Assert.IsTrue(result.Found);
            Assert.Greater(result.Moves.Count, 0);
        }

        [Test]
        public void HintSolver_이미_클리어된_보드는_빈_경로를_반환()
        {
            var board = BoardFactory.Create(3, new[] { new[] { 1, 1, 1 }, new int[] { } });
            var result = HintSolver.FindShortestSolution(board);

            Assert.IsTrue(result.Found);
            Assert.AreEqual(0, result.Moves.Count);
        }

        [Test]
        public void HintSolver_한_수만에_클리어되는_보드는_1수를_찾는다()
        {
            // container0: [1] (1칸), container1: [1,1] (여유 1칸) -> 한 번에 클리어
            var board = BoardFactory.Create(3, new[] { new[] { 1 }, new[] { 1, 1 } });

            var result = HintSolver.FindShortestSolution(board);

            Assert.IsTrue(result.Found);
            Assert.AreEqual(1, result.Moves.Count);
            Assert.AreEqual(0, result.Moves[0].FromIndex);
            Assert.AreEqual(1, result.Moves[0].ToIndex);
        }

        [Test]
        public void DifficultyScorer_Score_산식대로_계산()
        {
            float score = DifficultyScorer.Score(
                slotCount: 6, colorCount: 5, colorSpread: 2f, shuffleDepth: 10,
                emptyContainerCount: 2, containerCount: 7);

            float expected = 6 * 3f + 5 * 4f + 2f * 5f + 10 * 1f - 2 * 8f - 7 * 1f;
            Assert.AreEqual(expected, score, 0.0001f);
        }

        [Test]
        public void DifficultyScorer_MeasureColorSpread_색상별_분포_막대_수_평균()
        {
            // color 1 -> container 0,1 (2곳) / color 2 -> container 1 (1곳)
            var board = BoardFactory.Create(3, new[] { new[] { 1 }, new[] { 1, 2 } });

            float spread = DifficultyScorer.MeasureColorSpread(board);

            Assert.AreEqual(1.5f, spread, 0.0001f);
        }
    }
}
