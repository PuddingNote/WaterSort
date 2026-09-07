using System;
using System.Collections.Generic;

namespace ColorSort.Core
{
    /// <summary>
    /// 막대/용기 하나(물병, 렌치 걸이, 레고 기둥 등 — 소재 무관). 유닛을
    /// 바닥(index 0)부터 입구(마지막 index) 순서로 스택처럼 쌓는다.
    /// 내용물 변형(Push/Pop)은 internal로 잠가, 외부(UI 등)는 항상
    /// <see cref="MoveRules"/>를 거치도록 강제한다. Solver는
    /// InternalsVisibleTo로 예외적으로 직접 접근 가능(라운드 생성/탐색용).
    /// </summary>
    public sealed class Container
    {
        private readonly List<ColorId> _units;

        public int Capacity { get; }

        /// <summary>지금 실제로 쓸 수 있는 칸 수 — 보통은 Capacity와 같지만, 병 추가
        /// (광고 보상) 기능으로 생기는 병은 처음엔 0에서 시작해 버튼 누를 때마다
        /// 1칸씩 늘어난다(Unlock 참고). Capacity는 "이 병이 최종적으로 도달할 수
        /// 있는 크기"라 항상 고정이고, 실제 이동 가능 여부는 전부 이 값(과
        /// FreeSlots/IsFull이 이 값 기준으로 계산되는 것) 하나로 판정된다 — 아직
        /// 안 열린 칸은 존재하지 않는 것처럼 취급된다.</summary>
        public int UnlockedCapacity { get; private set; }

        public IReadOnlyList<ColorId> Units => _units;
        public int Count => _units.Count;
        public bool IsEmpty => _units.Count == 0;
        public bool IsFull => _units.Count == UnlockedCapacity;
        public int FreeSlots => UnlockedCapacity - _units.Count;

        /// <summary>더 이상 손댈 필요가 없는 상태: 비어있거나, 한 색으로 가득 참.</summary>
        public bool IsResolved => IsEmpty || (IsFull && TopRunLength() == Count);

        public ColorId? TopColor => IsEmpty ? (ColorId?)null : _units[_units.Count - 1];

        /// <param name="unlockedCapacity">null이면 capacity와 같다(항상 전부 열려있는
        /// 일반 병 — 기존 호출부는 전부 이 경우). 병 추가 기능처럼 일부만 열린 채로
        /// 시작하려면 명시적으로 넘긴다(0 이상 capacity 이하로 clamp됨).</param>
        public Container(int capacity, IEnumerable<ColorId> initialUnitsBottomToTop = null, int? unlockedCapacity = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            _units = initialUnitsBottomToTop != null
                ? new List<ColorId>(initialUnitsBottomToTop)
                : new List<ColorId>(capacity);
            if (_units.Count > capacity)
                throw new ArgumentException("초기 유닛 수가 capacity를 초과합니다.", nameof(initialUnitsBottomToTop));
            UnlockedCapacity = unlockedCapacity.HasValue
                ? Math.Clamp(unlockedCapacity.Value, 0, capacity)
                : capacity;
            if (_units.Count > UnlockedCapacity)
                throw new ArgumentException("초기 유닛 수가 unlockedCapacity를 초과합니다.", nameof(initialUnitsBottomToTop));
        }

        /// <summary>최상단부터 연속으로 같은 색인 유닛 개수(0개면 비어있음).</summary>
        public int TopRunLength()
        {
            if (IsEmpty) return 0;
            var color = _units[_units.Count - 1];
            int run = 0;
            for (int i = _units.Count - 1; i >= 0 && _units[i].Equals(color); i--)
                run++;
            return run;
        }

        internal void Push(ColorId color)
        {
            if (IsFull) throw new InvalidOperationException("Container가 가득 찼습니다.");
            _units.Add(color);
        }

        internal void PushRange(ColorId color, int count)
        {
            for (int i = 0; i < count; i++) Push(color);
        }

        internal ColorId PopTop()
        {
            if (IsEmpty) throw new InvalidOperationException("Container가 비어있습니다.");
            var color = _units[_units.Count - 1];
            _units.RemoveAt(_units.Count - 1);
            return color;
        }

        internal void PopRange(int count)
        {
            for (int i = 0; i < count; i++) PopTop();
        }

        /// <summary>UnlockedCapacity를 절대값으로 맞춘다(0~Capacity로 clamp) — 병 추가
        /// 버튼이 누적으로 몇 칸을 열었는지는 PuzzleSession이 별도로 기억해 두고,
        /// Undo/Reset으로 Board가 통째로 옛 스냅샷으로 바뀔 때마다 이 메서드로
        /// 다시 맞춰준다(스냅샷 자체엔 그 시점의 값이 그대로 남아있지만, 그 뒤에
        /// 더 연 만큼은 스냅샷에 없으므로).</summary>
        internal void SetUnlockedCapacity(int value) => UnlockedCapacity = Math.Clamp(value, 0, Capacity);

        /// <summary>UnlockedCapacity를 amount만큼 늘린다(Capacity를 넘지 않게 clamp).
        /// 병 추가 버튼 한 번 = 이 호출 한 번(amount 기본 1).</summary>
        internal void Unlock(int amount = 1) => SetUnlockedCapacity(UnlockedCapacity + amount);

        public Container Clone() => new Container(Capacity, _units, UnlockedCapacity);
    }
}
