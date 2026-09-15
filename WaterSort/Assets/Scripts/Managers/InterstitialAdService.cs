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

        /// <summary>Preload가 걸려 있는데도 아직 로드가 안 끝났을 때 ShowAsync가
        /// 포기하기 전에 기다려 주는 최대 시간. 4라운드마다 자동으로 뜨는 전면
        /// 광고는(GameDesign 확정) 유저가 딱히 "지금 봐야지" 하고 기다리는 게
        /// 아니라서 무한정 기다리게 하면 안 되지만, 0초 대기(이전 동작)는 실제
        /// 비공개 테스트에서 문제가 됐다(사용자 제보, 2026-09-15) — MobileAds.
        /// Initialize 콜드스타트 비용 + 실제 광고 요청 왕복시간을 합치면, 특히
        /// 앱을 막 켠 뒤 앞쪽 쉬운 라운드 4개를 빠르게 깨버리는 경우 4라운드째
        /// 클리어 시점에 아직 로드가 안 끝나 있어서 그 회차의 광고가 통째로
        /// 스킵되곤 했다. 이 구간은 이미 "STAGE CLEAR" 화면이 불투명하게 덮고
        /// 있는 유지 구간이라(StageClearOverlay), 몇 초 더 기다려도 화면이
        /// 비어 보이거나 하지는 않는다.</summary>
        private static readonly TimeSpan MaxWaitForLoad = TimeSpan.FromSeconds(4);

        /// <summary>광고를 보여주고, 닫히면 완료되는 Task를 돌려준다. 아직 로드 중이면
        /// 위 MaxWaitForLoad만큼만 기다려 봤다가(폴링), 그래도 준비가 안 됐으면
        /// 포기하고 다음을 위해 로드만 다시 시도한다 — 광고 때문에 라운드 전환이
        /// 영영 막히면 안 되므로 실패는 전부 "그냥 넘어감"으로 처리한다.</summary>
        public static async Task ShowAsync(string adUnitId)
        {
            if (!IsReady(adUnitId))
            {
                var deadline = DateTime.UtcNow + MaxWaitForLoad;
                while (!IsReady(adUnitId) && DateTime.UtcNow < deadline)
                    await Task.Delay(200);
            }

            if (!_loadedAds.TryGetValue(adUnitId, out var ad) || ad == null)
            {
                Debug.Log($"[InterstitialAdService] {MaxWaitForLoad.TotalSeconds}초 기다려도 준비 안 됨 — 이번 회차는 건너뜀({adUnitId})");
                Preload(adUnitId);
                return;
            }

            _loadedAds.Remove(adUnitId); // 한 번 쓰면 소모됨.
            var tcs = new TaskCompletionSource<bool>();

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
            await tcs.Task;
        }

        private static void EnsureInitialized(Action onReady)
        {
            if (_initialized) { onReady(); return; }
            if (_initializing) return;

            // RewardedAdService와 같은 이유 — 동의(ConsentService) 전엔 SDK를 안 켠다.
            if (!ConsentService.CanRequestAds)
            {
                Debug.Log("[InterstitialAdService] 동의 대기 중 — 아직 광고 SDK를 초기화하지 않음");
                return;
            }

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
