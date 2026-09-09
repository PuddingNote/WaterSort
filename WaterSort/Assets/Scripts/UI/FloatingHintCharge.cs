using System;
using System.Collections;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 힌트가 충전됐을 때(3라운드 클리어마다, HintStore 참고) 힌트 버튼 위쪽에
    /// 잠깐 나타나는 "+1" 텍스트 — 등장은 즉시(페이드인 없이 바로 100% 불투명),
    /// 그대로 위로 떠오르면서 동시에 투명해지다가 사라진다(사용자 확정). Toast와
    /// 같은 "코루틴을 붙인 컴포넌트가 재생 후 자기 자신을 파괴" 패턴이지만, Toast는
    /// 화면 중앙 고정 위치인 반면 이건 임의의 UI 요소(힌트 버튼) 기준으로 시작
    /// 좌표를 매번 계산한다.
    /// </summary>
    public static class FloatingHintCharge
    {
        /// <param name="canvasRoot">Canvas 바로 아래 등, 현재 화면 위에 그려질 부모
        /// (Toast/HintLoadingOverlay와 같은 이유로 화면 자신이 아니라 Canvas 직속에 둠).</param>
        /// <param name="anchorTarget">이 위쪽 가운데를 기준(+ UiTheme.FloatingHintChargeStartExtraRiseY만큼
        /// 더 위)으로 시작한다(힌트 버튼의 RectTransform).</param>
        public static void Show(Transform canvasRoot, RectTransform anchorTarget)
        {
            var text = UiFactory.CreateText(canvasRoot, "+1", UiTheme.FloatingHintChargeFontSize, UiTheme.FloatingHintChargeColor);
            text.raycastTarget = false;

            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(UiTheme.FloatingHintChargeWidth, UiTheme.FloatingHintChargeHeight);
            rect.SetAsLastSibling(); // 항상 다른 화면 요소들보다 위에 보이게.

            // anchorTarget(힌트 버튼)의 위쪽 모서리 중앙 월드 좌표를 canvasRoot 기준
            // 로컬 좌표로 변환한다 — PourAnimator.ToLocal/UpdateStream과 같은 방식
            // (이미 검증된 패턴, Screen Space Overlay Canvas 기준이라 카메라 없이 동작).
            var corners = new Vector3[4];
            anchorTarget.GetWorldCorners(corners); // 0=BL, 1=TL, 2=TR, 3=BR
            Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, topCenterWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvasRoot, screenPoint, null, out var buttonTopCenter);
            // 버튼 모서리에 딱 붙은 자리가 아니라 그보다 더 위(사용자가 스크린샷으로 지정한 지점)에서 시작한다.
            Vector2 startPos = buttonTopCenter + new Vector2(0f, UiTheme.FloatingHintChargeStartExtraRiseY);
            rect.anchoredPosition = startPos;

            var group = text.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f; // 페이드인 없이 즉시 등장(사용자 확정).
            group.blocksRaycasts = false;
            group.interactable = false;

            text.gameObject.AddComponent<Runner>().Play(rect, group, startPos);
        }

        /// <summary>떠오르며 사라지는 연출을 재생하고 끝나면 자기 자신(텍스트 오브젝트
        /// 전체)을 파괴하는 최소 컴포넌트 — Toast.ToastRunner와 같은 구조.</summary>
        private sealed class Runner : MonoBehaviour
        {
            public void Play(RectTransform rect, CanvasGroup group, Vector2 startPos) =>
                StartCoroutine(Run(rect, group, startPos));

            private IEnumerator Run(RectTransform rect, CanvasGroup group, Vector2 startPos)
            {
                Vector2 endPos = startPos + new Vector2(0f, UiTheme.FloatingHintChargeRiseDistance);

                yield return Tween(UiTheme.FloatingHintChargeDuration, p =>
                {
                    float e = Ease(p);
                    rect.anchoredPosition = Vector2.Lerp(startPos, endPos, e);
                    group.alpha = 1f - e;
                });

                Destroy(gameObject);
            }

            private static IEnumerator Tween(float duration, Action<float> onUpdate)
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

            private static float Ease(float p) => p * p * (3f - 2f * p); // smoothstep — Toast/PourAnimator와 동일.
        }
    }
}
