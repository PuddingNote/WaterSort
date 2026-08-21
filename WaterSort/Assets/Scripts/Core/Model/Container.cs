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
        public IReadOnlyList<ColorId> Units => _units;
        public int Count => _units.Count;
        public bool IsEmpty => _units.Count == 0;
        public bool IsFull => _units.Count == Capacity;
        public int FreeSlots => Capacity - _units.Count;

        /// <summary>더 이상 손댈 필요가 없는 상태: 비어있거나, 한 색으로 가득 참.</summary>
        public bool IsResolved => IsEmpty || (IsFull && TopRunLength() == Count);

        public ColorId? TopColor => IsEmpty ? (ColorId?)null : _units[_units.Count - 1];

        public Container(int capacity, IEnumerable<ColorId> initialUnitsBottomToTop = null)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            _units = initialUnitsBottomToTop != null
                ? new List<ColorId>(initialUnitsBottomToTop)
                : new List<ColorId>(capacity);
            if (_units.Count > capacity)
                throw new ArgumentException("초기 유닛 수가 capacity를 초과합니다.", nameof(initialUnitsBottomToTop));
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

        public Container Clone() => new Container(Capacity, _units);
    }
}
