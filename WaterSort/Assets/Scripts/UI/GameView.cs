using System;
using System.Collections.Generic;
using ColorSort.Core;
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
            public Action OnSettings;
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
        private PourAnimator _pourAnimator;

        private int? _selectedIndex;
        private (int from, int to)? _hintMove;
        private RectTransform _activeDialog;

        private static readonly Color SelectedHighlight = new Color(0.36f, 0.79f, 0.89f, 0.9f); // UiTheme.PrimaryColor 톤
        private static readonly Color HintHighlight = new Color(1f, 0.84f, 0.2f, 0.9f); // TODO(sprite): 힌트 강조 색/연출 정식 확정 전 임시

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

            RebuildBottles();
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

            var settings = UiFactory.CreateIconButton(root, UiTheme.Skin?.SettingsIcon, UiTheme.IconButtonSize, UiTheme.PanelColor,
                () => _callbacks?.OnSettings?.Invoke(), fallbackText: "SETTINGS");
            var settingsRect = (RectTransform)settings.transform;
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.pivot = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = new Vector2(-UiTheme.ScreenPadding, -UiTheme.ScreenPadding);

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
            // 220 -> 310: 아이콘 버튼이 96->140으로 커진 만큼 그룹 폭도 같이 늘림(140*2+16여백).
            const float groupWidth = 310f;

            var leftGroup = UiFactory.CreatePanel(root, "LeftButtons", Color.clear);
            leftGroup.anchorMin = leftGroup.anchorMax = new Vector2(0f, 0f);
            leftGroup.pivot = new Vector2(0f, 0f);
            leftGroup.sizeDelta = new Vector2(groupWidth, UiTheme.ButtonHeightSmall);
            leftGroup.anchoredPosition = new Vector2(UiTheme.ScreenPadding, UiTheme.ScreenPadding);
            UiFactory.AddHorizontalLayout(leftGroup, spacing: 16f, forceExpandWidth: false, forceExpandHeight: true);

            _undoButton = UiFactory.CreateIconButton(leftGroup, UiTheme.Skin?.UndoIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnUndoClicked, fallbackText: "UNDO");
            UiFactory.CreateIconButton(leftGroup, UiTheme.Skin?.ResetIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnResetClicked, fallbackText: "RESET");

            var rightGroup = UiFactory.CreatePanel(root, "RightButtons", Color.clear);
            rightGroup.anchorMin = rightGroup.anchorMax = new Vector2(1f, 0f);
            rightGroup.pivot = new Vector2(1f, 0f);
            rightGroup.sizeDelta = new Vector2(groupWidth, UiTheme.ButtonHeightSmall);
            rightGroup.anchoredPosition = new Vector2(-UiTheme.ScreenPadding, UiTheme.ScreenPadding);
            UiFactory.AddHorizontalLayout(rightGroup, spacing: 16f, forceExpandWidth: false, forceExpandHeight: true);

            _hintButton = UiFactory.CreateIconButton(rightGroup, UiTheme.Skin?.HintIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnHintClicked, fallbackText: "HINT");
            UiFactory.CreateIconButton(rightGroup, UiTheme.Skin?.AddContainerIcon, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnAddContainerClicked, fallbackText: "ADD");
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
                var bottle = new BottleView(row, containers[containerIndex].Capacity, containerIndex, OnBottleTapped);
                _bottleViews.Add(bottle);
            }
        }

        private void OnBottleTapped(int index)
        {
            // 지금 붓고 있는(원래 자리로 아직 안 돌아온) 병은 탭을 완전히 무시한다 —
            // 다른 병끼리 겹쳐서 동시에 움직이는 건 괜찮고, 도착 병도 여러 병에서
            // 연달아 쏟아붓는 걸 그대로 허용한다(둘 다 사용자 확정).
            if (_pourAnimator.IsBusy(index)) return;

            _hintMove = null; // 힌트는 다음 조작 전까지만 유효

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
            var result = _session.TryMove(from, index);

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
            _hintMove = null;
            RefreshAllBottles();
        }

        private void OnResetClicked()
        {
            _pourAnimator.CancelAll();
            _session.ResetToInitial();
            _selectedIndex = null;
            _hintMove = null;
            RefreshAllBottles();
        }

        private void OnHintClicked()
        {
            var move = HintSolver.FindNextMove(_session.Board);
            if (move == null)
            {
                Debug.Log("[GameView] 힌트: 다음 수를 못 찾음");
                return;
            }
            _hintMove = (move.Value.FromIndex, move.Value.ToIndex);
            RefreshHighlights(); // 내용물은 안 바뀌었으니 하이라이트만 — 진행 중인 연출을 안 건드림.
        }

        private void OnAddContainerClicked()
        {
            // TODO: 병 추가는 광고/재화(Managers) 연동 이후 — 정책 자체가 GameDesign.md TBD.
            Debug.Log("[GameView] 병 추가 — 아직 정책 미확정");
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
                _bottleViews[i].Refresh(containers[i]);

            RefreshHighlights();
        }

        /// <summary>선택/힌트 하이라이트와 버튼 활성 상태만 다시 그린다 — 병 내용물은
        /// 안 건드리므로 다른 병에서 진행 중인 붓기 연출을 방해하지 않는다.</summary>
        private void RefreshHighlights()
        {
            for (int i = 0; i < _bottleViews.Count; i++)
                _bottleViews[i].SetHighlight(Color.clear);

            if (_selectedIndex.HasValue)
                _bottleViews[_selectedIndex.Value].SetHighlight(SelectedHighlight);

            if (_hintMove.HasValue)
            {
                var (from, to) = _hintMove.Value;
                _bottleViews[from].SetHighlight(HintHighlight);
                _bottleViews[to].SetHighlight(HintHighlight);
            }

            _undoButton.interactable = _session.CanUndo;
            _hintButton.interactable = !_session.IsCleared;
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
