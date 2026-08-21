using System.Collections.Generic;

namespace ColorSort.Core
{
    /// <summary>
    /// Undo/Reset을 "행동 역산"이 아니라 풀 스냅샷 스택으로 구현한다 — 상태가
    /// 작은 퍼즐(막대 최대 12개)이라 매번 통째로 복제해도 비용이 없고, 역연산
    /// 로직이 실제 이동 로직과 어긋나 생기는 버그를 구조적으로 없앤다.
    /// </summary>
    public sealed class BoardHistory
    {
        private readonly Stack<Board> _undoStack = new Stack<Board>();
        private Board _initial;

        public bool CanUndo => _undoStack.Count > 0;

        public void Initialize(Board board)
        {
            _initial = board.Clone();
            _undoStack.Clear();
        }

        /// <summary>이동을 실행하기 직전에 호출해, 그 시점 국면을 되돌릴 수 있게 기록한다.</summary>
        public void RecordBeforeMove(Board board)
        {
            _undoStack.Push(board.Clone());
        }

        /// <summary>가장 최근에 기록된 국면으로 되돌린다. 더 되돌릴 게 없으면 null.</summary>
        public Board Undo()
        {
            return _undoStack.Count > 0 ? _undoStack.Pop() : null;
        }

        /// <summary>라운드 시작 시점 국면의 복제본. Reset은 이걸로 교체 후 다시 Initialize한다.</summary>
        public Board GetInitialSnapshot()
        {
            return _initial?.Clone();
        }
    }
}
