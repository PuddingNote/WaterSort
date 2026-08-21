namespace ColorSort.Core
{
    /// <summary>클리어 판정 + 교착 상태(더 이상 유효한 이동이 없는 상태) 감지.</summary>
    public static class ClearChecker
    {
        public static bool IsCleared(Board board)
        {
            foreach (var container in board.Containers)
                if (!container.IsResolved) return false;
            return true;
        }

        public static bool HasAnyValidMove(Board board)
        {
            var containers = board.Containers;
            for (int from = 0; from < containers.Count; from++)
            {
                if (containers[from].IsEmpty) continue;
                for (int to = 0; to < containers.Count; to++)
                {
                    if (MoveRules.CanMove(board, from, to)) return true;
                }
            }
            return false;
        }
    }
}
