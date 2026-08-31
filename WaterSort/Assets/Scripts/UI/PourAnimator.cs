using System;
using System.Collections;
using System.Collections.Generic;
using ColorSort.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 이동 하나(<see cref="MoveResult"/>)를 실제 붓기 연출로 재생한다: 출발 병을
    /// 그리드에서 잠깐 떼어내 도착 병 바로 위(스파웃이 도착 병과 같은 X)로 들어올려
    /// 기울이고 → 물줄기(직선, 스파웃→도착 병의 실제 수면) + 양쪽 병 물 높이 동시
    /// 변화 → 제자리로 복귀. 총 소요시간과 각 구간 비중은 GameDesign.md TBD
    /// 확정값(<see cref="UiTheme"/>) 그대로.
    ///
    /// 입력은 막지 않는다(사용자 확정) — 다른 병 이동이 애니메이션 도중에 또
    /// 들어오면 그냥 각자 따로 재생된다. 대신 겹칠 때: 나중에 시작한 물줄기가
    /// 항상 캔버스 최상단에 그려지고(SetAsLastSibling), 사운드는 공유
    /// AudioSource 하나를 매번 다시 Play()해서 이전 재생을 자동으로 끊는다.
    /// Undo/Reset처럼 상태를 강제로 되돌리는 조작만 <see cref="CancelAll"/>로
    /// 진행 중인 연출을 전부 끊고, 뒤이어 오는 즉시 새로고침이 최종 상태로 스냅한다.
    /// </summary>
    public sealed class PourAnimator
    {
        private readonly MonoBehaviour _host;
        private readonly PuzzleSession _session;
        private readonly RectTransform _effectsLayer;
        private readonly AudioSource _audioSource;
        private readonly List<Coroutine> _active = new List<Coroutine>();
        private readonly List<GameObject> _activeStreams = new List<GameObject>();
        private readonly Dictionary<GameObject, Image> _streamImages = new Dictionary<GameObject, Image>();
        private readonly HashSet<int> _busySourceIndices = new HashSet<int>();

        public PourAnimator(MonoBehaviour host, PuzzleSession session, RectTransform effectsLayer, AudioSource audioSource)
        {
            _host = host;
            _session = session;
            _effectsLayer = effectsLayer;
            _audioSource = audioSource;
        }

        /// <summary>containerIndex가 지금 붓는 병(출발 병)으로 자리를 비우고 있는지 —
        /// 그 병이 원래 자리로 돌아올 때까지는 GameView가 탭을 무시한다(사용자 확정).
        /// 도착 병은 대상이 아니다 — 여러 병에서 같은 병으로 연달아 쏟아붓는 건
        /// 기존처럼 계속 가능해야 한다(사용자 확정).</summary>
        public bool IsBusy(int containerIndex) => _busySourceIndices.Contains(containerIndex);

        /// <param name="onComplete">이 이동의 붓기 연출이 끝난 뒤 호출된다. 단, 그
        /// 시점에 다른 이동의 연출이 아직 겹쳐서 진행 중이면 부르지 않고, 마지막
        /// 하나가 끝날 때 한 번만 부른다 — 클리어 판정(라운드 클리어 → 다음 라운드
        /// 전환)이 애니메이션 도중에, 화면이 아직 다 안 찼는데 성급하게 일어나면
        /// 안 되기 때문(사용자가 실제로 겪은 버그: 마지막 이동의 붓기가 채 끝나기도
        /// 전에 다음 라운드로 넘어감).</param>
        public void Play(MoveResult move, BottleView source, BottleView dest, Action onComplete = null)
        {
            _busySourceIndices.Add(move.FromIndex);

            Coroutine routine = null;
            routine = _host.StartCoroutine(RunAndUntrack());
            _active.Add(routine);

            IEnumerator RunAndUntrack()
            {
                yield return PlayRoutine(move, source, dest);
                _active.Remove(routine);
                _busySourceIndices.Remove(move.FromIndex);
                if (_active.Count == 0) onComplete?.Invoke();
            }
        }

        /// <summary>Undo/Reset 등 즉시 스냅해야 하는 조작 전에 호출 — 진행 중인 연출을
        /// 전부 끊는다. 물이 어디까지 옮겨졌는지는 뒤이어 호출되는 BottleView.Refresh가
        /// Board 기준으로 그대로 다시 그려서 정리한다.</summary>
        public void CancelAll()
        {
            foreach (var routine in _active)
                if (routine != null) _host.StopCoroutine(routine);
            _active.Clear();

            foreach (var stream in _activeStreams)
                if (stream != null) UnityEngine.Object.Destroy(stream);
            _activeStreams.Clear();
            _streamImages.Clear();
            _busySourceIndices.Clear(); // StopCoroutine은 RunAndUntrack의 정리 코드를 건너뛰므로 여기서 직접 비움.
        }

        private IEnumerator PlayRoutine(MoveResult move, BottleView source, BottleView dest)
        {
            var shrink = source.BeginShrinkTop();
            if (shrink == null) yield break; // 방어적 — 규칙상 출발 병은 항상 비어있지 않음.
            var grow = dest.BeginGrowTop(move.Color);

            float shrinkStart = shrink.UnitCount;
            float shrinkTarget = Mathf.Max(0f, shrinkStart - move.Count);
            float growStart = grow.UnitCount;
            float growTarget = growStart + move.Count;
            float sign = TiltSign(source, dest);

            // 붓는 병을 자기 줄(HorizontalLayoutGroup)에서 잠깐 떼어내 자유롭게
            // 움직일 수 있게 한다. 자리엔 spacer를 남겨서 같은 줄의 다른 병들이
            // 밀리지 않게 한다.
            var root = source.Root;
            Transform originalParent = root.parent;
            int siblingIndex = root.GetSiblingIndex();
            var spacer = CreateSpacer(originalParent, siblingIndex);

            Vector3 startWorldPos = root.position;
            root.SetParent(_effectsLayer, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(UiTheme.BottleWidth, UiTheme.BottleHeight);
            root.position = startWorldPos; // 화면상 위치는 그대로 유지한 채로 부모만 교체.

            float fullAngle = sign * UiTheme.PourTiltAngleDeg;

            // 스파웃(입구의 처지는 쪽 모서리)이 도착 병 바로 위, 도착 병과 같은 X에
            // 오도록 목표 Root 위치를 미리 한 번만 정확히 구한다 — 그래야 다 기울었을
            // 때 물줄기가 대각선이 아니라 똑바로 아래로 떨어진다. pivot·패딩을 직접
            // 유도해서 미리 계산하는 방식은 오차가 나기 쉬웠다(실제로 겪음) — 대신
            // "일단 그 각도로 놓고 스파웃이 실제로 어디 있는지 측정해서, 목표와의
            // 차이만큼 그대로 옮기면 된다"는 방식을 쓴다. 병진이동은 회전과 무관하게
            // 그대로 더해지므로 이 보정은 근사가 아니라 항상 정확하다. 측정하는 동안
            // 잠깐 각도를 바꿨다가 같은 프레임 안에서 0으로 되돌리므로 화면엔 전혀
            // 안 보인다.
            Vector3 hoverSpoutTarget = HoverSpoutTarget(dest);
            source.SetTilt(fullAngle);
            Vector3 measuredSpoutAtFullTilt = SpoutWorldPosition(source); // 이때 root.position은 아직 startWorldPos.
            Vector3 hoverRootTarget = startWorldPos + (hoverSpoutTarget - measuredSpoutAtFullTilt);
            source.SetTilt(0f); // phase 1이 각도 0에서 시작해야 하므로 원상복구.

            // 1) 들어올려서 목표 병 위로 이동 + 기울이기 시작.
            yield return Tween(UiTheme.PourLiftTime, p =>
            {
                float e = Ease(p);
                source.SetTilt(fullAngle * e);
                root.position = Vector3.Lerp(startWorldPos, hoverRootTarget, e);
            });

            // 2) 붓기 — 물줄기 + 양쪽 물 높이 변화를 같은 시간 동안 동시 진행. 위치와
            // 기울기는 고정(1번 마지막 프레임에서 이미 hoverRootTarget/fullAngle로
            // 정확히 도달해 있음).
            PlaySound();
            var stream = CreateStream();
            yield return Tween(UiTheme.PourFlowTime, p =>
            {
                shrink.SetUnitCount(Mathf.Lerp(shrinkStart, shrinkTarget, p));
                grow.SetUnitCount(Mathf.Lerp(growStart, growTarget, p));
                UpdateStream(stream, source, dest, move.Color, move.Count);
            });
            DestroyStream(stream);

            // 3) 제자리로 복귀 + 세우기.
            yield return Tween(UiTheme.PourLiftTime, p =>
            {
                float e = Ease(p);
                source.SetTilt(fullAngle * (1f - e));
                root.position = Vector3.Lerp(hoverRootTarget, startWorldPos, e);
            });
            source.SetTilt(0f);
            root.position = startWorldPos; // 부동소수 오차 없이 정확히 원위치로 스냅.

            // 원래 자리로 복귀 — childControlWidth/Height=true인 레이아웃 그룹이라
            // 다음 갱신에서 anchoredPosition/sizeDelta를 알아서 다시 맞춰준다.
            if (spacer != null) UnityEngine.Object.Destroy(spacer);
            root.SetParent(originalParent, false);
            root.SetSiblingIndex(siblingIndex);

            // 최종 스냅 — 겹친 이동이나 부동소수 오차로 어긋났을 수 있는 걸 Board
            // 기준으로 확실히 정리한다(이 두 병만 건드리고, 다른 진행 중인 연출은 안 건드림).
            source.Refresh(_session.Board.Containers[move.FromIndex]);
            dest.Refresh(_session.Board.Containers[move.ToIndex]);
        }

        private static GameObject CreateSpacer(Transform parent, int siblingIndex)
        {
            var go = new GameObject("BottleSpacer", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(siblingIndex);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minWidth = layoutElement.preferredWidth = UiTheme.BottleWidth;
            layoutElement.minHeight = layoutElement.preferredHeight = UiTheme.BottleHeight;
            return go;
        }

        /// <summary>도착 병 바로 위, 붓기 시작할 때 붓는 병의 스파웃(입구의 처지는
        /// 쪽 모서리)이 최종적으로 있어야 할 위치(월드 좌표) — 도착 병 입구와 같은
        /// X라 물줄기가 똑바로 아래로 떨어진다. 도착 병 자신의 실제 화면 높이에
        /// 비례해서 띄우니 기기별 캔버스 스케일과 무관하게 항상 자연스러운 여유가
        /// 나온다.</summary>
        private static Vector3 HoverSpoutTarget(BottleView dest)
        {
            Vector3 mouth = MouthWorldPosition(dest);
            float destHeight = WorldHeight(dest.Root);
            return mouth + Vector3.up * (destHeight * UiTheme.PourHoverHeightRatio);
        }

        private IEnumerator Tween(float duration, Action<float> onUpdate)
        {
            if (duration <= 0f) { onUpdate(1f); yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                onUpdate(Mathf.Clamp01(t / duration));
                yield return null;
            }
            onUpdate(1f);
        }

        private static float Ease(float p) => p * p * (3f - 2f * p); // smoothstep — 뚝뚝 끊기지 않게

        private static float TiltSign(BottleView source, BottleView dest)
        {
            // 도착 병이 오른쪽에 있으면 위쪽(입구)이 오른쪽으로 기울도록 시계방향(-).
            return dest.Root.position.x >= source.Root.position.x ? -1f : 1f;
        }

        private void PlaySound()
        {
            var clip = UiTheme.Skin != null ? UiTheme.Skin.PourSound : null;
            if (clip == null || _audioSource == null) return;
            _audioSource.clip = clip;
            _audioSource.Play(); // 같은 AudioSource를 다시 Play()하면 이전 재생은 그 순간 끊긴다.
        }

        // 물줄기는 곡선 대신 직선 하나로 그린다 — 짧은 사각형 여러 개를 곡선으로
        // 이어 붙였더니 마디마다 각도가 꺾여 보여서 오히려 부자연스러웠다(사용자
        // 확정: 그냥 일직선이 낫다). UiFactory.CreateImage는 버튼/세그먼트에서
        // 이미 검증된 방식이라 처음 시도했던 커스텀 MaskableGraphic 메시보다 안전하다
        // (그 메시는 실제로 화면에 아예 안 보이는 문제가 있었음).
        private GameObject CreateStream()
        {
            var go = new GameObject("PourStream", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_effectsLayer, false);
            UiFactory.Stretch(rect);
            rect.SetAsLastSibling(); // 나중에 시작한 붓기가 항상 캔버스 최상단(사용자 확정).

            var img = UiFactory.CreateImage(rect, "Stream", sprite: null, Color.clear);
            img.raycastTarget = false;
            var imgRect = (RectTransform)img.transform;
            imgRect.anchorMin = imgRect.anchorMax = new Vector2(0.5f, 0.5f);
            imgRect.pivot = new Vector2(0f, 0.5f); // 왼쪽 끝 = 시작점(스파웃)에 정확히 붙임.

            _streamImages[go] = img;
            _activeStreams.Add(go);
            return go;
        }

        private void DestroyStream(GameObject streamGo)
        {
            _activeStreams.Remove(streamGo);
            _streamImages.Remove(streamGo);
            if (streamGo != null) UnityEngine.Object.Destroy(streamGo);
        }

        private void UpdateStream(GameObject streamGo, BottleView source, BottleView dest, ColorId color, int count)
        {
            if (streamGo == null || !_streamImages.TryGetValue(streamGo, out var img)) return;
            var rect = (RectTransform)streamGo.transform;

            // 시작점은 병 입구의 "가운데"가 아니라, 기울어져서 실제로 더 아래로 처진
            // 쪽 모서리(스파웃) — 안 그러면 입구 한복판에서 물이 솟아나는 것처럼
            // 보인다. 끝점도 도착 병 입구(항상 고정된 자리)가 아니라 그 안에 실제로
            // 차 있는 물의 수면 — 안 그러면 물이 병 위쪽에서 뚝 끊긴 채 허공에
            // 떨어지는 것처럼 보인다(둘 다 사용자 확정 버그). 물이 차오르면서
            // 수면도 매 프레임 같이 올라가니 물줄기가 계속 자연스럽게 따라붙는다.
            Vector2 start = ToLocal(rect, SpoutWorldPosition(source));
            Vector2 end = ToLocal(rect, dest.WaterSurfaceWorldPosition());

            Vector2 diff = end - start;
            float length = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            var imgRect = (RectTransform)img.transform;
            imgRect.sizeDelta = new Vector2(length, StreamThickness(count));
            imgRect.anchoredPosition = start;
            imgRect.localRotation = Quaternion.Euler(0f, 0f, angle);
            img.color = WaterPalette.Get(color);
        }

        private static float StreamThickness(int count)
            => Mathf.Clamp(UiTheme.PourStreamBaseThickness + count * 1.5f, 10f, 26f);

        /// <summary>병 입구(물이 드나드는 지점) 월드 좌표 — FillArea 위쪽 변의 중앙.
        /// 안 기울었을 때(도착 병, 또는 붓는 병의 기울기 0 기준점) 쓴다. 붓는 병이
        /// 실제로 기울어진 동안의 물줄기 시작점은 <see cref="SpoutWorldPosition"/>을 쓴다
        /// — 입구 한복판이 아니라 처진 쪽 모서리라야 실제로 물이 흘러나오는 지점과 맞다.</summary>
        private static Vector3 MouthWorldPosition(BottleView bottle)
        {
            var corners = new Vector3[4];
            bottle.FillArea.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
            return (corners[1] + corners[2]) * 0.5f;
        }

        /// <summary>기울어진 병에서 물이 실제로 흘러나오는 지점(스파웃) — 입구 양쪽
        /// 모서리 중 더 아래로 처진 쪽. 안 기울었을 때는 두 모서리 높이가 같아서
        /// 자연히 <see cref="MouthWorldPosition"/>과 같은 결과가 된다.</summary>
        private static Vector3 SpoutWorldPosition(BottleView bottle)
        {
            var corners = new Vector3[4];
            bottle.FillArea.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
            return corners[1].y <= corners[2].y ? corners[1] : corners[2];
        }

        private static float WorldHeight(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[1].y - corners[0].y; // TL.y - BL.y
        }

        private static Vector2 ToLocal(RectTransform relativeTo, Vector3 worldPos)
        {
            // Screen Space Overlay Canvas 기준(UiFactory.CreateRootCanvas) — 카메라 없음.
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(relativeTo, screenPoint, null, out var local);
            return local;
        }
    }
}
