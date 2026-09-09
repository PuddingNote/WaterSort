using UnityEngine;

namespace ColorSort.Managers
{
    /// <summary>
    /// 남은 힌트 개수 저장. GameDesign.md 확정 정책(2026-08-25, 충전 주기는
    /// 2026-09-09에 5라운드→1라운드→3라운드로 두 번 재확정): 무료 2회로 시작,
    /// 3라운드 클리어마다 1회 충전, 최대 보유 5개(그 이상은 충전 안 됨, 다
    /// 찼으면 배지에 숫자 대신 "MAX" 표시 — GameView.UpdateHintBadge 참고) —
    /// 다 쓰면 이후엔 보상형 광고 1회 = 1회 사용이지만, 그 광고 연동은 아직 안
    /// 붙였다(병 추가 쪽처럼 나중에 따로 요청하면 진행 — Architecture.md 참고).
    /// 그래서 지금은 0개가 되면 힌트 버튼이 그냥 비활성화된다.
    ///
    /// ProgressStore와 같은 이유로 PlayerPrefs를 쓴다 — 앱을 다시 켜도 남은 개수가
    /// 유지돼야 한다. 이 파일만 저장 방식(키 이름, 초기값/상한 클램프)을 알고,
    /// UI(GameView)와 라운드 전환(GameBootstrap)은 이 API만 호출한다.
    /// </summary>
    public static class HintStore
    {
        private const string HintCountKey = "ColorSort.HintCount";

        public const int InitialFreeHints = 2;
        public const int MaxHints = 5;

        /// <summary>지금 남은 힌트 개수 — PlayerPrefs에 값이 아예 없으면(첫 실행)
        /// 무료 2회로 시작한다.</summary>
        public static int LoadCount()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(HintCountKey, InitialFreeHints), 0, MaxHints);
        }

        private static void Save(int count)
        {
            PlayerPrefs.SetInt(HintCountKey, Mathf.Clamp(count, 0, MaxHints));
            PlayerPrefs.Save();
        }

        /// <summary>힌트를 실제로 하나 사용했을 때만 부른다 — "다음 수를 못 찾음"으로
        /// 끝난 시도(GameView.OnHintClicked의 NO HINT AVAILABLE 분기)는 소비가
        /// 아니므로 여기로 오면 안 된다. 반환값은 소비 후 남은 개수.</summary>
        public static int Consume()
        {
            int count = Mathf.Max(0, LoadCount() - 1);
            Save(count);
            return count;
        }

        /// <summary>3라운드 클리어마다(2026-09-09 재확정) GameBootstrap이 부른다 —
        /// 그 주기 판단은 호출부가 하고, 여기는 상한(MaxHints)을 넘지 않게 1개
        /// 더하는 것만 담당한다. 반환값은 충전 후 개수.</summary>
        public static int AddCharge()
        {
            int count = Mathf.Min(MaxHints, LoadCount() + 1);
            Save(count);
            return count;
        }
    }
}
