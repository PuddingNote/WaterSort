namespace ColorSort.Managers
{
    /// <summary>
    /// AdMob 콘솔에서 발급받은 실제 광고 단위 ID 모음. 앱 ID(계정 전체 하나, 슬래시가
    /// 아니라 물결(~)로 구분되는 값)는 여기 코드가 아니라 Unity 에디터의
    /// Assets > Google Mobile Ads > Settings 창에서 설정한다(RewardedAdService 참고) —
    /// SDK가 빌드 시점에 그 값으로 AndroidManifest를 자동으로 채워 넣어 준다.
    ///
    /// 지금은 병 추가(광고 보상) 버튼 하나만 연동한다(사용자 확정, 2026-09-08) —
    /// 나머지 버튼(힌트 등)의 광고 연동은 나중에 요청이 오면 여기에 새 상수를 추가하고
    /// GameView의 해당 버튼 핸들러에서 RewardedAdService.Show를 부르면 된다.
    /// </summary>
    public static class AdUnitIds
    {
        /// <summary>앱 ID(참고용, 코드에서 실제로 쓰이진 않음): ca-app-pub-6387288948977074~1221971886</summary>
        private const string BonusContainerRewardedProd = "ca-app-pub-6387288948977074/3903975543";

        /// <summary>Google 공식 테스트용 보상형 광고 ID(Android, 모든 개발자가 공용으로
        /// 쓰는 값 — Google 문서/샘플에 그대로 나오는 고정 ID). 실제 ID로는 Unity
        /// 에디터에서 광고 자체가 안 뜨는데(사용자가 실제로 확인 — 에디터는 테스트
        /// ID만 서빙함) 이 값은 에디터에서도 정상적으로 뜬다.</summary>
        private const string BonusContainerRewardedTest = "ca-app-pub-3940256099942544/5224354917";

        /// <summary>에디터/개발 빌드에서는 자동으로 테스트 ID를, 실제 출시 빌드에서만
        /// 진짜 ID를 쓴다 — 손으로 바꿨다가 되돌리는 걸 깜빡해서 테스트 트래픽이
        /// 실제 광고 단위로 나가 버리는 사고를 원천 차단한다(AdMob 정책상 실제 ID로
        /// 테스트하면 무효 트래픽으로 계정에 불이익이 갈 수 있음).</summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const string BonusContainerRewarded = BonusContainerRewardedTest;
#else
        public const string BonusContainerRewarded = BonusContainerRewardedProd;
#endif
    }
}
