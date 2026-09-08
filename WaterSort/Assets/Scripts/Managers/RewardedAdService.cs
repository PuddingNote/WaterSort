using System;
using System.Collections.Generic;
using UnityEngine;
#if ADS_ENABLED
using GoogleMobileAds.Api;
#endif

namespace ColorSort.Managers
{
    /// <summary>
    /// 보상형 광고 로드/노출을 감싸는 서비스. 지금은 병 추가 버튼 하나만 쓰지만,
    /// adUnitId를 인자로 받게 만들어서 나중에 다른 버튼(힌트 등)도 그대로 재사용할
    /// 수 있다(사용자가 "나머지는 나중에 요청하겠다"고 확정).
    ///
    /// Google Mobile Ads Unity SDK가 프로젝트에 아직 없으면(아래 설치 단계 참고 —
    /// 이 클래스가 대신 설치해 주지 않는다, Unity 패키지 매니저로 되는 게 아니라
    /// 에디터에서 직접 .unitypackage를 임포트해야 하는 절차라 코드로 자동화가
    /// 안 된다) ADS_ENABLED 스크립팅 정의가 꺼진 채로 컴파일되어 이 서비스는 항상
    /// "광고 없음"으로 동작한다 — SDK 부재가 프로젝트 전체 컴파일을 깨뜨리지
    /// 않게 하는 안전장치다. 이 상태에서는 병 추가 버튼이 계속 비활성화된
    /// 채로 있는 게 정상이다(GameDesign.md "광고 미시청/로드 실패 시 그 자리에서
    /// 비활성화" 정책과 완전히 같은 코드 경로를 그대로 탄다 — 별도 처리 불필요).
    ///
    /// SDK 설치 방법(에디터에서 직접 해야 함, 4단계):
    /// 1. https://github.com/googleads/googleads-mobile-unity/releases 에서 최신
    ///    GoogleMobileAdsPlugin.unitypackage를 받아 Assets > Import Package >
    ///    Custom Package로 임포트한다(딸려오는 External Dependency Manager 포함).
    /// 2. Assets > External Dependency Manager > Android Resolver > Resolve를
    ///    실행해 네이티브 안드로이드 의존성을 받는다.
    /// 3. Assets > Google Mobile Ads > Settings를 열어 App ID에
    ///    ca-app-pub-6387288948977074~1221971886 을 입력하고 저장한다.
    /// 4. Player Settings > Other Settings > Scripting Define Symbols(Android 탭)에
    ///    ADS_ENABLED를 추가한다.
    /// </summary>
    public static class RewardedAdService
    {
#if ADS_ENABLED
        private static readonly Dictionary<string, RewardedAd> _loadedAds = new Dictionary<string, RewardedAd>();
        private static readonly HashSet<string> _loading = new HashSet<string>();
        private static bool _initialized;
        private static bool _initializing;

        /// <summary>어떤 광고 단위든 새로 로드가 끝나 IsReady가 false→true로 바뀔 때마다
        /// 그 adUnitId와 함께 불린다 — 라운드 시작 시 미리 로드해 둔 광고가 늦게
        /// 도착하거나, 한 번 쓴 뒤 다음 걸 다시 로드하는 동안 버튼이 계속 비활성화된
        /// 채로 멈춰 있지 않도록 GameView가 이 이벤트를 구독해서 그때그때
        /// RefreshHighlights를 다시 부른다(구독은 반드시 OnDestroy에서 해지할 것 —
        /// 이건 static 이벤트라 안 끊으면 화면이 없어져도 델리게이트가 안 없어짐).</summary>
        public static event Action<string> AdReady;

        /// <summary>이 광고 단위가 지금 당장 보여줄 준비가 됐는지 — 버튼 활성화 여부를
        /// 이 값으로 결정한다(GameView 참고).</summary>
        public static bool IsReady(string adUnitId) =>
            _loadedAds.TryGetValue(adUnitId, out var ad) && ad != null;

        /// <summary>미리 로드해 둔다 — 라운드 시작 시 한 번, 그리고 광고를 한 번 쓸 때마다
        /// (성공/실패 무관) 다음 사용을 위해 다시 부른다. 이미 로드돼 있거나 로드
        /// 진행 중이면 조용히 무시한다.</summary>
        public static void Preload(string adUnitId) => EnsureInitialized(() => LoadInternal(adUnitId));

        /// <summary>광고를 보여준다. 준비가 안 됐으면 즉시 onUnavailable을 부르고
        /// (재화 등 대체 지급 없음 — GameDesign.md 확정 정책) 다음을 위해 다시
        /// 로드를 시도한다. 끝까지 다 봐야 onRewardEarned, 중간에 닫으면
        /// onClosedWithoutReward가 불린다.</summary>
        public static void Show(string adUnitId, Action onRewardEarned, Action onClosedWithoutReward, Action onUnavailable)
        {
            if (!_loadedAds.TryGetValue(adUnitId, out var ad) || ad == null)
            {
                onUnavailable?.Invoke();
                Preload(adUnitId);
                return;
            }

            _loadedAds.Remove(adUnitId); // 한 번 쓰면 소모됨 — 다음 사용을 위해 곧바로 다시 로드해야 함.
            bool rewarded = false;

            ad.OnAdFullScreenContentClosed += () =>
            {
                if (rewarded) onRewardEarned?.Invoke();
                else onClosedWithoutReward?.Invoke();
                Preload(adUnitId);
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning($"[RewardedAdService] 광고 표시 실패({adUnitId}): {error}");
                onUnavailable?.Invoke();
                Preload(adUnitId);
            };

            ad.Show(reward => { rewarded = true; });
        }

        private static void EnsureInitialized(Action onReady)
        {
            if (_initialized) { onReady(); return; }
            if (_initializing) return; // 이미 초기화 중 — 끝나면 이후 Preload 호출들이 알아서 로드함.

            _initializing = true;
            MobileAds.Initialize(_ =>
            {
                _initializing = false;
                _initialized = true;
                onReady();
            });
        }

        private static void LoadInternal(string adUnitId)
        {
            if (_loading.Contains(adUnitId) || IsReady(adUnitId)) return;
            _loading.Add(adUnitId);

            Debug.Log($"[RewardedAdService] 광고 로드 시작({adUnitId})");
            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                _loading.Remove(adUnitId);
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[RewardedAdService] 광고 로드 실패({adUnitId}): {error}");
                    return;
                }
                _loadedAds[adUnitId] = ad;
                AdReady?.Invoke(adUnitId);
            });
        }
#else
        public static event Action<string> AdReady; // SDK 없을 땐 절대 안 불림 — 구독은 해도 안전함.

        public static bool IsReady(string adUnitId) => false;

        public static void Preload(string adUnitId) =>
            // 이 로그가 보인다는 건 ADS_ENABLED가 지금 컴파일에는 반영 안 됐다는 뜻이다 —
            // 흔한 원인: Player Settings에서 Android 플랫폼에 심볼을 넣어놨어도, 에디터의
            // "Active Build Target"(File > Build Settings)이 Android가 아니면 Editor
            // Play 모드는 그 심볼을 안 쓴다. File > Build Settings에서 Android로
            // "Switch Platform"부터 확인해볼 것.
            Debug.Log($"[RewardedAdService] ADS_ENABLED 꺼짐(또는 SDK 미설치) — 광고 없음으로 동작({adUnitId})");

        public static void Show(string adUnitId, Action onRewardEarned, Action onClosedWithoutReward, Action onUnavailable)
            => onUnavailable?.Invoke();
#endif
    }
}
