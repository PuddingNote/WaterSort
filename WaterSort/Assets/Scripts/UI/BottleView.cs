using System;
using ColorSort.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 병 하나의 시각 표현. Capacity만큼 슬롯 Image를 미리 만들어두고, Refresh할
    /// 때마다 색만 다시 칠한다(매 이동마다 파괴/생성하지 않아 GC 압박이 없다).
    /// 슬롯이 비어도 칸 자체는 그대로 남고(투명 처리) — 절반만 찬 병 위쪽에
    /// 빈 공간이 보이는 것과 같은 원리.
    /// </summary>
    public sealed class BottleView
    {
        // TODO(sprite): bottle_outline — docs/Sprites.md. 지금은 반투명 사각형으로 유리 느낌만 흉내.
        private static readonly Color OutlinePlaceholder = new Color(1f, 1f, 1f, 0.06f);

        public RectTransform Root { get; }

        private readonly Image[] _slots;
        private readonly Image _highlight;

        public BottleView(Transform parent, int capacity, int containerIndex, Action<int> onTapped)
        {
            var go = new GameObject($"Bottle_{containerIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            Root = (RectTransform)go.transform;
            Root.SetParent(parent, false);
            UiFactory.FixedSize(go, 120f, 420f);

            go.GetComponent<Image>().color = OutlinePlaceholder;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None; // 색 변화는 SetHighlight로 직접 관리
            button.onClick.AddListener(() => onTapped?.Invoke(containerIndex));

            var slotsContainer = UiFactory.CreatePanel(Root, "Slots", Color.clear);
            UiFactory.Stretch(slotsContainer, padding: 6f);
            var layout = slotsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.reverseArrangement = true; // index 0(바닥 유닛)이 화면 아래쪽에 오도록

            _slots = new Image[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var slot = UiFactory.CreateImage(slotsContainer, $"Slot_{i}", sprite: null, Color.clear);
                slot.raycastTarget = false; // 탭은 병 전체(Root의 Button)가 받는다
                _slots[i] = slot;
            }

            _highlight = UiFactory.CreateImage(Root, "Highlight", sprite: null, Color.clear);
            _highlight.raycastTarget = false;
            UiFactory.Stretch((RectTransform)_highlight.transform, padding: -4f);
        }

        public void Refresh(Container container)
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i].color = i < container.Count ? WaterPalette.Get(container.Units[i]) : Color.clear;
        }

        public void SetHighlight(Color color) => _highlight.color = color;
    }
}
