using System;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 재사용 가능한 확인 다이얼로그(딤 배경 + 제목 + 좌우 버튼 2개). "타이틀에서
    /// 종료할지", "게임 화면에서 타이틀로 돌아갈지" 둘 다 이걸로 만든다 —
    /// Desktop의 캐주얼_게임_UI_레이아웃_컨벤션.md 3장 "다이얼로그 표준" 참고.
    /// 패널은 고정 크기(UiTheme.DialogWidth × DialogHeight)라 레이아웃 그룹 없이
    /// 제목/버튼 줄을 절대 좌표로 배치한다.
    /// </summary>
    public static class ConfirmDialog
    {
        /// <param name="canvasRoot">Canvas 바로 아래 등, 현재 화면 위에 그려질 부모.
        /// 화면 자신의 하위가 아니라 Canvas 직속으로 넣어야 항상 최상단에 그려진다.</param>
        /// <param name="leftAction">보통 "취소/뒤로" 성격의 선택지(중립색).</param>
        /// <param name="rightAction">보통 "확정" 성격의 선택지(강조색) — 종료/타이틀 이동 등.</param>
        public static RectTransform Show(
            Transform canvasRoot, string title,
            string leftLabel, Action leftAction,
            string rightLabel, Action rightAction)
        {
            var root = UiFactory.CreatePanel(canvasRoot, "ConfirmDialog", Color.clear);
            UiFactory.Stretch(root);

            // 딤 배경 — 뒤 화면이 살짝 비치면서 클릭은 막는다(image 자체가 raycastTarget).
            var dim = UiFactory.CreateImage(root, "Dim", null, UiTheme.DimBackground);
            UiFactory.Stretch((RectTransform)dim.transform);

            var panel = UiFactory.CreateDialogPanel(root, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(UiTheme.DialogWidth, UiTheme.DialogHeight);
            panel.anchoredPosition = Vector2.zero;

            var titleText = UiFactory.CreateText(panel, title, UiTheme.DialogTitleFontSize, UiTheme.TextPrimary);
            var titleRect = (RectTransform)titleText.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(UiTheme.DialogWidth - 80f, 130f);
            titleRect.anchoredPosition = new Vector2(0f, -90f);

            var buttonsRow = UiFactory.CreatePanel(panel, "ButtonsRow", Color.clear);
            buttonsRow.anchorMin = buttonsRow.anchorMax = new Vector2(0.5f, 0f);
            buttonsRow.pivot = new Vector2(0.5f, 0f);
            buttonsRow.sizeDelta = new Vector2(UiTheme.DialogWidth, UiTheme.DialogButtonHeight);
            buttonsRow.anchoredPosition = new Vector2(0f, 70f);
            UiFactory.AddHorizontalLayout(buttonsRow, spacing: UiTheme.PanelSpacing, forceExpandWidth: false, forceExpandHeight: true);

            UiFactory.CreateButton(buttonsRow, leftLabel, UiTheme.DialogButtonWidth, UiTheme.DialogButtonHeight, UiTheme.SecondaryColor,
                () => { Close(root); leftAction?.Invoke(); });
            UiFactory.CreateButton(buttonsRow, rightLabel, UiTheme.DialogButtonWidth, UiTheme.DialogButtonHeight, UiTheme.DangerColor,
                () => { Close(root); rightAction?.Invoke(); });

            return root;
        }

        private static void Close(RectTransform root)
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
