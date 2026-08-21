namespace ColorSort.Core
{
    /// <summary>
    /// 이동 시도 1회의 결과. UI 연출(붓기 애니메이션 등)이 "무엇이 얼마나
    /// 옮겨졌는지"를 그대로 재생할 수 있도록 필요한 정보만 담는다.
    /// </summary>
    public readonly struct MoveResult
    {
        public bool Success { get; }
        public int FromIndex { get; }
        public int ToIndex { get; }
        public ColorId Color { get; }
        public int Count { get; }

        private MoveResult(bool success, int fromIndex, int toIndex, ColorId color, int count)
        {
            Success = success;
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Color = color;
            Count = count;
        }

        public static MoveResult Succeeded(int fromIndex, int toIndex, ColorId color, int count)
            => new MoveResult(true, fromIndex, toIndex, color, count);

        public static MoveResult Failed(int fromIndex, int toIndex)
            => new MoveResult(false, fromIndex, toIndex, default, 0);
    }
}
