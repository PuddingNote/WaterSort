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
    /// 안드로이드 기본 진동 한 번뿐이다(플러그인 없이 쓸 수 있는 선에서는 이게
    /// 전부) — 그래서 스테이지 클리어처럼 더 축하하는 느낌을 주고 싶은 순간은
    /// 그 한 번을 짧게 두 번 이어서(더블 펄스) 무효 이동(한 번)과 느낌을 구분한다.
    ///
    /// 켬/끔은 <see cref="HapticsStore"/>(PlayerPrefs)가 진실 소스 — 이 클래스는
    /// 매 재생 시점에 그 값을 읽기만 한다(SoundService가 볼륨을 미리 반영해 두는
    /// 것과 달리, 진동은 "세기"가 없어서 매번 다시 확인하는 것으로 충분하다).
    /// </summary>
    public sealed class HapticsService : MonoBehaviour
    {
        public static HapticsService Instance { get; private set; }

        public enum Cue { InvalidMove, StageClear }

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
                    Handheld.Vibrate();
                    break;
                case Cue.StageClear:
                    StartCoroutine(DoublePulse());
                    break;
            }
        }

        private IEnumerator DoublePulse()
        {
            Handheld.Vibrate();
            yield return new WaitForSecondsRealtime(0.12f);
            Handheld.Vibrate();
        }
    }
}
