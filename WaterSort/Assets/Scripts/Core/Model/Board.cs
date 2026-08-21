using System.Collections.Generic;

namespace ColorSort.Core
{
    /// <summary>
    /// 한 라운드의 전체 국면(막대들의 배열). <see cref="SlotCount"/>는 그 라운드의
    /// 기준 슬롯 수(메타데이터)일 뿐, 실제 용량은 각 Container가 스스로 들고
    /// 있다 — 병 추가 등으로 용량이 다른 Container가 섞일 가능성을 열어 둔다.
    /// </summary>
    public sealed class Board
    {
        private readonly List<Container> _containers;

        public int SlotCount { get; }
        public IReadOnlyList<Container> Containers => _containers;

        public Board(int slotCount, IEnumerable<Container> containers)
        {
            SlotCount = slotCount;
            _containers = new List<Container>(containers);
        }

        public Board Clone()
        {
            var clones = new List<Container>(_containers.Count);
            foreach (var container in _containers)
                clones.Add(container.Clone());
            return new Board(SlotCount, clones);
        }
    }
}
