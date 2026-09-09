using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 라운드 클리어 시 화면 전체를 덮는 딤 배경 + "STAGE CLEAR" 텍스트 연출.
    /// 배경색은 뒤로가기 확인 다이얼로그와 같은 <see cref="UiTheme.DimBackground"/>
    /// (0A0A1A, 목표 알파 166/255) — 새 색을 만들지 않고 그대로 재사용한다(사용자
    /// 확정, 2026-09-08).
    ///
    /// 텍스트와 배경은 서로 다른 알파로 따로 움직인다(재확정, 2026-09-08):
    /// 페이드인 구간은 같이 0에서 각자 목표치로 올라가지만, 페이드아웃 구간은
    /// 배경이 목표 알파에 고정돼 있다가 텍스트 알파가 그 밑으로 내려오는 순간부터
    /// 텍스트와 정확히 같은 값을 취하며(<c>Mathf.Min</c> 한 줄) 함께 0까지 내려간다 —
    /// "모든 화면이 자연스럽게 투명해지는 것처럼" 보이게 하기 위함. 그래서 배경은
    /// 텍스트처럼 CanvasGroup 하나로 묶어 페이드할 수 없고, 각자의 Graphic.alpha를
    /// 매 프레임 별도로 계산해서 넣는다 — <see cref="Play"/> 참고.
    ///
    /// 실제 라운드 전환(다음 라운드를 미리 다 만든 뒤 화면을 바꿔치기)은 이 클래스가
    /// 아니라 호출부(GameBootstrap)가 <see cref="Play"/>에 콜백으로 넘긴다 — 이
    /// 클래스는 "만들기 + 정해진 3구간 애니메이션 재생"만 담당하고, 그 재생 도중
    /// 정확히 어느 시점에 화면 뒤에서 무슨 일이 일어나는지는 모른다.
    ///
    /// 다른 오버레이(Toast/HintLoadingOverlay)들과 달리 입력을 막는다
    /// (blocksRaycasts = true, 전용 CanvasGroup으로 항상 alpha=1 고정) — 이건
    /// 유저가 끼어들 이유가 없는 완전히 자동인 전환이라, 반투명하게 페이드하는
    /// 도중에도 병을 조작하거나 Undo/Reset을 눌러서 곧 사라질 GameView와 경합이
    /// 생기는 걸 막기 위해서다.
    /// </summary>
    public static class StageClearOverlay
    {
        public readonly struct Handle
        {
            public readonly RectTransform Root;
            public readonly Image Background;
            public readonly RectTransform BurstLayer;
            public readonly TMP_Text Text;
            public Handle(RectTransform root, Image background, RectTransform burstLayer, TMP_Text text)
            {
                Root = root;
                Background = background;
                BurstLayer = burstLayer;
                Text = text;
            }
        }

        /// <param name="canvasRoot">Canvas 바로 아래 등, 현재 화면 위에 그려질 부모
        /// (ConfirmDialog/HintLoadingOverlay와 같은 이유로 화면 자신이 아니라 Canvas 직속에 둠).</param>
        public static Handle Show(Transform canvasRoot)
        {
            // Root 자신은 안 보이는 입력 차단용 껍데기일 뿐이고(Color.clear), 실제
            // 딤 배경은 따로 만든 Background 자식이 맡는다 — 배경/텍스트 알파를 서로
            // 다른 곡선으로 움직여야 해서(위 클래스 doc 참고) 하나의 CanvasGroup으로
            // 묶을 수 없기 때문.
            var root = UiFactory.CreatePanel(canvasRoot, "StageClearOverlay", Color.clear);
            UiFactory.Stretch(root);
            root.SetAsLastSibling(); // 항상 최상단.

            var blockGroup = root.gameObject.AddComponent<CanvasGroup>();
            blockGroup.alpha = 1f; // 시각적 페이드와 무관하게 항상 고정 — 입력 차단 전용.
            blockGroup.blocksRaycasts = true;
            blockGroup.interactable = false;

            var backgroundRect = UiFactory.CreatePanel(root, "Background", UiTheme.DimBackground);
            UiFactory.Stretch(backgroundRect);
            var background = backgroundRect.GetComponent<Image>();
            SetBackgroundAlpha(background, 0f); // 처음엔 완전히 투명.

            // Background 다음, Text보다 먼저 만들어서 z-order를 "배경 위, 텍스트
            // 아래"로 고정해 둔다(StageClearBurst 참고) — 실제로 파티클을 채워
            // 넣고 재생하는 건 Play()가 텍스트가 완전히 나타나는 순간에 한다.
            var burstLayer = UiFactory.CreatePanel(root, "Burst", Color.clear);
            UiFactory.Stretch(burstLayer);
            burstLayer.GetComponent<Image>().raycastTarget = false;

            var text = UiFactory.CreateText(root, "STAGE CLEAR", UiTheme.FontSizeStageClear, UiTheme.TextPrimary);
            text.enableWordWrapping = false; // 두 줄로 꺾이지 않고 항상 한 줄로.
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(1080f, 200f);
            textRect.anchoredPosition = Vector2.zero;
            text.alpha = 0f; // 처음엔 완전히 투명.

            return new Handle(root, background, burstLayer, text);
        }

        public static void Hide(Handle overlay)
        {
            if (overlay.Root != null) UnityEngine.Object.Destroy(overlay.Root.gameObject);
        }

        /// <summary>
        /// 정해진 3구간(사용자 확정, UiTheme 참고)을 순서대로 재생한다.
        /// <paramref name="onHoldPhase"/>는 2구간(유지) 동안 실행되는 실제 라운드
        /// 전환 — 그 작업이 끝날 때까지, 그리고 최소 <see cref="UiTheme.StageClearHoldTime"/>
        /// 초가 지날 때까지 둘 다 기다린 뒤에야 3구간(페이드아웃)으로 넘어간다.
        /// </summary>
        public static async Task Play(Handle overlay, Func<Task> onHoldPhase)
        {
            float bgTarget = UiTheme.DimBackground.a; // 다이얼로그 딤 배경과 동일한 목표 알파(166/255).

            // 1) 페이드인: 텍스트 0->100%, 배경도 함께 0->목표 알파로 등장.
            // 원형 파티클 축하 이펙트(StageClearBurst)는 텍스트가 다 나타난 뒤가
            // 아니라 "나타나고 있는 도중"(페이드인 절반 지점, 0.5초)에 맞춰 튼다
            // (사용자 확정 — 처음엔 페이드인이 끝난 직후였는데, 텍스트가 아직 채
            // 안 보일 때 이펙트가 먼저 시작되는 편이 더 낫다고 재요청). t는 0~1
            // 진행률이고 StageClearFadeInTime이 1초라 t>=0.5가 정확히 0.5초 시점과
            // 같다 — Animate 콜백 안에서 한 번만 트는 플래그로 구현.
            bool burstStarted = false;
            await Animate(UiTheme.StageClearFadeInTime, t =>
            {
                SetTextAlpha(overlay.Text, t);
                SetBackgroundAlpha(overlay.Background, Mathf.Lerp(0f, bgTarget, t));
                if (!burstStarted && t >= 0.5f)
                {
                    burstStarted = true;
                    StageClearBurst.Play(overlay.BurstLayer);
                }
            });
            if (!burstStarted) StageClearBurst.Play(overlay.BurstLayer); // 방어적 — 프레임이 너무 커서 0.5 지점을 건너뛴 경우.

            // 2) 유지: 텍스트/배경 그대로 둔 채 실제 라운드 전환을 실행.
            // 버그 수정(2026-09-08): onHoldPhase가 만드는 새 GameView는 canvas의 맨
            // 마지막 자식으로 붙어서(기본 동작) 전환되는 그 순간 이 오버레이보다 위로
            // 올라가 버렸다 — 전환과 동시에 "STAGE CLEAR"가 갑자기 뒤로 가려져 뚝
            // 끊긴 것처럼 사라져 보이던 원인. 전환이 끝나자마자(=holdTask 완료 즉시)
            // 오버레이를 다시 맨 위로 올려서, 남은 유지 시간과 페이드아웃 내내 항상
            // 최상단에 남아있게 한다.
            SetTextAlpha(overlay.Text, 1f);
            SetBackgroundAlpha(overlay.Background, bgTarget);
            var holdTask = RunHoldPhase(overlay, onHoldPhase);
            await Task.WhenAll(holdTask, Task.Delay(TimeSpan.FromSeconds(UiTheme.StageClearHoldTime)));

            // 3) 페이드아웃: 텍스트 100%->0%. 배경은 텍스트 알파가 자기 목표치보다
            // 높은 동안은 그 목표치에 고정, 텍스트 알파가 목표치 밑으로 내려오는
            // 순간부터 텍스트와 같은 값을 취해 함께 0까지 내려간다.
            await Animate(UiTheme.StageClearFadeOutTime, t =>
            {
                float textAlpha = Mathf.Lerp(1f, 0f, t);
                SetTextAlpha(overlay.Text, textAlpha);
                SetBackgroundAlpha(overlay.Background, Mathf.Min(bgTarget, textAlpha));
            });
        }

        /// <summary>onHoldPhase(실제 라운드 전환)를 실행하고, 끝나는 즉시 오버레이를
        /// 다시 맨 위 sibling으로 올린다 — 새로 생긴 GameView가 그 사이 위로 올라온
        /// 걸 되돌리는 처리(위 Play() 주석 참고).</summary>
        private static async Task RunHoldPhase(Handle overlay, Func<Task> onHoldPhase)
        {
            if (onHoldPhase != null) await onHoldPhase();
            if (overlay.Root != null) overlay.Root.SetAsLastSibling();
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text != null) text.alpha = alpha;
        }

        private static void SetBackgroundAlpha(Image background, float alpha)
        {
            if (background == null) return;
            var c = UiTheme.DimBackground;
            background.color = new Color(c.r, c.g, c.b, alpha);
        }

        /// <summary>duration초에 걸쳐 진행도 t(0~1)를 프레임마다 갱신하며 apply를 호출한다
        /// (Time.deltaTime 누적 — GameBootstrap엔 코루틴을 돌릴 MonoBehaviour가 없어서
        /// Task/await 기반으로 짰다, Toast/PourAnimator의 코루틴 버전과 같은 원리).</summary>
        private static async Task Animate(float duration, Action<float> apply)
        {
            if (duration <= 0f) { apply(1f); return; }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                apply(Mathf.Clamp01(elapsed / duration));
                await Task.Yield();
            }
            apply(1f);
        }
    }
}
