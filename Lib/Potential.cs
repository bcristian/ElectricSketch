using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ElectricLib
{
    public struct Potential : IEquatable<Potential>
    {
        public Volt Voltage;
        public Hz Frequency;
        public Phase Phase;
        public object Source; // Identifies the energy source, so that we can differentiate between e.g. mains power and transformer or VFD output

        public Potential(Volt voltage, Hz frequency, Phase phase, object source)
        {
            if (frequency.IsDC && phase != Phase.Zero)
                throw new ArgumentException("DC cannot have phase");
            Voltage = voltage;
            Frequency = frequency;
            Phase = phase;
            Source = source;
        }

        public override bool Equals(object obj) => (obj is Potential other) && Equals(other);
        public bool Equals([AllowNull] Potential other) => (Source, Phase, Voltage, Frequency) == (other.Source, other.Phase, other.Voltage, other.Frequency);
        public override int GetHashCode() => (Source, Phase, Voltage, Frequency).GetHashCode();
        public static bool operator ==(Potential a, Potential b) => Equals(a, b);
        public static bool operator !=(Potential a, Potential b) => !Equals(a, b);

        public override string ToString() => $"{Voltage} {Frequency} {(Frequency.IsDC ? "" : Phase.ToString())} {Source ?? ""}";
    }
}
