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

        // 병 최소 7개~최대 12개, 용량 4~8칸, 색 5~10종(사용자 확정 스펙).
        public static readonly RoundDifficultyCurve.ThemeLimits ThemeLimits = new RoundDifficultyCurve.ThemeLimits
        {
            MinColorCount = 5,
            MaxColorCount = Colors.Length, // 10
            MinSlotCount = 4,
            MaxSlotCount = 8,
            // 1개면 무작위 배분 자체가 거의 안 풀린다(실측: 색10·슬롯8 기준 1개=0% 성공,
            // 2개=99~100% 성공) — "적을수록 어렵다"가 아니라 최소한의 풀림 여유선.
            MinEmptyContainerCount = 2,
            MaxEmptyContainerCount = 3,
            MinContainerCount = 7,
            MaxContainerCount = 12
        };
    }
}
