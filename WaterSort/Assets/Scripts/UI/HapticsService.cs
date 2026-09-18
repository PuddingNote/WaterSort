using System.Collections;
using ColorSort.Managers;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 앱 전체에서 딱 하나만 존재하는 진동(햅틱) 재생기 — <see cref="SoundService"/>와
    /// 같은 자리(GameBootstrap이 DontDestroyOnLoad 루트에 한 번 생성)에 둔다. 타이틀↔
    /// 게임↔스테이지클리어를 오가며 계속 쓰이는 앱 전역 서비스라 이유도 동일하다.
    ///
    /// Unity 표준 API(<see cref="Handheld.Vibrate"/>)는 세기·길이를 지정할 수 없는
    /// 안드로이드 기본 진동 한 번뿐이라(플러그인 없이 쓸 수 있는 선에서는 이게
    /// 전부) 세기를 낮춰달라는 요청(2026-09-18, "진동이 생각보다 쎄서... 강도 1을
    /// 0.5 정도로")에 대응할 수 없었다 — 그래서 API 26+(Android 8.0+)에서는
    /// <see cref="AndroidJavaObject"/>로 네이티브 Vibrator.vibrate(VibrationEffect)를
    /// 직접 호출해 <see cref="VibrationIntensity"/>로 세기(amplitude)를 조절한다.
    /// minSdk가 25라 더 낮은 기기는 amplitude API 자체가 없어 Handheld.Vibrate()로
    /// 폴백(그 기기들만 예전처럼 최대 세기).
    ///
    /// 스테이지 클리어처럼 더 축하하는 느낌을 주고 싶은 순간은 진동 한 번을 짧게
    /// 두 번 이어서(더블 펄스) 무효 이동(한 번)과 느낌을 구분한다.
    ///
    /// 켬/끔은 <see cref="HapticsStore"/>(PlayerPrefs)가 진실 소스 — 이 클래스는
    /// 매 재생 시점에 그 값을 읽기만 한다(SoundService가 볼륨을 미리 반영해 두는
    /// 것과 달리, 진동은 매번 다시 확인하는 것으로 충분하다).
    /// </summary>
    public sealed class HapticsService : MonoBehaviour
    {
        public static HapticsService Instance { get; private set; }

        public enum Cue { InvalidMove, StageClear }

        /// <summary>진동 세기 — 0(무진동)~1(기기 최대). 사용자 피드백(2026-09-18)으로
        /// 기본 최대 세기(1.0)가 너무 강해서 절반으로 낮췄다.</summary>
        private const float VibrationIntensity = 0.5f;

        /// <summary>한 번 진동의 길이(ms). Handheld.Vibrate()엔 길이 개념이 없었지만
        /// VibrationEffect.createOneShot은 필수 인자라 짧은 탭 느낌으로 새로 정함.</summary>
        private const int VibrationDurationMs = 40;

        public static HapticsService Create(Transform parent)
        {
            var go = new GameObject("HapticsService");
            go.transform.SetParent(parent, false);
            return go.AddComponent<HapticsService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>설정에서 꺼져 있으면 조용히 아무 일도 안 한다.</summary>
        public void Play(Cue cue)
        {
            if (!HapticsStore.HapticsEnabled) return;

            switch (cue)
            {
                case Cue.InvalidMove:
                    Vibrate();
                    break;
                case Cue.StageClear:
                    StartCoroutine(DoublePulse());
                    break;
            }
        }

        private IEnumerator DoublePulse()
        {
            Vibrate();
            yield return new WaitForSecondsRealtime(0.12f);
            Vibrate();
        }

        private static void Vibrate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (TryVibrateWithAmplitude(VibrationDurationMs, VibrationIntensity)) return;
#endif
            Handheld.Vibrate();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>API 26+에서 세기(amplitude)를 지정해 진동시킨다. 실패하거나(구형
        /// 기기 등) API 26 미만이면 false를 돌려줘서 호출 쪽이 Handheld.Vibrate()로
        /// 폴백하게 한다.</summary>
        private static bool TryVibrateWithAmplitude(int durationMs, float intensity)
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") < 26) return false;
                }

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null) return false;

                    int amplitude = Mathf.Clamp(Mathf.RoundToInt(255 * intensity), 1, 255);
                    using (var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", (long)durationMs, amplitude))
                    {
                        vibrator.Call("vibrate", effect);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}
