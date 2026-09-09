using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// "STAGE CLEAR" 텍스트가 완전히 나타나는 순간, 그 뒤(z-order상 텍스트보다
    /// 아래, 딤 배경보다 위)에서 원형 파티클 여러 개가 방사형으로 튀어나가며
    /// 옅어지다 사라지는 축하 이펙트(사용자 확정: "원형으로 팡 터지는 효과").
    ///
    /// 새 파티클 시스템이나 스프라이트 없이, 이미 있는 원형 그림(circle.png —
    /// 힌트 배지·로딩 스피너와 같은 에셋)을 <see cref="UiTheme.StageClearBurstParticleCount"/>개
    /// 만큼 복제해서 각자 다른 각도로 동시에 밀어내는 방식으로 만든다 —
    /// 이 프로젝트가 붓기 물줄기·플로팅 텍스트 등 모든 연출을 코드로 직접
    /// 짜는 것과 같은 방식(재사용 노트 4장 패턴).
    /// </summary>
    public static class StageClearBurst
    {
        /// <param name="layer">StageClearOverlay.Show가 미리 만들어 둔 빈 컨테이너
        /// (Background와 Text 사이 z-order에 이미 자리 잡혀 있음, 라운드 클리어
        /// 오버레이와 생명주기를 같이함) — 여기에 파티클을 채워 넣고 재생만 한다.</param>
        public static void Play(RectTransform layer)
        {
            if (layer == null) return;

            int count = UiTheme.StageClearBurstParticleCount;
            var particles = new RectTransform[count];
            var images = new Image[count];
            var directions = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                float angleDeg = i * (360f / count);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                directions[i] = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                var img = UiFactory.CreateImage(layer, "Particle", UiTheme.LoadingSpinnerSprite, UiTheme.StageClearBurstColor);
                img.type = Image.Type.Simple; // CreateImage 기본값(Sliced)이 아니라 원본 그림 그대로.
                img.preserveAspect = true;
                img.raycastTarget = false;

                var rect = (RectTransform)img.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(UiTheme.StageClearBurstParticleSize, UiTheme.StageClearBurstParticleSize);
                rect.anchoredPosition = Vector2.zero; // 전부 중앙에서 시작 — 여기서부터 방사형으로 퍼짐.

                particles[i] = rect;
                images[i] = img;
            }

            layer.gameObject.AddComponent<Runner>().Play(particles, directions, images);
        }

        /// <summary>파티클 전부를 한 코루틴에서 같이 굴리는 최소 컴포넌트 — Toast/
        /// FloatingHintCharge처럼 파티클마다 따로 컴포넌트를 붙이지 않고, 여기
        /// 하나가 배열을 순회하며 매 프레임 위치/알파를 갱신한다(파티클 수가 많아
        /// 코루틴 N개보다 저렴).</summary>
        private sealed class Runner : MonoBehaviour
        {
            public void Play(RectTransform[] particles, Vector2[] directions, Image[] images) =>
                StartCoroutine(Run(particles, directions, images));

            private IEnumerator Run(RectTransform[] particles, Vector2[] directions, Image[] images)
            {
                float duration = UiTheme.StageClearBurstDuration;
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    float p = Mathf.Clamp01(t / duration);
                    float e = EaseOut(p); // 빠르게 튀어나갔다가 서서히 멈추는 느낌.
                    float alpha = 1f - p; // 퍼져나가는 동안 동시에 옅어짐.

                    for (int i = 0; i < particles.Length; i++)
                    {
                        if (particles[i] == null) continue;
                        particles[i].anchoredPosition = directions[i] * (UiTheme.StageClearBurstMaxDistance * e);
                        var c = images[i].color;
                        images[i].color = new Color(c.r, c.g, c.b, alpha);
                    }
                    yield return null;
                }

                // 다 옅어진 뒤엔 필요 없다 — 오버레이 전체는 아직 몇 초 더 살아있으니
                // (페이드아웃까지) 미리 정리해 둔다. layer 자체(부모)는 여기서 안 건드림 —
                // StageClearOverlay.Hide가 오버레이 전체를 지울 때 같이 정리된다.
                foreach (var p in particles)
                    if (p != null) Destroy(p.gameObject);
                Destroy(this);
            }

            private static float EaseOut(float p) => 1f - Mathf.Pow(1f - p, 3f); // ease-out cubic.
        }
    }
}
