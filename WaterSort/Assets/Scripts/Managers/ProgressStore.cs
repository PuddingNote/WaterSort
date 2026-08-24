using UnityEngine;

namespace ColorSort.Managers
{
    /// <summary>
    /// 플레이어 진행 상태 저장. 지금은 "다음에 시작할 라운드 번호"만 기억한다 —
    /// 중단된 판의 중간 상태(이동 이력 등)는 저장하지 않는다. 다시 게임을 켜면
    /// 그 라운드를 처음부터 다시 시작하되, 같은 라운드 번호는 항상 같은 배치로
    /// 재생성된다(라운드 번호 자체가 생성 시드이기 때문 — GameBootstrap 참고).
    /// PlayerPrefs를 쓰는 이 파일만 이 정책을 안다 — UI는 이 API만 호출한다.
    /// </summary>
    public static class ProgressStore
    {
        private const string NextRoundIdKey = "ColorSort.NextRoundId";

        public static int LoadNextRoundId()
        {
            return Mathf.Max(1, PlayerPrefs.GetInt(NextRoundIdKey, 1));
        }

        public static void SaveNextRoundId(int roundId)
        {
            PlayerPrefs.SetInt(NextRoundIdKey, roundId);
            PlayerPrefs.Save();
        }
    }
}
