using ColorSort.Core;
using ColorSort.Solver;
using UnityEngine;

namespace ColorSort.UI
{
    /// <summary>
    /// WaterSort 테마 전용 색상 팔레트 + 라운드 파라미터 한도(GameDesign.md 2.3/6장).
    /// 다른 소재로 이 프로젝트를 복사할 때는 **이 파일 하나만** 그 소재에 맞게
    /// 다시 채우면 된다 — Core/Solver는 이 파일의 존재 자체를 모른다.
    /// </summary>
    public static class WaterPalette
    {
        private static readonly Color[] Colors =
        {
            new Color32(0xE7, 0x4C, 0x3C, 0xFF), // 레드
            new Color32(0x2E, 0xCC, 0x71, 0xFF), // 그린
            new Color32(0x29, 0x80, 0xB9, 0xFF), // 블루
            new Color32(0xF1, 0xC4, 0x0F, 0xFF), // 옐로우
            new Color32(0xF3, 0x68, 0xB2, 0xFF), // 핑크
            new Color32(0x8E, 0x44, 0xAD, 0xFF), // 퍼플
            new Color32(0x95, 0xA5, 0xA6, 0xFF), // 그레이
            new Color32(0x5D, 0xC9, 0xE2, 0xFF), // 스카이블루
            new Color32(0x7B, 0xE0, 0xB0, 0xFF), // 스카이그린
            new Color32(0x8B, 0x4A, 0x2B, 0xFF), // 브라운
        };

        public static Color Get(ColorId colorId)
        {
            int index = ((colorId.Value % Colors.Length) + Colors.Length) % Colors.Length;
            return Colors[index];
        }

        // 병 최소 7개~최대 11개(일반 병 기준), 용량 4~8칸, 색 5~10종(사용자 확정
        // 스펙). 화면엔 여기에 병 추가(광고 보상)용 보너스 병이 항상 1개 더
        // 붙어서 최종적으로 최대 12개가 뜬다(RoundBuilder.AppendContainer 참고,
        // 2026-09-08: "화면의 병은 최대 12개로" 확정 — 기존 일반 병 최대
        // 12개였던 걸 11개로 한 칸 낮춰서 보너스 병 자리를 확보).
        public static readonly RoundDifficultyCurve.ThemeLimits ThemeLimits = new RoundDifficultyCurve.ThemeLimits
        {
            MinColorCount = 5,
            MaxColorCount = Colors.Length, // 10
            MinSlotCount = 4,
            MaxSlotCount = 8,
            // 1개면 무작위 배분 자체가 거의 안 풀린다(실측: 색10·슬롯8 기준 1개=0% 성공,
            // 2개=99~100% 성공) — "적을수록 어렵다"가 아니라 최소한의 풀림 여유선이라
            // MinEmptyContainerCount는 2 밑으로 못 내린다. 화면에 보이는 빈 병은
            // 여기(일반 빈 병) + 병 추가용 보너스 병(항상 빈 채로 시작, 1개)을 합친
            // 값인데, 최대 3개로 묶어 달라는 요청(2026-09-08 확정)을 만족시키려면
            // 일반 빈 병 쪽 상한을 2로 낮춰야 한다(2+보너스 1=3, 절대 안 넘음) — 그
            // 결과 일반 빈 병 개수 자체가 Min=Max=2로 고정돼서, 이제 이 값은 더 이상
            // 라운드마다 무작위로 2~3 사이를 오가지 않는다(예전엔 난이도에 살짝
            // 곁들이던 변주였는데, 그 여지가 없어짐 — 사용자 확정 사항이라 그대로 둠).
            MinEmptyContainerCount = 2,
            MaxEmptyContainerCount = 2,
            MinContainerCount = 7,
            MaxContainerCount = 11
        };
    }
}
