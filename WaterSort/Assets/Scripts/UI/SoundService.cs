using ColorSort.Managers;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// 앱 전체에서 딱 하나만 존재하는 사운드 재생기. GameBootstrap이 DontDestroyOnLoad
    /// 루트에 붙여 두므로 타이틀↔게임↔스테이지클리어를 오가도 BGM이 끊기지 않는다.
    ///
    /// 실제 오디오 클립은 <see cref="UiSkin"/>(Inspector에서 드래그)에서 읽는다 —
    /// 아직 안 넣었으면(null) 그 소리만 조용히 무음이고, 나중에 드래그해서 넣으면
    /// 재컴파일/재생 없이 바로 적용된다(클립은 매 호출 시점에 새로 읽음. BGM만은
    /// 시작 시점에 한 번 잡으므로, 플레이 중에 BGM을 새로 넣었으면 <see cref="StartBgm"/>가
    /// 다시 불릴 때 — 설정 토글 등 — 반영된다).
    ///
    /// 켬/끔·볼륨은 <see cref="SettingsStore"/>(PlayerPrefs)가 진실 소스이고, 이
    /// 클래스는 그 값을 세 AudioSource에 반영만 한다(<see cref="ApplySettings"/>).
    /// </summary>
    public sealed class SoundService : MonoBehaviour
    {
        public static SoundService Instance { get; private set; }

        /// <summary>PlayOneShot로 겹쳐 울려도 되는 단발성 효과음들. 물 붓기(Pour)는
        /// "이전 재생을 끊고 다시"라는 별도 정책이라 여기 안 넣고 <see cref="PlayPour"/>로 뺐다.</summary>
        public enum Sfx { ButtonTouch, Refresh, BottleComplete, StageClear }

        private AudioSource _bgm;
        private AudioSource _sfx;   // 단발성 효과음 — PlayOneShot(겹침 허용).
        private AudioSource _pour;  // 물 붓기 전용 — 새 붓기가 이전 붓기를 끊는다(아래 PlayPour 참고).

        public static SoundService Create(Transform parent)
        {
            var go = new GameObject("SoundService");
            go.transform.SetParent(parent, false);
            return go.AddComponent<SoundService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // 이 프로젝트는 카메라를 안 만들어서(전부 ScreenSpaceOverlay Canvas) 씬에
            // AudioListener가 없을 수 있다 — 없으면 여기에 하나 붙인다(있으면 그대로 둠).
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();

            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.playOnAwake = false;
            _bgm.loop = true;

            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;

            _pour = gameObject.AddComponent<AudioSource>();
            _pour.playOnAwake = false;

            ApplySettings();
            StartBgm();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static UiSkin Skin => UiTheme.Skin;

        /// <summary><see cref="SettingsStore"/>의 켬/끔·볼륨을 세 소스에 반영한다 —
        /// SettingsDialog의 토글/슬라이더가 값을 바꿀 때마다 부른다. 끄면 볼륨 0
        /// (재생은 계속되지만 안 들림 — 다시 켜면 즉시 원래 볼륨).</summary>
        public void ApplySettings()
        {
            if (_bgm != null) _bgm.volume = SettingsStore.EffectiveBgmVolume;
            float sfxVol = SettingsStore.EffectiveSfxVolume;
            if (_sfx != null) _sfx.volume = sfxVol;
            if (_pour != null) _pour.volume = sfxVol;
        }

        /// <summary>메인 BGM을 (있으면) 루프 재생 시작. 이미 그 클립을 돌고 있으면 아무것도 안 함.</summary>
        public void StartBgm()
        {
            if (_bgm == null) return;
            var clip = Skin != null ? Skin.MainBgm : null;
            if (clip == null) { _bgm.Stop(); return; }
            if (_bgm.clip != clip) _bgm.clip = clip;
            if (!_bgm.isPlaying) _bgm.Play();
        }

        public void Play(Sfx cue)
        {
            if (_sfx == null || Skin == null) return;
            AudioClip clip = cue switch
            {
                Sfx.ButtonTouch => Skin.ButtonTouchSfx,
                Sfx.Refresh => Skin.RefreshSfx,
                Sfx.BottleComplete => Skin.BottleCompleteSfx,
                Sfx.StageClear => Skin.StageClearSfx,
                _ => null,
            };
            if (clip != null) _sfx.PlayOneShot(clip);
        }

        /// <summary>물 붓기 소리 — 새로 부르면 이전 재생을 끊고 처음부터 다시 튼다.
        /// 여러 병을 동시에 옮기는 상황에서 붓기 소리가 겹쳐 울려 뭉개지지 않고
        /// "가장 최근에 시작한 붓기" 하나만 들린다 — 물줄기 z-order를 "나중 것 우선"으로
        /// 두는 것과 같은 정책(PourAnimator 클래스 주석 참고).</summary>
        public void PlayPour()
        {
            if (_pour == null || Skin == null) return;
            var clip = Skin.PourSound;
            if (clip == null) return;
            _pour.clip = clip;
            _pour.Play(); // 같은 소스를 다시 Play() = 이전 재생 즉시 중단하고 처음부터.
        }
    }
}
