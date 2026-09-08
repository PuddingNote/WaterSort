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
        private static Sprite _loadingSpinnerSprite;
        private static bool _loadingSpinnerLoadAttempted;

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

        /// <summary>로딩 스피너(힌트 계산 중 등)용 단순 원형 그림 — 병/물처럼 소재별로
        /// 바뀌는 테마 그림이 아니라 Font처럼 항상 쓰는 시스템 UI 요소라 UiSkin이 아니라
        /// 여기서 직접 로드한다. Resources/Sprites/circle.png가 없으면 null — 호출부가
        /// Image.Type.Filled(Radial360)에 sprite=null을 써도 기본 사각형 텍스처 위에
        /// 그대로 동작하니(모양만 원이 아니라 사각형이 됨) 완전히 막히진 않는다.</summary>
        public static Sprite LoadingSpinnerSprite
        {
            get
            {
                if (!_loadingSpinnerLoadAttempted)
                {
                    _loadingSpinnerSprite = Resources.Load<Sprite>("Sprites/circle");
                    _loadingSpinnerLoadAttempted = true;
                }
                return _loadingSpinnerSprite;
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
        public const float ButtonHeightSmall = 140f; // 96 -> 140: 너무 작다는 피드백 반영. 160으로 키웠다가 사용자 요청으로 원복(2026-09-07)
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

        // 로딩 스피너(힌트 계산 중 등) 규격.
        public const float LoadingSpinnerSize = 160f;
        public const float LoadingSpinnerDegreesPerSecond = 260f;

        // 계산이 이 시간(ms) 안에 끝나면 로딩 오버레이를 아예 안 띄운다 — 라운드
        // 생성처럼 대부분은 순식간에 끝나는 작업에 매번 화면을 깜빡이며 로딩을
        // 보여주면 오히려 거슬린다(사용자 확정). 계산이 이보다 오래 걸릴 때만
        // 그제서야 오버레이를 띄운다(GameBootstrap.ShowGame 참고).
        public const int LoadingOverlayShowDelayMs = 150;

        // 병 하나의 고정 크기 — BottleView와 PourAnimator(붓는 병을 그리드에서 떼어내
        // 자유롭게 옮길 때) 둘 다 같은 값을 써야 해서 상수로 뺐다.
        public const float BottleWidth = 120f;
        public const float BottleHeight = 420f;

        // 병 추가(광고 보상) 기능 — 아직 안 열린 부분의 병 배경 알파 배율. 처음엔
        // 50%로 시작했는데 실제로 보니 다른 병과 구분이 잘 안 된다는 피드백으로
        // 25%(=투명도 75%)로 더 낮췄다(사용자 확정, 2026-09-08). 원래 알파에
        // 곱해서 쓴다(원래도 반투명한 자리표시자 색이면 거기서 더 흐려짐).
        public const float LockedBottleAlpha = 0.25f;

        // 붓기 애니메이션 규격 — GameDesign.md TBD 확정(2026-08-25): 총 소요시간 약 1초.
        // 실제 물병 게임처럼 붓는 병이 도착 병 위로 들려 올라가 기울여지고, 다 부으면
        // 제자리로 돌아온다(들어올리기/복귀에 나머지 시간을 나눠 쓴다). 입력은 막지
        // 않으므로 겹친 이동은 그냥 각자 재생되고(PourAnimator 참고), z-order/사운드만
        // "나중 것 우선"으로 정리한다.
        public const float PourLiftTime = 0.2f;
        public const float PourFlowTime = 0.6f;
        public const float PourTiltAngleDeg = 65f; // 도착 병 위에서 붓는 각도라 많이 기울어야 자연스러움.
        public const float PourHoverHeightRatio = 0.85f; // 도착 병 높이 대비, 그 위로 얼마나 띄울지.
        // 물줄기는 곡선이 아니라 직선 하나 — 짧은 사각형을 여러 개 이어 곡선으로
        // 그렸더니 마디마다 꺾여 보여서 오히려 부자연스러웠다(사용자 확정, 2026-08-26).
        public const float PourStreamBaseThickness = 10f;
        // BottleMask와 BottleBackground 그림 윗부분(입구 쪽 모서리)이 살짝 어긋나 있어서,
        // 기울어진 채로 붓는 동안 물 쪽(BottleView._waterVisual)을 이만큼 옆으로 밀어야
        // 병 그림이 물줄기 시작점에 자연스럽게 맞아 보인다(사용자가 Scene 뷰에서 직접
        // 맞춰본 값, 2026-09-08 확정 — 오른쪽으로 기울 때 기준, 왼쪽으로 기울 때는 부호
        // 반대로 적용). BottleView.SetWaterHorizontalOffset 참고.
        public const float PourVisualHorizontalNudge = 18f;

        // 화면 가운데 잠깐 떴다가 사라지는 토스트 텍스트(힌트 더 못 찾을 때 등) 규격 —
        // Toast 참고. 등장: 가운데보다 ToastRiseDistance만큼 위에서 생성돼 가운데로
        // 부드럽게 이동하며 투명->불투명. 유지 후 퇴장: 자리 그대로 불투명->투명.
        public const float ToastFontSize = 55f;
        public const float ToastWidth = 900f;
        public const float ToastHeight = 140f;
        public const float ToastRiseDistance = 60f;
        public const float ToastInDuration = 0.35f;
        public const float ToastHoldDuration = 1.1f;
        public const float ToastOutDuration = 0.4f;

        // 라운드 클리어 시 화면을 덮는 "STAGE CLEAR" 연출(StageClearOverlay) 타이밍 —
        // 재확정(2026-09-08): 총 약 3초, 3구간.
        //   0~1초(FadeIn): 텍스트 0%->100% 불투명, 배경(딤)도 함께 0->목표 알파로 등장.
        //   1~2초(Hold):   텍스트/배경 그대로 유지한 채 뒤에서 실제 라운드 전환 실행
        //                  (전환이 늦어지면 이 구간만 자연히 늘어남 — "약 3초").
        //   2~3초(FadeOut):텍스트 100%->0%. 배경은 딤 다이얼로그와 동일한 목표 알파에
        //                  고정돼 있다가, 텍스트 알파가 그 목표치 밑으로 내려오는
        //                  순간부터 텍스트와 정확히 같은 값으로 함께 0까지 내려간다
        //                  (사용자 확정: "모든 화면이 자연스럽게 투명해지는 것처럼").
        public const float StageClearFadeInTime = 1f;
        public const float StageClearHoldTime = 1f;
        public const float StageClearFadeOutTime = 1f;

        // STAGE CLEAR 텍스트가 두 줄로 꺾이던 문제 수정(2026-09-08) — Title(140)보다
        // 작게, 줄바꿈도 아예 꺼서(StageClearOverlay 참고) 항상 한 줄로 나오게 한다.
        public const float FontSizeStageClear = 110f;
    }
}
