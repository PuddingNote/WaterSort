using System;

namespace ColorSort.Core
{
    /// <summary>
    /// 이동 가능 여부 판정과 실제 이동 실행. 기획서 2.3(범용형) 규칙을 그대로
    /// 코드로 옮긴 것 — 이 클래스를 거치지 않는 Container 조작은 없어야 한다.
    /// </summary>
    public static class MoveRules
    {
        public static bool CanMove(Board board, int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return false;
            var from = board.Containers[fromIndex];
            var to = board.Containers[toIndex];

            if (from.IsEmpty) return false;
            if (to.FreeSlots <= 0) return false;
            if (!to.IsEmpty && to.TopColor != from.TopColor) return false;

            return true;
        }

        /// <summary>
        /// from 최상단부터 연속된 동일 색을, to의 여유 슬롯이 허용하는 만큼만 옮긴다.
        /// 조건 불충족 시 아무것도 바꾸지 않고 실패 결과를 돌려준다.
        /// </summary>
        public static MoveResult TryMove(Board board, int fromIndex, int toIndex)
        {
            if (!CanMove(board, fromIndex, toIndex))
                return MoveResult.Failed(fromIndex, toIndex);

            var from = board.Containers[fromIndex];
            var to = board.Containers[toIndex];

            var color = from.TopColor ?? throw new InvalidOperationException("CanMove가 true인데 from이 비어있습니다.");
            int moveCount = Math.Min(from.TopRunLength(), to.FreeSlots);

            from.PopRange(moveCount);
            to.PushRange(color, moveCount);

            return MoveResult.Succeeded(fromIndex, toIndex, color, moveCount);
        }
    }
}
