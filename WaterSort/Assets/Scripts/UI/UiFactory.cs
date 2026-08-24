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

        /// <summary>한 줄짜리 텍스트 입력창(TMP_InputField). 지금은 개발자용 라운드
        /// 지정 입력(에디터 전용)에만 쓴다 — 실제 플레이어용 입력 UI가 필요해지면
        /// 그때 옵션(멀티라인, 자리표시자 스타일 등)을 더 넓힌다.</summary>
        public static TMP_InputField CreateInputField(Transform parent, string placeholder, float width, float height)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            FixedSize(go, width, height);

            go.GetComponent<Image>().color = UiTheme.PanelColor;

            var textArea = CreatePanel(rect, "TextArea", Color.clear);
            Stretch(textArea, padding: 12f);
            textArea.gameObject.AddComponent<RectMask2D>();

            var placeholderText = CreateText(textArea, placeholder, UiTheme.FontSizeBody, UiTheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
            placeholderText.fontStyle = FontStyles.Italic;
            Stretch((RectTransform)placeholderText.transform);

            var valueText = CreateText(textArea, string.Empty, UiTheme.FontSizeBody, UiTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)valueText.transform);

            var inputField = go.GetComponent<TMP_InputField>();
            inputField.textViewport = textArea;
            inputField.textComponent = valueText;
            inputField.placeholder = placeholderText;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;

            return inputField;
        }

        public static Button CreateButton(
            Transform parent, string label, float width, float height, Color background, Action onClick)
        {
            var go = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            // 부모가 레이아웃 그룹이면 childControlWidth/Height가 이 자식 크기를 다시
            // 계산해서 덮어쓴다 — LayoutElement 없이는 "지정한 값 없음(0)"으로 간주돼
            // 폭이 0으로 무너진다(실제로 겪은 버그). 그래서 항상 명시적으로 못박아 둔다.
            FixedSize(go, width, height);

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

        /// <summary>아이콘만 있는 정사각 버튼(설정 톱니바퀴 등). 아이콘 스프라이트가
        /// 아직 없으면(<paramref name="icon"/>이 null) <paramref name="fallbackText"/>를
        /// 대신 작게 표시한다 — 뭘 하는 버튼인지조차 알 수 없는 빈 사각형을 막기 위함.</summary>
        public static Button CreateIconButton(
            Transform parent, Sprite icon, float size, Color background, Action onClick, string fallbackText = null)
        {
            var go = new GameObject("IconButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            FixedSize(go, size, size); // CreateButton과 같은 이유

            go.GetComponent<Image>().color = background;

            var button = go.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (icon != null)
            {
                var iconImage = CreateImage(rect, "Icon", icon, Color.white);
                iconImage.preserveAspect = true;
                Stretch((RectTransform)iconImage.transform, padding: size * 0.2f);
            }
            else if (!string.IsNullOrEmpty(fallbackText))
            {
                var text = CreateText(rect, fallbackText, UiTheme.FontSizeBadge, UiTheme.TextPrimary);
                Stretch((RectTransform)text.transform, padding: 4f);
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
            // childControl*을 명시적으로 켜야 LayoutElement 힌트/강제확장이 실제로 자식
            // 크기에 반영된다(꺼져 있으면 레이아웃 그룹이 자식 크기를 아예 안 건드림).
            layout.childControlWidth = true;
            layout.childControlHeight = true;
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
            layout.childControlWidth = true;
            layout.childControlHeight = true;
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
        /// 레이아웃 그룹 안에서 특정 자식의 크기를 못박고 싶을 때 쓴다. width 또는
        /// height에 **음수(예: -1f)를 넘기면 그 축은 건드리지 않는다**(Unity
        /// LayoutElement 관례상 음수 = "지정 안 함" — 0을 넘기면 실제로 폭/높이 0으로
        /// 강제되어 버리니 "이 축은 신경 안 씀"의 의미로 0을 쓰면 안 된다. 실제로 이
        /// 실수 때문에 타이틀 화면 버튼 폭이 0이 되는 버그가 났었다).
        /// 지정한 축은 그 부모 레이아웃 그룹의 childForceExpand가 해당 축에서 false일
        /// 때만 이 값이 그대로 적용된다(true면 강제확장이 우선 — 재사용 노트 5장 함정 1).
        /// </summary>
        public static LayoutElement FixedSize(GameObject go, float width, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            if (width >= 0f)
            {
                element.preferredWidth = width;
                element.flexibleWidth = 0f;
            }
            if (height >= 0f)
            {
                element.preferredHeight = height;
                element.flexibleHeight = 0f;
            }
            return element;
        }
    }
}
