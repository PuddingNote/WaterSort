using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 런타임 코드로 UI 계층을 쌓기 위한 팩토리 메서드 모음(재사용 노트 4장 패턴).
    /// 실제 화면(TitleView, GameView 등)은 이 메서드들을 조합해서 짓는다 —
    /// 프리팹/씬 편집 없음(diff 가독성, 실수로 씬이 바뀌는 사고 방지가 목적).
    /// </summary>
    public static class UiFactory
    {
        public static Canvas CreateRootCanvas(string name = "RootCanvas")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return rect;
        }

        /// <summary>스프라이트가 아직 없으면 sprite=null로 두면 단색 사각형으로 대체된다
        /// (부가 표현이 없다고 기능이 막히면 안 된다는 원칙 — 오디오 없을 때 무음 처리와 동일).</summary>
        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            if (sprite != null) image.type = Image.Type.Sliced;
            return image;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent, string content, float fontSize, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = UiTheme.Font; // 프로젝트 전체 텍스트는 예외 없이 이 폰트(UiTheme 참고)
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        public static Button CreateButton(
            Transform parent, string label, float width, float height, Color background, Action onClick)
        {
            var go = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);

            go.GetComponent<Image>().color = background;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.15f);
            colors.disabledColor = UiTheme.Disabled;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = CreateText(rect, label, UiTheme.FontSizeButton, UiTheme.TextPrimary);
            Stretch((RectTransform)text.transform);

            return button;
        }

        /// <summary>아이콘만 있는 정사각 버튼(설정 톱니바퀴 등).</summary>
        public static Button CreateIconButton(Transform parent, Sprite icon, float size, Color background, Action onClick)
        {
            var go = new GameObject("IconButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);

            go.GetComponent<Image>().color = background;

            var button = go.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (icon != null)
            {
                var iconImage = CreateImage(rect, "Icon", icon, Color.white);
                iconImage.preserveAspect = true;
                Stretch((RectTransform)iconImage.transform, padding: size * 0.2f);
            }

            return button;
        }

        public static VerticalLayoutGroup AddVerticalLayout(
            RectTransform target, float spacing = 0f, RectOffset padding = null,
            bool forceExpandWidth = true, bool forceExpandHeight = false)
        {
            var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = forceExpandHeight;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(
            RectTransform target, float spacing = 0f, RectOffset padding = null,
            bool forceExpandWidth = false, bool forceExpandHeight = true)
        {
            var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset();
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = forceExpandHeight;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return layout;
        }

        /// <summary>레이아웃 그룹이 없는 부모 안에서 자식을 부모 크기에 꽉 채운다
        /// (재사용 노트 5장 함정 3 — 레이아웃 그룹 없는 컨테이너는 자식을 자동으로
        /// 채우지 않고 Unity 기본 크기로 남기므로, 명시적으로 채워야 한다).</summary>
        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>
        /// 레이아웃 그룹 안에서 특정 자식만 고정 크기로 못박고 싶을 때 쓴다. 주의:
        /// 이 값은 그 부모 레이아웃 그룹의 childForceExpand가 해당 축에서 false일 때만
        /// 적용된다(true면 무조건 늘어남 — 재사용 노트 5장 함정 1). 부모 전체를
        /// force-expand로 둬야 하는데 이 자식 하나만 고정하고 싶다면, 이 자식을
        /// 자체 레이아웃 그룹으로 한 번 더 감싸고 그 안쪽 그룹의 force-expand를 끈다.
        /// </summary>
        public static LayoutElement FixedSize(GameObject go, float width, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
            return element;
        }
    }
}
