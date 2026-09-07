using System;
using System.Collections;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 화면 가운데 잠깐 떴다가 사라지는 짧은 텍스트 알림 — 힌트를 더 진행할 수 없을
    /// 때(더 이상 유효한 수가 없음) 등, 그 자리에서 바로 알려줘야 하는 메시지에
    /// 쓴다(사용자 확정). 다른 오버레이들과 같은 원칙으로 입력은 막지 않는다.
    ///
    /// 연출(사용자 확정): 가운데보다 조금 위에서 생성돼 투명한 채로 시작 → 가운데로
    /// 부드럽게 이동하며 동시에 투명→불투명 → 잠깐 유지 → 위치는 그대로 두고
    /// 불투명→투명으로 사라짐.
    /// </summary>
    public static class Toast
    {
        /// <param name="canvasRoot">Canvas 바로 아래 등, 현재 화면 위에 그려질 부모
        /// (ConfirmDialog/HintLoadingOverlay와 같은 이유로 화면 자신이 아니라 Canvas 직속에 둠).</param>
        public static void Show(Transform canvasRoot, string message)
        {
            var text = UiFactory.CreateText(canvasRoot, message, UiTheme.ToastFontSize, UiTheme.TextPrimary);
            text.raycastTarget = false;

            var rect = (RectTransform)text.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(UiTheme.ToastWidth, UiTheme.ToastHeight);
            rect.SetAsLastSibling(); // 항상 다른 화면 요소들보다 위에 보이게.

            var group = text.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            text.gameObject.AddComponent<ToastRunner>().Play(rect, group);
        }

        /// <summary>등장/유지/퇴장 타임라인을 코루틴으로 재생하고 끝나면 자기 자신(텍스트
        /// 오브젝트 전체)을 파괴하는 최소 컴포넌트 — Toast.Show가 만든 오브젝트에 붙여
        /// 쓴다.</summary>
        private sealed class ToastRunner : MonoBehaviour
        {
            public void Play(RectTransform rect, CanvasGroup group) => StartCoroutine(Run(rect, group));

            private IEnumerator Run(RectTransform rect, CanvasGroup group)
            {
                Vector2 restPos = Vector2.zero; // 앵커가 이미 화면 가운데(0.5,0.5)라 (0,0) = 가운데.
                Vector2 startPos = restPos + new Vector2(0f, UiTheme.ToastRiseDistance);
                rect.anchoredPosition = startPos;

                // 1) 등장 — 위에서 가운데로 부드럽게 이동하며 투명 -> 불투명, 도착과 동시에 멈춤.
                yield return Tween(UiTheme.ToastInDuration, p =>
                {
                    float e = Ease(p);
                    rect.anchoredPosition = Vector2.Lerp(startPos, restPos, e);
                    group.alpha = e;
                });
                rect.anchoredPosition = restPos;
                group.alpha = 1f;

                // 2) 유지 — 자리/불투명 그대로.
                yield return new WaitForSeconds(UiTheme.ToastHoldDuration);

                // 3) 퇴장 — 위치는 그대로 두고 불투명 -> 투명만(사용자 확정).
                yield return Tween(UiTheme.ToastOutDuration, p => group.alpha = 1f - p);
                group.alpha = 0f;

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

            private static float Ease(float p) => p * p * (3f - 2f * p); // smoothstep — PourAnimator와 동일한 완만한 감속.
        }
    }
}
