using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ElectricLib
{
    public struct Volt(float value) : IEquatable<Volt>, IComparable<Volt>
    {
        public float Value { get; set; } = value;
        public static readonly Volt Zero = new(0);
        public bool IsZero => Value == 0;

        public override bool Equals(object obj) => (obj is Volt other) && Equals(other);
        public bool Equals([AllowNull] Volt other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Volt a, Volt b) => Equals(a, b);
        public static bool operator !=(Volt a, Volt b) => !Equals(a, b);
        public int CompareTo([AllowNull] Volt other) => Value.CompareTo(other.Value);
        public static Volt operator +(Volt v) => v;
        public static Volt operator -(Volt v) => new(-v.Value);
        public static Volt operator +(Volt a, Volt b) => new(a.Value + b.Value);
        public static Volt operator -(Volt a, Volt b) => new(a.Value - b.Value);
        public static Volt operator *(Volt v, float f) => new(v.Value * f);
        public static Volt operator /(Volt v, float f) => new(v.Value / f);
        public static bool operator >(Volt a, Volt b) => a.Value > b.Value;
        public static bool operator <(Volt a, Volt b) => a.Value < b.Value;
        public static bool operator >=(Volt a, Volt b) => a.Value >= b.Value;
        public static bool operator <=(Volt a, Volt b) => a.Value <= b.Value;

        public override string ToString() => $"{Value}V";
    }

    public struct Hz(float value) : IEquatable<Hz>, IComparable<Hz>
    {
        public float Value { get; set; } = value;
        public static readonly Hz Zero = new(0);
        public static readonly Hz DC = new(0);
        public bool IsDC => Value == 0;

        public override bool Equals(object obj) => (obj is Hz other) && Equals(other);
        public bool Equals([AllowNull] Hz other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Hz a, Hz b) => Equals(a, b);
        public static bool operator !=(Hz a, Hz b) => !Equals(a, b);
        public int CompareTo([AllowNull] Hz other) => Value.CompareTo(other.Value);
        public static Hz operator +(Hz v) => v;
        public static Hz operator -(Hz v) => new(-v.Value);
        public static Hz operator +(Hz a, Hz b) => new(a.Value + b.Value);
        public static Hz operator -(Hz a, Hz b) => new(a.Value - b.Value);
        public static Hz operator *(Hz v, float f) => new(v.Value * f);
        public static Hz operator /(Hz v, float f) => new(v.Value / f);
        public static bool operator >(Hz a, Hz b) => a.Value > b.Value;
        public static bool operator <(Hz a, Hz b) => a.Value < b.Value;
        public static bool operator >=(Hz a, Hz b) => a.Value >= b.Value;
        public static bool operator <=(Hz a, Hz b) => a.Value <= b.Value;

        public override string ToString() => Value != 0 ? $"{Value}Hz" : "DC";
    }

    public struct Phase(int degrees) : IEquatable<Phase>, IComparable<Phase>
    {
        public static readonly Phase Zero = new(0);

        public static readonly Phase R = new(0);
        public static readonly Phase S = new(120);
        public static readonly Phase T = new(240);

        public static readonly Phase U = new(0);
        public static readonly Phase V = new(120);
        public static readonly Phase W = new(240);

        public static readonly Phase L1 = new(0);
        public static readonly Phase L2 = new(120);
        public static readonly Phase L3 = new(240);

        public int Degrees { get; set; } = degrees;
        public override bool Equals(object obj) => (obj is Phase other) && Equals(other);
        public bool Equals([AllowNull] Phase other) => Degrees == other.Degrees;
        public override int GetHashCode() => Degrees.GetHashCode();
        public static bool operator ==(Phase a, Phase b) => Equals(a, b);
        public static bool operator !=(Phase a, Phase b) => !Equals(a, b);
        public int CompareTo([AllowNull] Phase other) => Degrees.CompareTo(other.Degrees);

        public override string ToString() => $"{Degrees}deg";

        public static int operator -(Phase a, Phase b)
        {
            var d = a.Degrees - b.Degrees;
            if (d > 180)
                d -= 360;
            else if (d < -180)
                d += 360;
            return d;
        }
    }
}
