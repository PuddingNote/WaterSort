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
        private static UiSkin _skin;
        private static bool _skinLoadAttempted;

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

        /// <summary>Assets/Resources/UiSkin.asset이 있으면 그걸 쓰고, 없으면 null —
        /// 호출부는 항상 null 체크 후 단색으로 대체해야 한다(Desktop의
        /// 캐주얼_게임_UI_레이아웃_컨벤션.md 참고).</summary>
        public static UiSkin Skin
        {
            get
            {
                if (!_skinLoadAttempted)
                {
                    _skin = Resources.Load<UiSkin>("UiSkin");
                    _skinLoadAttempted = true;
                }
                return _skin;
            }
        }

        // 색상 — 물병 테마 톤(어두운 네이비 + 청량한 포인트 컬러). GameDesign.md 5장 참고.
        // 소재가 바뀌면 이 파일의 값만 바꾸면 된다(로직 코드는 색상값을 모름).
        public static readonly Color BackgroundTop = new Color32(0x14, 0x17, 0x2B, 0xFF); // 타이틀/게임 화면 공용 메인 배경
        public static readonly Color BackgroundBottom = new Color32(0x0A, 0x10, 0x1C, 0xFF);
        public static readonly Color PanelColor = new Color32(0x1E, 0x29, 0x3D, 0xFF);
        public static readonly Color PrimaryColor = new Color32(0x5D, 0xC9, 0xE2, 0xFF);
        public static readonly Color PrimaryColorPressed = new Color32(0x46, 0xA8, 0xC0, 0xFF);
        public static readonly Color SecondaryColor = new Color32(0x8E, 0x44, 0xAD, 0xFF);
        public static readonly Color DangerColor = new Color32(0xE7, 0x4C, 0x3C, 0xFF);
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new Color32(0xB8, 0xC2, 0xD6, 0xFF);
        // 밝은 색 배경 버튼(종료/시작/다이얼로그) 위 텍스트 — 흰 글씨보다 대비가 또렷함.
        public static readonly Color TextOnButton = new Color32(0x1A, 0x1A, 0x1A, 0xFF);
        public static readonly Color Disabled = new Color32(0x55, 0x5D, 0x6E, 0xFF);
        public static readonly Color DialogBackground = new Color32(0x20, 0x24, 0x4A, 0xFF);
        public static readonly Color DimBackground = new Color32(0x0A, 0x0A, 0x1A, 0xA6); // 다이얼로그 뒤 딤 처리(알파는 기존 0.65 유지, 색만 변경)

        // 크기(px, 1080 기준 해상도 가정 — CanvasScaler 참고 해상도와 맞춰서 쓴다)
        public const float ButtonWidthLarge = 400f;
        public const float ButtonHeightLarge = 120f;
        public const float ButtonHeightSmall = 140f; // 96 -> 140: 너무 작다는 피드백 반영
        public const float IconButtonSize = 140f;
        public const float ScreenPadding = 48f;
        public const float PanelSpacing = 24f;
        public const float BottleRowSpacing = 32f; // 같은 줄 안 병 사이 간격
        public const float BottleRowGap = 144f; // 위/아래 줄 사이 간격 (48의 3배 — 사용자 확정)

        // 폰트 크기 — Desktop 캐주얼_게임_UI_레이아웃_컨벤션.md "타이틀/다이얼로그 레이아웃 표준"과 짝을 맞춤.
        public const float FontSizeTitle = 140f;
        public const float FontSizeSubtitle = 50f;
        public const float FontSizeButton = 55f;
        public const float FontSizeBody = 32f;
        public const float FontSizeBadge = 28f;

        // 다이얼로그(팝업 확인창) 규격 — Desktop의 캐주얼_게임_UI_레이아웃_컨벤션.md 참고.
        public const float DialogWidth = 840f;
        public const float DialogHeight = 420f;
        public const float DialogButtonWidth = 300f;
        public const float DialogButtonHeight = 110f;
        public const float DialogTitleFontSize = 56f;
    }
}
