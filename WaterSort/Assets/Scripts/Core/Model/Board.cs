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

        /// <summary>생성 이후에 컨테이너를 하나 더 붙인다 — 병 추가(광고 보상) 기능처럼
        /// 난이도 검증이 끝난 뒤에 순수 보조용으로 덧붙이는 병에 쓴다(RoundBuilder
        /// 참고). 이미 검증된 라운드의 풀림 여부에 영향을 주면 안 되므로, 반드시
        /// 처음엔 비어있고 못 쓰는 상태(UnlockedCapacity 0)로 넣어야 한다 — 그건
        /// 호출부 책임이다.</summary>
        public void AppendContainer(Container container) => _containers.Add(container);
    }
}
