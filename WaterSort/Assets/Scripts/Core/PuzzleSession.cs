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

        // 병 추가(광고 보상) 버튼으로 지금까지 연 총 칸 수 — Board 자체(스냅샷)와
        // 별개로 세션이 끝까지 직접 들고 있는다. TryMove가 만드는 Undo 스냅샷이나
        // Reset의 "라운드 시작 상태"는 전부 이 값이 아직 반영되기 전 시점의
        // Container를 그대로 담고 있으므로, Board를 통째로 옛 스냅샷으로 갈아
        // 끼울 때마다(TryUndo/ResetToInitial) 이 값을 다시 덮어써서 동기화한다
        // — 광고를 이미 봐서 받은 보상은 Undo/Reset으로 없어지면 안 되기
        // 때문이다(사용자 확정).
        private int _bonusUnlockedAmount;

        public Board Board { get; private set; }
        public bool CanUndo => _history.CanUndo;
        public bool IsCleared => ClearChecker.IsCleared(Board);
        public bool HasAnyValidMove => ClearChecker.HasAnyValidMove(Board);

        /// <summary>병 추가(광고 보상) 버튼을 지금 눌러서 효과가 있는지 — 보너스 병이
        /// 이미 최대 용량까지 다 열렸으면 false. 항상 Board의 마지막 컨테이너를
        /// 그 병으로 취급한다(RoundBuilder가 매 라운드 마지막에 하나 붙여 둠).</summary>
        public bool CanUnlockBonusContainer
        {
            get
            {
                var bonus = BonusContainer;
                return bonus != null && bonus.UnlockedCapacity < bonus.Capacity;
            }
        }

        private Container BonusContainer =>
            Board.Containers.Count > 0 ? Board.Containers[Board.Containers.Count - 1] : null;

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

        /// <summary>병 추가 버튼 한 번 = 보너스 병(항상 마지막 컨테이너) 한 칸 열기.
        /// 이미 다 열렸으면 아무것도 안 하고 false. 지금은(광고 SDK 연동 전) 누르는
        /// 즉시 적용되지만, 나중에 보상형 광고 시청 성공 콜백에서 이 메서드를
        /// 부르는 방식으로 그대로 이어붙일 수 있다.</summary>
        public bool TryUnlockBonusContainer()
        {
            if (!CanUnlockBonusContainer) return false;
            _bonusUnlockedAmount++;
            BonusContainer.Unlock();
            return true;
        }

        /// <summary>직전 1회 이동을 되돌린다. 더 되돌릴 이동이 없으면 false.</summary>
        public bool TryUndo()
        {
            var previous = _history.Undo();
            if (previous == null) return false;
            Board = previous;
            SyncBonusUnlock();
            return true;
        }

        /// <summary>라운드 시작 상태로 복원하고, 실행취소 이력도 함께 초기화한다.
        /// 병 추가로 이미 연 칸은 이 복원과 무관하게 유지된다(사용자 확정).</summary>
        public void ResetToInitial()
        {
            Board = _history.GetInitialSnapshot();
            _history.Initialize(Board);
            SyncBonusUnlock();
        }

        /// <summary>Board를 스냅샷으로 통째로 갈아 끼운 직후 항상 불러야 한다 — 그
        /// 스냅샷의 보너스 병은 스냅샷을 뜬 시점까지만 열려있으므로, 그 뒤로
        /// 추가로 연 만큼(_bonusUnlockedAmount)을 다시 적용해서 어긋나지 않게
        /// 맞춘다.</summary>
        private void SyncBonusUnlock()
        {
            BonusContainer?.SetUnlockedCapacity(_bonusUnlockedAmount);
        }
    }
}
