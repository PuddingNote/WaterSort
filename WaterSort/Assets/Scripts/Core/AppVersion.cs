using System;

namespace ColorSort.Core
{
    /// <summary>"1.2.3" 형식 버전 문자열의 안전한 비교(강제 업데이트 시스템 전용,
    /// 재사용_시스템_모음.md 1장). 문자열 그대로 비교하면 자릿수가 다른 숫자에서
    /// "0.9.9" > "0.10.0"으로 잘못 판정되므로("9" > "1"), 점(.)으로 나눠 각 자리를
    /// 정수로 비교한다.</summary>
    public static class AppVersion
    {
        /// <summary>current가 minVersion보다 낮으면(구버전이면) true.</summary>
        public static bool IsOlderThan(string current, string minVersion)
        {
            int[] currentParts = ParseSegments(current);
            int[] minParts = ParseSegments(minVersion);
            int length = Math.Max(currentParts.Length, minParts.Length);
            for (int i = 0; i < length; i++)
            {
                int c = i < currentParts.Length ? currentParts[i] : 0;
                int m = i < minParts.Length ? minParts[i] : 0;
                if (c != m) return c < m;
            }
            return false;
        }

        private static int[] ParseSegments(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return Array.Empty<int>();
            string[] rawParts = version.Split('.');
            var result = new int[rawParts.Length];
            for (int i = 0; i < rawParts.Length; i++)
                result[i] = int.TryParse(rawParts[i], out int n) ? n : 0;
            return result;
        }
    }
}
