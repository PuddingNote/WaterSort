using TMPro;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 색상·폰트·크기·여백 등 모든 매직 넘버를 모아둔 곳(재사용 노트 4장 패턴).
    /// 디자인을 바꿀 땐 여기 한 곳만 고친다.
    /// </summary>
    public static class UiTheme
    {
        private static TMP_FontAsset _font;

        /// <summary>프로젝트 전체 텍스트가 예외 없이 이 폰트를 쓴다(사용자 지정 고정값).
        /// 프리팹/씬에 미리 꽂아두지 않고 코드에서 로드하는 이유는 이 프로젝트가 UI를
        /// 전부 코드로 짓기 때문(재사용 노트 4장) — Resources/Fonts/에 그 폰트만 두면
        /// 새 프로젝트로 복사해도 코드 수정 없이 그대로 동작한다.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                if (_font == null)
                    _font = Resources.Load<TMP_FontAsset>("Fonts/ONE Mobile POP SDF");
                return _font;
            }
        }

        // 색상 — 물병 테마 톤(어두운 네이비 + 청량한 포인트 컬러). GameDesign.md 5장 참고.
        // 소재가 바뀌면 이 파일의 값만 바꾸면 된다(로직 코드는 색상값을 모름).
        public static readonly Color BackgroundTop = new Color32(0x12, 0x1A, 0x2B, 0xFF);
        public static readonly Color BackgroundBottom = new Color32(0x0A, 0x10, 0x1C, 0xFF);
        public static readonly Color PanelColor = new Color32(0x1E, 0x29, 0x3D, 0xFF);
        public static readonly Color PrimaryColor = new Color32(0x5D, 0xC9, 0xE2, 0xFF);
        public static readonly Color PrimaryColorPressed = new Color32(0x46, 0xA8, 0xC0, 0xFF);
        public static readonly Color SecondaryColor = new Color32(0x8E, 0x44, 0xAD, 0xFF);
        public static readonly Color DangerColor = new Color32(0xE7, 0x4C, 0x3C, 0xFF);
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new Color32(0xB8, 0xC2, 0xD6, 0xFF);
        public static readonly Color Disabled = new Color32(0x55, 0x5D, 0x6E, 0xFF);

        // 크기(px, 1080 기준 해상도 가정 — CanvasScaler 참고 해상도와 맞춰서 쓴다)
        public const float ButtonHeightLarge = 140f;
        public const float ButtonHeightSmall = 96f;
        public const float IconButtonSize = 96f;
        public const float ScreenPadding = 48f;
        public const float PanelSpacing = 24f;

        // 폰트 크기
        public const float FontSizeTitle = 96f;
        public const float FontSizeSubtitle = 40f;
        public const float FontSizeButton = 44f;
        public const float FontSizeBody = 32f;
        public const float FontSizeBadge = 28f;
    }
}
