using System;
using System.Collections.Generic;
using ColorSort.Core;
using ColorSort.Solver;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 게임 화면(GameDesign.md UI 배치). <see cref="PuzzleSession"/>을 유일한
    /// 진실 소스로 삼고, 조작 한 번마다 그 상태를 그대로 다시 그린다.
    ///
    /// 지금은 붓기 애니메이션·무효 이동 진동·클리어/교착 팝업 같은 연출이 전부
    /// 로그로만 남는다 — 이런 연출은 물 소재 특유의 것(docs/Sprites.md)이라
    /// 스프라이트와 정책(붓기 시간 등, GameDesign.md TBD)이 정해진 뒤에 붙인다.
    /// 지금 이 단계의 목표는 "규칙이 실제로 정확하게 동작하는 화면"이다.
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
        private int _roundId;
        private readonly List<BottleView> _bottleViews = new List<BottleView>();
        private RectTransform _bottleArea;
        private Button _undoButton;
        private Button _hintButton;

        private int? _selectedIndex;
        private (int from, int to)? _hintMove;

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
            _roundId = roundId;
            _session = session;
            _callbacks = callbacks;

            var background = UiFactory.CreatePanel(root, "Background", UiTheme.BackgroundTop);
            UiFactory.Stretch(background);

            BuildTopBar(root);
            BuildBottleArea(root);
            BuildBottomBar(root);

            RebuildBottles();
        }

        private void BuildTopBar(RectTransform root)
        {
            // TODO(sprite): icon_back_arrow
            var back = UiFactory.CreateIconButton(root, null, UiTheme.IconButtonSize, UiTheme.PanelColor,
                () => _callbacks?.OnBack?.Invoke(), fallbackText: "뒤로");
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(UiTheme.ScreenPadding, -UiTheme.ScreenPadding);

            // TODO(sprite): icon_settings_gear
            var settings = UiFactory.CreateIconButton(root, null, UiTheme.IconButtonSize, UiTheme.PanelColor,
                () => _callbacks?.OnSettings?.Invoke(), fallbackText: "설정");
            var settingsRect = (RectTransform)settings.transform;
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.pivot = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = new Vector2(-UiTheme.ScreenPadding, -UiTheme.ScreenPadding);

            var roundLabel = UiFactory.CreateText(root, $"라운드 {_roundId}", UiTheme.FontSizeSubtitle, UiTheme.TextPrimary);
            var roundRect = (RectTransform)roundLabel.transform;
            roundRect.anchorMin = roundRect.anchorMax = new Vector2(0.5f, 1f);
            roundRect.pivot = new Vector2(0.5f, 1f);
            roundRect.sizeDelta = new Vector2(300f, 60f);
            roundRect.anchoredPosition = new Vector2(0f, -UiTheme.ScreenPadding - 18f);
        }

        private void BuildBottleArea(RectTransform root)
        {
            _bottleArea = UiFactory.CreatePanel(root, "BottleArea", Color.clear);
            _bottleArea.anchorMin = new Vector2(0f, 0f);
            _bottleArea.anchorMax = new Vector2(1f, 1f);
            _bottleArea.offsetMin = new Vector2(UiTheme.ScreenPadding, 260f); // 하단 바 자리 비워둠
            _bottleArea.offsetMax = new Vector2(-UiTheme.ScreenPadding, -220f); // 상단 바 자리 비워둠

            // forceExpandHeight는 일부러 false — true면 줄이 남는 공간을 억지로 채우려고
            // 늘어나면서 병까지 같이 늘어나 버린다(실제로 겪은 버그). 줄은 항상 병의
            // 실제 높이(BottleView가 못박은 고정값)만큼만 차지하고, 두 줄이 서로
            // 붙은 채로 이 영역 안에서 가운데 정렬되면 된다.
            var rows = UiFactory.AddVerticalLayout(_bottleArea, spacing: UiTheme.PanelSpacing, forceExpandWidth: true, forceExpandHeight: false);
            rows.childAlignment = TextAnchor.MiddleCenter;
        }

        private void BuildBottomBar(RectTransform root)
        {
            var leftGroup = UiFactory.CreatePanel(root, "LeftButtons", Color.clear);
            leftGroup.anchorMin = leftGroup.anchorMax = new Vector2(0f, 0f);
            leftGroup.pivot = new Vector2(0f, 0f);
            leftGroup.sizeDelta = new Vector2(220f, UiTheme.ButtonHeightSmall);
            leftGroup.anchoredPosition = new Vector2(UiTheme.ScreenPadding, UiTheme.ScreenPadding);
            UiFactory.AddHorizontalLayout(leftGroup, spacing: 16f, forceExpandWidth: false, forceExpandHeight: true);

            // TODO(sprite): icon_undo, icon_reset — GameDesign.md 4.2 순서: 실행취소, 초기화.
            _undoButton = UiFactory.CreateIconButton(leftGroup, null, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnUndoClicked, fallbackText: "취소");
            UiFactory.CreateIconButton(leftGroup, null, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnResetClicked, fallbackText: "초기화");

            var rightGroup = UiFactory.CreatePanel(root, "RightButtons", Color.clear);
            rightGroup.anchorMin = rightGroup.anchorMax = new Vector2(1f, 0f);
            rightGroup.pivot = new Vector2(1f, 0f);
            rightGroup.sizeDelta = new Vector2(220f, UiTheme.ButtonHeightSmall);
            rightGroup.anchoredPosition = new Vector2(-UiTheme.ScreenPadding, UiTheme.ScreenPadding);
            UiFactory.AddHorizontalLayout(rightGroup, spacing: 16f, forceExpandWidth: false, forceExpandHeight: true);

            // TODO(sprite): icon_hint_bulb, icon_add_container — 순서: 힌트, 병 추가.
            _hintButton = UiFactory.CreateIconButton(rightGroup, null, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnHintClicked, fallbackText: "힌트");
            UiFactory.CreateIconButton(rightGroup, null, UiTheme.ButtonHeightSmall, UiTheme.PanelColor, OnAddContainerClicked, fallbackText: "추가");
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

            RefreshAllBottles();
        }

        private void BuildRow(int startIndex, int count, IReadOnlyList<Container> containers)
        {
            var row = UiFactory.CreatePanel(_bottleArea, $"Row_{startIndex}", Color.clear);
            // forceExpandHeight: false — 위의 rows 레이아웃과 같은 이유(병이 늘어나면 안 됨).
            UiFactory.AddHorizontalLayout(row, spacing: 16f, forceExpandWidth: false, forceExpandHeight: false);

            for (int i = 0; i < count; i++)
            {
                int containerIndex = startIndex + i;
                var bottle = new BottleView(row, containers[containerIndex].Capacity, containerIndex, OnBottleTapped);
                _bottleViews.Add(bottle);
            }
        }

        private void OnBottleTapped(int index)
        {
            _hintMove = null; // 힌트는 다음 조작 전까지만 유효

            if (_selectedIndex == null)
            {
                if (_session.Board.Containers[index].IsEmpty) return; // 빈 병은 출발점이 될 수 없음
                _selectedIndex = index;
                RefreshAllBottles();
                return;
            }

            if (_selectedIndex.Value == index)
            {
                _selectedIndex = null; // 같은 병 재탭 = 선택 취소
                RefreshAllBottles();
                return;
            }

            int from = _selectedIndex.Value;
            _selectedIndex = null;
            var result = _session.TryMove(from, index);
            RefreshAllBottles();

            if (!result.Success)
                Debug.Log("[GameView] 무효 이동 — TODO: 진동/튕김 피드백");

            EvaluateBoardState();
        }

        private void OnUndoClicked()
        {
            _session.TryUndo();
            _selectedIndex = null;
            _hintMove = null;
            RefreshAllBottles();
        }

        private void OnResetClicked()
        {
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
            RefreshAllBottles();
        }

        private void OnAddContainerClicked()
        {
            // TODO: 병 추가는 광고/재화(Managers) 연동 이후 — 정책 자체가 GameDesign.md TBD.
            Debug.Log("[GameView] 병 추가 — 아직 정책 미확정");
        }

        private void RefreshAllBottles()
        {
            var containers = _session.Board.Containers;
            for (int i = 0; i < _bottleViews.Count; i++)
            {
                _bottleViews[i].Refresh(containers[i]);
                _bottleViews[i].SetHighlight(Color.clear);
            }

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
