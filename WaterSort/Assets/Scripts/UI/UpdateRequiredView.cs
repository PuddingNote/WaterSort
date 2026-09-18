using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 강제 업데이트 차단 창 — <see cref="ConfirmDialog"/>와 같은 규격(딤 배경 +
    /// UiTheme.DialogWidth 패널 + 제목/버튼 절대 좌표)을 그대로 쓰되, ConfirmDialog와
    /// 달리 **절대 닫히지 않는다**(버튼을 눌러도 Destroy 안 함, X 없음) — 강제
    /// 업데이트는 사용자가 실제로 업데이트하기 전까지 계속 막아야 한다
    /// (재사용_시스템_모음.md 1장 "뒤로가기·이탈 방어").
    ///
    /// [QUIT]는 게임을 종료하고, [UPDATE]는 스토어로 보내되 창은 그대로 열어 둔다 —
    /// 업데이트 안 하고 스토어에서 돌아오면 여전히 막혀 있어야 하기 때문.
    ///
    /// 안드로이드 뒤로가기(Escape)는 TitleScreen/GameView가 각자 자기 화면에서
    /// 처리하는데, 이 창이 떠 있는 동안은 그 처리를 완전히 무시시켜야 한다(안 그러면
    /// 뒤로가기로 그 화면 자신의 다이얼로그가 이 차단 창 뒤에서 열려버린다) — 그래서
    /// <see cref="IsActive"/> 정적 플래그를 두고, TitleScreen.Update/GameView.Update
    /// 맨 앞에서 이 값을 확인해 true면 그 화면은 아무것도 안 하게 했다.
    /// </summary>
    public static class UpdateRequiredView
    {
        public static bool IsActive { get; private set; }

        public static void Show(Transform canvasRoot, string message, string storeUrl)
        {
            if (IsActive) return; // 중복 호출 방어(이미 떠 있으면 또 안 만듦).
            IsActive = true;

            var root = UiFactory.CreatePanel(canvasRoot, "UpdateRequiredView", Color.clear);
            UiFactory.Stretch(root);

            var dim = UiFactory.CreateImage(root, "Dim", null, UiTheme.DimBackground);
            UiFactory.Stretch((RectTransform)dim.transform);

            var panel = UiFactory.CreateDialogPanel(root, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(UiTheme.DialogWidth, UiTheme.UpdateRequiredDialogHeight);
            panel.anchoredPosition = Vector2.zero;

            var titleText = UiFactory.CreateText(panel, "Update Required", UiTheme.DialogTitleFontSize, UiTheme.TextPrimary);
            var titleRect = (RectTransform)titleText.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(UiTheme.DialogWidth - 80f, 100f);
            titleRect.anchoredPosition = new Vector2(0f, -64f);

            // 본문 — 원격 JSON의 message(운영자가 자유롭게 바꿔 넣는 안내 문구, 여러
            // 줄일 수 있음). 제목 한 줄만 있던 ConfirmDialog와 달리 이 자리가 새로
            // 필요해서 UpdateRequiredDialogHeight로 패널을 더 키웠다.
            var bodyText = UiFactory.CreateText(panel, message, UiTheme.UpdateRequiredBodyFontSize, UiTheme.TextSecondary);
            var bodyRect = (RectTransform)bodyText.transform;
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.sizeDelta = new Vector2(UiTheme.DialogWidth - 120f, 240f);
            bodyRect.anchoredPosition = new Vector2(0f, -200f);
            bodyText.enableWordWrapping = true; // 원격 메시지 길이를 통제할 수 없으니 명시적으로 켜 둔다.

            var buttonsRow = UiFactory.CreatePanel(panel, "ButtonsRow", Color.clear);
            buttonsRow.anchorMin = buttonsRow.anchorMax = new Vector2(0.5f, 0f);
            buttonsRow.pivot = new Vector2(0.5f, 0f);
            buttonsRow.sizeDelta = new Vector2(UiTheme.DialogWidth, UiTheme.DialogButtonHeight);
            buttonsRow.anchoredPosition = new Vector2(0f, 44f);
            UiFactory.AddHorizontalLayout(buttonsRow, spacing: UiTheme.PanelSpacing, forceExpandWidth: false, forceExpandHeight: true);

            UiFactory.CreateButton(buttonsRow, "QUIT", UiTheme.DialogButtonWidth, UiTheme.DialogButtonHeight, UiTheme.DangerColor, QuitGame);
            UiFactory.CreateButton(buttonsRow, "UPDATE", UiTheme.DialogButtonWidth, UiTheme.DialogButtonHeight, UiTheme.PrimaryColor,
                () => Application.OpenURL(storeUrl));
        }

        /// <summary>GameBootstrap의 QuitGame과 완전히 같은 처리(에디터에서는 Play
        /// 모드 종료) — 별도 파일에서 한 번 더 쓰기엔 너무 작은 로직이라 그대로 복제.</summary>
        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
