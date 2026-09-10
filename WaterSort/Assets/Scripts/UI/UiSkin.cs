using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 스프라이트가 필요한 UI 요소들을 Inspector에서 끌어다 연결할 수 있게 모아둔
    /// ScriptableObject. 코드는 이 값이 비어 있으면(스프라이트 준비 전) 조용히 단색
    /// 사각형으로 대체한다 — 그림이 준비되는 순서와 무관하게 개발이 막히지 않는다.
    ///
    /// 사용법: Project 창에서 우클릭 → Create → ColorSort → UI Skin으로 에셋을
    /// 만들고(딱 1개만 필요), 반드시 <c>Assets/Resources/UiSkin.asset</c> 경로에
    /// 둔다(코드가 Resources.Load로 찾음). 그 에셋의 Inspector에서 스프라이트를
    /// 끌어다 놓으면 끝 — 코드를 다시 안 건드려도 자동 반영된다.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSkin", menuName = "ColorSort/UI Skin")]
    public sealed class UiSkin : ScriptableObject
    {
        [Header("버튼 배경 (9-slice 권장)")]
        public Sprite ButtonBackground;

        [Header("아이콘 버튼 배경 (설정/뒤로/실행취소/초기화/힌트/추가 등 정사각 버튼 전부 공용)")]
        public Sprite IconButtonBackground;

        [Header("아이콘 버튼 전경 그림 (버튼마다 다른 그림 — 배경 위에 얹힘)")]
        public Sprite SettingsIcon;
        public Sprite BackIcon;
        public Sprite UndoIcon;
        public Sprite ResetIcon;
        public Sprite HintIcon;
        public Sprite AddContainerIcon;

        [Header("팝업/다이얼로그 배경 (9-slice 권장)")]
        public Sprite DialogBackground;

        [Header("물병 배경 (9-slice 권장 — 병 윤곽/유리 그림. 이미 완성된 그림이라 틴트 없이 그대로 씀)")]
        public Sprite BottleBackground;

        [Tooltip("BottleBackground의 실루엣(목이 좁아지거나 바닥이 둥근 모양 등) 안쪽, 즉 실제로 " +
                 "물이 차 있어도 되는 영역만 불투명(알파 255)으로 칠하고 나머지(병 바깥 + 유리 " +
                 "테두리)는 완전히 투명(알파 0)으로 만든 그림 — Unity UI Mask 컴포넌트로 물을 " +
                 "이 실루엣 안에만 보이게 잘라낸다. 비워두면 물이 사각형 그대로 나온다.")]
        public Sprite BottleMask;

        [Header("물 채우기 (9-slice 권장 — 흰색/밝은 회색 바탕으로 만들면 색상별로 자동 틴트됨)")]
        [Tooltip("색마다 그냥 단색으로 칠한 것처럼 밋밋해 보이지 않게 하려면, 이 그림 " +
                 "자체에 세로 방향 명암(예: 왼쪽 30~40%는 밝게, 오른쪽은 어둡게 — 유리 " +
                 "원통에 빛이 비치는 느낌)을 미리 그려 넣으면 된다. 색 틴트는 이 그림의 " +
                 "명암 위에 곱해지는 방식이라, 그림에 그런 굴곡을 넣어두면 색마다 자동으로 " +
                 "같은 굴곡의 하이라이트가 생긴다 — 코드 수정 없이 그림만 바꿔서 됨.")]
        public Sprite WaterFill;

        [Header("유리 하이라이트 (물 위에 얹는 세로 빛줄기 — 그림이 아니라 코드로 생성)")]
        [Tooltip("병에 얹히는 빛줄기의 위치·폭·밝기. 게임 실행 중에 이 슬라이더를 움직여도 " +
                 "바로 반영된다(OnValidate → UiTheme.RefreshGlassHighlightSprite가 같은 텍스처를 " +
                 "그 자리에서 다시 칠한다). 마음에 드는 값을 찾으면 UiTheme의 기본값에도 옮겨 두면 " +
                 "UiSkin이 없어도 같은 모양이 나온다.")]
        [Range(0f, 1f)] public float GlassHighlightPeakX = 0.28f;      // 가장 밝은 지점(0=왼쪽 끝, 1=오른쪽 끝)
        [Range(0.02f, 1f)] public float GlassHighlightSoftness = 0.36f; // 이 폭만큼 좌우로 퍼지며 옅어짐(0이면 나눗셈 터짐 → 하한 0.02)
        [Range(0f, 1f)] public float GlassHighlightMaxAlpha = 0.18f;    // 가장 밝은 지점의 최대 불투명도

        private void OnValidate() => UiTheme.RefreshGlassHighlightSprite();

        [Header("병 완성 축하 이펙트 (BottleCompleteBurst)")]
        [Tooltip("물병 하나를 한 색으로 다 채웠을 때 튀는 작은 이펙트의 시작 위치를, 병 " +
                 "윗변 중앙에서 얼마나 옮길지(디자인 픽셀 — x=오른쪽, y=위). 게임 실행 중에 " +
                 "이 값을 바꾸고 병을 완성시켜 보면서 맞추면 된다. (0,0)이면 병 윗변 정중앙.")]
        public Vector2 BottleCompleteBurstOffset = Vector2.zero;

        [Header("로딩 스피너 (힌트 계산 중 등 — 정사각형, 가운데 정렬된 원형 도트 배치 권장)")]
        [Tooltip("HintLoadingOverlay가 이 그림을 그대로 빙글빙글 회전만 시킨다(따로 애니메이션 " +
                 "프레임 필요 없음) — 점점 흐려지는 도트처럼 그림 자체에 '잔상' 효과가 이미 " +
                 "들어있는 디자인이 회전만으로 가장 자연스럽다. 비워두면 예전처럼 기본 원(circle.png)을 " +
                 "Radial360로 부채꼴 채워서 대신 돌린다.")]
        public Sprite LoadingSpinner;

        [Header("사운드 (선택 — 비워두면 무음 처리)")]
        [Tooltip("물 붓기 사운드. 여러 붓기가 겹치면 항상 이 클립을 재생하는 AudioSource " +
                 "하나를 공유해서, Play()가 자동으로 이전 재생을 끊고 새로 시작한다.")]
        public AudioClip PourSound;
    }
}
