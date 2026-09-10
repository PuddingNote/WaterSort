using UnityEngine;

namespace ColorSort.Managers
{
    /// <summary>
    /// 사운드 설정(BGM/SFX 각각의 켬/끔 + 볼륨) 저장. HintStore/ProgressStore와
    /// 같은 이유로 PlayerPrefs를 쓴다 — 앱을 다시 켜도 유지돼야 한다. 이 파일만
    /// 저장 방식(키 이름, 기본값)을 알고, 실제 재생/음소거는 <c>SoundService</c>가,
    /// 조절 UI는 <c>SettingsDialog</c>가 이 API만 통해서 읽고 쓴다.
    ///
    /// 볼륨은 0~1. "켬/끔"은 볼륨과 별개 — 끄면 볼륨 값과 무관하게 완전히
    /// 무음이고, 다시 켜면 마지막 볼륨 값으로 돌아온다.
    /// </summary>
    public static class SettingsStore
    {
        private const string BgmEnabledKey = "ColorSort.Sound.BgmEnabled";
        private const string BgmVolumeKey = "ColorSort.Sound.BgmVolume";
        private const string SfxEnabledKey = "ColorSort.Sound.SfxEnabled";
        private const string SfxVolumeKey = "ColorSort.Sound.SfxVolume";

        public static bool BgmEnabled
        {
            get => PlayerPrefs.GetInt(BgmEnabledKey, 1) != 0;
            set { PlayerPrefs.SetInt(BgmEnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>기본값 0.5 — 처음엔 50%에서 시작(사용자 확정, 2026-09-10).</summary>
        public static float BgmVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 0.5f));
            set { PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
            set { PlayerPrefs.SetInt(SfxEnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float SfxVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.5f));
            set { PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        /// <summary>켬/끔과 볼륨을 합친 실제 적용 볼륨(끄면 0).</summary>
        public static float EffectiveBgmVolume => BgmEnabled ? BgmVolume : 0f;
        public static float EffectiveSfxVolume => SfxEnabled ? SfxVolume : 0f;
    }
}
