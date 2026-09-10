using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if ADS_ENABLED
using GoogleMobileAds.Api;
#endif

namespace ColorSort.Managers
{
    /// <summary>
    /// 전면(interstitial) 광고 로드/노출을 감싸는 서비스 — 4라운드 클리어마다
    /// 라운드가 바뀌기 직전에 한 번 보여준다(GameBootstrap.PlayStageClearThenAdvance).
    /// 보상형(<see cref="RewardedAdService"/>)과 달리 "끝까지 봤는지"에 따른 보상이
    /// 없으므로, 광고가 닫히면(또는 준비 안 됐으면 즉시) 완료되는 <see cref="ShowAsync"/>
    /// 하나만 노출한다.
    ///
    /// SDK가 없거나 <c>ADS_ENABLED</c>가 꺼져 있으면 <see cref="ShowAsync"/>는 곧바로
    /// 완료되는 Task를 돌려줘서 라운드 전환이 그대로 이어진다(설치 방법은
    /// <see cref="RewardedAdService"/> 상단 주석 참고 — 같은 SDK다).
    /// </summary>
    public static class InterstitialAdService
    {
#if ADS_ENABLED
        private static readonly Dictionary<string, InterstitialAd> _loadedAds = new Dictionary<string, InterstitialAd>();
        private static readonly HashSet<string> _loading = new HashSet<string>();
        private static bool _initialized;
        private static bool _initializing;

        public static bool IsReady(string adUnitId) =>
            _loadedAds.TryGetValue(adUnitId, out var ad) && ad != null;

        /// <summary>미리 로드해 둔다 — 라운드 시작마다 한 번, 그리고 한 번 쓴 뒤 다시.
        /// 이미 로드됐거나 로드 중이면 조용히 무시한다.</summary>
        public static void Preload(string adUnitId) => EnsureInitialized(() => LoadInternal(adUnitId));

        /// <summary>광고를 보여주고, 닫히면 완료되는 Task를 돌려준다. 준비가 안 됐으면
        /// 즉시 완료(대기 없음)하고 다음을 위해 로드만 다시 시도한다 — 광고 때문에
        /// 라운드 전환이 막히면 안 되므로 실패는 전부 "그냥 넘어감"으로 처리한다.</summary>
        public static Task ShowAsync(string adUnitId)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!_loadedAds.TryGetValue(adUnitId, out var ad) || ad == null)
            {
                Preload(adUnitId);
                tcs.SetResult(false);
                return tcs.Task;
            }

            _loadedAds.Remove(adUnitId); // 한 번 쓰면 소모됨.

            void Finish(bool shown)
            {
                Preload(adUnitId);      // 다음 전면 광고를 미리 다시 로드.
                tcs.TrySetResult(shown);
            }

            ad.OnAdFullScreenContentClosed += () => Finish(true);
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning($"[InterstitialAdService] 광고 표시 실패({adUnitId}): {error}");
                Finish(false);
            };

            ad.Show();
            return tcs.Task;
        }

        private static void EnsureInitialized(Action onReady)
        {
            if (_initialized) { onReady(); return; }
            if (_initializing) return;

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

            Debug.Log($"[InterstitialAdService] 광고 로드 시작({adUnitId})");
            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                _loading.Remove(adUnitId);
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[InterstitialAdService] 광고 로드 실패({adUnitId}): {error}");
                    return;
                }
                _loadedAds[adUnitId] = ad;
            });
        }
#else
        public static bool IsReady(string adUnitId) => false;

        public static void Preload(string adUnitId) =>
            Debug.Log($"[InterstitialAdService] ADS_ENABLED 꺼짐(또는 SDK 미설치) — 전면 광고 없음으로 동작({adUnitId})");

        public static Task ShowAsync(string adUnitId) => Task.CompletedTask;
#endif
    }
}
