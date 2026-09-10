using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 물병 하나가 한 색으로 가득 차 "완성"되는 순간, 그 병 위치에서 작은 원형
    /// 파티클 여러 개가 방사형으로 튀어나가며 옅어지다 사라지는 축하 이펙트 —
    /// <see cref="StageClearBurst"/>의 축소판(사용자 요청, 2026-09-10 "스테이지
    /// 클리어 이펙트처럼 조그맣게").
    ///
    /// StageClearBurst는 라운드 클리어 오버레이가 미리 만들어 둔 layer(항상 화면
    /// 중앙)를 빌려 쓰지만, 이건 병마다 위치가 달라서 자기 컨테이너를 직접 만들고
    /// (월드 좌표로 병 중앙에 놓음) 다 끝나면 그 컨테이너째 스스로 정리한다.
    /// 파티클 재생 방식(circle.png를 여러 개 복제해 각자 다른 각도로 밀어내며
    /// 알파를 낮춤)은 StageClearBurst와 같다 — 이 프로젝트가 모든 연출을 코드로
    /// 직접 짜는 방식(재사용 노트 4장 패턴).
    /// </summary>
    public static class BottleCompleteBurst
    {
        /// <param name="parent">이펙트를 담을 부모(보통 GameView의 EffectsLayer — 병/버튼보다 위).</param>
        /// <param name="worldCenter">터짐이 시작될 월드 좌표(보통 완성된 병 윗부분 + Inspector 오프셋).</param>
        /// <param name="color">파티클 색 — 완성된 병을 채운 물 색(WaterPalette)을 그대로 넘긴다.</param>
        public static void Play(RectTransform parent, Vector3 worldCenter, Color color)
        {
            if (parent == null) return;

            var container = UiFactory.CreatePanel(parent, "BottleCompleteBurst", Color.clear);
            container.GetComponent<Image>().raycastTarget = false;
            container.anchorMin = container.anchorMax = new Vector2(0.5f, 0.5f);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = Vector2.zero;
            container.position = worldCenter; // 같은 Canvas 안이라 월드 좌표로 바로 놓아도 된다.

            int count = UiTheme.BottleCompleteBurstParticleCount;
            var particles = new RectTransform[count];
            var images = new Image[count];
            var directions = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                float angleRad = i * (2f * Mathf.PI / count);
                directions[i] = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                var img = UiFactory.CreateImage(container, "Particle", UiTheme.LoadingSpinnerSprite, color);
                img.type = Image.Type.Simple; // CreateImage 기본값(Sliced)이 아니라 원본 그림 그대로.
                img.preserveAspect = true;
                img.raycastTarget = false;

                var rect = (RectTransform)img.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(UiTheme.BottleCompleteBurstParticleSize, UiTheme.BottleCompleteBurstParticleSize);
                rect.anchoredPosition = Vector2.zero;

                particles[i] = rect;
                images[i] = img;
            }

            container.gameObject.AddComponent<Runner>().Play(container, particles, directions, images);
        }

        /// <summary>파티클 전부 + 컨테이너를 한 코루틴에서 굴리고, 다 옅어지면 컨테이너째
        /// 파괴한다(StageClearBurst.Runner와 달리 부모 layer를 공유하지 않으므로 자기가
        /// 만든 걸 자기가 치운다).</summary>
        private sealed class Runner : MonoBehaviour
        {
            public void Play(RectTransform container, RectTransform[] particles, Vector2[] directions, Image[] images) =>
                StartCoroutine(Run(container, particles, directions, images));

            private IEnumerator Run(RectTransform container, RectTransform[] particles, Vector2[] directions, Image[] images)
            {
                float duration = UiTheme.BottleCompleteBurstDuration;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / duration);
                    float e = 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic — 빠르게 튀어나갔다가 서서히 멈춤.
                    float alpha = 1f - p;                  // 퍼지는 동안 같이 옅어짐.

                    for (int i = 0; i < particles.Length; i++)
                    {
                        if (particles[i] == null) continue;
                        particles[i].anchoredPosition = directions[i] * (UiTheme.BottleCompleteBurstMaxDistance * e);
                        var c = images[i].color;
                        images[i].color = new Color(c.r, c.g, c.b, alpha);
                    }
                    yield return null;
                }

                if (container != null) Destroy(container.gameObject); // Runner도 이 컨테이너에 붙어 있으니 같이 사라짐.
            }
        }
    }
}
