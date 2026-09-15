using UnityEngine;

namespace ColorSort.Managers
{
    /// <summary>
    /// 진동(햅틱) 켬/끔 저장 — <see cref="SettingsStore"/>와 같은 패턴(PlayerPrefs).
    /// BGM/SFX와 달리 "세기" 개념이 없다(기기 진동 세기를 코드로 조절할 방법이
    /// 없음 — HapticsService 참고) — 그래서 볼륨 없이 켬/끔 한 값만 저장한다.
    /// </summary>
    public static class HapticsStore
    {
        private const string HapticsEnabledKey = "ColorSort.Haptics.Enabled";

        /// <summary>기본값 켜짐 — BGM/SFX 기본 켬과 같은 이유(2026-09-15).</summary>
        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt(HapticsEnabledKey, 1) != 0;
            set { PlayerPrefs.SetInt(HapticsEnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
