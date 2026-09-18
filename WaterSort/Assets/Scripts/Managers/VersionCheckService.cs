using System;
using System.Threading.Tasks;
using ColorSort.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace ColorSort.Managers
{
    /// <summary>
    /// 강제 업데이트(Force Update Gate) 원격 확인 — 재사용_시스템_모음.md 1장 구조
    /// 그대로: 부팅 직후 <see cref="VersionUrl"/>의 정적 JSON을 조회해 현재 버전이
    /// minVersion보다 낮으면 <see cref="CheckResult.UpdateRequired"/>를 true로 돌려준다.
    ///
    /// 반드시 fail-open — 오프라인/타임아웃/404/JSON 파싱 실패 등 확인에 실패하는
    /// 모든 경우는 그냥 통과(UpdateRequired=false)시킨다. 오타 하나로 전체 유저가
    /// 게임을 못 켜는 사고가, 일부가 구버전을 쓰는 것보다 훨씬 나쁘다는 원칙(가이드
    /// 문서 근거) — 그래서 이 클래스는 예외를 절대 밖으로 던지지 않는다.
    /// </summary>
    public static class VersionCheckService
    {
        // {계정}.github.io 허브 저장소의 이 게임 폴더 — 개인정보처리방침_재사용_가이드.md
        // 2장 구조 그대로(privacy-policy.html과 같은 자리에 둘 예정). 2026-09-18
        // 기준 아직 이 저장소/폴더/파일이 실제로 만들어지지 않았다 — 그래도 fail-open
        // 덕분에 지금은 그냥 "확인 실패 → 통과"로 아무 영향이 없고, 나중에 실제로
        // 이 URL에 version.json을 올리는 순간부터 동작을 시작한다.
        private const string VersionUrl = "https://puddingnote.github.io/watersort/version.json";

        private const int TimeoutSeconds = 5;

        public readonly struct CheckResult
        {
            public readonly bool UpdateRequired;
            public readonly string StoreUrl;
            public readonly string Message;

            public CheckResult(bool updateRequired, string storeUrl, string message)
            {
                UpdateRequired = updateRequired;
                StoreUrl = storeUrl;
                Message = message;
            }

            public static readonly CheckResult Pass = new CheckResult(false, null, null);
        }

        [Serializable]
        private sealed class VersionInfo
        {
            public string minVersion;
            public string storeUrl;
            public string message;
        }

        public static async Task<CheckResult> CheckAsync(string currentVersion)
        {
            try
            {
                using (var request = UnityWebRequest.Get(VersionUrl))
                {
                    request.timeout = TimeoutSeconds;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone) await Task.Yield();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[VersionCheckService] 확인 실패({request.result}) — 통과: {request.error}");
                        return CheckResult.Pass;
                    }

                    var info = JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);
                    if (info == null || string.IsNullOrWhiteSpace(info.minVersion))
                    {
                        Debug.Log("[VersionCheckService] minVersion 없음/파싱 실패 — 통과");
                        return CheckResult.Pass;
                    }

                    if (!AppVersion.IsOlderThan(currentVersion, info.minVersion)) return CheckResult.Pass;

                    string storeUrl = string.IsNullOrWhiteSpace(info.storeUrl)
                        ? $"https://play.google.com/store/apps/details?id={Application.identifier}"
                        : info.storeUrl;
                    string message = string.IsNullOrWhiteSpace(info.message)
                        ? "A new version is available.\nPlease update to keep playing."
                        : info.message;

                    return new CheckResult(true, storeUrl, message);
                }
            }
            catch (Exception e)
            {
                // JSON 문법 오류 등 위에서 못 걸러낸 나머지 전부 — 역시 통과.
                Debug.Log($"[VersionCheckService] 확인 중 예외 — 통과: {e}");
                return CheckResult.Pass;
            }
        }
    }
}
