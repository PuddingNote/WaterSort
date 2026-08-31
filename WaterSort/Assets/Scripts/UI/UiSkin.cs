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

        [Header("사운드 (선택 — 비워두면 무음 처리)")]
        [Tooltip("물 붓기 사운드. 여러 붓기가 겹치면 항상 이 클립을 재생하는 AudioSource " +
                 "하나를 공유해서, Play()가 자동으로 이전 재생을 끊고 새로 시작한다.")]
        public AudioClip PourSound;
    }
}
