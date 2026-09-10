using System;
using ColorSort.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 사운드 설정 창(딤 배경 + 패널 + "SETTINGS" 제목 + BGM/SFX 두 줄 + CLOSE).
    /// 각 줄은 [라벨] [ON/OFF 토글] [볼륨 슬라이더]. 값은 <see cref="SettingsStore"/>
    /// (PlayerPrefs)가 진실 소스이고, 바꾸는 즉시 <see cref="SoundService.ApplySettings"/>/
    /// <see cref="SoundService.StartBgm"/>로 실제 재생에 반영한다.
    ///
    /// <see cref="ConfirmDialog"/>와 같은 패턴 — Canvas 직속에 붙여 최상단에 뜨고,
    /// 패널은 고정 크기라 레이아웃 그룹 없이 절대 좌표로 배치한다. 여는 쪽
    /// (TitleScreen)이 <c>_activeDialog</c>로 참조를 들고 있다가 Escape로 닫거나
    /// CLOSE(onClosed 콜백)로 닫는다.
    /// </summary>
    public static class SettingsDialog
    {
        public static RectTransform Show(Transform canvasRoot, Action onClosed = null)
        {
            var root = UiFactory.CreatePanel(canvasRoot, "SettingsDialog", Color.clear);
            UiFactory.Stretch(root);
            root.SetAsLastSibling();

            var dim = UiFactory.CreateImage(root, "Dim", null, UiTheme.DimBackground);
            UiFactory.Stretch((RectTransform)dim.transform);

            var panel = UiFactory.CreateDialogPanel(root, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(UiTheme.DialogWidth, UiTheme.SettingsDialogHeight);
            panel.anchoredPosition = Vector2.zero;

            var title = UiFactory.CreateText(panel, "SETTINGS", UiTheme.SettingsTitleFontSize, UiTheme.TextPrimary);
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(UiTheme.DialogWidth - 80f, 120f);
            titleRect.anchoredPosition = new Vector2(0f, -64f);

            // 세 그룹(제목 / BGM·SFX / CLOSE) 사이를 넉넉히 띄운다 — BGM 줄 중심을
            // 패널 위쪽 변에서 290 아래, 두 줄 사이는 RowHeight + 44.
            BuildRow(panel, "BGM", yFromTop: -290f,
                isOn: SettingsStore.BgmEnabled, volume: SettingsStore.BgmVolume,
                onToggle: on => { SettingsStore.BgmEnabled = on; SoundService.Instance?.StartBgm(); SoundService.Instance?.ApplySettings(); },
                onVolume: v => { SettingsStore.BgmVolume = v; SoundService.Instance?.ApplySettings(); });

            BuildRow(panel, "SFX", yFromTop: -290f - UiTheme.SettingsRowHeight - 44f,
                isOn: SettingsStore.SfxEnabled, volume: SettingsStore.SfxVolume,
                onToggle: on => { SettingsStore.SfxEnabled = on; SoundService.Instance?.ApplySettings(); },
                onVolume: v => { SettingsStore.SfxVolume = v; SoundService.Instance?.ApplySettings(); });

            var closeButton = UiFactory.CreateButton(panel, "CLOSE", UiTheme.DialogButtonWidth, UiTheme.DialogButtonHeight,
                UiTheme.PrimaryColor, () => { Close(root); onClosed?.Invoke(); });
            var closeRect = (RectTransform)closeButton.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 72f);

            return root;
        }

        /// <summary>[라벨] [ON/OFF 토글] [슬라이더] 한 줄. yFromTop은 패널 위쪽 변 기준
        /// 이 줄 중심의 y(음수).</summary>
        private static void BuildRow(
            RectTransform panel, string label, float yFromTop,
            bool isOn, float volume, Action<bool> onToggle, Action<float> onVolume)
        {
            var row = UiFactory.CreatePanel(panel, $"Row_{label}", Color.clear);
            row.GetComponent<Image>().raycastTarget = false;
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.sizeDelta = new Vector2(UiTheme.DialogWidth - 100f, UiTheme.SettingsRowHeight);
            row.anchoredPosition = new Vector2(0f, yFromTop);

            // 자식은 전부 row의 왼쪽 변 세로중앙(0, 0.5)에 앵커 — x는 왼쪽 변에서의 거리.
            float rowWidth = row.sizeDelta.x;
            const float labelWidth = 150f; // "BGM"이 두 줄로 안 꺾이게 넉넉히(그만큼 슬라이더가 좁아짐).
            const float gap = 28f;
            float toggleX = labelWidth + gap;
            float sliderX = toggleX + UiTheme.SettingsToggleWidth + gap;
            float sliderWidth = rowWidth - sliderX;

            var labelText = UiFactory.CreateText(row, label, UiTheme.FontSizeButton, UiTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            labelText.enableWordWrapping = false; // 폭이 살짝 모자라도 한 줄로.
            var labelRect = (RectTransform)labelText.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(labelWidth, UiTheme.SettingsRowHeight);
            labelRect.anchoredPosition = Vector2.zero;

            var toggle = BuildToggle(row, isOn, onToggle);
            var toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(0f, 0.5f);
            toggleRect.pivot = new Vector2(0f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(toggleX, 0f);

            var slider = UiFactory.CreateSlider(row, sliderWidth, UiTheme.SliderHandleSize, volume, onVolume);
            var sliderRect = (RectTransform)slider.transform;
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0f, 0.5f);
            sliderRect.pivot = new Vector2(0f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(sliderX, 0f);
        }

        /// <summary>ON/OFF 토글 버튼 — 상태에 따라 색(청록/회색)과 글자가 바뀐다.
        /// CreateButton을 안 쓰고 직접 만든다(글자·색을 눌릴 때마다 갈아끼워야 해서).</summary>
        private static Button BuildToggle(Transform parent, bool initialOn, Action<bool> onChanged)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(UiTheme.SettingsToggleWidth, UiTheme.SettingsRowHeight - 24f);

            var image = go.GetComponent<Image>();
            // UiSkin.ToggleButtonBackground가 있으면 9-slice로, 없으면 단색 사각형.
            // 색(ON=청록/OFF=회색)은 Render()가 image.color 틴트로 매번 갈아끼운다.
            var skinSprite = UiTheme.Skin != null ? UiTheme.Skin.ToggleButtonBackground : null;
            if (skinSprite != null)
            {
                image.sprite = skinSprite;
                image.type = Image.Type.Sliced;
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var text = UiFactory.CreateText(rect, string.Empty, UiTheme.FontSizeButton, UiTheme.TextOnButton);
            UiFactory.Stretch((RectTransform)text.transform);

            bool on = initialOn;
            void Render()
            {
                image.color = on ? UiTheme.SettingsToggleOnColor : UiTheme.SettingsToggleOffColor;
                text.text = on ? "ON" : "OFF";
            }
            Render();

            button.onClick.AddListener(() =>
            {
                SoundService.Instance?.Play(SoundService.Sfx.ButtonTouch); // 끄기 직전 클릭은 들리고, 켜면 켜진 뒤 다음 클릭부터 들림.
                on = !on;
                Render();
                onChanged?.Invoke(on);
            });
            return button;
        }

        private static void Close(RectTransform root)
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
