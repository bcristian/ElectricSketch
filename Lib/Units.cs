using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ElectricLib
{
    public struct Volt : IEquatable<Volt>, IComparable<Volt>
    {
        public Volt(float value) { Value = value; }

        public float Value { get; set; }

        public static readonly Volt Zero = new Volt(0);
        public bool IsZero => Value == 0;

        public override bool Equals(object obj) => (obj is Volt other) && Equals(other);
        public bool Equals([AllowNull] Volt other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Volt a, Volt b) => Equals(a, b);
        public static bool operator !=(Volt a, Volt b) => !Equals(a, b);
        public int CompareTo([AllowNull] Volt other) => Value.CompareTo(other.Value);
        public static Volt operator +(Volt v) => v;
        public static Volt operator -(Volt v) => new Volt(-v.Value);
        public static Volt operator +(Volt a, Volt b) => new Volt(a.Value + b.Value);
        public static Volt operator -(Volt a, Volt b) => new Volt(a.Value - b.Value);
        public static Volt operator *(Volt v, float f) => new Volt(v.Value * f);
        public static Volt operator /(Volt v, float f) => new Volt(v.Value / f);
        public static bool operator >(Volt a, Volt b) => a.Value > b.Value;
        public static bool operator <(Volt a, Volt b) => a.Value < b.Value;
        public static bool operator >=(Volt a, Volt b) => a.Value >= b.Value;
        public static bool operator <=(Volt a, Volt b) => a.Value <= b.Value;

        public override string ToString() => $"{Value}V";
    }

    public struct Hz : IEquatable<Hz>, IComparable<Hz>
    {
        public Hz(float value) { Value = value; }

        public float Value { get; set; }

        public static readonly Hz Zero = new Hz(0);
        public static readonly Hz DC = new Hz(0);
        public bool IsDC => Value == 0;

        public override bool Equals(object obj) => (obj is Hz other) && Equals(other);
        public bool Equals([AllowNull] Hz other) => Value == other.Value;
        public override int GetHashCode() => Value.GetHashCode();
        public static bool operator ==(Hz a, Hz b) => Equals(a, b);
        public static bool operator !=(Hz a, Hz b) => !Equals(a, b);
        public int CompareTo([AllowNull] Hz other) => Value.CompareTo(other.Value);
        public static Hz operator +(Hz v) => v;
        public static Hz operator -(Hz v) => new Hz(-v.Value);
        public static Hz operator +(Hz a, Hz b) => new Hz(a.Value + b.Value);
        public static Hz operator -(Hz a, Hz b) => new Hz(a.Value - b.Value);
        public static Hz operator *(Hz v, float f) => new Hz(v.Value * f);
        public static Hz operator /(Hz v, float f) => new Hz(v.Value / f);
        public static bool operator >(Hz a, Hz b) => a.Value > b.Value;
        public static bool operator <(Hz a, Hz b) => a.Value < b.Value;
        public static bool operator >=(Hz a, Hz b) => a.Value >= b.Value;
        public static bool operator <=(Hz a, Hz b) => a.Value <= b.Value;

        public override string ToString() => Value != 0 ? $"{Value}Hz" : "DC";
    }

    public struct Phase : IEquatable<Phase>, IComparable<Phase>
    {
        public Phase(int degrees) { Degrees = degrees; }

        public static readonly Phase Zero = new Phase(0);

        public static readonly Phase R = new Phase(0);
        public static readonly Phase S = new Phase(120);
        public static readonly Phase T = new Phase(240);

        public static readonly Phase U = new Phase(0);
        public static readonly Phase V = new Phase(120);
        public static readonly Phase W = new Phase(240);

        public static readonly Phase L1 = new Phase(0);
        public static readonly Phase L2 = new Phase(120);
        public static readonly Phase L3 = new Phase(240);

        public int Degrees { get; set; }

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
