using System.Collections.Generic;

namespace ColorSort.Solver
{
    /// <summary>
    /// 최소 힙(우선순위 큐). BCL의 System.Collections.Generic.PriorityQueue는
    /// .NET 6+ 전제라 Unity 런타임(Mono/IL2CPP)에서 항상 쓸 수 있다고 보장하기
    /// 어려워, 직접 구현해 이식성을 확보한다.
    /// </summary>
    internal sealed class MinHeap<T>
    {
        private readonly List<(float Priority, T Value)> _items = new List<(float, T)>();

        public int Count => _items.Count;

        public void Push(float priority, T value)
        {
            _items.Add((priority, value));
            int i = _items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_items[parent].Priority <= _items[i].Priority) break;
                (_items[parent], _items[i]) = (_items[i], _items[parent]);
                i = parent;
            }
        }

        public T Pop()
        {
            var root = _items[0].Value;
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);

            int i = 0;
            int count = _items.Count;
            while (true)
            {
                int left = i * 2 + 1;
                int right = i * 2 + 2;
                int smallest = i;
                if (left < count && _items[left].Priority < _items[smallest].Priority) smallest = left;
                if (right < count && _items[right].Priority < _items[smallest].Priority) smallest = right;
                if (smallest == i) break;
                (_items[smallest], _items[i]) = (_items[i], _items[smallest]);
                i = smallest;
            }
            return root;
        }
    }
}
