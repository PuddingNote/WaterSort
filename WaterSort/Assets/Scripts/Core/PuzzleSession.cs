namespace ColorSort.Core
{
    /// <summary>
    /// UI가 실제로 붙잡고 쓰는 단일 진입점. 이동/실행취소/초기화/클리어·교착
    /// 판정을 한 곳에 모아, UI가 Board를 직접 조작하지 않고 이 API만 거치게 한다.
    /// (기획서 3.1 초기화, 3.2 실행취소, 2.4/2.5 클리어·교착 판정에 대응)
    /// </summary>
    public sealed class PuzzleSession
    {
        private readonly BoardHistory _history = new BoardHistory();

        public Board Board { get; private set; }
        public bool CanUndo => _history.CanUndo;
        public bool IsCleared => ClearChecker.IsCleared(Board);
        public bool HasAnyValidMove => ClearChecker.HasAnyValidMove(Board);

        public PuzzleSession(Board initialBoard)
        {
            Board = initialBoard;
            _history.Initialize(initialBoard);
        }

        public bool CanMove(int fromIndex, int toIndex) => MoveRules.CanMove(Board, fromIndex, toIndex);

        public MoveResult TryMove(int fromIndex, int toIndex)
        {
            if (!CanMove(fromIndex, toIndex))
                return MoveResult.Failed(fromIndex, toIndex);

            _history.RecordBeforeMove(Board);
            return MoveRules.TryMove(Board, fromIndex, toIndex);
        }

        /// <summary>직전 1회 이동을 되돌린다. 더 되돌릴 이동이 없으면 false.</summary>
        public bool TryUndo()
        {
            var previous = _history.Undo();
            if (previous == null) return false;
            Board = previous;
            return true;
        }

        /// <summary>라운드 시작 상태로 복원하고, 실행취소 이력도 함께 초기화한다.</summary>
        public void ResetToInitial()
        {
            Board = _history.GetInitialSnapshot();
            _history.Initialize(Board);
        }
    }
}
