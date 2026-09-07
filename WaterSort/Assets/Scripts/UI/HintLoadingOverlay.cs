using UnityEngine;
using UnityEngine.UI;

namespace ColorSort.UI
{
    /// <summary>
    /// 힌트를 계산하는 동안 "지금 뭔가 진행 중"임을 보여주는 오버레이 — 딤 배경 +
    /// 가운데서 빙글빙글 도는 원형 스피너. <see cref="ConfirmDialog"/>와 겉모양은
    /// 비슷하지만(딤 배경 + 화면 위에 얹힘) 결정적으로 다른 점 하나: 이건 입력을
    /// 막지 않는다(사용자 확정) — 힌트를 계산하는 동안에도 다른 병 조작이나 버튼은
    /// 계속 눌릴 수 있어야 해서, 배경/스피너 둘 다 raycastTarget을 끈다.
    /// </summary>
    public static class HintLoadingOverlay
    {
        /// <param name="canvasRoot">Canvas 바로 아래 등, 현재 화면 위에 그려질 부모
        /// (ConfirmDialog와 같은 이유로 화면 자신이 아니라 Canvas 직속에 둠).</param>
        public static RectTransform Show(Transform canvasRoot)
        {
            var root = UiFactory.CreatePanel(canvasRoot, "HintLoadingOverlay", Color.clear);
            UiFactory.Stretch(root);
            root.GetComponent<Image>().raycastTarget = false; // 색이 clear라도 raycastTarget이 켜져 있으면 클릭을 막아버린다(실제 함정).

            var dim = UiFactory.CreateImage(root, "Dim", null, UiTheme.DimBackground);
            UiFactory.Stretch((RectTransform)dim.transform);
            dim.raycastTarget = false;

            // UiSkin.LoadingSpinner(도트가 점점 흐려지는 것처럼 "잔상"이 이미 그림에
            // 들어있는 디자인)가 있으면 그 그림을 그대로 회전만 시킨다 — 그림 자체가
            // 이미 회전 방향의 잔상을 표현하고 있어서 추가 효과 없이 회전만으로
            // 자연스러운 로딩 스피너가 된다. 없으면 예전처럼 기본 원(circle.png)을
            // Radial360로 부채꼴 채워서 대신 돌린다(둘 다 흰색 틴트 — 사용자 확정).
            var skinSpinnerSprite = UiTheme.Skin != null ? UiTheme.Skin.LoadingSpinner : null;
            bool useSkinSpinner = skinSpinnerSprite != null;
            var spinnerSprite = useSkinSpinner ? skinSpinnerSprite : UiTheme.LoadingSpinnerSprite;

            var spinner = UiFactory.CreateImage(root, "Spinner", spinnerSprite, Color.white);
            spinner.raycastTarget = false;
            if (useSkinSpinner)
            {
                spinner.type = Image.Type.Simple; // 이미 완성된 그림 그대로 — 9-slice/부채꼴 채우기 필요 없음.
                spinner.preserveAspect = true;
            }
            else
            {
                spinner.type = Image.Type.Filled; // CreateImage가 sprite 있으면 기본 Sliced로 두는 걸 덮어씀.
                spinner.fillMethod = Image.FillMethod.Radial360;
                spinner.fillClockwise = false;
                spinner.fillAmount = 0.75f; // 완전히 안 닫힌 원 — 계속 돌면서 "로딩 중" 느낌.
            }

            var spinnerRect = (RectTransform)spinner.transform;
            spinnerRect.anchorMin = spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRect.pivot = new Vector2(0.5f, 0.5f);
            spinnerRect.sizeDelta = new Vector2(UiTheme.LoadingSpinnerSize, UiTheme.LoadingSpinnerSize);
            spinnerRect.anchoredPosition = Vector2.zero;

            spinner.gameObject.AddComponent<SpinnerRotation>();

            return root;
        }

        public static void Hide(RectTransform overlay)
        {
            if (overlay != null) UnityEngine.Object.Destroy(overlay.gameObject);
        }

        /// <summary>매 프레임 자기 자신을 일정 각속도로 돌리기만 하는 최소 컴포넌트 —
        /// Image.fillAmount를 고정해 둔 채로 이것만 돌리면 "빙글빙글 도는 로딩 스피너"
        /// 모양이 된다.</summary>
        private sealed class SpinnerRotation : MonoBehaviour
        {
            private void Update() =>
                transform.Rotate(0f, 0f, -UiTheme.LoadingSpinnerDegreesPerSecond * Time.deltaTime);
        }
    }
}
