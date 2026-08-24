using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 타이틀 화면. 제목은 넉넉한 간격을 두고 위쪽에, 부제+버튼은 서로 가깝게
    /// 묶어 그 아래 배치한다(다른 게임 타이틀 화면 레퍼런스 반영). 설정 버튼만
    /// 좌상단 코너에 별도로 둔다(GameDesign.md 4.1). 버튼 클릭은 콜백으로만
    /// 알리고 씬 전환은 이 클래스 밖(부트스트랩)에서 한다.
    /// </summary>
    public static class TitleScreen
    {
        public sealed class Callbacks
        {
            /// <summary>null이면 저장된 진행도(다음 라운드)로 시작. 값이 있으면
            /// 그 라운드로 강제 시작(에디터 전용 테스트 입력에서만 옴).</summary>
            public Action<int?> OnStart;
            public Action OnSettings;
            public Action OnQuit;
        }

        public static RectTransform Build(Transform parent, string title, string subtitle, Callbacks callbacks)
        {
            var root = UiFactory.CreatePanel(parent, "TitleScreen", UiTheme.BackgroundTop);
            UiFactory.Stretch(root);

            BuildSettingsButton(root, callbacks);
            BuildTitleCluster(root, title, subtitle, callbacks);

            return root;
        }

        private static void BuildSettingsButton(RectTransform root, Callbacks callbacks)
        {
            // TODO(sprite): icon_settings_gear — 톱니바퀴 아이콘 준비되면 icon 인자로 교체.
            var button = UiFactory.CreateIconButton(root, icon: null, UiTheme.IconButtonSize, UiTheme.PanelColor,
                () => callbacks?.OnSettings?.Invoke(), fallbackText: "설정");
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(UiTheme.ScreenPadding, -UiTheme.ScreenPadding);
        }

        /// <summary>
        /// 제목 / (부제+버튼) 두 덩어리로 나눠 쌓는다 — 제목과 나머지 사이는
        /// 넓게(56px), 부제와 버튼 사이는 좁게(20px) 붙여야 레퍼런스처럼 "제목만
        /// 살짝 떨어져 있고 부제+버튼은 한 세트로 붙어있는" 느낌이 난다. 각 덩어리
        /// 높이는 ContentSizeFitter가 실제 내용에 맞춰 자동 계산한다.
        /// </summary>
        private static void BuildTitleCluster(RectTransform root, string title, string subtitle, Callbacks callbacks)
        {
            var cluster = UiFactory.CreatePanel(root, "TitleCluster", Color.clear);
            cluster.anchorMin = cluster.anchorMax = new Vector2(0.5f, 1f);
            cluster.pivot = new Vector2(0.5f, 1f);
            cluster.sizeDelta = new Vector2(860f, 0f); // 높이 0 = ContentSizeFitter가 채움
            cluster.anchoredPosition = new Vector2(0f, -560f); // 화면 상단에서 ~29% 지점부터 시작

            var outerLayout = UiFactory.AddVerticalLayout(cluster, spacing: 56f, forceExpandWidth: true, forceExpandHeight: false);
            outerLayout.childAlignment = TextAnchor.UpperCenter;
            AddAutoHeight(cluster);

            // TODO(sprite): logo_title — 글자 대신 로고 이미지로 교체할 수도 있음.
            UiFactory.CreateText(cluster, title, UiTheme.FontSizeTitle, UiTheme.TextPrimary);

            var subGroup = UiFactory.CreatePanel(cluster, "SubtitleAndButtons", Color.clear);
            var subLayout = UiFactory.AddVerticalLayout(subGroup, spacing: 20f, forceExpandWidth: true, forceExpandHeight: false);
            subLayout.childAlignment = TextAnchor.UpperCenter;
            AddAutoHeight(subGroup);

            UiFactory.CreateText(subGroup, subtitle, UiTheme.FontSizeSubtitle, UiTheme.TextSecondary);

            var buttonsRow = UiFactory.CreatePanel(subGroup, "ButtonsRow", Color.clear);
            UiFactory.FixedSize(buttonsRow.gameObject, -1f, UiTheme.ButtonHeightLarge); // 높이만 고정, 폭은 부모 폭에 맞춰 늘어나도 됨(안쪽 정렬로 가운데 모임)
            UiFactory.AddHorizontalLayout(buttonsRow, spacing: UiTheme.PanelSpacing, forceExpandWidth: false, forceExpandHeight: true);

            // TODO(sprite): bg_button_rounded — 준비되면 CreateButton에 배경 스프라이트 전달.
            UiFactory.CreateButton(buttonsRow, "종료", 220f, UiTheme.ButtonHeightLarge, UiTheme.SecondaryColor,
                () => callbacks?.OnQuit?.Invoke());

#if UNITY_EDITOR
            // 에디터에서만 보이는 테스트용 라운드 지정 입력 — 빌드에는 아예 안 들어간다.
            var debugRoundField = BuildDebugRoundField(subGroup);
#endif

            UiFactory.CreateButton(buttonsRow, "시작", 220f, UiTheme.ButtonHeightLarge, UiTheme.PrimaryColor, () =>
            {
                int? overrideRoundId = null;
#if UNITY_EDITOR
                if (int.TryParse(debugRoundField.text, out int parsed) && parsed >= 1)
                    overrideRoundId = parsed;
#endif
                callbacks?.OnStart?.Invoke(overrideRoundId);
            });
        }

#if UNITY_EDITOR
        /// <summary>"테스트 라운드" 입력창 — 값을 넣고 시작을 누르면 그 라운드로
        /// 바로 진입한다(비워두면 평소처럼 저장된 진행도로 시작). 에디터 전용.</summary>
        private static TMP_InputField BuildDebugRoundField(RectTransform subGroup)
        {
            var row = UiFactory.CreatePanel(subGroup, "DebugRoundRow", Color.clear);
            UiFactory.FixedSize(row.gameObject, -1f, 64f);
            var rowLayout = UiFactory.AddHorizontalLayout(row, spacing: 12f, forceExpandWidth: false, forceExpandHeight: true);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;

            UiFactory.CreateText(row, "테스트 라운드", UiTheme.FontSizeBadge, UiTheme.TextSecondary);
            return UiFactory.CreateInputField(row, "예: 30", 140f, 56f);
        }
#endif

        private static void AddAutoHeight(RectTransform rect)
        {
            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
