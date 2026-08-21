using System;

namespace ColorSort.Core
{
    /// <summary>
    /// 유닛(칸)의 색상을 나타내는 소재 무관 식별자. 실제 팔레트(HEX, 이름 등)는
    /// Presentation 계층(UI/ScriptableObject)에서 이 값에 매핑한다 — Core는
    /// "몇 번 색인지"만 알고 "무슨 색으로 보이는지"는 모른다.
    /// </summary>
    public readonly struct ColorId : IEquatable<ColorId>
    {
        public int Value { get; }

        public ColorId(int value)
        {
            Value = value;
        }

        public bool Equals(ColorId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ColorId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"Color({Value})";

        public static bool operator ==(ColorId left, ColorId right) => left.Equals(right);
        public static bool operator !=(ColorId left, ColorId right) => !left.Equals(right);
    }
}
