using System;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 타이틀 화면(GameDesign.md "타이틀 화면" 배치: 좌상단 설정, 중앙상단 제목,
    /// 시작/종료 버튼 위에 부제, 하단 시작/종료). 버튼 클릭은 콜백으로만 알리고
    /// 씬 전환/게임 흐름 제어는 이 클래스 밖(부트스트랩)에서 한다.
    /// </summary>
    public static class TitleScreen
    {
        public sealed class Callbacks
        {
            public Action OnStart;
            public Action OnSettings;
            public Action OnQuit;
        }

        public static RectTransform Build(Transform parent, string title, string subtitle, Callbacks callbacks)
        {
            var root = UiFactory.CreatePanel(parent, "TitleScreen", UiTheme.BackgroundTop);
            UiFactory.Stretch(root);

            BuildSettingsButton(root, callbacks);
            BuildTitle(root, title);
            BuildBottomArea(root, subtitle, callbacks);

            return root;
        }

        private static void BuildSettingsButton(RectTransform root, Callbacks callbacks)
        {
            // TODO(sprite): icon_settings_gear — 톱니바퀴 아이콘 준비되면 icon 인자로 교체.
            var button = UiFactory.CreateIconButton(root, icon: null, UiTheme.IconButtonSize, UiTheme.PanelColor,
                () => callbacks?.OnSettings?.Invoke());
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(UiTheme.ScreenPadding, -UiTheme.ScreenPadding);
        }

        private static void BuildTitle(RectTransform root, string title)
        {
            // TODO(sprite): logo_title — 글자 대신 로고 이미지로 교체할 수도 있음(기획서 4.1 "게임 제목(로고)").
            var titleText = UiFactory.CreateText(root, title, UiTheme.FontSizeTitle, UiTheme.TextPrimary);
            var rect = (RectTransform)titleText.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(900f, 220f);
            rect.anchoredPosition = new Vector2(0f, -220f);
        }

        private static void BuildBottomArea(RectTransform root, string subtitle, Callbacks callbacks)
        {
            var bottomArea = UiFactory.CreatePanel(root, "BottomArea", Color.clear);
            bottomArea.anchorMin = new Vector2(0f, 0f);
            bottomArea.anchorMax = new Vector2(1f, 0f);
            bottomArea.pivot = new Vector2(0.5f, 0f);
            bottomArea.sizeDelta = new Vector2(0f, 420f);
            bottomArea.anchoredPosition = new Vector2(0f, UiTheme.ScreenPadding);

            var bottomLayout = UiFactory.AddVerticalLayout(bottomArea, spacing: UiTheme.PanelSpacing, forceExpandWidth: true, forceExpandHeight: false);
            bottomLayout.childAlignment = TextAnchor.LowerCenter;

            var subtitleText = UiFactory.CreateText(bottomArea, subtitle, UiTheme.FontSizeSubtitle, UiTheme.TextSecondary);
            UiFactory.FixedSize(subtitleText.gameObject, 0f, 80f);

            var buttonsRow = UiFactory.CreatePanel(bottomArea, "ButtonsRow", Color.clear);
            UiFactory.FixedSize(buttonsRow.gameObject, 0f, UiTheme.ButtonHeightLarge);
            // 이 안쪽 그룹만 force-expand를 꺼서 버튼이 고정폭을 유지하게 한다(재사용 노트 5장 함정 1).
            UiFactory.AddHorizontalLayout(buttonsRow, spacing: UiTheme.PanelSpacing, forceExpandWidth: false, forceExpandHeight: true);

            UiFactory.CreateButton(buttonsRow, "종료", 260f, UiTheme.ButtonHeightLarge, UiTheme.SecondaryColor,
                () => callbacks?.OnQuit?.Invoke());
            UiFactory.CreateButton(buttonsRow, "시작", 260f, UiTheme.ButtonHeightLarge, UiTheme.PrimaryColor,
                () => callbacks?.OnStart?.Invoke());
        }
    }
}
