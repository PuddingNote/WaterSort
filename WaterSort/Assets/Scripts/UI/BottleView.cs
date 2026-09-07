using System;
using System.Collections.Generic;
using ColorSort.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 병 하나의 시각 표현. 이전엔 슬롯(칸) 하나하나를 개별 Image로 두고 색만
    /// 즉시 바꿔치는 방식이었는데, 그러면 "물이 조금씩 빠지고 조금씩 차오르는"
    /// 붓기 애니메이션을 표현할 수 없다(색이 팍 바뀌거나 안 바뀌거나 둘 뿐이라서).
    /// 그래서 내용물을 색상별 연속 구간(세그먼트)으로 표현하고, 세그먼트 하나의
    /// 높이를 직접 Lerp할 수 있게 <see cref="PourAnimator"/>에게 핸들을 내준다.
    ///
    /// 탭 히트박스(<see cref="Root"/>)와 실제로 기울어지는 그림(<see cref="Visual"/>)을
    /// 분리했다 — 붓는 병이 기울어도 손가락이 누르는 판정 영역은 고정된 사각형
    /// 그대로 유지해야 조작감이 안 흔들린다.
    /// </summary>
    public sealed class BottleView
    {
        // TODO(sprite): bottle_outline — docs/Sprites.md. 지금은 반투명 사각형으로 유리 느낌만 흉내.
        // 알파 0.06은 붓는 병이 기울어졌을 때 "그릇 자체가 기울었다"는 게 거의 안
        // 보일 만큼 흐려서(물 색깔 띠만 둥둥 떠 있는 것처럼 보임) 0.16으로 올렸다.
        private static readonly Color OutlinePlaceholder = new Color(1f, 1f, 1f, 0.16f);

        public RectTransform Root { get; }

        /// <summary>실제로 기울어지는 그림 루트 — PourAnimator.SetTilt가 이 transform만 돌린다.</summary>
        public RectTransform Visual { get; }

        /// <summary>물이 채워지는 안쪽 영역(패딩 뺀 부분). 세그먼트들이 이 안에서
        /// 바닥부터 쌓인다 — 물줄기 애니메이션이 "병 입구" 좌표를 구할 때도 이
        /// 영역의 위쪽 변을 기준으로 쓴다.</summary>
        public RectTransform FillArea { get; }

        public int Capacity { get; }

        // internal(private 아님) — C#의 private는 "중첩 타입 자신 + 그 안에 또
        // 중첩된 타입"까지만 보이고 바깥 클래스(BottleView 본체)로는 안 넓어진다
        // (반대 방향, 즉 바깥의 private 멤버가 중첩 타입에서 보이는 것만 성립).
        // BeginShrinkTop/BeginGrowTop이 SegmentHandle 생성자를 호출해야 해서 필요.
        internal sealed class Segment
        {
            public ColorId Color;
            public float UnitCount; // 애니메이션 중간값을 표현하려고 소수 허용.
            public Image Image;
        }

        /// <summary>붓기 애니메이션이 세그먼트 하나의 높이를 프레임마다 갱신할 때 쓰는 핸들.
        /// 세그먼트의 Image가 다른 이유로(Refresh 등) 이미 파괴됐으면 조용히 무시한다 —
        /// 겹친 이동으로 이 병이 그 사이 다른 연출에 의해 다시 그려졌을 수 있어서다.</summary>
        public sealed class SegmentHandle
        {
            private readonly BottleView _owner;
            private readonly Segment _segment;
            internal SegmentHandle(BottleView owner, Segment segment) { _owner = owner; _segment = segment; }
            public float UnitCount => _segment.UnitCount;
            public void SetUnitCount(float unitCount) => _owner.ApplySegmentUnitCount(_segment, unitCount);
        }

        // Visual의 회전 축(pivot)의 Y — 바닥에 가까운 축이라야 "따르는" 느낌이 난다.
        private const float VisualPivotY = 0.08f;

        private readonly List<Segment> _segments = new List<Segment>();
        private readonly Image _highlight;

        public BottleView(Transform parent, int capacity, int containerIndex, Action<int> onTapped)
        {
            Capacity = capacity;

            var go = new GameObject($"Bottle_{containerIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            Root = (RectTransform)go.transform;
            Root.SetParent(parent, false);
            UiFactory.FixedSize(go, UiTheme.BottleWidth, UiTheme.BottleHeight);
            go.GetComponent<Image>().color = Color.clear; // 탭 히트박스 전용 — 안 보이고, 기울지도 않음.

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None; // 색 변화는 SetHighlight로 직접 관리
            button.onClick.AddListener(() => onTapped?.Invoke(containerIndex));

            Visual = UiFactory.CreatePanel(Root, "Visual", OutlinePlaceholder);
            UiFactory.Stretch(Visual);
            Visual.pivot = new Vector2(0.5f, VisualPivotY); // 바닥에 가까운 축 — 실제로 "따르는" 느낌이 나는 기울기.

            // UiSkin.BottleBackground가 있으면 그 스프라이트를 그대로 쓴다. 버튼/다이얼로그
            // 배경과 달리 여기서는 OutlinePlaceholder(거의 안 보이는 자리표시자 색)를
            // 그대로 틴트로 남기면 안 된다 — 완성된 그림 위에 16% 알파를 곱하면 그림
            // 자체가 거의 안 보이게 된다. 그래서 스프라이트가 있을 땐 색을 흰색(원래
            // 그림 그대로)으로 되돌린다.
            var bottleSprite = UiTheme.Skin != null ? UiTheme.Skin.BottleBackground : null;
            if (bottleSprite != null)
            {
                var visualImage = Visual.GetComponent<Image>();
                visualImage.sprite = bottleSprite;
                visualImage.type = Image.Type.Sliced;
                visualImage.color = Color.white;
            }

            // 물(세그먼트)은 그냥 네모난 사각형이라, 병 그림이 시험관처럼 목이 좁아지거나
            // 바닥이 둥글면 그 모양 밖으로 네모난 물이 삐져나와 보인다(실제로 겪은 문제).
            // UiSkin.BottleMask(사용자가 직접 그린, 안쪽만 불투명인 그림)가 있으면 Unity의
            // Mask 컴포넌트로 그 실루엣 밖을 진짜로 잘라낸다. 이전에 회전하는 콘텐츠(붓는
            // 병의 물 기울임 효과)에 Mask를 썼을 땐 원인 불명으로 안 먹혔지만(Architecture.md
            // 참고), 이건 회전 없는 고정 실루엣 클리핑이라 같은 문제가 아니다.
            var maskSprite = UiTheme.Skin != null ? UiTheme.Skin.BottleMask : null;
            Transform fillAreaParent = Visual;
            if (maskSprite != null)
            {
                var maskRoot = UiFactory.CreatePanel(Visual, "WaterMaskRoot", Color.white);
                UiFactory.Stretch(maskRoot);
                var maskRootImage = maskRoot.GetComponent<Image>();
                maskRootImage.sprite = maskSprite;
                maskRootImage.type = Image.Type.Sliced;
                maskRootImage.raycastTarget = false;

                var mask = maskRoot.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false; // 마스크 그림 자체는 안 보이고 클리핑 역할만.

                fillAreaParent = maskRoot;
            }

            FillArea = UiFactory.CreatePanel(fillAreaParent, "FillArea", Color.clear);
            // 물(세그먼트) 높이는 FillArea.rect.height를 Capacity로 나눈 값을 기준으로
            // 계산하는데(UnitHeight), Visual과 WaterMaskRoot는 병 그림/마스크 그림을
            // 각각 독립적으로 자기 RectTransform에 꽉 채워 늘린다 — 이 둘의 원본
            // 트리밍된 크기가 다르면(예: 병 그림은 목 부분까지 포함해서 트리밍되고,
            // 마스크는 몸통만 트리밍됨) FillArea가 실제 마스크가 보여주는 영역과
            // 어긋난다. 물이 얼마 안 찼을 땐 안 보이다가, 병이 거의 다 찼을 때만
            // 맨 위 물이 마스크에 가려 작아 보이는 버그로 실제로 나타났다(라운드
            // 300, 8칸/5칸 병). 두 그림이 같은 원본 캔버스 크기라는 전제로, 마스크의
            // 트리밍된 사각형을 병 그림의 트리밍된 사각형 기준 비율로 환산해서
            // FillArea 앵커를 맞춘다. 여기에 추가 픽셀 패딩은 더 넣지 않는다(사용자
            // 확정) — 그 비율 자체가 이미 마스크가 실제로 드러내는 영역과 정확히
            // 일치하고, 세그먼트끼리는 baseHeight가 이어 붙여 쌓아서 색 간격이
            // 서로 안 어긋난다(ApplySegmentUnitCount 참고).
            Rect fillNormalized = ComputeFillAreaNormalizedRect(bottleSprite, maskSprite);
            FillArea.anchorMin = new Vector2(fillNormalized.xMin, fillNormalized.yMin);
            FillArea.anchorMax = new Vector2(fillNormalized.xMax, fillNormalized.yMax);
            FillArea.offsetMin = Vector2.zero;
            FillArea.offsetMax = Vector2.zero;

            _highlight = UiFactory.CreateImage(Root, "Highlight", sprite: null, Color.clear);
            _highlight.raycastTarget = false;
            UiFactory.Stretch((RectTransform)_highlight.transform, padding: -4f);
        }

        /// <summary>bottleSprite와 maskSprite가 둘 다 있고 같은 크기의 원본 텍스처에서
        /// 나왔다는 전제 하에, 마스크가 실제로 드러내는 영역이 병 그림 기준 몇 %
        /// 위치인지(0~1)를 계산한다. <see cref="Sprite.rect"/>는 Unity가 알파 기준으로
        /// 자동 트리밍한 뒤의 픽셀 사각형이라(각 텍스처 자기 좌표계 기준), 둘 다 원본
        /// 캔버스 크기가 같으면 그 픽셀 좌표를 그대로 비교해도 된다. 둘 중 하나가
        /// 없거나 전제가 안 맞으면(원본 크기가 다르면) 그냥 (0,0,1,1) 전체를 돌려줘서
        /// 예전처럼 동작한다.</summary>
        private static Rect ComputeFillAreaNormalizedRect(Sprite bottleSprite, Sprite maskSprite)
        {
            Rect fallback = new Rect(0f, 0f, 1f, 1f);
            if (bottleSprite == null || maskSprite == null) return fallback;
            if (bottleSprite.texture == null || maskSprite.texture == null) return fallback;
            if (bottleSprite.texture.width != maskSprite.texture.width ||
                bottleSprite.texture.height != maskSprite.texture.height)
            {
                Debug.LogWarning("[BottleView] BottleBackground와 BottleMask의 원본 텍스처 크기가 달라서 " +
                                  "비율을 맞출 수 없다 — 두 그림을 같은 캔버스 크기로 다시 만들어야 한다. " +
                                  "지금은 예전처럼 6px 패딩만 적용된다.");
                return fallback;
            }

            Rect bottleRect = bottleSprite.rect; // 텍스처 픽셀 좌표(자동 트리밍된 실제 영역).
            Rect maskRect = maskSprite.rect;
            if (bottleRect.width <= 0f || bottleRect.height <= 0f) return fallback;

            float xMin = (maskRect.xMin - bottleRect.xMin) / bottleRect.width;
            float xMax = (maskRect.xMax - bottleRect.xMin) / bottleRect.width;
            float yMin = (maskRect.yMin - bottleRect.yMin) / bottleRect.height;
            float yMax = (maskRect.yMax - bottleRect.yMin) / bottleRect.height;

            return Rect.MinMaxRect(
                Mathf.Clamp01(xMin), Mathf.Clamp01(yMin),
                Mathf.Clamp01(xMax), Mathf.Clamp01(yMax));
        }

        /// <summary>슬롯 1칸의 실제 픽셀 높이 — Count만큼 옮길 때 세그먼트를 얼마나
        /// 늘리고 줄일지 계산하는 기준.</summary>
        public float UnitHeight => FillArea.rect.height / Capacity;

        /// <summary>기울기(도). 0 = 똑바로 선 상태. 붓는 병(출발 병)에만 호출한다.</summary>
        public void SetTilt(float degrees) => Visual.localEulerAngles = new Vector3(0f, 0f, degrees);

        /// <summary>애니메이션 없이 컨테이너 내용을 즉시 반영 — 초기 배치, undo/reset,
        /// 그리고 붓기 애니메이션이 끝난 뒤 최종 스냅에 쓴다.</summary>
        public void Refresh(Container container)
        {
            foreach (var s in _segments)
                if (s.Image != null) UnityEngine.Object.Destroy(s.Image.gameObject);
            _segments.Clear();

            var units = container.Units; // index 0 = 바닥
            int i = 0;
            while (i < units.Count)
            {
                var color = units[i];
                int count = 1;
                while (i + count < units.Count && units[i + count].Equals(color)) count++;
                AppendSegment(color, count);
                i += count;
            }
        }

        public void SetHighlight(Color color) => _highlight.color = color;

        /// <summary>지금 쌓인 물의 총 유닛 수(소수 가능 — 애니메이션 중간값 포함).
        /// <see cref="WaterSurfaceWorldPosition"/>이 이 값으로 실제 수면 높이를 계산한다.</summary>
        public float FilledUnitCount
        {
            get
            {
                float total = 0f;
                foreach (var s in _segments) total += s.UnitCount;
                return total;
            }
        }

        /// <summary>물이 실제로 차 있는 맨 위 표면의 월드 좌표 — 입구(병이 비어 있어도
        /// 항상 고정된 자리)와 다르다. 도착 병으로 쏟아지는 물줄기는 입구가 아니라
        /// 이 수면까지 이어져야 자연스럽다(사용자 확정) — 물이 차오르면서 이 값도
        /// 매 프레임 같이 올라간다.</summary>
        public Vector3 WaterSurfaceWorldPosition()
        {
            var corners = new Vector3[4];
            FillArea.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
            Vector3 bottomCenter = (corners[0] + corners[3]) * 0.5f;
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            float filledFraction = Capacity > 0 ? Mathf.Clamp01(FilledUnitCount / Capacity) : 0f;
            return Vector3.Lerp(bottomCenter, topCenter, filledFraction);
        }

        /// <summary>출발 병 — 맨 위(가장 최근에 쌓인) 세그먼트를 붓는 동안 줄여나갈
        /// 핸들. 세그먼트가 없으면(빈 병) null — 이동 규칙상 출발 병은 항상
        /// 비어있지 않으므로 정상 흐름에서는 일어나지 않는다.</summary>
        public SegmentHandle BeginShrinkTop()
        {
            if (_segments.Count == 0) return null;
            return new SegmentHandle(this, _segments[_segments.Count - 1]);
        }

        /// <summary>도착 병 — 색과 무관하게 항상 높이 0짜리 새 세그먼트를 만들어서
        /// 키워나갈 핸들을 돌려준다(색이 같아도 기존 맨 위 세그먼트를 재사용하지
        /// 않음 — 같은 병에 두 병에서 동시에 같은 색을 쏟아부으면, 둘이 같은
        /// Segment 객체를 공유해서 서로의 애니메이션 진행값을 덮어써버리는 버그가
        /// 실제로 있었다: growStart/growTarget을 각자 다른 시점에 스냅샷 떠서
        /// 절대값으로 SetUnitCount하다 보니, 나중에 시작한 쪽이 앞선 쪽의 진행을
        /// 지워버리거나, 먼저 끝난 쪽의 Refresh가 아직 애니메이션 중인 세그먼트를
        /// 통째로 파괴해서 물이 갑자기 확 차오르는 것처럼 보였다. 색이 같으면
        /// 세그먼트 경계가 안 보이니(둘 다 같은 색 사각형) 여러 개로 쌓여도
        /// 시각적으로는 하나처럼 보이고, 각 붓기가 끝나면 Refresh가 Board 기준으로
        /// 다시 하나로 합쳐 그린다.</summary>
        public SegmentHandle BeginGrowTop(ColorId color)
        {
            var segment = new Segment { Color = color, UnitCount = 0f, Image = CreateSegmentImage() };
            _segments.Add(segment);
            ApplySegmentUnitCount(segment, 0f);
            return new SegmentHandle(this, segment);
        }

        private Image CreateSegmentImage()
        {
            var img = UiFactory.CreateImage(FillArea, "Segment", sprite: null, Color.clear);
            img.raycastTarget = false;

            // UiSkin.WaterFill이 있으면 그 스프라이트를 쓴다 — 흰색/밝은 회색 바탕으로
            // 만든 그림이라 가정하고, 색은 ApplySegmentUnitCount가 매번 WaterPalette
            // 색으로 계속 틴트한다(여기서 색을 건드리지 않음 — 버튼/다이얼로그와 같은
            // 이유로, 스프라이트가 생겼다고 지정한 색이 무시되면 안 됨).
            var waterSprite = UiTheme.Skin != null ? UiTheme.Skin.WaterFill : null;
            if (waterSprite != null)
            {
                img.sprite = waterSprite;
                img.type = Image.Type.Sliced;
            }

            var rect = (RectTransform)img.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f); // 바닥 기준 — 높이만 바뀌어도 아래쪽은 고정.
            return img;
        }

        private void AppendSegment(ColorId color, float unitCount)
        {
            var segment = new Segment { Color = color, UnitCount = unitCount, Image = CreateSegmentImage() };
            _segments.Add(segment);
            ApplySegmentUnitCount(segment, unitCount);
        }

        private void ApplySegmentUnitCount(Segment segment, float unitCount)
        {
            segment.UnitCount = Mathf.Max(0f, unitCount);
            if (segment.Image == null) return; // 다른 붓기가 이미 Refresh로 갈아치웠으면 조용히 무시.

            float baseHeight = 0f;
            foreach (var s in _segments)
            {
                if (s == segment) break;
                baseHeight += s.UnitCount * UnitHeight;
            }

            var rect = (RectTransform)segment.Image.transform;
            rect.anchoredPosition = new Vector2(0f, baseHeight);
            rect.sizeDelta = new Vector2(0f, segment.UnitCount * UnitHeight);
            segment.Image.color = WaterPalette.Get(segment.Color);
        }
    }
}
