using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColorSort.Core;
using ColorSort.Managers;
using ColorSort.Solver;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 게임 화면(GameDesign.md UI 배치). <see cref="PuzzleSession"/>을 유일한
    /// 진실 소스로 삼는다 — 조작이 성공하면 Board는 그 즉시 바뀌지만, 화면은
    /// <see cref="PourAnimator"/>가 붓기 연출로 서서히 따라잡는다(선택 표시만
    /// 즉시 갱신). 무효 이동 진동·클리어/교착 팝업은 아직 로그로만 남는다.
    /// </summary>
    public sealed class GameView : MonoBehaviour
    {
        public sealed class Callbacks
        {
            public Action OnBack;
            /// <summary>라운드가 클리어된 순간 호출 — 인자는 이 라운드를 클리어한
            /// 마지막 물의 색(스테이지 클리어 파티클 색을 그 색으로 맞추는 데 씀,
            /// 2026-09-11). 못 구했으면 UiTheme.StageClearBurstColor(기본값).</summary>
            public Action<Color> OnCleared;
        }

        private PuzzleSession _session;
        private Callbacks _callbacks;
        private Transform _canvasRoot;
        private int _roundId;
        private readonly List<BottleView> _bottleViews = new List<BottleView>();
        private RectTransform _bottleArea;
        private Button _undoButton;
        private Button _hintButton;
        private Button _addContainerButton;
        private PourAnimator _pourAnimator;

        // 힌트 버튼 우측 상단 "남은 힌트 개수" 배지(UiTheme.HintBadge* 참고) —
        // 값 자체는 HintStore(PlayerPrefs)가 진실 소스이고, 이건 그 값을 보여주는
        // 텍스트/배경 이미지에 대한 참조일 뿐. 라운드가 바뀌면 GameView 자체가 새로
        // 만들어지므로 Initialize에서 매번 HintStore.LoadCount()로 다시 읽는다.
        private int _hintCount;
        private TextMeshProUGUI _hintCountText;
        private Image _hintBadgeImage;
        private RectTransform _watchAdBadge; // 병 추가 버튼 위 "광고 봐야 함" 이미지 배지.
        private RectTransform _hintAdBadge;  // 힌트가 0개일 때 힌트 버튼 위에 뜨는 같은 배지.

        /// <summary>가장 최근에 한 색으로 완성된 병의 물 색 — 그 이동으로 라운드가
        /// 클리어됐으면 스테이지 클리어 파티클 색을 여기 맞춘다(2026-09-11 사용자
        /// 요청: "항상 파란 계열이던 걸 마지막으로 채운 물 색으로"). PerformMove가
        /// 병 완성을 감지할 때마다 갱신하고, EvaluateBoardState가 클리어 판정
        /// 시점에 OnCleared 콜백으로 넘긴다.</summary>
        private Color? _lastCompletedColor;

        /// <summary>이번 라운드에 "광고 시청 → 힌트 1개"를 이미 한 번 썼는지. 라운드가
        /// 바뀌면 GameView 자체가 새로 만들어져서 자연히 false로 돌아가고, 새로고침
        /// (RESET)에서는 GameView가 유지되므로 OnResetClicked에서 직접 false로 되돌린다.
        /// true면 힌트가 다시 0이 돼도 광고 흐름을 안 열고 버튼을 그냥 비활성화한다
        /// (사용자 확정, 2026-09-11).</summary>
        private bool _adHintUsedThisRound;

        private int? _selectedIndex;
        private RectTransform _activeDialog;
        private RectTransform _effectsLayer;
        private bool _hintInFlight;

        // 선택된 병을 "손으로 살짝 들어올린" 것처럼 표현하는 연출(2026-09-09 확정 —
        // 하이라이트 색 덮어씌우기 대신 y축으로 부드럽게 들어올리는 방식으로 교체).
        // _liftedIndex는 지금 들려 있어야(또는 들리는 애니메이션 진행 중이어야) 할
        // 병 인덱스 — RefreshHighlights가 _selectedIndex와 비교해서 달라졌을 때만
        // 애니메이션을 새로 건다(매번 다시 트는 게 아니라 상태가 바뀐 시점에만).
        private int? _liftedIndex;
        private readonly Dictionary<int, Coroutine> _liftRoutines = new Dictionary<int, Coroutine>();

        /// <param name="showHintChargeAnimation">이 라운드로 넘어오면서 힌트가 실제로
        /// 1개 충전됐으면(3라운드 클리어마다, GameBootstrap 참고) true — 배지가
        /// 만들어진 직후 그 위에 "+1" 애니메이션(FloatingHintCharge)을 재생한다.</param>
        public static GameView Build(Transform parent, int roundId, PuzzleSession session, Callbacks callbacks, bool showHintChargeAnimation = false)
        {
            var go = new GameObject("GameView", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            UiFactory.Stretch(rect);

            var view = go.AddComponent<GameView>();
            view.Initialize(rect, roundId, session, callbacks, showHintChargeAnimation);
            return view;
        }

        private void Initialize(RectTransform root, int roundId, PuzzleSession session, Callbacks callbacks, bool showHintChargeAnimation)
        {
            _canvasRoot = root.parent;
            _roundId = roundId;
            _session = session;
            _callbacks = callbacks;
            _hintCount = HintStore.LoadCount(); // BuildBottomBar가 배지를 만들 때 바로 쓸 수 있게 미리 읽어 둠.

            var background = UiFactory.CreatePanel(root, "Background", UiTheme.BackgroundTop);
            UiFactory.Stretch(background);

            BuildTopBar(root);
            BuildBottleArea(root);
            BuildBottomBar(root);

            // 이 라운드로 넘어오면서 힌트가 실제로 충전됐으면 힌트 버튼 위(가운데)에
            // "+1"이 잠깐 떴다 사라지는 연출을 튼다. 배지(버튼 오른쪽 위 모서리)가
            // 아니라 버튼 전체를 기준으로 잡아야 가운데에서 뜬다 — 배지 기준이었을
            // 땐 너무 오른쪽에 치우쳐 보인다는 피드백으로 버튼 기준으로 바꿨었다.
            //
            // ForceUpdateCanvases가 필요한 이유(실제로 겪은 버그, 2026-09-09):
            // rightGroup은 HorizontalLayoutGroup이 버튼들의 실제 위치(anchoredPosition)를
            // 계산해서 배치하는데, 이 레이아웃 재계산은 Unity가 프레임 끝에 한 번
            // 모아서 처리한다 — 방금 만든 _hintButton의 GetWorldCorners를 이 자리에서
            // 바로 읽으면 아직 레이아웃이 안 끝난 상태(엉뚱한 위치)를 읽어서 "+1"이
            // 버튼 중앙이 아니라 다른 자리(사용자가 보기엔 배지 근처)에서 시작하는
            // 것처럼 보였다. RebuildBottles가 병 배치 직후에 이미 같은 이유로
            // 쓰고 있는 것과 동일한 처방.
            Canvas.ForceUpdateCanvases();

            if (showHintChargeAnimation)
                FloatingHintCharge.Show(_canvasRoot, (RectTransform)_hintButton.transform);

            // 붓는 병(그리드에서 잠깐 떼어내 자유롭게 움직임)과 물줄기 둘 다 병/버튼보다
            // 항상 위에 그려져야 하니 마지막에 만든 형제로 둔다.
            var effectsLayer = UiFactory.CreatePanel(root, "EffectsLayer", Color.clear);
            UiFactory.Stretch(effectsLayer);
            effectsLayer.gameObject.GetComponent<Image>().raycastTarget = false;
            _effectsLayer = effectsLayer; // 병 완성 축하 이펙트(BottleCompleteBurst)도 이 레이어에 얹는다.
            // BottleArea와 같은 이유(모바일 최적화, 2026-09-11) — 물줄기·붓는 병·파티클이
            // 매 프레임 움직여서 따로 뗀다. 여기 그래픽은 전부 raycastTarget=false라
            // (탭 판정을 가로채면 안 됨, 위 GetComponent<Image>().raycastTarget=false와
            // 그 안에서 만들어지는 모든 요소) GraphicRaycaster는 필요 없다.
            effectsLayer.gameObject.AddComponent<Canvas>();

            _pourAnimator = new PourAnimator(this, _session, effectsLayer);

            // 병 추가 버튼을 누르는 순간 바로 뜨도록 라운드 시작 시 미리 로드해 둔다 —
            // 로드는 비동기라 탭한 뒤에야 요청하면 그 자리에서 못 보여줄 수 있다.
            // 로드가 늦게 끝나거나(라운드 시작 직후) 한 번 쓴 뒤 다음 걸 다시 로드하는
            // 동안엔 버튼이 비활성 상태로 멈춰 있는데, 그 상태에서 유저가 아무 것도
            // 안 건드리면 로드가 끝나도 버튼이 계속 비활성으로 보인다(RefreshHighlights를
            // 다시 부를 계기가 없어서) — AdReady 이벤트를 구독해서 그 순간 바로 다시
            // 그려준다. static 이벤트라 OnDestroy에서 반드시 구독 해지해야 한다.
            RewardedAdService.AdReady += OnRewardedAdReady;
            RewardedAdService.Preload(AdUnitIds.BonusContainerRewarded);
            RewardedAdService.Preload(AdUnitIds.HintRewarded); // 힌트 0개일 때 쓰는 광고도 미리(지금은 같은 단위라 중복 무시됨).

            RebuildBottles();
        }

        private void OnDestroy()
        {
            // RewardedAdService.AdReady는 static 이벤트라 여기서 안 끊으면 이 GameView가
            // (다음 라운드로 넘어가며) 파괴된 뒤에도 델리게이트가 계속 남아서, 이후
            // 라운드의 GameView들이 쌓일 때마다 죽은 인스턴스를 계속 부르려 시도하는
            // 메모리 누수/불필요한 null 체크가 쌓인다.
            RewardedAdService.AdReady -= OnRewardedAdReady;
        }

        private void OnRewardedAdReady(string adUnitId)
        {
            if (adUnitId != AdUnitIds.BonusContainerRewarded && adUnitId != AdUnitIds.HintRewarded) return;
            RefreshHighlights(); // 로드가 막 끝난 순간 버튼이 비활성 상태로 멈춰 있지 않게.
        }

        private void Update()
        {
            // 안드로이드 뒤로가기 = Input System에서는 Escape 키로 들어온다.
            // 다이얼로그가 이미 열려있으면 "닫기"로, 없으면 "타이틀 복귀 확인 열기"로 —
            // 한 곳에서만 처리해야 같은 프레임에 닫혔다가 바로 다시 열리는 경합이 안 생긴다.
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (_activeDialog != null)
            {
                var dialog = _activeDialog;
                _activeDialog = null;
                Destroy(dialog.gameObject);
                return;
            }

            RequestBackToTitle();
        }

        private void BuildTopBar(RectTransform root)
        {
            var back = UiFactory.CreateIconButton(root, UiTheme.Skin?.BackIcon, UiTheme.IconButtonSize, UiTheme.PanelColor,
                RequestBackToTitle, fallbackText: "BACK");
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(UiTheme.ScreenPadding, -UiTheme.ScreenPadding);

            // 게임 플레이 화면에는 설정 버튼을 아예 안 둔다(사용자 확정) — 그 자리를
            // 그대로 재활용해서 초기화(RESET) 버튼을 놓는다. 기존에 하단 좌측
            // 그룹에 있던 초기화 버튼은 여기로 옮겨왔으니 그 자리는 없앤다(BuildBottomBar 참고).
            var reset = UiFactory.CreateIconButton(root, UiTheme.Skin?.ResetIcon, UiTheme.IconButtonSize, UiTheme.PanelColor,
                OnResetClicked, fallbackText: "RESET", clickSfx: SoundService.Sfx.Refresh);
            var resetRect = (RectTransform)reset.transform;
            resetRect.anchorMin = resetRect.anchorMax = new Vector2(1f, 1f);
            resetRect.pivot = new Vector2(1f, 1f);
            resetRect.anchoredPosition = new Vector2(-UiTheme.ScreenPadding, -UiTheme.ScreenPadding);

            // 폰트 80으로 커진 만큼 상단 코너 버튼(140 높이)과 안 겹치게 박스를 넉넉히 잡음.
            var roundLabel = UiFactory.CreateText(root, $"ROUND {_roundId}", 80f, UiTheme.TextPrimary);
            var roundRect = (RectTransform)roundLabel.transform;
            roundRect.anchorMin = roundRect.anchorMax = new Vector2(0.5f, 1f);
            roundRect.pivot = new Vector2(0.5f, 1f);
            roundRect.sizeDelta = new Vector2(560f, 110f);
            roundRect.anchoredPosition = new Vector2(0f, -70f);
        }

        private void BuildBottleArea(RectTransform root)
        {
            _bottleArea = UiFactory.CreatePanel(root, "BottleArea", Color.clear);
            _bottleArea.anchorMin = new Vector2(0f, 0f);
            _bottleArea.anchorMax = new Vector2(1f, 1f);
            // 버튼이 140으로 커진 만큼, 그리고 병 사이 여백을 더 넉넉히 달라는 피드백대로
            // 상/하단 바 자리를 더 넓게 비워둔다.
            _bottleArea.offsetMin = new Vector2(UiTheme.ScreenPadding, 300f); // 하단 바 자리 비워둠
            _bottleArea.offsetMax = new Vector2(-UiTheme.ScreenPadding, -280f); // 상단 바 자리 비워둠

            // forceExpandHeight는 일부러 false — true면 줄이 남는 공간을 억지로 채우려고
            // 늘어나면서 병까지 같이 늘어나 버린다(실제로 겪은 버그). 줄은 항상 병의
            // 실제 높이(BottleView가 못박은 고정값)만큼만 차지하고, 두 줄이 서로
            // 붙은 채로 이 영역 안에서 가운데 정렬되면 된다.
            var rows = UiFactory.AddVerticalLayout(_bottleArea, spacing: UiTheme.BottleRowGap, forceExpandWidth: true, forceExpandHeight: false);
            rows.childAlignment = TextAnchor.MiddleCenter;

            // 모바일 최적화(2026-09-11, 빌드 전 점검) — 붓는 동안 물 세그먼트 크기·회전이
            // 매 프레임 바뀌는데, 이게 전부 같은(단 하나뿐인) Canvas 안에 있으면 Unity가
            // 그 변화를 반영할 때마다 캔버스 전체(상/하단 바, 안 움직이는 다른 병들 포함)의
            // 배치(geometry batch)를 다시 계산한다 — 실제로 안 바뀌는 UI까지 매 프레임
            // 다시 그릴 준비를 하는 셈이라 병이 많을수록 손해가 커진다. 병 영역만 별도
            // Canvas로 떼어내면 그 다시 계산 범위가 병 영역 안으로만 좁혀진다(공식 uGUI
            // 최적화 가이드의 "여러 Canvas로 나누기"). 병은 Button(탭 판정)이라 자체
            // GraphicRaycaster가 있어야 별도 Canvas 밑에서도 탭이 인식된다 — 안 붙이면
            // 상위 Canvas의 Raycaster가 이 밑의 그래픽은 못 찾아서 병을 못 누르게 된다.
            _bottleArea.gameObject.AddComponent<Canvas>();
            _bottleArea.gameObject.AddComponent<GraphicRaycaster>();
        }

        private void BuildBottomBar(RectTransform root)
        {
            // 버튼 2개 + 사이 여백 16 — 아이콘 버튼 크기(UiTheme.ButtonHeightSmall)가
            // 바뀌어도 그룹 폭이 자동으로 같이 맞춰지게 상수로 계산해 둔다(하드코딩된
            // 숫자를 매번 손으로 다시 맞추다 어긋난 적이 있어서, 2026-09-07).
            const float groupWidth = UiTheme.ButtonHeightSmall * 2f + 16f;

            // 왼쪽 그룹은 이제 버튼이 하나(Undo)뿐이라 두 칸짜리 폭(groupWidth) 대신
            // 버튼 하나 크기 그대로 잡는다 — 초기화 버튼은 상단바로 옮겨감(BuildTopBar 참고,
            // 설정 버튼 자리를 재활용, 사용자 확정: 게임 화면에 설정 버튼 자체를 없앰).
            var leftGroup = UiFactory.CreatePanel(root, "LeftButtons", Color.clear);
            leftGroup.anchorMin = leftGroup.anchorMax = new Vector2(0f, 0f);
            leftGroup.pivot = new Vector2(0f, 0f);
            leftGroup.sizeDelta = new Vector2(UiTheme.ButtonHeightSmall, UiTheme.ButtonHeightSmall);
            leftGroup.anchoredPosition = new Vector2(UiTheme.ScreenPadding, UiTheme.ScreenPadding);
            UiFactory.AddHorizontalLayout(leftGroup, spacing: 16f, forceExpandWidth: false, forceExpandHeight: true);

            _undoButton = UiFactory.CreateIconButton(leftGroup, UiTheme.Skin?.UndoIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnUndoClicked, fallbackText: "UNDO");

            var rightGroup = UiFactory.CreatePanel(root, "RightButtons", Color.clear);
            rightGroup.anchorMin = rightGroup.anchorMax = new Vector2(1f, 0f);
            rightGroup.pivot = new Vector2(1f, 0f);
            rightGroup.sizeDelta = new Vector2(groupWidth, UiTheme.ButtonHeightSmall);
            rightGroup.anchoredPosition = new Vector2(-UiTheme.ScreenPadding, UiTheme.ScreenPadding);
            UiFactory.AddHorizontalLayout(rightGroup, spacing: 16f, forceExpandWidth: false, forceExpandHeight: true);

            _hintButton = UiFactory.CreateIconButton(rightGroup, UiTheme.Skin?.HintIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnHintClicked, fallbackText: "HINT");
            BuildHintCountBadge(_hintButton.transform);
            UpdateHintBadge(); // _hintCount는 Initialize 맨 앞에서 이미 HintStore.LoadCount()로 읽어 둠.
            _hintAdBadge = CreateWatchAdBadge(_hintButton.transform,
                anchorPivot: new Vector2(0.5f, 0.5f), offset: UiTheme.HintAdBadgeOffset, rotationZ: UiTheme.HintAdBadgeRotationZ);
            _addContainerButton = UiFactory.CreateIconButton(rightGroup, UiTheme.Skin?.AddContainerIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnAddContainerClicked, fallbackText: "ADD");
            _watchAdBadge = CreateWatchAdBadge(_addContainerButton.transform,
                anchorPivot: new Vector2(1f, 0f), offset: UiTheme.WatchAdBadgeOffset, rotationZ: 0f);
        }

        /// <summary>"광고를 봐야 한다"를 텍스트가 아니라 그림으로 알리는 배지 —
        /// watch_ad.png(필름 클래퍼) + 그 뒤 둥근 사각형 배경(white_square_rounded_128,
        /// 6B9EB7 틴트). 병 추가 버튼(오른쪽 아래 모서리)과 힌트 버튼(중앙에서 왼쪽 위로
        /// 크게 띄우고 기울임) 양쪽에서 쓴다(2026-09-11). watch_ad 그림이 없으면 null을
        /// 돌려준다 — 그럼 호출부의 배지 참조가 null이라 표시 토글이 조용히 무시되고
        /// 버튼 기능엔 지장 없다. 배경을 바깥 컨테이너로 삼고 아이콘을 그 자식으로 둬서
        /// SetActive 한 번에 같이 켜지고 꺼진다.</summary>
        private RectTransform CreateWatchAdBadge(Transform parent, Vector2 anchorPivot, Vector2 offset, float rotationZ)
        {
            var sprite = UiTheme.WatchAdBadgeSprite;
            if (sprite == null) return null;

            var bgSprite = UiTheme.WatchAdBadgeBgSprite;
            RectTransform outer;
            if (bgSprite != null)
            {
                var bg = UiFactory.CreateImage(parent, "WatchAdBadge", bgSprite, UiTheme.WatchAdBadgeBgColor);
                bg.type = Image.Type.Sliced; // 둥근 모서리 유지(9-slice).
                bg.raycastTarget = false;
                outer = (RectTransform)bg.transform;
                outer.sizeDelta = new Vector2(UiTheme.WatchAdBadgeBgSize, UiTheme.WatchAdBadgeBgSize);
            }
            else
            {
                outer = UiFactory.CreatePanel(parent, "WatchAdBadge", Color.clear);
                outer.GetComponent<Image>().raycastTarget = false;
                outer.sizeDelta = new Vector2(UiTheme.WatchAdBadgeSize, UiTheme.WatchAdBadgeSize);
            }
            outer.anchorMin = outer.anchorMax = anchorPivot;
            outer.pivot = new Vector2(0.5f, 0.5f);
            outer.anchoredPosition = offset;
            outer.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            var icon = UiFactory.CreateImage(outer, "Icon", sprite, Color.white);
            icon.type = Image.Type.Simple; // 원본 그림 그대로.
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(UiTheme.WatchAdBadgeSize, UiTheme.WatchAdBadgeSize);
            iconRect.anchoredPosition = Vector2.zero;
            return outer;
        }

        /// <summary>힌트 버튼 우측 상단에 얹는 원형 배지(사용자가 다른 게임 스크린샷을
        /// 참고로 요청, 2026-09-09) — 검은 숫자 텍스트(TextOnButton). 배경색은
        /// _hintBadgeImage에 저장해 두고 UpdateHintBadge가 매번 갱신한다(평소 흰색,
        /// 꽉 차면 노란색). 별도 스프라이트를 새로 만들지 않고 이미 있는 원형 그림
        /// (UiTheme.LoadingSpinnerSprite, 로딩 스피너와 같은 에셋)을 재사용한다 —
        /// 여긴 부채꼴로 안 채우고(Type.Simple) 꽉 찬 원 그대로 쓴다. 버튼(정사각형)
        /// 오른쪽 위 모서리에 중심을 살짝 안쪽으로 당겨서(HintBadgeOffset) 걸치게
        /// 배치 — 완전히 절반만 밖으로 나가면 바로 옆(16px 간격)의 병 추가 버튼과
        /// 겹친다.</summary>
        private void BuildHintCountBadge(Transform hintButtonTransform)
        {
            _hintBadgeImage = UiFactory.CreateImage(hintButtonTransform, "CountBadge", UiTheme.LoadingSpinnerSprite, UiTheme.HintBadgeNormalColor);
            _hintBadgeImage.type = Image.Type.Simple; // CreateImage 기본값(Sliced)이 아니라 원본 그림 그대로.
            _hintBadgeImage.preserveAspect = true;
            _hintBadgeImage.raycastTarget = false; // 버튼 클릭 판정을 가로채면 안 됨.

            var badgeRect = (RectTransform)_hintBadgeImage.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(UiTheme.HintBadgeSize, UiTheme.HintBadgeSize);
            badgeRect.anchoredPosition = UiTheme.HintBadgeOffset;

            _hintCountText = UiFactory.CreateText(_hintBadgeImage.transform, string.Empty, UiTheme.HintBadgeFontSize, UiTheme.TextOnButton);
            _hintCountText.raycastTarget = false;
            UiFactory.Stretch((RectTransform)_hintCountText.transform, padding: 4f);
        }

        /// <summary>배지 숫자와 배경색을 _hintCount 기준으로 갱신한다 — 숫자는 항상
        /// 그대로 보여주고, 최대치(HintStore.MaxHints)에 도달하면 배경색만 노란색으로
        /// 바뀐다(사용자 확정, 2026-09-09 — 처음엔 숫자 대신 "MAX" 텍스트를 넣었는데
        /// 좁은 원 안이라 잘 안 보인다는 피드백으로 색 변경 방식으로 교체).</summary>
        private void UpdateHintBadge()
        {
            _hintCountText.text = _hintCount.ToString();
            _hintBadgeImage.color = _hintCount >= HintStore.MaxHints
                ? UiTheme.HintBadgeFullColor
                : UiTheme.HintBadgeNormalColor;
        }

        private void RebuildBottles()
        {
            var existing = new Transform[_bottleArea.childCount];
            for (int i = 0; i < existing.Length; i++) existing[i] = _bottleArea.GetChild(i);
            foreach (var child in existing) Destroy(child.gameObject);
            _bottleViews.Clear();

            var containers = _session.Board.Containers;
            // 위/아래 줄을 항상 비슷하게 채운다 — 홀수면 아래 줄이 1개 더 많게
            // (7개 → 위3/아래4, 10개 → 위5/아래5). WaterPalette.ThemeLimits의
            // MinContainerCount(7)가 최소 위3/아래4는 항상 나오게 보장한다.
            int bottomRowCount = (containers.Count + 1) / 2;
            int topRowCount = containers.Count - bottomRowCount;

            if (topRowCount > 0) BuildRow(0, topRowCount, containers);
            BuildRow(topRowCount, bottomRowCount, containers);

            // 레이아웃 그룹이 병들의 실제 크기를 아직 계산하기 전일 수 있다 — 그 상태로
            // Refresh하면 BottleView.FillArea.rect.height가 확정 전 값(기본 100 등)으로
            // 읽혀서 물이 잔뜩 얇게 나오는 버그가 있었다(이동시킨 병만 그 뒤에 저절로
            // 정상 크기로 고쳐졌음 — 그때는 이미 레이아웃이 끝난 뒤라서). 초기 배치
            // 전에 레이아웃을 강제로 지금 확정시킨다.
            Canvas.ForceUpdateCanvases();

            RefreshAllBottles();
        }

        private void BuildRow(int startIndex, int count, IReadOnlyList<Container> containers)
        {
            var row = UiFactory.CreatePanel(_bottleArea, $"Row_{startIndex}", Color.clear);
            // forceExpandHeight: false — 위의 rows 레이아웃과 같은 이유(병이 늘어나면 안 됨).
            UiFactory.AddHorizontalLayout(row, spacing: UiTheme.BottleRowSpacing, forceExpandWidth: false, forceExpandHeight: false);

            for (int i = 0; i < count; i++)
            {
                int containerIndex = startIndex + i;
                var container = containers[containerIndex];
                var bottle = new BottleView(row, container.Capacity, container.UnlockedCapacity, containerIndex, OnBottleTapped);
                _bottleViews.Add(bottle);
            }
        }

        private void OnBottleTapped(int index)
        {
            // 지금 붓고 있는(원래 자리로 아직 안 돌아온) 병은 탭을 완전히 무시한다 —
            // 다른 병끼리 겹쳐서 동시에 움직이는 건 괜찮고, 도착 병도 여러 병에서
            // 연달아 쏟아붓는 걸 그대로 허용한다(둘 다 사용자 확정).
            if (_pourAnimator.IsBusy(index)) return;

            SoundService.Instance?.Play(SoundService.Sfx.ButtonTouch); // 물병 터치음(버튼과 공용).

            if (_selectedIndex == null)
            {
                if (_session.Board.Containers[index].IsEmpty) return; // 빈 병은 출발점이 될 수 없음
                _selectedIndex = index;
                RefreshHighlights();
                return;
            }

            if (_selectedIndex.Value == index)
            {
                _selectedIndex = null; // 같은 병 재탭 = 선택 취소
                RefreshHighlights();
                return;
            }

            int from = _selectedIndex.Value;
            _selectedIndex = null;
            PerformMove(from, index);
        }

        /// <summary>from → to로 실제 이동을 한 번 실행한다 — 병을 두 번 탭해서 고른
        /// 경우와 힌트 버튼을 눌러 자동으로 실행하는 경우가 둘 다 이 경로를 탄다
        /// (사용자 확정: 힌트는 이제 하이라이트만 하지 않고 실제로 옮겨준다 — 유저가
        /// 직접 두 병을 탭했을 때와 완전히 동일하게 처리).</summary>
        private void PerformMove(int from, int to)
        {
            var result = _session.TryMove(from, to);

            // 내용물 갱신은 여기서 즉시 하지 않는다 — 성공한 이동은 PourAnimator가
            // 붓기 연출로 서서히 반영하고, 실패한 이동은 애초에 Board가 안 바뀌었으니
            // 아래 RefreshHighlights가 들어올려져 있던 출발 병을 부드럽게 내려놓기만
            // 하면 된다. 다른 병에서 진행 중인 연출은 그대로 둔다(입력을 막지 않기로
            // 확정 — GameDesign.md).
            //
            // 클리어/교착 판정(EvaluateBoardState)은 성공한 이동이면 붓기 연출이
            // 실제로 다 끝난 뒤에 한다 — Board 자체는 TryMove 순간 이미 바뀌어서
            // 그 즉시 판정하면 마지막 물병이 화면에 다 차는 걸 보여주기도 전에
            // 클리어 처리되어 버린다(실제로 겪은 버그).
            if (result.Success)
            {
                // 선택 중 들어올려져 있던 만큼을 여기서 즉시 0으로 스냅한다(애니메이션
                // 없이) — 이제부터는 PourAnimator가 훨씬 큰 폭으로 스스로 들어올리는
                // 연출을 처음부터 새로 재생하므로, 남은 선택-리프트 코루틴이 그 위에
                // 겹쳐 더 들뜬 것처럼 보이면 안 된다(SnapBottleLift가 진행 중이던
                // 코루틴도 같이 멈춘다).
                SnapBottleLift(from, 0f);
                _pourAnimator.Play(result, _bottleViews[result.FromIndex], _bottleViews[result.ToIndex], onComplete: EvaluateBoardState);

                // 이 이동으로 도착 병이 한 색으로 가득 찼으면(완성) 그 병에서 작은 축하
                // 이펙트를 터뜨린다 — 물이 실제로 다 차오르는 시점(붓기 들어올리기+흐르기가
                // 끝나는 때)에 맞춰 잠깐 늦춰서 재생한다. 도착 병은 붓기 전엔 IsFull이면
                // 애초에 부을 수 없으니, 지금 가득 찼다면 방금 이 이동으로 완성된 것이다.
                //
                // 이 색은 _lastCompletedColor에도 저장해 둔다 — 만약 이 이동으로 라운드
                // 자체가 클리어됐다면(모든 병이 resolved) 그게 곧 "이 라운드를 마지막으로
                // 완성시킨 물 색"이라, 스테이지 클리어 파티클 색을 여기 맞춘다(2026-09-11).
                // 이동은 항상 출발 병을 완전히 비우거나 도착 병을 완전히 채워야만 둘 다
                // resolved가 될 수 있으므로, 라운드를 클리어하는 이동은 반드시 이 분기를
                // 탄다 — 못 구하는 경우는 사실상 없지만 EvaluateBoardState가 방어적으로
                // 기본값(UiTheme.StageClearBurstColor)을 대신 쓴다.
                var toContainer = _session.Board.Containers[result.ToIndex];
                if (IsFullyStacked(toContainer))
                {
                    if (toContainer.TopColor.HasValue) _lastCompletedColor = WaterPalette.Get(toContainer.TopColor.Value);
                    StartCoroutine(PlayBottleCompleteBurstAfterPour(result.ToIndex));
                }
            }
            else
            {
                Debug.Log("[GameView] 무효 이동 — TODO: 진동/튕김 피드백");
                EvaluateBoardState();
            }

            RefreshHighlights();
        }

        /// <summary>한 색으로 가득 찬(= 더 손댈 필요 없는, 비어있지 않은) 병인지.
        /// Container.IsResolved는 빈 병도 포함하므로 여기선 "실제로 다 채워 완성"만 본다.</summary>
        private static bool IsFullyStacked(Container container) =>
            container.IsFull && container.Count > 0 && container.TopRunLength() == container.Count;

        /// <summary>방금 완성된 병에서 작은 축하 이펙트를 터뜨린다 — 붓기 연출로 물이
        /// 실제로 다 차오르는 시점(들어올리기 + 흐르기 구간이 끝나는 때)에 맞춰
        /// 잠깐 기다렸다 재생한다. GameView가 파괴되면(라운드 전환) 코루틴도 같이
        /// 멈추므로 별도 정리는 필요 없다.</summary>
        private IEnumerator PlayBottleCompleteBurstAfterPour(int containerIndex)
        {
            yield return new WaitForSeconds(UiTheme.PourLiftTime + UiTheme.PourFlowTime);
            if (containerIndex < 0 || containerIndex >= _bottleViews.Count) yield break;
            if (containerIndex >= _session.Board.Containers.Count) yield break;
            var container = _session.Board.Containers[containerIndex];
            // 기다리는 사이 Undo/Reset 등으로 완성이 풀렸으면 조용히 취소.
            if (!IsFullyStacked(container)) yield break;

            // 시작 위치: 병 Root의 윗변 중앙 + Inspector 오프셋(디자인 픽셀 → 캔버스 배율 반영).
            var root = _bottleViews[containerIndex].Root;
            var corners = new Vector3[4];
            root.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
            Vector2 off = UiTheme.BottleCompleteBurstOffset;
            Vector3 start = (corners[1] + corners[2]) * 0.5f
                            + new Vector3(off.x * root.lossyScale.x, off.y * root.lossyScale.y, 0f);

            // 색: 그 병을 채운 물 색 그대로(완성 병이라 단색).
            Color color = container.TopColor.HasValue
                ? WaterPalette.Get(container.TopColor.Value)
                : UiTheme.PrimaryColor;

            BottleCompleteBurst.Play(_effectsLayer, start, color);
            SoundService.Instance?.Play(SoundService.Sfx.BottleComplete);
        }

        private void OnUndoClicked()
        {
            _pourAnimator.CancelAll(); // 진행 중인 붓기 연출을 끊고 즉시 이전 상태로 스냅.
            _session.TryUndo();
            _selectedIndex = null;
            RefreshAllBottles();
        }

        private void OnResetClicked()
        {
            _pourAnimator.CancelAll();
            _session.ResetToInitial();
            _selectedIndex = null;
            _adHintUsedThisRound = false; // 새로고침하면 "광고 보고 힌트" 기회가 다시 생긴다(사용자 확정, 2026-09-11).
            RefreshAllBottles();
        }

        /// <summary>힌트는 더 이상 "어디서 어디로 옮기면 되는지" 하이라이트만 보여주지
        /// 않는다 — 유저가 직접 두 병을 탭했을 때와 완전히 동일하게, 그 이동을 그
        /// 자리에서 바로 실행한다(사용자 확정).
        ///
        /// <see cref="HintSolver.FindNextMove"/>는 상태공간이 큰 보드에서 최대
        /// 수만~수십만 states를 뒤질 수 있어 메인 스레드에서 그대로 부르면 그
        /// 계산이 끝날 때까지 프레임이 통째로 멈춘다 — 실제로 겪은 버그: 힌트를
        /// 누르면 잠깐 렉이 걸린 것처럼 뚝 멈췄다가, 그 멈춘 실제 시간만큼
        /// Time.deltaTime이 커진 첫 프레임 때문에 붓기 연출의 1단계(들어올리기)가
        /// 통째로 스킵된 것처럼 중간부터 재생됐다. 그래서 계산을
        /// <see cref="Task.Run(Action)"/>으로 백그라운드 스레드에 맡기고, 끝나면
        /// (Unity의 SynchronizationContext 덕분에) 다시 메인 스레드로 돌아와서
        /// PerformMove를 부른다 — 그동안 메인 스레드/화면은 전혀 안 멈춘다.
        /// Board를 그대로 넘기지 않고 미리 복제해 두는 이유는, 계산하는 동안
        /// 유저가 다른 조작으로 실제 Board를 바꿀 수 있어서(입력을 안 막음) —
        /// 백그라운드 스레드가 그 시점의 스냅샷만 안전하게 들여다보게 한다.
        ///
        /// 백그라운드로 옮겨서 렉은 없어졌지만, 계산하는 동안(수백 ms~수 초) 화면이
        /// 아무 반응 없이 가만히 있으면 유저 입장에선 "버튼이 안 눌렸나?" 싶은
        /// 빈 시간이 생긴다(실제로 겪은 피드백). 그래서 그동안 <see cref="HintLoadingOverlay"/>로
        /// 딤 배경 + 회전하는 스피너를 보여준다 — ConfirmDialog와 달리 입력은 막지
        /// 않는다(사용자 확정: 계산 중에도 다른 병 조작은 계속 가능해야 함).</summary>
        private async void OnHintClicked()
        {
            if (_hintInFlight) return;

            // 힌트가 0개면: 이번 라운드에 아직 광고를 안 썼으면 "광고 보고 힌트 1개?"
            // 확인 창을 띄우고(Yes → ShowHintAd), 이미 썼으면 아무것도 안 한다(버튼이
            // 비활성화돼 있어야 정상이지만 방어적으로 막음, 2026-09-11 사용자 확정).
            if (_hintCount <= 0)
            {
                if (_adHintUsedThisRound || _activeDialog != null) return;
                _activeDialog = ConfirmDialog.Show(_canvasRoot, "Watch Ad\nto get 1 hint?",
                    "NO", () => _activeDialog = null,
                    "Yes", () => { _activeDialog = null; ShowHintAd(); });
                return;
            }

            _hintInFlight = true;
            RefreshHighlights(); // 힌트 버튼을 계산하는 동안 비활성화된 걸로 보여줌.
            var loading = HintLoadingOverlay.Show(_canvasRoot);

            Board snapshot = _session.Board.Clone();
            HintSolver.Move? move;
            try
            {
                move = await Task.Run(() => HintSolver.FindNextMove(snapshot));
            }
            finally
            {
                _hintInFlight = false;
                // 힌트 물병이 실제로 움직이기 시작하는(또는 포기하는) 바로 그 타이밍에
                // 로딩 화면을 치운다(사용자 확정) — 아래 어느 분기로 빠지든 이후로는
                // 이 오버레이가 더 이상 필요 없다.
                HintLoadingOverlay.Hide(loading);
            }

            if (this == null) return; // 계산하는 동안 화면 자체가 없어졌을 수 있음(뒤로가기 등).

            if (move == null)
            {
                Debug.Log("[GameView] 힌트: 다음 수를 못 찾음");
                Toast.Show(_canvasRoot, "NO HINT AVAILABLE");
                RefreshHighlights();
                return;
            }

            // 계산하는 동안 다른 조작으로 실제 Board가 이미 바뀌었을 수 있다 — 그
            // 경우 이 힌트는 지금 상태 기준으로 유효하지 않을 수 있지만, PerformMove
            // 안의 TryMove가 어차피 유효성을 다시 검사해서 무효면 조용히 무시되니
            // 별도 처리 없이 그대로 시도한다.
            //
            // 힌트가 가리키는 출발 병이 마침 다른 이동으로 아직 붓는 중이면(원래
            // 자리로 안 돌아온 상태) 이번 힌트는 포기한다 — 탭으로 직접 고를 때와
            // 같은 규칙(OnBottleTapped 맨 위 참고).
            if (_pourAnimator.IsBusy(move.Value.FromIndex))
            {
                RefreshHighlights();
                return;
            }

            // 여기까지 왔으면 힌트를 실제로 소비한다(이동이 유효한지와 무관하게 —
            // 어차피 위에서 이미 "다음 수를 찾음/붓는 중 아님"까지 확인했고, 아래
            // PerformMove의 TryMove 재검증은 계산 중 다른 조작으로 상태가 바뀐
            // 드문 경우에 대한 방어일 뿐이라 정상적으로는 항상 성공한다).
            _hintCount = HintStore.Consume();
            UpdateHintBadge();

            _selectedIndex = null; // 유저가 이미 뭔가 골라둔 상태였으면 힌트 실행으로 대체.
            PerformMove(move.Value.FromIndex, move.Value.ToIndex);
        }

        /// <summary>힌트가 0개일 때 확인 창에서 Yes를 눌렀을 때만 부른다 — 보상형 광고를
        /// 끝까지 봐야 힌트가 1개 생기고, 그 시점에 "이번 라운드 광고 힌트 사용함"으로
        /// 잠근다(라운드당 1번, 새로고침 시 OnResetClicked가 다시 풀어 줌). 중간에 닫거나
        /// 광고가 준비 안 됐으면 대체 지급 없이 조용히 넘어간다(병 추가 광고와 같은 정책).</summary>
        private void ShowHintAd()
        {
            if (_adHintUsedThisRound || _hintCount > 0) return;

            RewardedAdService.Show(
                AdUnitIds.HintRewarded,
                onRewardEarned: () =>
                {
                    if (this == null) return; // 광고 보는 동안 화면이 없어졌을 수 있음.
                    _adHintUsedThisRound = true;
                    _hintCount = HintStore.AddCharge(); // 0 → 1(상한 MaxHints까지지만 여기선 항상 0에서 옴).
                    UpdateHintBadge();
                    RefreshHighlights();
                },
                onClosedWithoutReward: () =>
                {
                    Debug.Log("[GameView] 힌트 광고: 끝까지 안 봄 — 힌트 없음");
                },
                onUnavailable: () =>
                {
                    Debug.Log("[GameView] 힌트 광고: 아직 준비 안 됨");
                    if (this == null) return;
                    RefreshHighlights();
                });

            RefreshHighlights(); // 광고 표시/재로드 시작 — 그동안 버튼을 비활성 상태로.
        }

        /// <summary>병 추가(광고 보상) 버튼 — 누르면 바로 광고가 아니라 먼저 확인 창을
        /// 띄운다(뒤로가기 창과 같은 ConfirmDialog, 왼쪽 NO / 오른쪽 Yes). Yes를
        /// 눌러야 <see cref="ShowBonusContainerAd"/>가 실제 광고를 재생한다(사용자
        /// 확정, 2026-09-10 — 그전까지는 누르면 광고가 곧바로 떴음).</summary>
        private void OnAddContainerClicked()
        {
            if (!_session.CanUnlockBonusContainer) return;
            if (_activeDialog != null) return; // 이미 다른 창(뒤로가기 등)이 떠 있으면 무시.

            _activeDialog = ConfirmDialog.Show(_canvasRoot, "Watch AD\nto get extra bottle?",
                "NO", () => _activeDialog = null,
                "Yes", () => { _activeDialog = null; ShowBonusContainerAd(); });
        }

        /// <summary>실제 보상형 광고 재생 — 병 추가 확인 창에서 Yes를 눌렀을 때만 부른다.
        /// 광고를 끝까지 봐야 매 라운드 마지막 병(RoundBuilder가 항상 붙여 둠)의 잠긴
        /// 칸이 1칸 열린다(사용자 확정, 2026-09-08). 광고가 아직 안 떴거나(로드 전)
        /// 표시 자체가 실패하면 대체 지급 없이 조용히 아무 일도 안 일어난다
        /// (GameDesign.md "광고 미시청/로드 실패 시" 확정 정책). 내용물이 아니라
        /// "그 병이 얼마나 열려 있는지"만 바뀌는 거라 붓기 연출과는 무관 — 애니메이션
        /// 진행 중이어도 안전하다.</summary>
        private void ShowBonusContainerAd()
        {
            if (!_session.CanUnlockBonusContainer) return;

            RewardedAdService.Show(
                AdUnitIds.BonusContainerRewarded,
                onRewardEarned: () =>
                {
                    if (this == null) return; // 광고 보는 동안 화면 자체가 없어졌을 수 있음(뒤로가기 등).
                    if (!_session.TryUnlockBonusContainer()) return;

                    int bonusIndex = _session.Board.Containers.Count - 1;
                    var bonus = _session.Board.Containers[bonusIndex];
                    _bottleViews[bonusIndex].SetUnlockedCapacity(bonus.UnlockedCapacity);
                    RefreshHighlights();
                },
                onClosedWithoutReward: () =>
                {
                    Debug.Log("[GameView] 병 추가: 광고를 끝까지 안 봄 — 보상 없음");
                },
                onUnavailable: () =>
                {
                    Debug.Log("[GameView] 병 추가: 광고가 아직 준비 안 됨");
                    if (this == null) return;
                    RefreshHighlights(); // 광고 재로드가 끝날 때까지 버튼을 비활성 상태로 보여줌.
                });

            RefreshHighlights(); // 광고 표시/재로드 시작 — 그동안 버튼을 비활성화.
        }

        private void RequestBackToTitle()
        {
            if (_activeDialog != null) return; // 이미 열려있으면 Escape 연타로 중복 생성 안 함
            _activeDialog = ConfirmDialog.Show(_canvasRoot, "Return to title?",
                "BACK", () => _activeDialog = null,
                "TITLE", () => { _activeDialog = null; _callbacks?.OnBack?.Invoke(); });
        }

        /// <summary>내용물 + 하이라이트를 전부 즉시 다시 그린다(애니메이션 없음) —
        /// 진행 중인 붓기 연출을 그대로 덮어써버리므로, 라운드 최초 배치나 Undo/Reset
        /// 처럼 상태를 강제로 스냅해야 할 때만 쓴다.</summary>
        private void RefreshAllBottles()
        {
            var containers = _session.Board.Containers;
            for (int i = 0; i < _bottleViews.Count; i++)
            {
                _bottleViews[i].Refresh(containers[i]);
                // 병 추가로 열린 칸 수도 같이 맞춘다 — Undo/Reset은 Board를 통째로
                // 옛 스냅샷으로 갈아 끼우므로(PuzzleSession이 그 시점에 맞게
                // 보정은 해 주지만) 화면 쪽 오버레이는 따로 갱신해 줘야 한다.
                // 보너스 병이 아닌 일반 병은 오버레이 자체가 없어서 그냥 무시된다.
                _bottleViews[i].SetUnlockedCapacity(containers[i].UnlockedCapacity);
            }

            RefreshHighlights();
        }

        /// <summary>선택된 병의 들어올리기 연출과 버튼 활성 상태만 다시 그린다 — 병
        /// 내용물은 안 건드리므로 다른 병에서 진행 중인 붓기 연출을 방해하지 않는다.
        /// 힌트는 더 이상 하이라이트를 남기지 않고(OnHintClicked 참고) 그 자리에서
        /// 바로 이동을 실행하므로 여기서 따로 처리할 게 없다.</summary>
        private void RefreshHighlights()
        {
            // _liftedIndex(지금 실제로 들려 있는/들리는 중인 병)와 _selectedIndex(지금
            // 선택된 병)가 다를 때만 애니메이션을 새로 건다 — RefreshHighlights는
            // 선택과 무관한 이유(광고 버튼 상태 등)로도 자주 불리므로, 매번 무조건
            // 다시 트면 이미 도착한 애니메이션을 불필요하게 재시작하게 된다.
            if (_liftedIndex != _selectedIndex)
            {
                if (_liftedIndex.HasValue) SetBottleLifted(_liftedIndex.Value, false);
                if (_selectedIndex.HasValue) SetBottleLifted(_selectedIndex.Value, true);
                _liftedIndex = _selectedIndex;
            }

            _undoButton.interactable = _session.CanUndo;
            // 힌트가 남아있으면 평소대로. 0개면 "이번 라운드에 아직 광고 힌트를 안 썼고
            // 광고가 준비됐을 때"만 눌러서 확인 창(→ 광고 → 힌트 1개)을 열 수 있다 —
            // 이미 썼으면(_adHintUsedThisRound) 그냥 비활성(새로고침하면 다시 풀림,
            // OnResetClicked). 병 추가 버튼이 광고 로드 상태에 따라 켜졌다 꺼졌다 하는
            // 것과 같은 방식.
            bool canAdHint = _hintCount <= 0 && !_adHintUsedThisRound
                && RewardedAdService.IsReady(AdUnitIds.HintRewarded);
            _hintButton.interactable = !_session.IsCleared && !_hintInFlight && (_hintCount > 0 || canAdHint);
            // 힌트 버튼 위 광고 배지: 힌트가 0개이고 이번 라운드에 아직 광고 힌트를
            // 안 썼을 때만 보인다(광고 로드 여부와는 무관 — 병 추가 배지와 같은 규칙).
            if (_hintAdBadge != null)
                _hintAdBadge.gameObject.SetActive(_hintCount <= 0 && !_adHintUsedThisRound);
            // 다 열렸거나(용량 소진) 광고가 아직 준비 안 됐으면 못 누르게 — 광고 로드
            // 실패/시청 중에도 이 값이 자동으로 false가 돼서 버튼이 비활성화된다.
            _addContainerButton.interactable = _session.CanUnlockBonusContainer &&
                RewardedAdService.IsReady(AdUnitIds.BonusContainerRewarded);
            // "광고 봐야 함" 배지는 아직 열 칸이 남아있을 때만 — 다 열렸으면 버튼도
            // 의미가 없으니 배지도 숨긴다(광고 로드 여부와는 무관하게 항상 붙어 있음).
            if (_watchAdBadge != null)
                _watchAdBadge.gameObject.SetActive(_session.CanUnlockBonusContainer);
        }

        /// <summary>bottleViews[index]를 목표 상태(들림/안 들림)로 부드럽게 애니메이션한다
        /// — 이미 그 병으로 같은 목표를 향해 가는 중이거나 이미 도착해 있으면(부동소수
        /// 오차 감안) 아무것도 하지 않는다. 진행 중이던 반대 방향 애니메이션이 있으면
        /// 먼저 멈추고, 그 코루틴이 마지막으로 남긴 현재값(<see cref="BottleView.LiftOffset"/>)
        /// 에서부터 이어서 새 목표로 향한다 — 그래야 빠르게 다시 탭해도(들리는 도중
        /// 취소 등) 뚝 끊기지 않고 자연스럽게 방향만 바뀐다.</summary>
        private void SetBottleLifted(int index, bool lifted)
        {
            if (_liftRoutines.TryGetValue(index, out var existing) && existing != null)
                StopCoroutine(existing);

            var bottle = _bottleViews[index];
            float target = lifted ? UiTheme.BottleSelectLiftHeight : 0f;
            if (Mathf.Approximately(bottle.LiftOffset, target))
            {
                _liftRoutines.Remove(index);
                return;
            }
            _liftRoutines[index] = StartCoroutine(LiftRoutine(index, bottle, target));
        }

        /// <summary>애니메이션 없이 즉시 값으로 스냅한다 — 진행 중이던 리프트 코루틴이
        /// 있으면 먼저 멈춰서, 다음 프레임에 그 코루틴이 이 값을 덮어쓰는 일이 없게
        /// 한다(PerformMove가 성공한 이동을 PourAnimator에 넘기기 직전에 씀).</summary>
        private void SnapBottleLift(int index, float value)
        {
            if (_liftRoutines.TryGetValue(index, out var existing) && existing != null)
            {
                StopCoroutine(existing);
                _liftRoutines.Remove(index);
            }
            _bottleViews[index].SetLiftOffset(value);
        }

        private IEnumerator LiftRoutine(int index, BottleView bottle, float target)
        {
            float start = bottle.LiftOffset;
            float t = 0f;
            while (t < UiTheme.BottleSelectLiftTime)
            {
                t += Time.deltaTime;
                float e = Ease(Mathf.Clamp01(t / UiTheme.BottleSelectLiftTime));
                bottle.SetLiftOffset(Mathf.Lerp(start, target, e));
                yield return null;
            }
            bottle.SetLiftOffset(target);
            _liftRoutines.Remove(index);
        }

        private static float Ease(float p) => p * p * (3f - 2f * p); // smoothstep — PourAnimator와 같은 완급.

        private void EvaluateBoardState()
        {
            if (_session.IsCleared)
            {
                Debug.Log("[GameView] 라운드 클리어! — TODO: 결과 화면");
                _callbacks?.OnCleared?.Invoke(_lastCompletedColor ?? UiTheme.StageClearBurstColor);
            }
            else if (!_session.HasAnyValidMove)
            {
                Debug.Log("[GameView] 교착 상태 — TODO: 힌트/초기화/병추가 안내 팝업");
            }
        }
    }
}
