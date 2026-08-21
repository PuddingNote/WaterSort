using ColorSort.Core;
using NUnit.Framework;

namespace ColorSort.Tests
{
    public class CoreTests
    {
        // 유틸: 색상 int를 그대로 배열로 받아 Board 하나를 만든다 (바닥→입구 순서).
        private static Board Board(int slotCount, params int[][] containers)
            => BoardFactory.Create(slotCount, containers);

        [Test]
        public void CanMove_출발이_비어있으면_불가()
        {
            var board = Board(4, new int[] { }, new[] { 1 });
            Assert.IsFalse(MoveRules.CanMove(board, 0, 1));
        }

        [Test]
        public void CanMove_도착에_여유슬롯이_없으면_불가()
        {
            var board = Board(2, new[] { 1 }, new[] { 2, 2 });
            Assert.IsFalse(MoveRules.CanMove(board, 0, 1));
        }

        [Test]
        public void CanMove_도착_최상단_색이_다르면_불가()
        {
            var board = Board(4, new[] { 1 }, new[] { 2 });
            Assert.IsFalse(MoveRules.CanMove(board, 0, 1));
        }

        [Test]
        public void CanMove_도착이_비어있으면_항상_가능()
        {
            var board = Board(4, new[] { 1 }, new int[] { });
            Assert.IsTrue(MoveRules.CanMove(board, 0, 1));
        }

        [Test]
        public void TryMove_연속된_동일색만_여유슬롯만큼_이동()
        {
            // from: 바닥부터 3,1,1,1 (최상단 1이 3칸 연속) / to: 여유 2칸
            var board = Board(4, new[] { 3, 1, 1, 1 }, new[] { 1, 1 });

            var result = MoveRules.TryMove(board, 0, 1);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.Count); // to의 여유(2칸)만큼만 이동
            Assert.AreEqual(new ColorId(1), result.Color);
            CollectionAssert.AreEqual(new[] { 3, 1 }, ToValues(board.Containers[0]));
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, ToValues(board.Containers[1]));
        }

        [Test]
        public void TryMove_조건_불충족시_아무것도_바꾸지_않음()
        {
            var board = Board(4, new[] { 1 }, new[] { 2, 2, 2, 2 });

            var result = MoveRules.TryMove(board, 0, 1);

            Assert.IsFalse(result.Success);
            CollectionAssert.AreEqual(new[] { 1 }, ToValues(board.Containers[0]));
        }

        [Test]
        public void IsCleared_모든_막대가_비었거나_한색으로_가득차면_true()
        {
            var board = Board(3, new[] { 1, 1, 1 }, new int[] { }, new[] { 2, 2, 2 });
            Assert.IsTrue(ClearChecker.IsCleared(board));
        }

        [Test]
        public void IsCleared_섞여있으면_false()
        {
            var board = Board(3, new[] { 1, 2, 1 });
            Assert.IsFalse(ClearChecker.IsCleared(board));
        }

        [Test]
        public void HasAnyValidMove_교착상태면_false()
        {
            // 모든 막대가 가득 찼고 서로 최상단 색도 달라 옮길 곳이 없음
            var board = Board(2, new[] { 1, 2 }, new[] { 2, 1 });
            Assert.IsFalse(ClearChecker.HasAnyValidMove(board));
        }

        [Test]
        public void PuzzleSession_TryUndo_직전_이동을_되돌림()
        {
            var session = new PuzzleSession(Board(4, new[] { 1, 1 }, new int[] { }));

            session.TryMove(0, 1);
            CollectionAssert.AreEqual(new int[] { }, ToValues(session.Board.Containers[0])); // 이동 후 from은 비어있음

            bool undone = session.TryUndo();

            Assert.IsTrue(undone);
            CollectionAssert.AreEqual(new[] { 1, 1 }, ToValues(session.Board.Containers[0]));
            CollectionAssert.AreEqual(new int[] { }, ToValues(session.Board.Containers[1]));
        }

        [Test]
        public void PuzzleSession_ResetToInitial_여러번_이동해도_시작상태로_복원()
        {
            var session = new PuzzleSession(Board(4, new[] { 1, 1 }, new int[] { }, new[] { 2 }));

            session.TryMove(0, 1);
            session.TryMove(2, 0);

            session.ResetToInitial();

            CollectionAssert.AreEqual(new[] { 1, 1 }, ToValues(session.Board.Containers[0]));
            CollectionAssert.AreEqual(new int[] { }, ToValues(session.Board.Containers[1]));
            CollectionAssert.AreEqual(new[] { 2 }, ToValues(session.Board.Containers[2]));
            Assert.IsFalse(session.CanUndo);
        }

        private static int[] ToValues(Container container)
        {
            var values = new int[container.Count];
            for (int i = 0; i < values.Length; i++) values[i] = container.Units[i].Value;
            return values;
        }
    }
}
