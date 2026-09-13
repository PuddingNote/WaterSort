using System;
using UnityEngine;
#if ADS_ENABLED
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
#endif

namespace ColorSort.Managers
{
    /// <summary>
    /// GDPR(EEA/영국/스위스) 동의를 Google UMP(User Messaging Platform)로 수집하는
    /// 서비스 — 전 세계 배포 확정(사용자, 2026-09-13)에 따라 필요해졌다. 동의 메시지
    /// 자체의 문구/디자인은 AdMob 콘솔(앱 > 개인정보 및 메시지)에서 이미 작성 완료된
    /// 상태(사용자 확정) — 여기는 그 메시지를 언제·어떻게 요청·표시할지만 담당한다.
    /// 그 지역이 아니거나 이미 유효한 동의가 있으면 <see cref="GatherConsent"/>가
    /// 폼을 띄우지 않고 바로 완료된다(이용자는 아무것도 못 느낌) — 지역 판정은
    /// Google 서버가 한다.
    ///
    /// <see cref="RewardedAdService"/>/<see cref="InterstitialAdService"/>의
    /// EnsureInitialized는 반드시 <see cref="CanRequestAds"/>가 true일 때만
    /// MobileAds.Initialize를 부르도록 바꿔 뒀다 — 동의가 필요한 지역에서 동의를
    /// 받기 전에 광고 SDK를 초기화(=네트워크 요청 시작)하면 안 된다는 Google
    /// 정책 때문이다.
    ///
    /// SDK가 없거나(ADS_ENABLED 꺼짐) 상태에서는 CanRequestAds가 항상 true(기존
    /// "광고 없음"으로 계속 동작), IsPrivacyOptionsRequired는 항상 false(설정 창의
    /// PRIVACY OPTIONS 버튼 자체를 안 그림)로 동작한다.
    /// </summary>
    public static class ConsentService
    {
#if ADS_ENABLED
        /// <summary>지금 광고를 요청해도 되는지 — 이게 true가 되기 전까지는
        /// RewardedAdService/InterstitialAdService가 광고 SDK를 아예 초기화하지 않는다.</summary>
        public static bool CanRequestAds => ConsentInformation.CanRequestAds();

        /// <summary>설정 창의 "PRIVACY OPTIONS" 버튼을 보여줘야 하는지 — Google 정책상
        /// PrivacyOptionsRequirementStatus가 Required일 때만 노출해야 한다(그 외 지역
        /// 유저에게 이 버튼을 보여주면 오히려 정책 위반 — 동작할 동의 자체가 없음).</summary>
        public static bool IsPrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        /// <summary>앱 시작 시 한 번 호출 — 동의 정보를 최신화하고, 필요한 경우에만
        /// (해당 지역 + 아직 유효한 동의 없음) 폼을 자동으로 띄운다. 폼에서 어떤
        /// 선택을 하든(동의/거부/맞춤 해제 등), 또는 애초에 폼이 필요 없었든
        /// 끝나면 onComplete가 정확히 한 번 불린다 — 네트워크 실패 등으로 갱신
        /// 자체가 안 돼도 게임 진행을 막으면 안 되므로 항상 호출한다(그 경우
        /// CanRequestAds는 false로 남아 광고만 계속 비활성 상태가 된다).</summary>
        public static void GatherConsent(Action onComplete)
        {
            var request = new ConsentRequestParameters();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 에디터/개발 빌드에서는 지역과 무관하게 EEA로 강제해서 폼이 실제로
            // 뜨는지 직접 확인할 수 있게 한다 — 이 심볼은 실제 배포 빌드엔 안 들어가서
            // 실사용자에겐 전혀 영향 없다(AdUnitIds의 테스트/프로덕션 분기와 같은 이유).
            request.ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.EEA
            };
#endif
            ConsentInformation.Update(request, updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"[ConsentService] 동의 정보 갱신 실패: {updateError.Message}");
                    onComplete?.Invoke();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                        Debug.LogWarning($"[ConsentService] 동의 폼 표시 실패: {formError.Message}");
                    onComplete?.Invoke();
                });
            });
        }

        /// <summary>설정 창의 "PRIVACY OPTIONS" 버튼에서 호출 — 이미 한 동의 선택을
        /// 다시 열어 바꿀 수 있게 한다(IsPrivacyOptionsRequired가 true인 지역에서만
        /// 버튼 자체가 보이므로, 그 외 지역에서 이 메서드가 불릴 일은 없다).</summary>
        public static void ShowPrivacyOptionsForm(Action onClosed)
        {
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                if (error != null)
                    Debug.LogWarning($"[ConsentService] Privacy Options 폼 표시 실패: {error.Message}");
                onClosed?.Invoke();
            });
        }
#else
        public static bool CanRequestAds => true; // SDK 없음 — 기존처럼 항상 통과시켜 광고 흐름 그대로 유지.
        public static bool IsPrivacyOptionsRequired => false; // 버튼 자체를 안 보여줌.
        public static void GatherConsent(Action onComplete) => onComplete?.Invoke();
        public static void ShowPrivacyOptionsForm(Action onClosed) => onClosed?.Invoke();
#endif
    }
}
