using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColorSort.UI
{
    /// <summary>
    /// 타이틀 화면. 레이아웃 규격은 Desktop의 캐주얼_게임_UI_레이아웃_컨벤션.md 2장 "타이틀 화면 표준"을
    /// 그대로 따른다(제목/부제/버튼 절대 위치·크기 고정값). 설정 버튼만 좌상단
    /// 코너에 별도로 두고, 왼쪽 큰 버튼은 이 게임에서는 종료(QUIT)로 쓴다(설정이
    /// 이미 코너에 있으므로). 모든 표시 텍스트는 영어로 통일한다(사용자 확정 정책).
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        public sealed class Callbacks
        {
            /// <summary>null이면 저장된 진행도(다음 라운드)로 시작. 값이 있으면
            /// 그 라운드로 강제 시작(에디터 전용 테스트 입력에서만 옴).</summary>
            public Action<int?> OnStart;
            public Action OnSettings;
            public Action OnQuit;
        }

        private Transform _canvasRoot;
        private Callbacks _callbacks;
        private RectTransform _activeDialog;
#if UNITY_EDITOR
        private TMP_InputField _debugRoundField;
#endif

        public static TitleScreen Build(Transform canvasRoot, string title, string subtitle, Callbacks callbacks)
        {
            var go = new GameObject("TitleScreen", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasRoot, false);
            UiFactory.Stretch(rect);

            var screen = go.AddComponent<TitleScreen>();
            screen.Initialize(rect, canvasRoot, title, subtitle, callbacks);
            return screen;
        }

        private void Initialize(RectTransform root, Transform canvasRoot, string title, string subtitle, Callbacks callbacks)
        {
            _canvasRoot = canvasRoot;
            _callbacks = callbacks;

            var background = UiFactory.CreatePanel(root, "Background", UiTheme.BackgroundTop);
            UiFactory.Stretch(background);

            BuildSettingsButton(root);
            BuildTitle(root, title);
            BuildSubtitle(root, subtitle);
            BuildButtons(root);
        }

        private void Update()
        {
            // 안드로이드 뒤로가기 = Input System에서는 Escape 키로 들어온다.
            // 다이얼로그가 이미 열려있으면 "닫기"로, 없으면 "종료 확인 열기"로 —
            // 두 화면(다이얼로그/이 화면)이 각자 따로 Escape를 읽으면 같은 프레임에
            // 닫혔다가 바로 다시 열리는 경합이 생겨서, 여기 한 곳에서만 처리한다.
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (_activeDialog != null)
            {
                var dialog = _activeDialog;
                _activeDialog = null;
                Destroy(dialog.gameObject);
                return;
            }

            RequestQuit();
        }

        private void BuildSettingsButton(RectTransform root)
        {
            var button = UiFactory.CreateIconButton(root, icon: UiTheme.Skin?.SettingsIcon, UiTheme.IconButtonSize, UiTheme.PanelColor,
                () => _callbacks?.OnSettings?.Invoke(), fallbackText: "SETTINGS");
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(UiTheme.ScreenPadding, -UiTheme.ScreenPadding);
        }

        private void BuildTitle(RectTransform root, string title)
        {
            // TODO(sprite): logo_title — 글자 대신 로고 이미지로 교체할 수도 있음.
            var titleText = UiFactory.CreateText(root, title, UiTheme.FontSizeTitle, UiTheme.TextPrimary);
            AnchorTopCenter(titleText.transform, x: 0f, y: -600f, width: 1000f, height: 180f);
        }

        private void BuildSubtitle(RectTransform root, string subtitle)
        {
            var subtitleText = UiFactory.CreateText(root, subtitle, UiTheme.FontSizeSubtitle, UiTheme.TextSecondary);
            AnchorTopCenter(subtitleText.transform, x: 0f, y: -1100f, width: 900f, height: 70f);
        }

        private void BuildButtons(RectTransform root)
        {
            // TODO(sprite): bg_button_rounded — 준비되면 UiSkin.ButtonBackground에 연결.
            var quitButton = UiFactory.CreateButton(root, "QUIT", UiTheme.ButtonWidthLarge, UiTheme.ButtonHeightLarge,
                UiTheme.SecondaryColor, RequestQuit);
            AnchorTopCenter(quitButton.transform, x: -220f, y: -1200f, width: UiTheme.ButtonWidthLarge, height: UiTheme.ButtonHeightLarge);

            var startButton = UiFactory.CreateButton(root, "START", UiTheme.ButtonWidthLarge, UiTheme.ButtonHeightLarge,
                UiTheme.PrimaryColor, OnStartClicked);
            AnchorTopCenter(startButton.transform, x: 220f, y: -1200f, width: UiTheme.ButtonWidthLarge, height: UiTheme.ButtonHeightLarge);

#if UNITY_EDITOR
            _debugRoundField = BuildDebugRoundField(root);
#endif
        }

        private void OnStartClicked()
        {
            int? overrideRoundId = null;
#if UNITY_EDITOR
            if (_debugRoundField != null && int.TryParse(_debugRoundField.text, out int parsed) && parsed >= 1)
                overrideRoundId = parsed;
#endif
            _callbacks?.OnStart?.Invoke(overrideRoundId);
        }

        private void RequestQuit()
        {
            if (_activeDialog != null) return; // 이미 열려있으면 중복 생성 안 함
            _activeDialog = ConfirmDialog.Show(_canvasRoot, "Exit the game?",
                "BACK", () => _activeDialog = null,
                "QUIT", () => { _activeDialog = null; _callbacks?.OnQuit?.Invoke(); });
        }

#if UNITY_EDITOR
        /// <summary>"테스트 라운드" 입력창 — 값을 넣고 START를 누르면 그 라운드로
        /// 바로 진입한다(비워두면 평소처럼 저장된 진행도로 시작). 에디터 전용.</summary>
        private static TMP_InputField BuildDebugRoundField(RectTransform root)
        {
            var row = UiFactory.CreatePanel(root, "DebugRoundRow", Color.clear);
            AnchorTopCenter(row, x: 0f, y: -1360f, width: 320f, height: 64f);
            var rowLayout = UiFactory.AddHorizontalLayout(row, spacing: 12f, forceExpandWidth: false, forceExpandHeight: true);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;

            UiFactory.CreateText(row, "Test round", UiTheme.FontSizeBadge, UiTheme.TextSecondary);
            return UiFactory.CreateInputField(row, "e.g. 30", 140f, 56f);
        }
#endif

        private static void AnchorTopCenter(Transform target, float x, float y, float width, float height)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }
    }
}
