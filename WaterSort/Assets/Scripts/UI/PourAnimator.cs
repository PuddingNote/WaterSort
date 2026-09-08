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
        private readonly List<ActivePour> _active = new List<ActivePour>();
        private readonly List<GameObject> _activeStreams = new List<GameObject>();
        private readonly Dictionary<GameObject, Image> _streamImages = new Dictionary<GameObject, Image>();
        private readonly HashSet<int> _busySourceIndices = new HashSet<int>();

        /// <summary>도착 병 인덱스별로 지금 그 병으로 들어오고 있는 붓기가 몇 개인지 —
        /// 여러 병에서 같은 병으로 동시에 쏟아붓는 걸 허용하다 보니(사용자 확정),
        /// 그중 하나가 먼저 끝났다고 곧바로 BottleView.Refresh를 부르면 아직 애니메이션
        /// 중인 다른 붓기의 세그먼트까지 통째로 파괴돼서, 그 붓기가 눈에 안 보이게
        /// 멈췄다가 자기 차례에 갑자기 확 차오르는 것처럼 보이는 버그가 실제로 있었다
        /// (PlayRoutine 참고). 같은 도착 병을 향한 붓기가 전부 끝난 마지막 순간에만
        /// Refresh한다.</summary>
        private readonly Dictionary<int, int> _activeDestCounts = new Dictionary<int, int>();

        /// <summary>진행 중인 붓기 하나를 CancelAll이 "자연 종료와 똑같이" 되돌리는 데
        /// 필요한 최소 정보. PlayRoutine이 병을 그리드에서 떼어내는 순간 채워 넣는다
        /// (그 전에 취소되면 비어있는 채로 남는데, 그때는 애초에 되돌릴 것도 없다).</summary>
        private sealed class ActivePour
        {
            public Coroutine Routine;
            public BottleView Source;
            public Transform OriginalParent;
            public int SiblingIndex;
            public GameObject Spacer;
        }

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
            _activeDestCounts.TryGetValue(move.ToIndex, out int destCount);
            _activeDestCounts[move.ToIndex] = destCount + 1;

            var entry = new ActivePour { Source = source };
            entry.Routine = _host.StartCoroutine(RunAndUntrack());
            _active.Add(entry);

            IEnumerator RunAndUntrack()
            {
                yield return PlayRoutine(move, source, dest, entry);
                _active.Remove(entry);
                _busySourceIndices.Remove(move.FromIndex);

                // PlayRoutine이 정상적으로 끝났든, 방어적 분기(shrink == null)로
                // 일찍 끝났든 항상 여기까지는 도달한다 — _activeDestCounts를
                // Play() 시작 시점에 이미 늘려놨으니, 여기서도 항상 짝을 맞춰
                // 줄여야 한다(안 그러면 카운트가 영영 안 줄어들어서 그 도착 병은
                // 앞으로 절대 Refresh가 안 되는 버그가 생김).
                if (EndDestPour(move.ToIndex))
                    dest.Refresh(_session.Board.Containers[move.ToIndex]);

                if (_active.Count == 0) onComplete?.Invoke();
            }
        }

        /// <summary>Undo/Reset 등 즉시 스냅해야 하는 조작 전에 호출 — 진행 중인 연출을
        /// 전부 끊는다. StopCoroutine은 그 지점 뒤에 있는 "제자리로 복귀" 정리 코드를
        /// 통째로 건너뛰어서, 그냥 멈추기만 하면 붓던 병이 그리드에서 떨어져 나간 채
        /// (EffectsLayer에 붙어 자유 앵커·기울어진 각도로) 화면에 고정되어 버린다
        /// (실제로 겪은 버그 — Undo 도중 캡처한 스크린샷에서 병 하나가 붕 뜬 채 굳어
        /// 있었음). 그래서 여기서 자연 종료 때와 똑같은 정리(원래 부모/자리로 복귀,
        /// spacer 제거, 기울기 0)를 직접 해준다. 물이 어디까지 옮겨졌는지는 뒤이어
        /// 호출되는 BottleView.Refresh가 Board 기준으로 다시 그려서 정리한다.</summary>
        public void CancelAll()
        {
            foreach (var entry in _active)
            {
                if (entry.Routine != null) _host.StopCoroutine(entry.Routine);

                if (entry.Spacer != null) UnityEngine.Object.Destroy(entry.Spacer);
                if (entry.Source != null)
                {
                    entry.Source.SetTilt(0f);
                    entry.Source.SetWaterHorizontalOffset(0f); // 기울기와 짝을 이루는 보정값도 같이 원상복구.
                    if (entry.OriginalParent != null)
                    {
                        var root = entry.Source.Root;
                        root.SetParent(entry.OriginalParent, false);
                        root.SetSiblingIndex(entry.SiblingIndex);
                    }
                }
            }
            _active.Clear();

            foreach (var stream in _activeStreams)
                if (stream != null) UnityEngine.Object.Destroy(stream);
            _activeStreams.Clear();
            _streamImages.Clear();
            _busySourceIndices.Clear(); // StopCoroutine은 RunAndUntrack의 정리 코드를 건너뛰므로 여기서 직접 비움.
            _activeDestCounts.Clear(); // 마찬가지 이유 — 안 비우면 나중 붓기의 EndDestPour 카운트가 어긋난다.
        }

        /// <summary>도착 병 하나로 들어오던 붓기 중 하나가 끝났다고 알린다. 같은 병으로
        /// 들어오는 다른 붓기가 아직 남아있으면 false(지금은 Refresh하면 안 됨),
        /// 이번이 마지막이었으면 true(이제 Board 기준으로 최종 스냅해도 안전함).</summary>
        private bool EndDestPour(int destIndex)
        {
            if (!_activeDestCounts.TryGetValue(destIndex, out int count)) return true; // 방어적 — 정상 흐름에선 항상 있어야 함.
            count -= 1;
            if (count <= 0)
            {
                _activeDestCounts.Remove(destIndex);
                return true;
            }
            _activeDestCounts[destIndex] = count;
            return false;
        }

        private IEnumerator PlayRoutine(MoveResult move, BottleView source, BottleView dest, ActivePour entry)
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
            // CancelAll이 이 지점부터는 되돌릴 게 생겼다는 걸 알 수 있게 기록해 둔다.
            entry.OriginalParent = originalParent;
            entry.SiblingIndex = siblingIndex;
            entry.Spacer = spacer;

            Vector3 startWorldPos = root.position;
            root.SetParent(_effectsLayer, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(UiTheme.BottleWidth, UiTheme.BottleHeight);
            root.position = startWorldPos; // 화면상 위치는 그대로 유지한 채로 부모만 교체.

            float fullAngle = sign * UiTheme.PourTiltAngleDeg;
            // BottleMask/BottleBackground 윗부분이 살짝 어긋나 있어서, 기울어진 동안
            // 물 쪽(_waterVisual)을 이만큼 옆으로 밀어야 병 그림이 스파웃 위치에
            // 자연스럽게 맞아 보인다(사용자 확정치, UiTheme.PourVisualHorizontalNudge
            // 참고). 부호가 sign의 반대인 이유: 이 값은 물 쪽에 주는 오프셋이고,
            // 실제로 화면에서 "병 그림이 이만큼 움직여 보이는" 방향은 아래 델타 보정
            // (hoverRootTarget 계산)이 이 오프셋을 상쇄하면서 반대 방향으로 밀어내는
            // 결과이기 때문이다(BottleView.SetWaterHorizontalOffset 주석 참고) — 그래서
            // sign과 반대 부호를 써야 사용자가 원한 방향("오른쪽으로 기울 때 이 값,
            // 왼쪽으로 기울 때 반대")이 실제로 나온다.
            float fullNudge = -sign * UiTheme.PourVisualHorizontalNudge;

            // 스파웃(입구의 처지는 쪽 모서리)이 도착 병 바로 위, 도착 병과 같은 X에
            // 오도록 목표 Root 위치를 미리 한 번만 정확히 구한다 — 그래야 다 기울었을
            // 때 물줄기가 대각선이 아니라 똑바로 아래로 떨어진다. pivot·패딩을 직접
            // 유도해서 미리 계산하는 방식은 오차가 나기 쉬웠다(실제로 겪음) — 대신
            // "일단 그 각도로 놓고 스파웃이 실제로 어디 있는지 측정해서, 목표와의
            // 차이만큼 그대로 옮기면 된다"는 방식을 쓴다. 병진이동은 회전과 무관하게
            // 그대로 더해지므로 이 보정은 근사가 아니라 항상 정확하다 — 위 가로 보정
            // (fullNudge)도 측정 시점에 이미 적용해 둬야, 측정된 스파웃 위치 자체가
            // 보정 반영된 값이라 나머지 계산이 그대로 맞아떨어진다. 측정하는 동안
            // 잠깐 각도/보정을 바꿨다가 같은 프레임 안에서 원상복구하므로 화면엔 전혀
            // 안 보인다.
            Vector3 hoverSpoutTarget = HoverSpoutTarget(dest);
            source.SetTilt(fullAngle);
            source.SetWaterHorizontalOffset(fullNudge);
            Vector3 measuredSpoutAtFullTilt = SpoutWorldPosition(source); // 이때 root.position은 아직 startWorldPos.
            Vector3 hoverRootTarget = startWorldPos + (hoverSpoutTarget - measuredSpoutAtFullTilt);
            source.SetTilt(0f); // phase 1이 각도 0에서 시작해야 하므로 원상복구.
            source.SetWaterHorizontalOffset(0f);

            // 1) 들어올려서 목표 병 위로 이동 + 기울이기 시작.
            yield return Tween(UiTheme.PourLiftTime, p =>
            {
                float e = Ease(p);
                source.SetTilt(fullAngle * e);
                source.SetWaterHorizontalOffset(fullNudge * e);
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
                source.SetWaterHorizontalOffset(fullNudge * (1f - e));
                root.position = Vector3.Lerp(hoverRootTarget, startWorldPos, e);
            });
            source.SetTilt(0f);
            source.SetWaterHorizontalOffset(0f);
            root.position = startWorldPos; // 부동소수 오차 없이 정확히 원위치로 스냅.

            // 원래 자리로 복귀 — childControlWidth/Height=true인 레이아웃 그룹이라
            // 다음 갱신에서 anchoredPosition/sizeDelta를 알아서 다시 맞춰준다.
            if (spacer != null) UnityEngine.Object.Destroy(spacer);
            root.SetParent(originalParent, false);
            root.SetSiblingIndex(siblingIndex);

            // 출발 병 최종 스냅 — 겹친 이동이나 부동소수 오차로 어긋났을 수 있는 걸
            // Board 기준으로 확실히 정리한다. 출발 병은 항상 이 붓기 하나만의
            // 소유라(IsBusy가 같은 병을 또 출발점으로 못 고르게 막음) 바로
            // Refresh해도 안전하다. 도착 병 쪽은 다른 붓기가 아직 붓고 있을 수
            // 있어서(사용자 확정으로 허용됨) 여기서 바로 하지 않고 RunAndUntrack이
            // (PlayRoutine이 여기 도달하지 못하고 일찍 끝나도 항상 실행되는 지점)
            // EndDestPour로 판단해서 처리한다 — 자세한 이유는 그쪽 주석 참고.
            source.Refresh(_session.Board.Containers[move.FromIndex]);
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

        // 프레임 하나가 이 값보다 오래 걸렸으면(GC, 무거운 동기 계산 등으로 실제
        // 렉이 걸렸으면) 그만큼을 그대로 t에 더하지 않고 이 값으로 잘라 쓴다 —
        // 안 그러면 렉 걸린 그 한 프레임의 Time.deltaTime이 그대로 커져서, 다음
        // 프레임에 애니메이션 진행률이 한번에 훅 뛰어버려(구간을 통째로 건너뛴
        // 것처럼) 보인다(실제로 겪은 버그: 힌트 계산이 메인 스레드를 잠깐 막았을
        // 때 붓기 애니메이션 1단계가 통째로 스킵된 것처럼 재생됨). 힌트 계산 자체는
        // 백그라운드 스레드로 옮겨서 렉이 안 나게 고쳤지만(GameView.OnHintClicked),
        // 이 클램프는 그거와 별개로 앞으로 어떤 이유로든 프레임이 오래 걸리는
        // 상황이 생겨도 최소한 애니메이션이 "스킵"돼 보이진 않게 하는 안전장치다.
        private const float MaxFrameDelta = 1f / 30f;

        private IEnumerator Tween(float duration, Action<float> onUpdate)
        {
            if (duration <= 0f) { onUpdate(1f); yield break; }
            float t = 0f;
            while (t < duration)
            {
                t += Mathf.Min(Time.deltaTime, MaxFrameDelta);
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
