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
        private static Sprite _watchAdBadgeSprite;
        private static bool _watchAdBadgeLoadAttempted;
        private static Sprite _watchAdBadgeBgSprite;
        private static bool _watchAdBadgeBgLoadAttempted;
        private static Sprite _glassHighlightSprite;
        private static Texture2D _glassHighlightTexture; // _glassHighlightSprite가 감싸는 텍스처 — 값 바뀌면 이걸 그 자리에서 다시 칠한다.

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

        /// <summary>병 추가(광고 보상) 버튼 오른쪽 아래에 얹는 "광고 봐야 함" 배지 그림
        /// (초록 원형 배경 + 아이콘이 그림 하나에 다 들어있음 — 텍스트가 아니라 이미지로
        /// 알리는 게 요점, 2026-09-11 사용자 확정). LoadingSpinnerSprite와 같은 이유로
        /// UiSkin이 아니라 Resources/Sprites/watch_ad.png에서 직접 로드한다. 없으면 null —
        /// 그럼 배지가 안 만들어질 뿐 버튼 기능엔 지장 없다(GameView.BuildWatchAdBadge).</summary>
        public static Sprite WatchAdBadgeSprite
        {
            get
            {
                if (!_watchAdBadgeLoadAttempted)
                {
                    _watchAdBadgeSprite = Resources.Load<Sprite>("Sprites/watch_ad");
                    _watchAdBadgeLoadAttempted = true;
                }
                return _watchAdBadgeSprite;
            }
        }

        /// <summary>WatchAdBadgeSprite 뒤에 까는 둥근 사각형 배경(white_square_rounded_128,
        /// 9-slice) — 아이콘만 덩그러니 있으면 심심해서 넣는다(2026-09-11 사용자 확정).
        /// 색은 <see cref="WatchAdBadgeBgColor"/> 틴트. 없으면 배경 없이 아이콘만.</summary>
        public static Sprite WatchAdBadgeBgSprite
        {
            get
            {
                if (!_watchAdBadgeBgLoadAttempted)
                {
                    _watchAdBadgeBgSprite = Resources.Load<Sprite>("Sprites/white_square_rounded_128");
                    _watchAdBadgeBgLoadAttempted = true;
                }
                return _watchAdBadgeBgSprite;
            }
        }

        /// <summary>병(물) 위에 얹는 세로 유리 하이라이트 띠 — 그림 파일이 아니라
        /// 런타임에 코드로 생성한다(1x64 픽셀 가로 그라디언트 텍스처, 한 번만 만들고
        /// 캐시). "그냥 팔레트에 색만 띡 칠한 느낌"이라는 지적으로 예전에 AI로 만든
        /// 정적 이미지(glass_highlight.png)를 붙여봤다가 "그냥 별로"라는 피드백으로
        /// 완전히 롤백한 적이 있다(2026-09-08, docs/Architecture.md 참고) — 그때
        /// 레이어 순서(물 위, 병 그림 아래) 자체는 맞았지만 그림 품질이 별로였다.
        /// 이번엔 외부 그림 품질에 기대지 않고 코드로 직접 부드러운 종 모양
        /// (smoothstep) 알파 그라디언트를 만들어서, 위치/폭/밝기를 전부 아래 상수로
        /// 바로 조절할 수 있게 했다(BottleView가 FillArea 바로 위에 얹는다).</summary>
        public static Sprite GlassHighlightSprite
        {
            get
            {
                if (_glassHighlightSprite == null) _glassHighlightSprite = BuildGlassHighlightSprite();
                return _glassHighlightSprite;
            }
        }

        /// <summary>UiSkin의 유리 하이라이트 값이 바뀌었을 때(UiSkin.OnValidate) 호출 —
        /// 이미 만들어 둔 텍스처를 같은 자리에서 다시 칠하기만 한다. Sprite 객체는
        /// 그대로라 이 스프라이트를 쓰는 모든 병의 Highlight Image가 다음 렌더에
        /// 자동 반영된다(병마다 다시 안 붙여도 됨). 아직 한 번도 안 만들었으면
        /// (_glassHighlightTexture == null) 조용히 무시 — 다음에 처음 접근할 때
        /// 그때 값으로 만들어진다. 게임 실행 중에 Inspector에서 슬라이더를 움직이면
        /// 바로 눈에 보이라고 있는 경로.</summary>
        public static void RefreshGlassHighlightSprite()
        {
            if (_glassHighlightTexture != null) PaintGlassHighlightPixels(_glassHighlightTexture);
        }

        private static Sprite BuildGlassHighlightSprite()
        {
            const int width = 64;
            _glassHighlightTexture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "GlassHighlightGradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            PaintGlassHighlightPixels(_glassHighlightTexture);
            return Sprite.Create(_glassHighlightTexture, new Rect(0f, 0f, width, 1f), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void PaintGlassHighlightPixels(Texture2D texture)
        {
            int width = texture.width;
            var pixels = new Color[width];
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1); // 0(왼쪽 끝) ~ 1(오른쪽 끝).
                float dist = Mathf.Abs(t - GlassHighlightPeakX) / GlassHighlightSoftness;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha * (3f - 2f * alpha); // smoothstep — 부드러운 종 모양.
                pixels[x] = new Color(1f, 1f, 1f, alpha * GlassHighlightMaxAlpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
        }

        // GlassHighlightSprite 모양 조절값 — 이제 UiSkin.asset의 Inspector에서 게임
        // 실행 중에도 바꿀 수 있다(바꾸면 UiSkin.OnValidate가 RefreshGlassHighlightSprite를
        // 불러 같은 텍스처를 그 자리에서 다시 칠한다). UiSkin이 없을 때만 아래 기본값이
        // 쓰인다. 참고 이미지처럼 빛줄기가 왼쪽으로 살짝 치우친(28% 지점) 위치에 오도록.
        public static float GlassHighlightPeakX => Skin != null ? Skin.GlassHighlightPeakX : 0.28f;
        public static float GlassHighlightSoftness =>
            Skin != null ? Mathf.Max(0.02f, Skin.GlassHighlightSoftness) : 0.36f; // 0이면 나눗셈이 터진다.
        public static float GlassHighlightMaxAlpha => Skin != null ? Skin.GlassHighlightMaxAlpha : 0.18f;

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

        // 설정 창 슬라이더(BGM/SFX 볼륨) — 참고 이미지 톤(어두운 트랙 + 청록 채움 + 노란 핸들).
        public static readonly Color SettingsToggleOnColor = PrimaryColor;                       // ON = 청록
        public static readonly Color SettingsToggleOffColor = new Color32(0x55, 0x5D, 0x6E, 0xFF); // OFF = 회색(Disabled와 같은 톤)
        public static readonly Color SliderTrackColor = new Color32(0x3A, 0x3F, 0x52, 0xFF);      // 채워지지 않은 트랙(어두운 회색)
        public static readonly Color SliderFillColor = PrimaryColor;                              // 채워진 부분(청록)
        public static readonly Color SliderHandleColor = new Color32(0xF2, 0xC9, 0x4C, 0xFF);     // 손잡이(노랑)

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

        // 설정 창 규격 — 확인 다이얼로그보다 세로로 길다(제목 + 두 줄(BGM/SFX) + 닫기).
        // 세 그룹(제목 / BGM·SFX / CLOSE) 사이를 넉넉히 띄우려고 높이를 키웠다(2026-09-10).
        public const float SettingsDialogHeight = 720f;
        public const float SettingsRowHeight = 96f;
        public const float SettingsToggleWidth = 150f;
        public const float SettingsTitleFontSize = 68f; // 공용 DialogTitleFontSize(56)보다 크게(사용자 확정).
        public const float SliderTrackThickness = 12f;
        public const float SliderHandleSize = 60f; // 40 -> 60: 손으로 잡기 쉽게 키움(사용자 확정, 2026-09-10).

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

        // 병 추가(광고 보상) 기능 — 아직 안 열린 부분의 병 배경(Visual) 알파 배율.
        // 50% → 25% → 16/255로 점점 낮췄다(다른 병과 구분이 잘 안 된다는 피드백,
        // 마지막은 2026-09-10 사용자 지정 "alpha 16"). 원래 알파에 곱해서 쓴다
        // (배경 스프라이트가 있으면 원래 알파가 1이라 결과가 곧 이 값).
        public const float LockedBottleAlpha = 16f / 255f;

        // 위와 같은 목적의 유리 하이라이트(Highlight) 쪽 잠금 알파. 하이라이트는 항상
        // 흰색(알파 1)으로 만들어지므로 이 값이 곧 잠긴 구간의 절대 알파다. 열린
        // (아래쪽) 구간은 별도 복사본이 알파 1로 덮는다(BottleView 참고).
        public const float LockedHighlightAlpha = 16f / 255f;

        // 병 선택 시 "손으로 살짝 들어올린" 느낌을 주는 연출(2026-09-09 확정) — 이전엔
        // 선택된 병 위에 하이라이트 색을 덮어씌우는 방식이었는데, 그 대신 y축으로
        // 살짝 들어올리는 쪽으로 교체했다. BottleView.SetLiftOffset/GameView.SetBottleLifted
        // 참고. 붓기 자체(PourAnimator)의 큰 들어올리기와는 별개 — 그쪽은 도착 병
        // 높이에 비례(PourHoverHeightRatio)하지만 이건 그냥 "집어 든 티" 정도라 작고
        // 고정된 값.
        public const float BottleSelectLiftHeight = 32f;
        public const float BottleSelectLiftTime = 0.15f;

        // 힌트 버튼 우측 상단에 얹는 "남은 힌트 개수" 배지(2026-09-09 확정, 사이즈는
        // 같은 날 사용자 피드백으로 40→52 확대) — 검은 텍스트(TextOnButton 재사용,
        // 밝은 배경 위 대비용으로 이미 있던 색). 버튼(ButtonHeightSmall=140)보다
        // 훨씬 작게, 모서리에 살짝만 걸치는 정도로 — 바로 옆(16px 간격)에 붙은
        // 병 추가 버튼과 안 겹치게 너무 많이 밀어내지 않는다(HintBadgeOffset 참고).
        public const float HintBadgeSize = 52f;
        public const float HintBadgeFontSize = 36f;
        public static readonly Vector2 HintBadgeOffset = new Vector2(-13f, -13f);

        // 병 추가 버튼 오른쪽 아래 모서리에 걸치는 "광고 봐야 함" 배지(watch_ad.png)
        // + 그 뒤에 까는 둥근 사각형 배경(white_square_rounded_128, 6B9EB7 틴트).
        // 힌트 배지보다 조금 크게(아이콘이 더 복잡함), 모서리에서 살짝 안쪽으로 당겨
        // 화면 밖으로 안 나가고 옆 버튼과도 안 겹치게.
        public const float WatchAdBadgeSize = 68f;
        public const float WatchAdBadgeBgSize = 80f; // 아이콘(68) 뒤에 여백 있게 조금 더 크게.
        public static readonly Vector2 WatchAdBadgeOffset = new Vector2(-14f, 14f); // 병 추가 버튼: 오른쪽 아래(anchor 1,0) 기준.
        public static readonly Color WatchAdBadgeBgColor = new Color32(0x6B, 0x9E, 0xB7, 0xFF);

        // 힌트가 0개일 때 힌트 버튼 위에 뜨는 같은 광고 배지 — 힌트 버튼 중앙(anchor
        // 0.5,0.5) 기준으로 왼쪽 위로 띄우고 살짝 기울인다(2026-09-11 사용자 지정값).
        public static readonly Vector2 HintAdBadgeOffset = new Vector2(-50f, 50f);
        public const float HintAdBadgeRotationZ = 22f;

        // 배지 배경 색 — 평소엔 흰색, 최대치(HintStore.MaxHints)에 도달하면 노란색으로
        // 바뀌어서 "꽉 찼다"를 알려준다(2026-09-09 확정 — 처음엔 숫자 대신 "MAX"
        // 텍스트를 넣었는데, 좁은 원 안이라 잘 안 보인다는 피드백으로 색 변경 방식으로
        // 교체. 숫자는 항상 그대로 보여주고 배경색만 바뀜).
        public static readonly Color HintBadgeNormalColor = Color.white;
        public static readonly Color HintBadgeFullColor = new Color32(0xFF, 0xC8, 0x3D, 0xFF);

        // 힌트가 충전될 때(3라운드 클리어마다) 힌트 버튼 위에 잠깐 떴다 사라지는
        // "+1" 텍스트(2026-09-09 확정) — FloatingHintCharge 참고. 등장은 즉시
        // (페이드인 없음), 그 상태로 위로 떠오르면서 동시에 투명해지다가 사라진다
        // (사용자 확정: "나타났다가 위로 서서히 올라가면서 투명해지면서 사라지도록").
        // 처음엔 배지(버튼 오른쪽 위 모서리)를 기준으로 떴었는데, 그러면 위치가
        // 너무 오른쪽으로 치우쳐 보인다는 피드백으로 힌트 버튼 전체를 기준(=버튼
        // 위쪽 가운데)으로 바꿨다. 색도 처음엔 배지와 같은 노란색이었는데, 흰색이
        // 더 잘 보인다는 피드백으로 TextPrimary(흰색)로 바꿨다.
        //
        // 시작 위치도 재차 조정(2026-09-09) — 버튼 바로 위 모서리에 딱 붙어서
        // 시작하면 배지랑 너무 가까워 보인다고, 사용자가 참고 스크린샷(버튼보다
        // 확연히 위쪽 지점을 표시)을 보내와서 StartExtraRiseY만큼 위로 더 띄운
        // 지점에서 시작하게 했다. 떠오르는 거리(RiseDistance)는 시작점이 이미
        // 높아진 만큼 조금 줄였다(사용자 확정: "지금보다 조금만 더 짧게").
        public const float FloatingHintChargeFontSize = 44f;
        public const float FloatingHintChargeWidth = 160f;
        public const float FloatingHintChargeHeight = 70f;
        public const float FloatingHintChargeStartExtraRiseY = 60f;
        public const float FloatingHintChargeRiseDistance = 50f;
        public const float FloatingHintChargeDuration = 1f;
        public static readonly Color FloatingHintChargeColor = TextPrimary;

        // 붓기 애니메이션 규격 — GameDesign.md TBD 확정(2026-08-25): 총 소요시간 약 1초.
        // 실제 물병 게임처럼 붓는 병이 도착 병 위로 들려 올라가 기울여지고, 다 부으면
        // 제자리로 돌아온다(들어올리기/복귀에 나머지 시간을 나눠 쓴다). 입력은 막지
        // 않으므로 겹친 이동은 그냥 각자 재생되고(PourAnimator 참고), z-order/사운드만
        // "나중 것 우선"으로 정리한다.
        public const float PourLiftTime = 0.2f;
        public const float PourFlowTime = 0.6f;
        public const float PourTiltAngleDeg = 65f; // 도착 병 위에서 붓는 각도라 많이 기울어야 자연스러움.
        // 붓는 동안(PourFlowTime) 물이 줄어드는 만큼 여기까지 각도를 더 눕힌다 —
        // 기울어진 물 영역의 세로 높이는 대략 (병높이 × cosθ)라, θ가 90°(수평)에
        // 가까워질수록 줄어서 남은 물의 수면이 계속 주둥이 근처에 붙는다. 그래야
        // 물이 병 끝에서 자연스럽게 흘러나오는 것처럼 보인다(2026-09-10 사용자
        // 요청). 너무 눕히면 병이 쓰러지는 것처럼 보여서 이 정도에서 멈춘다.
        public const float PourFlowEndTiltAngleDeg = 80f;
        public const float PourHoverHeightRatio = 0.85f; // 도착 병 높이 대비, 그 위로 얼마나 띄울지.
        // 물줄기는 곡선이 아니라 직선 하나 — 짧은 사각형을 여러 개 이어 곡선으로
        // 그렸더니 마디마다 꺾여 보여서 오히려 부자연스러웠다(사용자 확정, 2026-08-26).
        public const float PourStreamBaseThickness = 10f;
        // BottleMask와 BottleBackground 그림 윗부분(입구 쪽 모서리)이 살짝 어긋나 있어서,
        // 물줄기 시작점(스파웃)을 측정할 때만 물 쪽(BottleView._waterVisual)을 이만큼
        // 옆으로 밀어야 병 그림이 물줄기 시작점에 자연스럽게 맞아 보인다(사용자가
        // Scene 뷰에서 직접 맞춰본 값, 2026-09-08 확정 — 오른쪽으로 기울 때 기준,
        // 왼쪽으로 기울 때는 부호 반대로 적용). 실제 렌더링(물/마스크 자체가 보이는
        // 위치)에는 절대 지속적으로 걸면 안 된다 — 그러면 물이 병 유리 실루엣 밖으로
        // 삐져나와 보인다(2026-09-08 버그 수정, PourAnimator.UpdateStream/
        // BottleView.SetWaterHorizontalOffset 참고).
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

        // "STAGE CLEAR" 텍스트 뒤(딤 배경 앞)에서 터지는 원형 파티클 축하 이펙트
        // (2026-09-09 확정, StageClearBurst 참고) — 텍스트가 완전히 나타나는 순간
        // (Hold 구간 시작)에 맞춰 재생한다. 새 파티클 시스템 없이 이미 있는 원형
        // 그림(circle.png)을 여러 개 복제해서 각자 다른 각도로 밀어내는 방식.
        public const int StageClearBurstParticleCount = 14;
        public const float StageClearBurstParticleSize = 30f;
        public const float StageClearBurstMaxDistance = 380f;
        public const float StageClearBurstDuration = 0.7f;
        public static readonly Color StageClearBurstColor = PrimaryColor;

        // 물병 하나를 한 색으로 다 채워 "완성"됐을 때 그 병 윗부분에서 잠깐 튀는 작은
        // 축하 이펙트(StageClearBurst의 축소판 — 2026-09-10 요청). 화면 전체가 아니라
        // 병 하나 크기라 파티클이 더 적고·작고·가깝고·짧다. 색은 상수가 아니라 그 병을
        // 채운 물 색(WaterPalette)을 그대로 쓴다(2026-09-10 — 예전엔 항상 PrimaryColor).
        public const int BottleCompleteBurstParticleCount = 8;
        public const float BottleCompleteBurstParticleSize = 16f;
        public const float BottleCompleteBurstMaxDistance = 90f;
        public const float BottleCompleteBurstDuration = 0.5f;

        // 이펙트 시작 위치를 병 윗변 중앙에서 얼마나 옮길지(디자인 픽셀, x=오른쪽/y=위).
        // 인게임에서 보면서 맞추라고 UiSkin.asset Inspector로 뺐다(없으면 (0,0) = 윗변 정중앙).
        public static Vector2 BottleCompleteBurstOffset =>
            Skin != null ? Skin.BottleCompleteBurstOffset : Vector2.zero;

        /// <summary>타이틀 화면 "Test round" 입력창(에디터 전용) 표시 여부 —
        /// UiSkin.asset의 Inspector 체크박스(기본 true). UiSkin이 없으면 기본값(켬).
        /// TitleScreen이 이 값으로 그 입력창 자체를 만들지 말지 결정한다(스크린샷 등을
        /// 위해 에디터 안에서도 잠깐 감출 수 있게, 2026-09-11 사용자 확정).</summary>
        public static bool ShowTestRoundField => Skin == null || Skin.ShowTestRoundField;
    }
}
