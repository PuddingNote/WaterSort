using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColorSort.Core;
using ColorSort.Managers;
using ColorSort.Solver;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 게임 화면(GameDesign.md UI 배치). <see cref="PuzzleSession"/>을 유일한
    /// 진실 소스로 삼는다 — 조작이 성공하면 Board는 그 즉시 바뀌지만, 화면은
    /// <see cref="PourAnimator"/>가 붓기 연출로 서서히 따라잡는다(하이라이트만
    /// 즉시 갱신). 무효 이동 진동·클리어/교착 팝업은 아직 로그로만 남는다.
    /// </summary>
    public sealed class GameView : MonoBehaviour
    {
        public sealed class Callbacks
        {
            public Action OnBack;
            public Action OnCleared;
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

        private int? _selectedIndex;
        private RectTransform _activeDialog;
        private bool _hintInFlight;

        private static readonly Color SelectedHighlight = new Color(0.36f, 0.79f, 0.89f, 0.9f); // UiTheme.PrimaryColor 톤

        public static GameView Build(Transform parent, int roundId, PuzzleSession session, Callbacks callbacks)
        {
            var go = new GameObject("GameView", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            UiFactory.Stretch(rect);

            var view = go.AddComponent<GameView>();
            view.Initialize(rect, roundId, session, callbacks);
            return view;
        }

        private void Initialize(RectTransform root, int roundId, PuzzleSession session, Callbacks callbacks)
        {
            _canvasRoot = root.parent;
            _roundId = roundId;
            _session = session;
            _callbacks = callbacks;

            var background = UiFactory.CreatePanel(root, "Background", UiTheme.BackgroundTop);
            UiFactory.Stretch(background);

            BuildTopBar(root);
            BuildBottleArea(root);
            BuildBottomBar(root);

            // 붓는 병(그리드에서 잠깐 떼어내 자유롭게 움직임)과 물줄기 둘 다 병/버튼보다
            // 항상 위에 그려져야 하니 마지막에 만든 형제로 둔다.
            var effectsLayer = UiFactory.CreatePanel(root, "EffectsLayer", Color.clear);
            UiFactory.Stretch(effectsLayer);
            effectsLayer.gameObject.GetComponent<Image>().raycastTarget = false;

            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            _pourAnimator = new PourAnimator(this, _session, effectsLayer, audioSource);

            // 병 추가 버튼을 누르는 순간 바로 뜨도록 라운드 시작 시 미리 로드해 둔다 —
            // 로드는 비동기라 탭한 뒤에야 요청하면 그 자리에서 못 보여줄 수 있다.
            // 로드가 늦게 끝나거나(라운드 시작 직후) 한 번 쓴 뒤 다음 걸 다시 로드하는
            // 동안엔 버튼이 비활성 상태로 멈춰 있는데, 그 상태에서 유저가 아무 것도
            // 안 건드리면 로드가 끝나도 버튼이 계속 비활성으로 보인다(RefreshHighlights를
            // 다시 부를 계기가 없어서) — AdReady 이벤트를 구독해서 그 순간 바로 다시
            // 그려준다. static 이벤트라 OnDestroy에서 반드시 구독 해지해야 한다.
            RewardedAdService.AdReady += OnRewardedAdReady;
            RewardedAdService.Preload(AdUnitIds.BonusContainerRewarded);

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
            if (adUnitId != AdUnitIds.BonusContainerRewarded) return;
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
                OnResetClicked, fallbackText: "RESET");
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
            _addContainerButton = UiFactory.CreateIconButton(rightGroup, UiTheme.Skin?.AddContainerIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnAddContainerClicked, fallbackText: "ADD");
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
            // 하이라이트만 정리하면 된다. 다른 병에서 진행 중인 연출은 그대로 둔다
            // (입력을 막지 않기로 확정 — GameDesign.md).
            //
            // 클리어/교착 판정(EvaluateBoardState)은 성공한 이동이면 붓기 연출이
            // 실제로 다 끝난 뒤에 한다 — Board 자체는 TryMove 순간 이미 바뀌어서
            // 그 즉시 판정하면 마지막 물병이 화면에 다 차는 걸 보여주기도 전에
            // 클리어 처리되어 버린다(실제로 겪은 버그).
            if (result.Success)
                _pourAnimator.Play(result, _bottleViews[result.FromIndex], _bottleViews[result.ToIndex], onComplete: EvaluateBoardState);
            else
            {
                Debug.Log("[GameView] 무효 이동 — TODO: 진동/튕김 피드백");
                EvaluateBoardState();
            }

            RefreshHighlights();
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

            _selectedIndex = null; // 유저가 이미 뭔가 골라둔 상태였으면 힌트 실행으로 대체.
            PerformMove(move.Value.FromIndex, move.Value.ToIndex);
        }

        /// <summary>병 추가(광고 보상) — 보상형 광고를 끝까지 봐야 매 라운드 마지막 병
        /// (RoundBuilder가 항상 붙여 둠)의 잠긴 칸이 1칸 열린다(사용자 확정,
        /// 2026-09-08 — 그전까지는 누르면 바로 적용되는 임시 동작이었음).
        /// 광고가 아직 안 떴거나(로드 전) 표시 자체가 실패하면 대체 지급 없이
        /// 조용히 아무 일도 안 일어난다(GameDesign.md "광고 미시청/로드 실패 시"
        /// 확정 정책). 내용물이 아니라 "그 병이 얼마나 열려 있는지"만 바뀌는
        /// 거라 붓기 연출과는 무관 — 애니메이션 진행 중이어도 아무 때나 눌러도
        /// 안전하다.</summary>
        private void OnAddContainerClicked()
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

        /// <summary>선택 하이라이트와 버튼 활성 상태만 다시 그린다 — 병 내용물은 안
        /// 건드리므로 다른 병에서 진행 중인 붓기 연출을 방해하지 않는다. 힌트는 더
        /// 이상 하이라이트를 남기지 않고(OnHintClicked 참고) 그 자리에서 바로
        /// 이동을 실행하므로 여기서 따로 처리할 게 없다.</summary>
        private void RefreshHighlights()
        {
            for (int i = 0; i < _bottleViews.Count; i++)
                _bottleViews[i].SetHighlight(Color.clear);

            if (_selectedIndex.HasValue)
                _bottleViews[_selectedIndex.Value].SetHighlight(SelectedHighlight);

            _undoButton.interactable = _session.CanUndo;
            _hintButton.interactable = !_session.IsCleared && !_hintInFlight; // 계산 중엔 중복 클릭 방지.
            // 다 열렸거나(용량 소진) 광고가 아직 준비 안 됐으면 못 누르게 — 광고 로드
            // 실패/시청 중에도 이 값이 자동으로 false가 돼서 버튼이 비활성화된다.
            _addContainerButton.interactable = _session.CanUnlockBonusContainer &&
                RewardedAdService.IsReady(AdUnitIds.BonusContainerRewarded);
        }

        private void EvaluateBoardState()
        {
            if (_session.IsCleared)
            {
                Debug.Log("[GameView] 라운드 클리어! — TODO: 결과 화면");
                _callbacks?.OnCleared?.Invoke();
            }
            else if (!_session.HasAnyValidMove)
            {
                Debug.Log("[GameView] 교착 상태 — TODO: 힌트/초기화/병추가 안내 팝업");
            }
        }
    }
}
